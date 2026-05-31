using ExpensesBackend.API.Domain;
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
        // Demo books always have credits — AI is free to explore
        if (await IsDemoBookAsync(bookId)) return true;

        var credits = await GetOrCreateAsync(bookId);
        await ApplyMonthlyResetIfDueAsync(credits);
        return credits.FreeCreditsLeft + credits.PaidCreditsLeft > 0;
    }

    public async Task DeductAsync(string bookId, string triggeredByUserId, List<string> toolsUsed)
    {
        // Demo books: log usage for analytics but do NOT deduct from the credit balance
        if (await IsDemoBookAsync(bookId))
        {
            await _context.CreditTransactions.InsertOneAsync(new CreditTransaction
            {
                ExpenseBookId     = bookId,
                TriggeredByUserId = triggeredByUserId,
                Amount            = 0,
                Reason            = "demo_ai_chat",
                ToolsUsed         = toolsUsed,
                Timestamp         = DateTime.UtcNow,
            });
            return;
        }

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

    public async Task<AutoClassifyConsumeResult> ConsumeAutoClassifyAsync(string bookId, string triggeredByUserId)
    {
        if (await IsDemoBookAsync(bookId))
        {
            // Demo books: treat as free, don't touch counters
            return new AutoClassifyConsumeResult(Allowed: true, UsedCredit: false, FreeUsed: 0, FreeQuota: 0);
        }

        var credits = await GetOrCreateAsync(bookId);
        await ApplyMonthlyResetIfDueAsync(credits);

        var quota = PlanLimits.AutoClassifyFreeQuota(credits.PlanType);

        if (credits.AutoClassifyFreeUsed < quota)
        {
            // Within free quota — consume one free use
            var newUsed = credits.AutoClassifyFreeUsed + 1;
            await _context.BookCredits.UpdateOneAsync(
                bc => bc.ExpenseBookId == bookId,
                Builders<BookCredits>.Update
                    .Set(bc => bc.AutoClassifyFreeUsed, newUsed)
                    .Set(bc => bc.UpdatedAt, DateTime.UtcNow));

            return new AutoClassifyConsumeResult(Allowed: true, UsedCredit: false, FreeUsed: newUsed, FreeQuota: quota);
        }

        // Free quota exhausted — try to charge a credit
        if (credits.FreeCreditsLeft + credits.PaidCreditsLeft <= 0)
            return new AutoClassifyConsumeResult(Allowed: false, UsedCredit: false, FreeUsed: credits.AutoClassifyFreeUsed, FreeQuota: quota);

        UpdateDefinition<BookCredits> update = credits.PaidCreditsLeft > 0
            ? Builders<BookCredits>.Update.Inc(bc => bc.PaidCreditsLeft, -1).Set(bc => bc.UpdatedAt, DateTime.UtcNow)
            : Builders<BookCredits>.Update.Inc(bc => bc.FreeCreditsLeft, -1).Set(bc => bc.UpdatedAt, DateTime.UtcNow);

        await _context.BookCredits.UpdateOneAsync(bc => bc.ExpenseBookId == bookId, update);

        await _context.CreditTransactions.InsertOneAsync(new CreditTransaction
        {
            ExpenseBookId     = bookId,
            TriggeredByUserId = triggeredByUserId,
            Amount            = -1,
            Reason            = "auto_classify",
            ToolsUsed         = ["auto_classify"],
            Timestamp         = DateTime.UtcNow,
        });

        return new AutoClassifyConsumeResult(Allowed: true, UsedCredit: true, FreeUsed: credits.AutoClassifyFreeUsed, FreeQuota: quota);
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

        // Only reset paid-plan books; Free-plan books have one-time trial credits
        var staleBooks = await _context.BookCredits
            .Find(bc => bc.LastResetDate < thisMonth && bc.PlanType != PlanType.Free)
            .ToListAsync();

        foreach (var book in staleBooks)
        {
            var update = Builders<BookCredits>.Update
                .Set(bc => bc.FreeCreditsLeft,      book.FreeCreditsLimit)
                .Set(bc => bc.AutoClassifyFreeUsed, 0)
                .Set(bc => bc.LastResetDate,        now)
                .Set(bc => bc.UpdatedAt,            now);

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

        var plan           = await GetPlanForBookAsync(bookId);
        var creditsLimit   = plan == PlanType.Free ? 15 : PlanLimits.MonthlyCredits(plan);

        var newRecord = new BookCredits
        {
            ExpenseBookId    = bookId,
            FreeCreditsLeft  = creditsLimit,
            PaidCreditsLeft  = 0,
            FreeCreditsLimit = creditsLimit,
            PlanType         = plan,
            LastResetDate    = DateTime.UtcNow,
        };

        await _context.BookCredits.InsertOneAsync(newRecord);
        return newRecord;
    }

    private async Task ApplyMonthlyResetIfDueAsync(BookCredits credits)
    {
        // Free-plan books have one-time trial credits — never reset
        if (credits.PlanType == PlanType.Free) return;

        var now       = DateTime.UtcNow;
        var thisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        if (credits.LastResetDate >= thisMonth) return;

        var update = Builders<BookCredits>.Update
            .Set(bc => bc.FreeCreditsLeft,       credits.FreeCreditsLimit)
            .Set(bc => bc.AutoClassifyFreeUsed,  0)
            .Set(bc => bc.LastResetDate,          now)
            .Set(bc => bc.UpdatedAt,              now);

        await _context.BookCredits.UpdateOneAsync(bc => bc.Id == credits.Id, update);

        credits.FreeCreditsLeft       = credits.FreeCreditsLimit;
        credits.AutoClassifyFreeUsed  = 0;
        credits.LastResetDate         = now;

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

    private async Task<bool> IsDemoBookAsync(string bookId)
    {
        var book = await _context.ExpenseBooks
            .Find(eb => eb.Id == bookId)
            .FirstOrDefaultAsync();
        return book?.IsTemplate == true;
    }

    private async Task<PlanType> GetPlanForBookAsync(string bookId)
    {
        var book = await _context.ExpenseBooks
            .Find(eb => eb.Id == bookId)
            .FirstOrDefaultAsync();

        if (book == null) return PlanType.Free;

        var user = await _context.Users
            .Find(u => u.Id == book.UserId)
            .FirstOrDefaultAsync();

        return user?.Plan ?? PlanType.Free;
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
