using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Channels;
using ExpensesBackend.API.Services.AI;

namespace ExpensesBackend.API.Services;

public class ImportProcessorService : BackgroundService
{
    private readonly Channel<ImportJobPayload> _channel;
    private readonly MongoDbContext _context;
    private readonly ILogger<ImportProcessorService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AiBankTransactionCategorizer _categorizer;

    private static readonly HashSet<string> ValidPaymentMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cash", "Credit Card", "Debit Card", "Bank Transfer", "UPI", "Cheque", "Other"
    };

    private const int BatchSize = 100;

    public ImportProcessorService(
        Channel<ImportJobPayload> channel,
        MongoDbContext context,
        ILogger<ImportProcessorService> logger,
        IServiceScopeFactory scopeFactory,
        AiBankTransactionCategorizer categorizer)
    {
        _channel      = channel;
        _context      = context;
        _logger       = logger;
        _scopeFactory = scopeFactory;
        _categorizer  = categorizer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(job, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing import {ImportId}", job.ImportSessionId);
                await MarkSessionFailedAsync(job.ImportSessionId, stoppingToken);
            }
        }
    }

    private async Task ProcessJobAsync(ImportJobPayload job, CancellationToken ct)
    {
        _logger.LogInformation("Starting import {ImportId} ({Count} rows, retry={Retry})",
            job.ImportSessionId, job.Rows.Count, job.IsRetry);

        if (!job.IsRetry)
        {
            await PatchSessionAsync(job.ImportSessionId,
                u => u.Set(s => s.Status, ImportStatus.Processing), ct);
        }

        var (categoryMap, categoryDisplayNames) = await BuildCategoryMapAsync(job.ExpenseBookId);

        // For bank sync imports, AI-classify every transaction description into a book category.
        // This runs as a single batched call before the row loop, so it's one API round-trip
        // per ~50 transactions rather than one per row.
        if (!string.IsNullOrEmpty(job.BankSyncSessionId))
        {
            var inputs = job.Rows
                .Select(r => (r.RowNumber, r.Description))
                .ToList();

            var assignments = await _categorizer.ClassifyAsync(inputs, categoryDisplayNames);

            foreach (var row in job.Rows)
            {
                if (assignments.TryGetValue(row.RowNumber, out var cat))
                    row.Category = cat;
            }

            _logger.LogInformation(
                "AI categorized {Count} bank sync rows for import {ImportId}",
                assignments.Count, job.ImportSessionId);
        }

        // Build a mutable set of allowed IDs for quick O(1) lookup
        // Empty list = no restriction (owners get the full list already populated by MemberService)
        var allowedIds = job.AllowedCategoryIds.Count > 0
            ? new HashSet<string>(job.AllowedCategoryIds, StringComparer.OrdinalIgnoreCase)
            : null; // null = all allowed

        var batches = job.Rows
            .Select((row, i) => new { row, i })
            .GroupBy(x => x.i / BatchSize)
            .Select(g => g.Select(x => x.row).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            if (ct.IsCancellationRequested) break;

            var validExpenses = new List<Expense>();
            var recordUpdates = new List<(int rowNumber, string status, string? error)>();

            foreach (var row in batch)
            {
                var (expense, error) = await MapRowToExpenseAsync(
                    row, job.UserId, job.ExpenseBookId, categoryMap, allowedIds, ct);

                if (error != null)
                {
                    recordUpdates.Add((row.RowNumber, ImportRecordStatus.Failed, error));
                }
                else
                {
                    validExpenses.Add(expense!);
                    recordUpdates.Add((row.RowNumber, ImportRecordStatus.Success, null));
                }
            }

            if (validExpenses.Count > 0)
                await _context.Expenses.InsertManyAsync(validExpenses, cancellationToken: ct);

            await ApplyBatchResultsAsync(job.ImportSessionId, recordUpdates, ct);
        }

        // Re-read session to get final counts (handles both initial import and retry)
        var filter       = Builders<ImportSession>.Filter.Eq(s => s.Id, job.ImportSessionId);
        var finalSession = await _context.ImportSessions.Find(filter).FirstOrDefaultAsync(ct);

        if (finalSession == null) return;

        var finalStatus = finalSession.FailedCount == 0
            ? ImportStatus.Completed
            : finalSession.SuccessCount == 0
                ? ImportStatus.Failed
                : ImportStatus.CompletedWithErrors;

        await PatchSessionAsync(job.ImportSessionId, u => u
            .Set(s => s.Status,      finalStatus)
            .Set(s => s.CompletedAt, DateTime.UtcNow), ct);

        _logger.LogInformation(
            "Finished import {ImportId}: {Ok} ok, {Fail} failed, status={Status}",
            job.ImportSessionId, finalSession.SuccessCount, finalSession.FailedCount, finalStatus);

        // Send push notification when the import came from a bank sync
        if (!string.IsNullOrEmpty(job.BankSyncSessionId))
            await SendBankSyncNotificationAsync(job, finalSession, ct);
    }

    private async Task SendBankSyncNotificationAsync(
        ImportJobPayload job, ImportSession session, CancellationToken ct)
    {
        try
        {
            var bankName = job.BankName ?? "Bank";

            string title, body;
            if (session.SuccessCount > 0 && session.FailedCount == 0)
            {
                title = $"{bankName} Sync Complete";
                body  = $"{session.SuccessCount} transaction{(session.SuccessCount == 1 ? "" : "s")} imported successfully.";
            }
            else if (session.SuccessCount > 0)
            {
                title = $"{bankName} Sync Complete";
                body  = $"{session.SuccessCount} imported, {session.FailedCount} failed. Check import history for details.";
            }
            else
            {
                title = $"{bankName} Sync Failed";
                body  = "No transactions could be imported. Check import history for details.";
            }

            var subscriptions = await _context.PushSubscriptions
                .Find(s => s.UserId == job.UserId)
                .ToListAsync(ct);

            if (subscriptions.Count == 0) return;

            using var scope = _scopeFactory.CreateScope();
            var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();

            var staleIds = new List<string>();

            foreach (var sub in subscriptions)
            {
                try
                {
                    await pushService.SendAsync(sub.Endpoint, sub.P256dh, sub.Auth, title, body, "/bank-sync");
                }
                catch (WebPush.WebPushException ex) when (
                    ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                    ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    staleIds.Add(sub.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Push notification failed for subscription {SubId}", sub.Id);
                }
            }

            // Clean up expired subscriptions
            if (staleIds.Count > 0)
            {
                var staleFilter = Builders<PushSubscription>.Filter.In(s => s.Id, staleIds);
                await _context.PushSubscriptions.DeleteManyAsync(staleFilter, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bank sync push notification failed for import {ImportId}", job.ImportSessionId);
        }
    }

    private async Task<(Expense? expense, string? error)> MapRowToExpenseAsync(
        CsvExpenseRow row, string userId, string expenseBookId,
        Dictionary<string, string> categoryMap, HashSet<string>? allowedIds,
        CancellationToken ct)
    {
        // Validate amount
        if (row.Amount <= 0)
            return (null, "Amount must be greater than zero");

        // Parse date
        if (!DateTime.TryParse(row.Date, out var date))
            return (null, $"Invalid date: '{row.Date}'");

        // Validate payment method
        var pm = row.PaymentMethod?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(pm))
            return (null, "Payment method is required");
        if (!ValidPaymentMethods.Contains(pm))
            return (null, $"Unknown payment method: '{pm}'");

        // Resolve or auto-create category
        if (string.IsNullOrWhiteSpace(row.Category))
            return (null, "Category is required");

        var categoryKey = row.Category.Trim().ToLowerInvariant();
        if (!categoryMap.TryGetValue(categoryKey, out var categoryId))
        {
            // Auto-create the category
            var newCategory = new Category
            {
                ExpenseBookId = expenseBookId,
                Name          = row.Category.Trim(),
                Type          = "expense",
                Icon          = "default",
                Color         = "#6366f1"
            };
            await _context.Categories.InsertOneAsync(newCategory, cancellationToken: ct);
            categoryId = newCategory.Id;
            categoryMap[categoryKey] = categoryId;

            // Grant access to this new category for the current user's allowed set
            allowedIds?.Add(categoryId);

            _logger.LogInformation(
                "Auto-created category '{Name}' ({Id}) for book {BookId}",
                newCategory.Name, categoryId, expenseBookId);
        }

        // Check category permission
        if (allowedIds != null && !allowedIds.Contains(categoryId))
            return (null, $"You don't have access to category '{row.Category.Trim()}'");

        var type = row.Type?.ToLowerInvariant() == "income" ? "income" : "expense";

        return (new Expense
        {
            UserId          = userId,
            ExpenseBookId   = expenseBookId,
            Type            = type,
            Amount          = row.Amount,
            Date            = date.ToUniversalTime(),
            Category        = row.Category.Trim(),
            PaymentMethod   = pm,
            Description     = row.Description.Trim(),
            Notes           = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes.Trim(),
            ExternalTxnRef  = string.IsNullOrWhiteSpace(row.ExternalTxnRef) ? null : row.ExternalTxnRef,
            CreatedAt       = DateTime.UtcNow,
            UpdatedAt       = DateTime.UtcNow
        }, null);
    }

    private async Task ApplyBatchResultsAsync(
        string sessionId,
        List<(int rowNumber, string status, string? error)> recordUpdates,
        CancellationToken ct)
    {
        var filter  = Builders<ImportSession>.Filter.Eq(s => s.Id, sessionId);
        var session = await _context.ImportSessions.Find(filter).FirstOrDefaultAsync(ct);
        if (session == null) return;

        var rowMap = recordUpdates.ToDictionary(r => r.rowNumber);
        foreach (var rec in session.Records)
        {
            if (rowMap.TryGetValue(rec.RowNumber, out var upd))
            {
                rec.Status       = upd.status;
                rec.ErrorMessage = upd.error;
            }
        }

        // Recalculate all counts from the full record list (works for both initial + retry)
        session.ProcessedCount = session.Records.Count(r => r.Status != ImportRecordStatus.Pending);
        session.SuccessCount   = session.Records.Count(r => r.Status == ImportRecordStatus.Success);
        session.FailedCount    = session.Records.Count(r => r.Status == ImportRecordStatus.Failed);

        await _context.ImportSessions.ReplaceOneAsync(filter, session, cancellationToken: ct);
    }

    private async Task PatchSessionAsync(string sessionId,
        Func<UpdateDefinitionBuilder<ImportSession>, UpdateDefinition<ImportSession>> buildUpdate,
        CancellationToken ct = default)
    {
        var filter = Builders<ImportSession>.Filter.Eq(s => s.Id, sessionId);
        var update = buildUpdate(Builders<ImportSession>.Update);
        await _context.ImportSessions.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    private async Task MarkSessionFailedAsync(string sessionId, CancellationToken ct)
    {
        await PatchSessionAsync(sessionId, u => u
            .Set(s => s.Status,      ImportStatus.Failed)
            .Set(s => s.CompletedAt, DateTime.UtcNow), ct);
    }

    private async Task<(Dictionary<string, string> nameToId, List<string> displayNames)> BuildCategoryMapAsync(string expenseBookId)
    {
        var filter     = Builders<Category>.Filter.Eq(c => c.ExpenseBookId, expenseBookId);
        var categories = await _context.Categories.Find(filter).ToListAsync();

        var nameToId = categories.ToDictionary(
            c => c.Name.ToLowerInvariant(),
            c => c.Id,
            StringComparer.OrdinalIgnoreCase);

        var displayNames = categories.Select(c => c.Name).ToList();

        return (nameToId, displayNames);
    }
}
