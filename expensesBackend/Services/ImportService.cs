using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;
using System.Threading.Channels;

namespace ExpensesBackend.API.Services;

public class ImportService : IImportService
{
    private readonly MongoDbContext _context;
    private readonly Channel<ImportJobPayload> _channel;

    public ImportService(MongoDbContext context, Channel<ImportJobPayload> channel)
    {
        _context = context;
        _channel = channel;
    }

    public async Task<ImportSessionDto> CreateImportSessionAsync(
        string expenseBookId, string userId, StartImportRequest request,
        List<string> allowedCategoryIds)
    {
        if (request.Rows.Count == 0)
            throw new ArgumentException("No rows to import");

        var session = new ImportSession
        {
            ExpenseBookId = expenseBookId,
            UserId        = userId,
            FileName      = request.FileName,
            Status        = ImportStatus.Queued,
            TotalRecords  = request.Rows.Count,
            Records       = request.Rows.Select(r => new ImportRecord
            {
                RowNumber     = r.RowNumber,
                Description   = r.Description,
                Amount        = r.Amount,
                Category      = r.Category,
                Date          = r.Date,
                PaymentMethod = r.PaymentMethod,
                Notes         = string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes,
                Type          = r.Type,
                Currency      = string.IsNullOrWhiteSpace(r.Currency) ? null : r.Currency,
                Status        = ImportRecordStatus.Pending
            }).ToList()
        };

        await _context.ImportSessions.InsertOneAsync(session);

        await _channel.Writer.WriteAsync(new ImportJobPayload
        {
            ImportSessionId   = session.Id,
            ExpenseBookId     = expenseBookId,
            UserId            = userId,
            Rows              = request.Rows,
            AllowedCategoryIds = allowedCategoryIds
        });

        return MapToDto(session);
    }

    public async Task<List<ImportSessionSummaryDto>> GetImportSessionsAsync(
        string expenseBookId, string userId)
    {
        var filter = Builders<ImportSession>.Filter.And(
            Builders<ImportSession>.Filter.Eq(s => s.ExpenseBookId, expenseBookId),
            Builders<ImportSession>.Filter.Eq(s => s.UserId, userId),
            Builders<ImportSession>.Filter.Ne(s => s.JobType, "templateCreation"));

        var sessions = await _context.ImportSessions
            .Find(filter)
            .SortByDescending(s => s.CreatedAt)
            .Limit(20)
            .ToListAsync();

        return sessions.Select(MapToSummaryDto).ToList();
    }

    public async Task<ImportSessionDto> GetImportSessionByIdAsync(
        string importId, string expenseBookId, string userId)
    {
        var session = await LoadSessionAsync(importId, expenseBookId, userId)
            ?? throw new KeyNotFoundException("Import session not found");

        return MapToDto(session);
    }

    public async Task<ImportSessionDto> RetryFailedAsync(
        string importId, string expenseBookId, string userId,
        List<string> allowedCategoryIds, List<int>? rowNumbers = null)
    {
        var session = await LoadSessionAsync(importId, expenseBookId, userId)
            ?? throw new KeyNotFoundException("Import session not found");

        var toRetry = session.Records
            .Where(r => r.Status == ImportRecordStatus.Failed
                && (rowNumbers == null || rowNumbers.Count == 0 || rowNumbers.Contains(r.RowNumber)))
            .ToList();

        if (toRetry.Count == 0)
            throw new InvalidOperationException("No failed records to retry");

        // Reset selected records to pending
        foreach (var rec in toRetry)
        {
            rec.Status       = ImportRecordStatus.Pending;
            rec.ErrorMessage = null;
        }

        // Recalculate counters
        session.ProcessedCount = session.Records.Count(r => r.Status != ImportRecordStatus.Pending);
        session.SuccessCount   = session.Records.Count(r => r.Status == ImportRecordStatus.Success);
        session.FailedCount    = session.Records.Count(r => r.Status == ImportRecordStatus.Failed);
        session.Status         = ImportStatus.Processing;
        session.CompletedAt    = null;

        var filter = Builders<ImportSession>.Filter.Eq(s => s.Id, session.Id);
        await _context.ImportSessions.ReplaceOneAsync(filter, session);

        // Build retry rows from the stored row data
        var rows = toRetry.Select(r => new CsvExpenseRow
        {
            RowNumber     = r.RowNumber,
            Description   = r.Description,
            Amount        = r.Amount,
            Category      = r.Category,
            Date          = r.Date,
            PaymentMethod = r.PaymentMethod,
            Notes         = r.Notes ?? string.Empty,
            Type          = r.Type,
            Currency      = r.Currency ?? string.Empty
        }).ToList();

        await _channel.Writer.WriteAsync(new ImportJobPayload
        {
            ImportSessionId    = session.Id,
            ExpenseBookId      = expenseBookId,
            UserId             = userId,
            Rows               = rows,
            AllowedCategoryIds = allowedCategoryIds,
            IsRetry            = true
        });

        return MapToDto(session);
    }

    // ── Private helpers ─────────────────────────────────────────────────────────

    private async Task<ImportSession?> LoadSessionAsync(
        string importId, string expenseBookId, string userId)
    {
        var filter = Builders<ImportSession>.Filter.And(
            Builders<ImportSession>.Filter.Eq(s => s.Id, importId),
            Builders<ImportSession>.Filter.Eq(s => s.ExpenseBookId, expenseBookId),
            Builders<ImportSession>.Filter.Eq(s => s.UserId, userId));

        return await _context.ImportSessions.Find(filter).FirstOrDefaultAsync();
    }

    private static ImportSessionDto MapToDto(ImportSession s) => new()
    {
        Id             = s.Id,
        ExpenseBookId  = s.ExpenseBookId,
        FileName       = s.FileName,
        Status         = s.Status,
        TotalRecords   = s.TotalRecords,
        ProcessedCount = s.ProcessedCount,
        SuccessCount   = s.SuccessCount,
        FailedCount    = s.FailedCount,
        Records        = s.Records.Select(r => new ImportRecordDto
        {
            RowNumber     = r.RowNumber,
            Description   = r.Description,
            Amount        = r.Amount,
            Category      = r.Category,
            Date          = r.Date,
            PaymentMethod = r.PaymentMethod,
            Status        = r.Status,
            ErrorMessage  = r.ErrorMessage
        }).ToList(),
        CreatedAt   = s.CreatedAt,
        CompletedAt = s.CompletedAt
    };

    private static ImportSessionSummaryDto MapToSummaryDto(ImportSession s) => new()
    {
        Id             = s.Id,
        FileName       = s.FileName,
        Status         = s.Status,
        TotalRecords   = s.TotalRecords,
        ProcessedCount = s.ProcessedCount,
        SuccessCount   = s.SuccessCount,
        FailedCount    = s.FailedCount,
        CreatedAt      = s.CreatedAt,
        CompletedAt    = s.CompletedAt
    };
}
