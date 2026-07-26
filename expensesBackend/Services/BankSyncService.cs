using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Services.BankSync;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;

namespace ExpensesBackend.API.Services;

public class BankSyncService : IBankSyncService
{
    private readonly MongoDbContext _context;
    private readonly BankStatementParserFactory _parserFactory;
    private readonly IImportService _importService;

    public BankSyncService(
        MongoDbContext context,
        BankStatementParserFactory parserFactory,
        IImportService importService)
    {
        _context       = context;
        _parserFactory = parserFactory;
        _importService = importService;
    }

    public async Task<BankStatementPreviewDto> ParseStatementAsync(
        string connectionId, string userId, IFormFile file, string? password = null)
    {
        var connection = await LoadConnectionAsync(connectionId, userId)
            ?? throw new KeyNotFoundException("Bank connection not found");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".csv" or ".xls" or ".xlsx" or ".pdf"))
            throw new ArgumentException("Only .csv, .xls, .xlsx, and .pdf files are supported");

        // Read file → detect format → parse (PDF uses password if provided)
        using var stream = file.OpenReadStream();
        var (transactions, detectedFormat) = await _parserFactory.ParseFileAsync(
            stream, file.FileName, connection.BankName, password);

        if (transactions.Count == 0)
            throw new InvalidOperationException(
                "No transactions were found in this file. " +
                "Please check that you have exported the correct date range.");

        // Store session (TTL 2h) for the confirm step
        var session = new BankSyncSession
        {
            UserId           = userId,
            BankConnectionId = connectionId,
            BankName         = connection.BankName,
            DetectedFormat   = detectedFormat,
            Transactions     = transactions,
            Status           = "preview",
            CreatedAt        = DateTime.UtcNow,
            ExpiresAt        = DateTime.UtcNow.AddHours(2)
        };

        await _context.BankSyncSessions.InsertOneAsync(session);

        return new BankStatementPreviewDto
        {
            SessionId      = session.Id,
            BankName       = connection.BankName,
            DetectedFormat = detectedFormat,
            TotalCount     = transactions.Count,
            Transactions   = transactions.Select(t => new ParsedBankTransactionDto
            {
                RowNumber   = t.RowNumber,
                Date        = t.Date.ToString("yyyy-MM-dd"),
                Description = t.Description,
                Amount      = t.Amount,
                Type        = t.Type
            }).ToList()
        };
    }

    public async Task<BankSyncConfirmResultDto> ConfirmSyncAsync(
        string sessionId, string userId, ConfirmBankSyncRequest request,
        List<string> allowedCategoryIds)
    {
        if (string.IsNullOrWhiteSpace(request.ExpenseBookId))
            throw new ArgumentException("ExpenseBookId is required");

        var session = await LoadSessionAsync(sessionId, userId)
            ?? throw new KeyNotFoundException("Sync session not found or has expired");

        if (session.Status == "confirmed")
            throw new InvalidOperationException("This sync session has already been confirmed");

        var excludeSet = new HashSet<int>(request.ExcludeRowNumbers);

        // Compute fingerprints now that we know the target book
        var candidates = session.Transactions
            .Where(t => !excludeSet.Contains(t.RowNumber))
            .ToList();

        foreach (var t in candidates)
            t.ExternalTxnRef = ComputeFingerprint(request.ExpenseBookId, t);

        // Batch duplicate check against the target book
        var fingerprints = candidates.Select(t => t.ExternalTxnRef).ToList();
        var existingRefs = await GetExistingRefsAsync(request.ExpenseBookId, fingerprints);

        var newRows       = new List<CsvExpenseRow>();
        int dupCount      = 0;

        foreach (var t in candidates)
        {
            if (existingRefs.Contains(t.ExternalTxnRef))
            {
                dupCount++;
                continue;
            }

            newRows.Add(new CsvExpenseRow
            {
                RowNumber      = t.RowNumber,
                Description    = t.Description,
                Amount         = t.Amount,
                Date           = t.Date.ToString("yyyy-MM-dd"),
                Category       = "Uncategorized",
                PaymentMethod  = DetectPaymentMethod(t.Description, request.DefaultPaymentMethod),
                Type           = t.Type,
                ExternalTxnRef = t.ExternalTxnRef
            });
        }

        ImportSessionDto importSession;

        if (newRows.Count > 0)
        {
            var importRequest = new StartImportRequest
            {
                FileName          = $"bank_sync_{session.BankName}_{DateTime.UtcNow:yyyyMMdd}.csv",
                Rows              = newRows,
                BankSyncSessionId = sessionId,
                BankName          = session.BankName
            };

            importSession = await _importService.CreateImportSessionAsync(
                request.ExpenseBookId, userId, importRequest, allowedCategoryIds);
        }
        else
        {
            // All rows were duplicates — return a synthetic completed session
            importSession = new ImportSessionDto
            {
                FileName       = $"bank_sync_{session.BankName}_{DateTime.UtcNow:yyyyMMdd}.csv",
                Status         = "completed",
                TotalRecords   = 0,
                SuccessCount   = 0,
                FailedCount    = 0,
                ProcessedCount = 0
            };
        }

        // Mark session confirmed
        var filter = Builders<BankSyncSession>.Filter.Eq(s => s.Id, sessionId);
        await _context.BankSyncSessions.UpdateOneAsync(filter, Builders<BankSyncSession>.Update
            .Set(s => s.Status, "confirmed")
            .Set(s => s.ImportSessionId, importSession.Id));

        // Update lastSyncedAt on the connection
        var connFilter = Builders<BankConnection>.Filter.Eq(c => c.Id, session.BankConnectionId);
        await _context.BankConnections.UpdateOneAsync(connFilter, Builders<BankConnection>.Update
            .Set(c => c.LastSyncedAt, DateTime.UtcNow)
            .Set(c => c.UpdatedAt, DateTime.UtcNow));

        return new BankSyncConfirmResultDto
        {
            ImportSession    = importSession,
            Imported         = newRows.Count,
            DuplicatesSkipped = dupCount
        };
    }

    // ── Private helpers ──────────────────────────────────────────────────────────

    private async Task<BankConnection?> LoadConnectionAsync(string connectionId, string userId)
    {
        var filter = Builders<BankConnection>.Filter.And(
            Builders<BankConnection>.Filter.Eq(c => c.Id, connectionId),
            Builders<BankConnection>.Filter.Eq(c => c.UserId, userId),
            Builders<BankConnection>.Filter.Eq(c => c.IsActive, true));

        return await _context.BankConnections.Find(filter).FirstOrDefaultAsync();
    }

    private async Task<BankSyncSession?> LoadSessionAsync(string sessionId, string userId)
    {
        var filter = Builders<BankSyncSession>.Filter.And(
            Builders<BankSyncSession>.Filter.Eq(s => s.Id, sessionId),
            Builders<BankSyncSession>.Filter.Eq(s => s.UserId, userId));

        return await _context.BankSyncSessions.Find(filter).FirstOrDefaultAsync();
    }

    private async Task<HashSet<string>> GetExistingRefsAsync(
        string expenseBookId, List<string> fingerprints)
    {
        if (fingerprints.Count == 0) return [];

        var filter = Builders<Expense>.Filter.And(
            Builders<Expense>.Filter.Eq(e => e.ExpenseBookId, expenseBookId),
            Builders<Expense>.Filter.In(e => e.ExternalTxnRef, fingerprints));

        var existing = await _context.Expenses
            .Find(filter)
            .Project(e => e.ExternalTxnRef!)
            .ToListAsync();

        return new HashSet<string>(existing, StringComparer.Ordinal);
    }

    private static string ComputeFingerprint(string expenseBookId, ParsedBankTransaction t)
    {
        var raw = $"{expenseBookId}|{t.Date:yyyy-MM-dd}|{Math.Abs(t.Amount):F2}|{t.Description.ToLowerInvariant().Trim()}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // Heuristic: UPI transactions usually say "UPI" in the description
    private static string DetectPaymentMethod(string description, string defaultMethod)
    {
        if (description.Contains("UPI", StringComparison.OrdinalIgnoreCase))
            return "UPI";
        if (description.Contains("NEFT", StringComparison.OrdinalIgnoreCase) ||
            description.Contains("RTGS", StringComparison.OrdinalIgnoreCase) ||
            description.Contains("IMPS", StringComparison.OrdinalIgnoreCase))
            return "Bank Transfer";
        if (description.Contains("ATM", StringComparison.OrdinalIgnoreCase))
            return "Cash";
        return defaultMethod;
    }
}
