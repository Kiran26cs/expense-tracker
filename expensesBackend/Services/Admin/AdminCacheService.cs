using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Cache;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services.Admin;

public class AdminCacheService : IAdminCacheService
{
    private readonly ICacheService _cache;
    private readonly MongoDbContext _ctx;

    public AdminCacheService(ICacheService cache, MongoDbContext ctx)
    {
        _cache = cache;
        _ctx   = ctx;
    }

    public async Task InvalidateUserAsync(string userId)
    {
        await Task.WhenAll(
            _cache.RemoveAsync(CacheKeys.UserSettings(userId)),
            _cache.RemoveAsync(CacheKeys.UserExpenseBooks(userId)),
            _cache.RemoveAsync(CacheKeys.DashboardSummary(userId, null)),
            _cache.RemoveAsync(CacheKeys.UserBudgets(userId, null, CacheKeys.CurrentMonthKey()))
        );
    }

    public async Task InvalidateBookAsync(string bookId)
    {
        var tasks = new List<Task>
        {
            _cache.RemoveAsync(CacheKeys.BookSettings(bookId)),
            _cache.RemoveAsync(CacheKeys.Categories(bookId)),
        };

        // Also invalidate the book owner's user-level cache
        var book = await _ctx.ExpenseBooks.Find(b => b.Id == bookId).FirstOrDefaultAsync();
        if (book != null)
        {
            tasks.Add(_cache.RemoveAsync(CacheKeys.UserExpenseBooks(book.UserId)));
            tasks.Add(_cache.RemoveAsync(CacheKeys.DashboardSummary(book.UserId, bookId)));

            // Invalidate member permission caches for all book members
            var members = await _ctx.ExpenseBookMembers
                .Find(m => m.ExpenseBookId == bookId)
                .ToListAsync();
            tasks.AddRange(members
                .Where(m => m.UserId != null)
                .Select(m => _cache.RemoveAsync(CacheKeys.MemberPermissions(bookId, m.UserId!))));
        }

        await Task.WhenAll(tasks);
    }
}
