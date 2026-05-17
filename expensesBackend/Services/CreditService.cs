using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services;

public class CreditService : ICreditService
{
    private readonly MongoDbContext _context;

    public CreditService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<CreditBalanceDto> GetBalanceAsync(string bookId)
    {
        var credits = await GetOrCreateAsync(bookId);
        await ApplyMonthlyResetIfDueAsync(credits);
        return MapToDto(credits);
    }

    public async Task<bool> HasCreditsAsync(string bookId)
    {
        var credits = await GetOrCreateAsync(bookId);
        await ApplyMonthlyResetIfDueAsync(credits);
        return credits.FreeCreditsLeft + credits.PaidCreditsLeft > 0;
    }

    public async Task DeductAsync(string bookId, string triggeredByUserId, List<string> toolsUsed)
    {
        var credits = await GetOrCreateAsync(bookId);
        await ApplyMonthlyResetIfDueAsync(credits);

        // Deduct from paid first, then free
        UpdateDefinition<BookCredits> update;
        if (credits.PaidCreditsLeft > 0)
        {
            update = Builders<BookCredits>.Update
                .Inc(bc => bc.PaidCreditsLeft, -1)
                .Set(bc => bc.UpdatedAt, DateTime.UtcNow);
        }
        else
        {
            update = Builders<BookCredits>.Update
                .Inc(bc => bc.FreeCreditsLeft, -1)
                .Set(bc => bc.UpdatedAt, DateTime.UtcNow);
        }

        await _context.BookCredits.UpdateOneAsync(bc => bc.ExpenseBookId == bookId, update);

        await _context.CreditTransactions.InsertOneAsync(new CreditTransaction
        {
            ExpenseBookId     = bookId,
            TriggeredByUserId = triggeredByUserId,
            Amount            = -1,
            Reason            = "ai_chat",
            ToolsUsed         = toolsUsed,
            Timestamp         = DateTime.UtcNow,
        });
    }

    public async Task AdminGrantAsync(string bookId, int amount, string grantedByUserId)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Grant amount must be greater than zero.");

        await GetOrCreateAsync(bookId);

        var update = Builders<BookCredits>.Update
            .Inc(bc => bc.PaidCreditsLeft, amount)
            .Set(bc => bc.UpdatedAt, DateTime.UtcNow);

        await _context.BookCredits.UpdateOneAsync(bc => bc.ExpenseBookId == bookId, update);

        await _context.CreditTransactions.InsertOneAsync(new CreditTransaction
        {
            ExpenseBookId     = bookId,
            TriggeredByUserId = grantedByUserId,
            Amount            = amount,
            Reason            = "admin_grant",
            ToolsUsed         = [],
            Timestamp         = DateTime.UtcNow,
        });
    }

    public async Task ResetMonthlyFreeCreditsAsync()
    {
        var now        = DateTime.UtcNow;
        var thisMonth  = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Find all books whose free credits were last reset before this calendar month
        var staleBooks = await _context.BookCredits
            .Find(bc => bc.LastResetDate < thisMonth)
            .ToListAsync();

        foreach (var book in staleBooks)
        {
            var update = Builders<BookCredits>.Update
                .Set(bc => bc.FreeCreditsLeft, book.FreeCreditsLimit)
                .Set(bc => bc.LastResetDate,   now)
                .Set(bc => bc.UpdatedAt,       now);

            await _context.BookCredits.UpdateOneAsync(bc => bc.Id == book.Id, update);

            await _context.CreditTransactions.InsertOneAsync(new CreditTransaction
            {
                ExpenseBookId     = book.ExpenseBookId,
                TriggeredByUserId = "system",
                Amount            = book.FreeCreditsLimit,
                Reason            = "monthly_reset",
                ToolsUsed         = [],
                Timestamp         = now,
            });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<BookCredits> GetOrCreateAsync(string bookId)
    {
        var existing = await _context.BookCredits
            .Find(bc => bc.ExpenseBookId == bookId)
            .FirstOrDefaultAsync();

        if (existing != null) return existing;

        var newRecord = new BookCredits
        {
            ExpenseBookId    = bookId,
            FreeCreditsLeft  = 50,
            PaidCreditsLeft  = 0,
            FreeCreditsLimit = 50,
            LastResetDate    = DateTime.UtcNow,
        };

        await _context.BookCredits.InsertOneAsync(newRecord);
        return newRecord;
    }

    private async Task ApplyMonthlyResetIfDueAsync(BookCredits credits)
    {
        var now       = DateTime.UtcNow;
        var thisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        if (credits.LastResetDate >= thisMonth) return;

        var update = Builders<BookCredits>.Update
            .Set(bc => bc.FreeCreditsLeft, credits.FreeCreditsLimit)
            .Set(bc => bc.LastResetDate,   now)
            .Set(bc => bc.UpdatedAt,       now);

        await _context.BookCredits.UpdateOneAsync(bc => bc.Id == credits.Id, update);

        credits.FreeCreditsLeft = credits.FreeCreditsLimit;
        credits.LastResetDate   = now;

        await _context.CreditTransactions.InsertOneAsync(new CreditTransaction
        {
            ExpenseBookId     = credits.ExpenseBookId,
            TriggeredByUserId = "system",
            Amount            = credits.FreeCreditsLimit,
            Reason            = "monthly_reset",
            ToolsUsed         = [],
            Timestamp         = now,
        });
    }

    private static CreditBalanceDto MapToDto(BookCredits bc) => new()
    {
        ExpenseBookId    = bc.ExpenseBookId,
        FreeCreditsLeft  = bc.FreeCreditsLeft,
        PaidCreditsLeft  = bc.PaidCreditsLeft,
        FreeCreditsLimit = bc.FreeCreditsLimit,
        LastResetDate    = bc.LastResetDate,
    };
}
