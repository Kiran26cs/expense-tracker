using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services.Admin;

public class AdminCreditService : IAdminCreditService
{
    private readonly MongoDbContext _ctx;

    public AdminCreditService(MongoDbContext ctx) => _ctx = ctx;

    public async Task<AdminCreditBalanceDto?> GetBalanceAsync(string bookId)
    {
        var book = await _ctx.ExpenseBooks.Find(b => b.Id == bookId).FirstOrDefaultAsync();
        if (book == null) return null;

        var credits = await _ctx.BookCredits.Find(bc => bc.ExpenseBookId == bookId).FirstOrDefaultAsync();
        var owner   = await _ctx.Users.Find(u => u.Id == book.UserId).FirstOrDefaultAsync();

        return new AdminCreditBalanceDto
        {
            BookId          = bookId,
            BookName        = book.Name,
            OwnerEmail      = owner?.Email ?? string.Empty,
            Plan            = (owner?.Plan ?? Domain.PlanType.Free).ToString(),
            FreeCreditsLeft  = credits?.FreeCreditsLeft  ?? 0,
            PaidCreditsLeft  = credits?.PaidCreditsLeft  ?? 0,
            FreeCreditsLimit = credits?.FreeCreditsLimit ?? 0,
            LastResetDate   = credits?.LastResetDate ?? DateTime.UtcNow,
        };
    }

    public async Task<AdminCreditBalanceDto> GrantCreditsAsync(
        string bookId, int amount, string? note, string adminId, string adminEmail)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be greater than zero.");

        var book = await _ctx.ExpenseBooks.Find(b => b.Id == bookId).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Expense book not found.");

        var existing = await _ctx.BookCredits.Find(bc => bc.ExpenseBookId == bookId).FirstOrDefaultAsync();
        if (existing == null)
        {
            await _ctx.BookCredits.InsertOneAsync(new BookCredits
            {
                ExpenseBookId   = bookId,
                PaidCreditsLeft = amount,
                UpdatedAt       = DateTime.UtcNow,
            });
        }
        else
        {
            await _ctx.BookCredits.UpdateOneAsync(
                bc => bc.ExpenseBookId == bookId,
                Builders<BookCredits>.Update
                    .Inc(bc => bc.PaidCreditsLeft, amount)
                    .Set(bc => bc.UpdatedAt, DateTime.UtcNow));
        }

        await _ctx.CreditTransactions.InsertOneAsync(new CreditTransaction
        {
            ExpenseBookId     = bookId,
            TriggeredByUserId = adminId,
            Amount            = amount,
            Reason            = "admin_grant",
            ToolsUsed         = [],
            Timestamp         = DateTime.UtcNow,
        });

        var owner = await _ctx.Users.Find(u => u.Id == book.UserId).FirstOrDefaultAsync();
        await _ctx.AdminAuditLogs.InsertOneAsync(new AdminAuditLog
        {
            AdminId    = adminId,
            AdminEmail = adminEmail,
            Action     = AdminActions.GrantCredits,
            TargetType = "book",
            TargetId   = bookId,
            Payload    = new Dictionary<string, object> { ["amount"] = amount, ["note"] = note ?? string.Empty },
            Summary    = $"Granted {amount} credits to \"{book.Name}\" ({owner?.Email})",
            Timestamp  = DateTime.UtcNow,
        });

        return (await GetBalanceAsync(bookId))!;
    }

    public async Task<AdminCreditTransactionListDto> GetTransactionsAsync(string bookId, int page, int pageSize)
    {
        var filter = Builders<CreditTransaction>.Filter.Eq(ct => ct.ExpenseBookId, bookId);
        var total  = await _ctx.CreditTransactions.CountDocumentsAsync(filter);
        var txns   = await _ctx.CreditTransactions.Find(filter)
            .SortByDescending(ct => ct.Timestamp)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return new AdminCreditTransactionListDto
        {
            Total = total,
            Transactions = txns.Select(t => new AdminCreditTransactionDto
            {
                Id                = t.Id,
                ExpenseBookId     = t.ExpenseBookId,
                TriggeredByUserId = t.TriggeredByUserId,
                Amount            = t.Amount,
                Reason            = t.Reason,
                ToolsUsed         = t.ToolsUsed,
                Timestamp         = t.Timestamp,
            }).ToList(),
        };
    }
}
