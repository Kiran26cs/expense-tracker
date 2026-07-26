using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;
using System.Threading.Channels;

namespace ExpensesBackend.API.Services.Admin;

public class AdminJobService : IAdminJobService
{
    private readonly MongoDbContext _ctx;

    public AdminJobService(MongoDbContext ctx) => _ctx = ctx;

    public async Task<List<AdminImportSessionDto>> GetImportSessionsAsync(string? status)
    {
        var filter = string.IsNullOrWhiteSpace(status)
            ? FilterDefinition<ImportSession>.Empty
            : Builders<ImportSession>.Filter.Eq(s => s.Status, status);

        var sessions = await _ctx.ImportSessions.Find(filter)
            .SortByDescending(s => s.CreatedAt)
            .Limit(100)
            .ToListAsync();

        return sessions.Select(MapToDto).ToList();
    }

    public async Task<AdminImportSessionDto?> GetSessionAsync(string sessionId)
    {
        var session = await _ctx.ImportSessions.Find(s => s.Id == sessionId).FirstOrDefaultAsync();
        return session == null ? null : MapToDto(session);
    }

    public async Task RetryImportAsync(string sessionId, Channel<ImportJobPayload> importChannel)
    {
        var session = await _ctx.ImportSessions.Find(s => s.Id == sessionId).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Import session not found.");

        if (session.Status != ImportStatus.Failed && session.Status != ImportStatus.CompletedWithErrors)
            throw new InvalidOperationException($"Session status '{session.Status}' cannot be retried. Only failed or completedWithErrors sessions can be retried.");

        // Load allowed category IDs for the book
        var categories = await _ctx.Categories
            .Find(c => c.ExpenseBookId == session.ExpenseBookId)
            .Project(c => c.Id)
            .ToListAsync();

        // Build rows from stored records (only pending/failed ones)
        var toRetry = session.Records
            .Where(r => r.Status != ImportRecordStatus.Success)
            .ToList();

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
            Currency      = r.Currency ?? string.Empty,
        }).ToList();

        // Reset session status to queued
        await _ctx.ImportSessions.UpdateOneAsync(
            s => s.Id == sessionId,
            Builders<ImportSession>.Update
                .Set(s => s.Status,         ImportStatus.Queued)
                .Set(s => s.ProcessedCount, 0)
                .Set(s => s.SuccessCount,   session.SuccessCount) // preserve already-succeeded records
                .Set(s => s.FailedCount,    0)
                .Set(s => s.CompletedAt,    (DateTime?)null));

        // Re-queue the job
        await importChannel.Writer.WriteAsync(new ImportJobPayload
        {
            ImportSessionId    = sessionId,
            ExpenseBookId      = session.ExpenseBookId,
            UserId             = session.UserId,
            Rows               = rows,
            AllowedCategoryIds = categories,
            IsRetry            = true,
        });
    }

    private static AdminImportSessionDto MapToDto(ImportSession s) => new()
    {
        Id            = s.Id,
        ExpenseBookId = s.ExpenseBookId,
        UserId        = s.UserId,
        FileName      = s.FileName,
        Status        = s.Status,
        TotalRecords  = s.TotalRecords,
        SuccessCount  = s.SuccessCount,
        FailedCount   = s.FailedCount,
        CreatedAt     = s.CreatedAt,
        CompletedAt   = s.CompletedAt,
    };
}
