using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;
using System.Threading.Channels;

namespace ExpensesBackend.API.Services;

public class TemplateBookProcessorService : BackgroundService
{
    private readonly Channel<TemplateCreationJobPayload> _channel;
    private readonly IServiceProvider _services;
    private readonly ILogger<TemplateBookProcessorService> _logger;

    public TemplateBookProcessorService(
        Channel<TemplateCreationJobPayload> channel,
        IServiceProvider services,
        ILogger<TemplateBookProcessorService> logger)
    {
        _channel  = channel;
        _services = services;
        _logger   = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(ct))
        {
            // ITemplateBlobService is Singleton; MongoDbContext is Singleton.
            // Create a scope so any future Scoped deps would work correctly.
            using var scope   = _services.CreateScope();
            var blobService   = scope.ServiceProvider.GetRequiredService<ITemplateBlobService>();
            var context       = scope.ServiceProvider.GetRequiredService<MongoDbContext>();

            try
            {
                await ProcessJobAsync(job, blobService, context, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in template creation {SessionId}", job.ImportSessionId);
                await MarkFailedAsync(job.ImportSessionId, context, ct);
            }
        }
    }

    private async Task ProcessJobAsync(
        TemplateCreationJobPayload job,
        ITemplateBlobService blobService,
        MongoDbContext context,
        CancellationToken ct)
    {
        _logger.LogInformation("Starting template creation {SessionId} for book {BookId}",
            job.ImportSessionId, job.ExpenseBookId);

        await SetStatusAsync(job.ImportSessionId, ImportStatus.Processing, context, ct);

        var template = await blobService.GetTemplateAsync();

        // ── Phase 1: Categories ─────────────────────────────────────────────
        var categoryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            // Remove any default categories created during book creation
            await context.Categories.DeleteManyAsync(
                Builders<Category>.Filter.Eq(c => c.ExpenseBookId, job.ExpenseBookId), ct);

            var categories = template.Categories.Select(c => new Category
            {
                ExpenseBookId = job.ExpenseBookId,
                Name          = c.Name,
                Icon          = c.Icon,
                Color         = c.Color,
                Type          = c.Type
            }).ToList();

            await context.Categories.InsertManyAsync(categories, cancellationToken: ct);

            foreach (var cat in categories)
                categoryMap[cat.Name.ToLowerInvariant()] = cat.Id;

            await CompletePhaseAsync(job.ImportSessionId, 1, ImportRecordStatus.Success, context, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Phase 1 (categories) failed for {SessionId}", job.ImportSessionId);
            await CompletePhaseAsync(job.ImportSessionId, 1, ImportRecordStatus.Failed, context, ct);
        }

        // ── Phase 2: Expenses ───────────────────────────────────────────────
        try
        {
            var now      = DateTime.UtcNow;
            var expenses = template.Expenses
                .Select(e =>
                {
                    categoryMap.TryGetValue(e.Category.ToLowerInvariant(), out var catId);
                    return new Expense
                    {
                        UserId        = job.UserId,
                        ExpenseBookId = job.ExpenseBookId,
                        Type          = e.Type,
                        Amount        = e.Amount,
                        Date          = now.AddDays(-e.DaysAgo).ToUniversalTime(),
                        Category      = catId ?? string.Empty,
                        PaymentMethod = e.PaymentMethod,
                        Description   = e.Description,
                        Notes         = e.Notes,
                        CreatedAt     = now,
                        UpdatedAt     = now
                    };
                })
                .Where(e => !string.IsNullOrEmpty(e.Category))
                .ToList();

            await context.Expenses.InsertManyAsync(expenses, cancellationToken: ct);
            await CompletePhaseAsync(job.ImportSessionId, 2, ImportRecordStatus.Success, context, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Phase 2 (expenses) failed for {SessionId}", job.ImportSessionId);
            await CompletePhaseAsync(job.ImportSessionId, 2, ImportRecordStatus.Failed, context, ct);
        }

        // ── Phase 3: Budgets ────────────────────────────────────────────────
        try
        {
            var now        = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd   = monthStart.AddMonths(1).AddSeconds(-1);

            var budgets = template.Budgets
                .Select(b =>
                {
                    categoryMap.TryGetValue(b.Category.ToLowerInvariant(), out var catId);
                    return new Budget
                    {
                        UserId        = job.UserId,
                        ExpenseBookId = job.ExpenseBookId,
                        Category      = catId ?? string.Empty,
                        Amount        = b.LimitAmount,
                        Spent         = Math.Round(b.LimitAmount * b.SpentPercent / 100m, 2),
                        Period        = "monthly",
                        Currency      = job.Currency,
                        AlertThreshold = b.AlertThreshold,
                        StartDate     = monthStart,
                        EndDate       = monthEnd,
                        LatestVersionNumber = 1,
                        Versions      =
                        [
                            new BudgetVersion
                            {
                                VersionNumber   = 1,
                                EffectivePeriod = $"{now.Year}-{now.Month:D2}",
                                EffectiveDate   = monthStart,
                                Amount          = b.LimitAmount
                            }
                        ]
                    };
                })
                .Where(b => !string.IsNullOrEmpty(b.Category))
                .ToList();

            await context.Budgets.InsertManyAsync(budgets, cancellationToken: ct);
            await CompletePhaseAsync(job.ImportSessionId, 3, ImportRecordStatus.Success, context, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Phase 3 (budgets) failed for {SessionId}", job.ImportSessionId);
            await CompletePhaseAsync(job.ImportSessionId, 3, ImportRecordStatus.Failed, context, ct);
        }

        // ── Phase 4: Upcoming Payments ──────────────────────────────────────
        try
        {
            var now = DateTime.UtcNow;
            var recurringExpenses = new List<RecurringExpense>();
            var upcomingPayments  = new List<UpcomingPayment>();

            foreach (var item in template.UpcomingPayments)
            {
                categoryMap.TryGetValue(item.Category.ToLowerInvariant(), out var catId);
                if (string.IsNullOrEmpty(catId)) continue;

                var dueDate = now.AddDays(item.DaysFromNow).Date.ToUniversalTime();

                var recurring = new RecurringExpense
                {
                    UserId        = job.UserId,
                    ExpenseBookId = job.ExpenseBookId,
                    Amount        = item.Amount,
                    Category      = catId,
                    PaymentMethod = item.PaymentMethod,
                    Description   = item.Description,
                    Frequency     = item.Frequency,
                    StartDate     = now.AddMonths(-1).ToUniversalTime(),
                    NextOccurrence = dueDate,
                    IsActive      = true
                };
                recurringExpenses.Add(recurring);

                var status = item.DaysFromNow < 0  ? "overdue"
                           : item.DaysFromNow == 0 ? "due"
                           : "upcoming";

                upcomingPayments.Add(new UpcomingPayment
                {
                    UserId             = job.UserId,
                    ExpenseBookId      = job.ExpenseBookId,
                    RecurringExpenseId = recurring.Id,
                    Amount             = item.Amount,
                    Category           = catId,
                    PaymentMethod      = item.PaymentMethod,
                    Description        = item.Description,
                    Frequency          = item.Frequency,
                    DueDate            = dueDate,
                    Status             = status
                });
            }

            if (recurringExpenses.Count > 0)
                await context.RecurringExpenses.InsertManyAsync(recurringExpenses, cancellationToken: ct);

            if (upcomingPayments.Count > 0)
                await context.UpcomingPayments.InsertManyAsync(upcomingPayments, cancellationToken: ct);

            await CompletePhaseAsync(job.ImportSessionId, 4, ImportRecordStatus.Success, context, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Phase 4 (upcoming payments) failed for {SessionId}", job.ImportSessionId);
            await CompletePhaseAsync(job.ImportSessionId, 4, ImportRecordStatus.Failed, context, ct);
        }

        // ── Phase 5: Lendings ───────────────────────────────────────────────
        try
        {
            var now      = DateTime.UtcNow;
            var lendings = new List<Lending>();
            var repayments = new List<LendingRepayment>();

            foreach (var item in template.Lendings)
            {
                var startDate = now.AddDays(-item.DaysAgo).ToUniversalTime();
                var totalRepaid = item.Repayments.Sum(r => r.Amount);

                var lending = new Lending
                {
                    ExpenseBookId        = job.ExpenseBookId,
                    UserId               = job.UserId,
                    BorrowerName         = item.BorrowerName,
                    BorrowerContact      = item.BorrowerContact,
                    PrincipalAmount      = item.PrincipalAmount,
                    AnnualInterestRate   = item.AnnualInterestRate,
                    StartDate            = startDate,
                    DueDate              = now.AddDays(item.DueDaysFromNow).ToUniversalTime(),
                    TotalRepaid          = totalRepaid,
                    OutstandingPrincipal = item.PrincipalAmount - totalRepaid,
                    RepaymentCount       = item.Repayments.Count,
                    Status               = totalRepaid >= item.PrincipalAmount ? "settled" : "active",
                    Notes                = item.Notes
                };
                lendings.Add(lending);

                foreach (var r in item.Repayments)
                {
                    repayments.Add(new LendingRepayment
                    {
                        LendingId     = lending.Id,
                        ExpenseBookId = job.ExpenseBookId,
                        UserId        = job.UserId,
                        Amount        = r.Amount,
                        Date          = now.AddDays(-r.DaysAgo).ToUniversalTime(),
                        Notes         = r.Notes
                    });
                }
            }

            if (lendings.Count > 0)
                await context.Lendings.InsertManyAsync(lendings, cancellationToken: ct);

            if (repayments.Count > 0)
                await context.LendingRepayments.InsertManyAsync(repayments, cancellationToken: ct);

            await CompletePhaseAsync(job.ImportSessionId, 5, ImportRecordStatus.Success, context, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Phase 5 (lendings) failed for {SessionId}", job.ImportSessionId);
            await CompletePhaseAsync(job.ImportSessionId, 5, ImportRecordStatus.Failed, context, ct);
        }

        // ── Update book description + savings goal ──────────────────────────
        try
        {
            await context.ExpenseBooks.UpdateOneAsync(
                Builders<ExpenseBook>.Filter.Eq(b => b.Id, job.ExpenseBookId),
                Builders<ExpenseBook>.Update
                    .Set(b => b.Description, "Explore budgets, expenses, upcoming payments, and lendings with real data.")
                    .Set(b => b.MonthlySavingsGoal, 500),
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update book defaults for {BookId}", job.ExpenseBookId);
        }

        // ── Finalize session ────────────────────────────────────────────────
        var sessionFilter = Builders<ImportSession>.Filter.Eq(s => s.Id, job.ImportSessionId);
        var finalSession  = await context.ImportSessions.Find(sessionFilter).FirstOrDefaultAsync(ct);

        var hasFailed  = finalSession?.Records.Any(r => r.Status == ImportRecordStatus.Failed) ?? false;
        var allFailed  = finalSession?.Records.All(r => r.Status == ImportRecordStatus.Failed) ?? false;
        var finalStatus = allFailed  ? ImportStatus.Failed
                        : hasFailed  ? ImportStatus.CompletedWithErrors
                        : ImportStatus.Completed;

        await context.ImportSessions.UpdateOneAsync(
            sessionFilter,
            Builders<ImportSession>.Update
                .Set(s => s.Status,      finalStatus)
                .Set(s => s.CompletedAt, DateTime.UtcNow),
            cancellationToken: ct);

        _logger.LogInformation("Template creation {SessionId} finished with status {Status}",
            job.ImportSessionId, finalStatus);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static async Task CompletePhaseAsync(
        string sessionId, int rowNumber, string status,
        MongoDbContext context, CancellationToken ct)
    {
        var session = await context.ImportSessions
            .Find(Builders<ImportSession>.Filter.Eq(s => s.Id, sessionId))
            .FirstOrDefaultAsync(ct);
        if (session == null) return;

        var record = session.Records.FirstOrDefault(r => r.RowNumber == rowNumber);
        if (record != null)
            record.Status = status;

        session.ProcessedCount = session.Records.Count(r => r.Status != ImportRecordStatus.Pending);
        session.SuccessCount   = session.Records.Count(r => r.Status == ImportRecordStatus.Success);
        session.FailedCount    = session.Records.Count(r => r.Status == ImportRecordStatus.Failed);

        await context.ImportSessions.ReplaceOneAsync(
            Builders<ImportSession>.Filter.Eq(s => s.Id, sessionId),
            session, cancellationToken: ct);
    }

    private static async Task SetStatusAsync(
        string sessionId, string status,
        MongoDbContext context, CancellationToken ct)
    {
        await context.ImportSessions.UpdateOneAsync(
            Builders<ImportSession>.Filter.Eq(s => s.Id, sessionId),
            Builders<ImportSession>.Update.Set(s => s.Status, status),
            cancellationToken: ct);
    }

    private static async Task MarkFailedAsync(
        string sessionId, MongoDbContext context, CancellationToken ct)
    {
        await context.ImportSessions.UpdateOneAsync(
            Builders<ImportSession>.Filter.Eq(s => s.Id, sessionId),
            Builders<ImportSession>.Update
                .Set(s => s.Status,      ImportStatus.Failed)
                .Set(s => s.CompletedAt, DateTime.UtcNow),
            cancellationToken: ct);
    }
}
