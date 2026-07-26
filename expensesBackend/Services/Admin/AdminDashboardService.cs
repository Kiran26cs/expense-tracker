using ExpensesBackend.API.Domain;
using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services.Admin;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly MongoDbContext _ctx;

    private const decimal StarterPrice = 199m;
    private const decimal ProPrice     = 399m;

    public AdminDashboardService(MongoDbContext ctx) => _ctx = ctx;

    // ── User Stats ────────────────────────────────────────────────────────────

    public async Task<AdminUserStatsDto> GetUserStatsAsync()
    {
        var now        = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var weekStart  = now.AddDays(-7);
        var sixMonthsAgo = now.AddMonths(-5);
        var sixMonthCutoff = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var allUsers = await _ctx.Users.Find(FilterDefinition<User>.Empty).ToListAsync();

        var byPlan = new PlanBreakdownDto
        {
            Free    = allUsers.Count(u => u.Plan == PlanType.Free),
            Starter = allUsers.Count(u => u.Plan == PlanType.Starter),
            Pro     = allUsers.Count(u => u.Plan == PlanType.Pro),
        };

        // Build last-6-month growth buckets
        var growth = new List<MonthlyCountDto>();
        for (var i = 5; i >= 0; i--)
        {
            var target   = now.AddMonths(-i);
            var mStart   = new DateTime(target.Year, target.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var mEnd     = mStart.AddMonths(1);
            var label    = mStart.ToString("yyyy-MM");
            var count    = allUsers.Count(u => u.CreatedAt >= mStart && u.CreatedAt < mEnd);
            growth.Add(new MonthlyCountDto { Month = label, Count = count });
        }

        return new AdminUserStatsDto
        {
            Total        = allUsers.Count,
            NewThisMonth = allUsers.Count(u => u.CreatedAt >= monthStart),
            NewThisWeek  = allUsers.Count(u => u.CreatedAt >= weekStart),
            ByPlan       = byPlan,
            GrowthByMonth = growth,
        };
    }

    // ── Subscription Stats ────────────────────────────────────────────────────

    public async Task<AdminSubscriptionStatsDto> GetSubscriptionStatsAsync()
    {
        var now        = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var subs = await _ctx.UserSubscriptions
            .Find(FilterDefinition<UserSubscription>.Empty)
            .ToListAsync();

        var active            = subs.Where(s => s.Status == "active").ToList();
        var starterActive     = active.Count(s => s.Plan == PlanType.Starter);
        var proActive         = active.Count(s => s.Plan == PlanType.Pro);
        var cancelledThisMonth = subs.Count(s => s.Status == "cancelled" && s.UpdatedAt >= monthStart);
        var pendingCancellation = active.Count(s => s.CancelAtPeriodEnd);

        return new AdminSubscriptionStatsDto
        {
            Active               = active.Count,
            NewThisMonth         = subs.Count(s => s.CreatedAt >= monthStart && s.Status == "active"),
            CancelledThisMonth   = cancelledThisMonth,
            PendingCancellation  = pendingCancellation,
            Mrr = new MrrBreakdownDto
            {
                StarterCount = starterActive,
                ProCount     = proActive,
                StarterMrr   = starterActive * StarterPrice,
                ProMrr       = proActive     * ProPrice,
                Total        = (starterActive * StarterPrice) + (proActive * ProPrice),
            },
        };
    }

    // ── Credit Stats ──────────────────────────────────────────────────────────

    public async Task<AdminCreditStatsDto> GetCreditStatsAsync()
    {
        var now        = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var debitFilter = Builders<CreditTransaction>.Filter.Lt(ct => ct.Amount, 0)
                        & Builders<CreditTransaction>.Filter.Gte(ct => ct.Timestamp, monthStart);

        var debits = await _ctx.CreditTransactions.Find(debitFilter).ToListAsync();

        var zeroCreditFilter = Builders<BookCredits>.Filter.Where(
            bc => bc.FreeCreditsLeft + bc.PaidCreditsLeft <= 0);

        var zeroBooks = await _ctx.BookCredits.Find(zeroCreditFilter).ToListAsync();

        var zeroCreditDtos = new List<ZeroCreditBookDto>();
        foreach (var bc in zeroBooks)
        {
            var book = await _ctx.ExpenseBooks
                .Find(eb => eb.Id == bc.ExpenseBookId && !eb.IsTemplate)
                .FirstOrDefaultAsync();
            if (book == null) continue;

            var owner = await _ctx.Users.Find(u => u.Id == book.UserId).FirstOrDefaultAsync();
            zeroCreditDtos.Add(new ZeroCreditBookDto
            {
                BookId     = bc.ExpenseBookId,
                BookName   = book.Name,
                OwnerEmail = owner?.Email ?? string.Empty,
                Plan       = (owner?.Plan ?? PlanType.Free).ToString(),
            });
        }

        return new AdminCreditStatsDto
        {
            ConsumedThisMonth = debits.Count,
            ByReason = new CreditReasonBreakdownDto
            {
                AiChat       = debits.Count(d => d.Reason == "ai_chat"),
                AutoClassify = debits.Count(d => d.Reason == "auto_classify"),
            },
            ZeroCreditBooks = zeroCreditDtos,
        };
    }

    // ── Book Stats ────────────────────────────────────────────────────────────

    public async Task<AdminBookStatsDto> GetBookStatsAsync()
    {
        var now        = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var books = await _ctx.ExpenseBooks.Find(FilterDefinition<ExpenseBook>.Empty).ToListAsync();

        return new AdminBookStatsDto
        {
            Total          = books.Count(b => !b.IsTemplate),
            TemplateBooks  = books.Count(b => b.IsTemplate),
            NewThisMonth   = books.Count(b => !b.IsTemplate && b.CreatedAt >= monthStart),
            AiChatEnabled  = books.Count(b => !b.IsTemplate && b.AiChatEnabled),
        };
    }

    // ── Import Stats ──────────────────────────────────────────────────────────

    public async Task<AdminImportStatsDto> GetImportStatsAsync()
    {
        var cutoff  = DateTime.UtcNow.AddHours(-24);
        var filter  = Builders<ImportSession>.Filter.Gte(s => s.CreatedAt, cutoff);
        var sessions = await _ctx.ImportSessions.Find(filter).ToListAsync();

        var failed = sessions
            .Where(s => s.Status == ImportStatus.Failed)
            .Select(s => new FailedImportSessionDto
            {
                Id       = s.Id,
                FileName = s.FileName,
                BookId   = s.ExpenseBookId,
                FailedAt = s.CompletedAt,
            })
            .ToList();

        return new AdminImportStatsDto
        {
            Last24h = new ImportStatusBreakdownDto
            {
                Completed           = sessions.Count(s => s.Status == ImportStatus.Completed),
                Failed              = sessions.Count(s => s.Status == ImportStatus.Failed),
                CompletedWithErrors = sessions.Count(s => s.Status == ImportStatus.CompletedWithErrors),
                Processing          = sessions.Count(s => s.Status == ImportStatus.Processing),
                Queued              = sessions.Count(s => s.Status == ImportStatus.Queued),
            },
            FailedSessions = failed,
        };
    }

    // ── Recent Admin Actions ──────────────────────────────────────────────────

    public async Task<AdminRecentActionsDto> GetRecentActionsAsync(int limit = 20)
    {
        var logs = await _ctx.AdminAuditLogs
            .Find(FilterDefinition<AdminAuditLog>.Empty)
            .SortByDescending(l => l.Timestamp)
            .Limit(limit)
            .ToListAsync();

        return new AdminRecentActionsDto
        {
            Actions = logs.Select(l => new AdminAuditLogDto
            {
                Id         = l.Id,
                AdminEmail = l.AdminEmail,
                Action     = l.Action,
                TargetType = l.TargetType,
                TargetId   = l.TargetId,
                Summary    = l.Summary,
                Timestamp  = l.Timestamp,
            }).ToList(),
        };
    }
}
