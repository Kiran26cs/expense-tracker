using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Cache;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services;

public class BudgetService : IBudgetService
{
    private readonly MongoDbContext _context;
    private readonly ICacheService _cache;
    private static readonly TimeSpan BudgetCacheTtl = TimeSpan.FromMinutes(5);

    public BudgetService(MongoDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<List<Budget>> GetBudgetsAsync(string userId, string? expenseBookId, string? month = null)
    {
        var monthKey = string.IsNullOrEmpty(month) ? CacheKeys.CurrentMonthKey() : month;
        var cacheKey = CacheKeys.UserBudgets(userId, expenseBookId, monthKey);
        var cached = await _cache.GetAsync<List<Budget>>(cacheKey);
        if (cached is not null)
            return cached;

        // ── Date range ──────────────────────────────────────────────────────────
        DateTime monthStart;
        string requestedPeriod;
        if (!string.IsNullOrEmpty(month) && DateTime.TryParse(month + "-01", out var parsedMonth))
        {
            monthStart = new DateTime(parsedMonth.Year, parsedMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            requestedPeriod = month;
        }
        else
        {
            var now = DateTime.UtcNow;
            monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            requestedPeriod = $"{now.Year}-{now.Month:D2}";
        }
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

        // ── Load categories for this expense book (to resolve ID ↔ name) ────────
        var allCategories = string.IsNullOrEmpty(expenseBookId)
            ? new List<Category>()
            : await _context.Categories
                .Find(Builders<Category>.Filter.Eq(c => c.ExpenseBookId, expenseBookId))
                .SortBy(c => c.Name)
                .ToListAsync();

        // Maps for resolving category ID ↔ name
        var idByName = allCategories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);
        var nameById = allCategories.ToDictionary(c => c.Id, c => c.Name, StringComparer.OrdinalIgnoreCase);

        // ── Load all existing budget records ────────────────────────────────────
        var budgetFilter = !string.IsNullOrEmpty(expenseBookId)
            ? Builders<Budget>.Filter.Eq(b => b.ExpenseBookId, expenseBookId)
            : Builders<Budget>.Filter.Eq(b => b.UserId, userId);
        var allBudgets = await _context.Budgets.Find(budgetFilter).ToListAsync();
        var budgetByName = allBudgets.ToDictionary(b => b.Category, b => b, StringComparer.OrdinalIgnoreCase);

        // ── Load all expenses for this month in one query (avoid N+1) ───────────
        var expFilter = Builders<Expense>.Filter.Gte(e => e.Date, monthStart)
            & Builders<Expense>.Filter.Lte(e => e.Date, monthEnd)
            & Builders<Expense>.Filter.Ne(e => e.Type, "income");   // only count expenses
        if (!string.IsNullOrEmpty(expenseBookId))
            expFilter &= Builders<Expense>.Filter.Eq(e => e.ExpenseBookId, expenseBookId);
        else
            expFilter &= Builders<Expense>.Filter.Eq(e => e.UserId, userId);
        var allExpenses = await _context.Expenses.Find(expFilter).ToListAsync();

        // Group spending by raw category value (could be an ID or a name)
        var spendingByRaw = allExpenses
            .GroupBy(e => e.Category ?? "", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount), StringComparer.OrdinalIgnoreCase);

        // Helper: total spending for a category, matching by both its ID and name
        decimal GetSpent(string catName)
        {
            spendingByRaw.TryGetValue(catName, out var byName);
            decimal byId = 0;
            if (idByName.TryGetValue(catName, out var catId))
                spendingByRaw.TryGetValue(catId, out byId);
            return byName + byId;
        }

        var result = new List<Budget>();

        // ── Process categories in order ─────────────────────────────────────────
        foreach (var cat in allCategories)
        {
            var spent = GetSpent(cat.Name);
            budgetByName.TryGetValue(cat.Name, out var budget);
            var effectiveVersion = budget != null ? ResolveEffectiveVersion(budget, requestedPeriod, monthEnd) : null;

            if (budget == null)
            {
                // No budget record: only include if there is actual spending to show
                if (spent == 0) continue;

                result.Add(new Budget
                {
                    Id = $"virtual-{cat.Id}",
                    UserId = userId,
                    ExpenseBookId = expenseBookId,
                    Category = cat.Name,
                    Amount = 0,
                    Spent = spent,
                    Period = "monthly",
                    Currency = "INR",
                    Versions = new List<BudgetVersion>()
                });
            }
            else
            {
                budget.Amount = effectiveVersion?.Amount ?? 0;
                budget.Spent = spent;
                result.Add(budget);
            }
        }

        var ordered = result.OrderBy(b => b.Category).ToList();
        await _cache.SetAsync(cacheKey, ordered, BudgetCacheTtl);
        return ordered;
    }

    /// <summary>
    /// Finds the most recent version applicable to (or before) the requested month.
    /// Prefers effectivePeriod-based versions (new); falls back to effectiveDate for legacy records.
    /// </summary>
    private static BudgetVersion? ResolveEffectiveVersion(Budget budget, string requestedPeriod, DateTime monthEnd)
    {
        if (budget.Versions.Count == 0) return null;

        // New: versions tagged with effectivePeriod (YYYY-MM lexicographic comparison)
        var byPeriod = budget.Versions
            .Where(v => !string.IsNullOrEmpty(v.EffectivePeriod) &&
                        string.Compare(v.EffectivePeriod, requestedPeriod, StringComparison.Ordinal) <= 0)
            .OrderByDescending(v => v.EffectivePeriod, StringComparer.Ordinal)
            .ThenByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        if (byPeriod != null) return byPeriod;

        // Legacy: versions without effectivePeriod, filter by effectiveDate
        return budget.Versions
            .Where(v => string.IsNullOrEmpty(v.EffectivePeriod) && v.EffectiveDate <= monthEnd)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();
    }

    public async Task<Budget?> GetBudgetByIdAsync(string userId, string budgetId)
    {
        return await _context.Budgets
            .Find(b => b.Id == budgetId && b.UserId == userId)
            .FirstOrDefaultAsync();
    }

    public async Task<Budget> CreateBudgetAsync(Budget budget)
    {
        budget.CreatedAt = DateTime.UtcNow;
        budget.UpdatedAt = DateTime.UtcNow;
        budget.Spent = 0;
        await _context.Budgets.InsertOneAsync(budget);
        await _cache.RemoveAsync(CacheKeys.UserBudgets(budget.UserId, budget.ExpenseBookId, CacheKeys.CurrentMonthKey()));
        return budget;
    }

    public async Task<Budget?> UpdateBudgetAsync(string userId, string budgetId, Budget budget)
    {
        var existing = await _context.Budgets
            .Find(b => b.Id == budgetId && b.UserId == userId)
            .FirstOrDefaultAsync();

        if (existing == null) return null;

        budget.Id = budgetId;
        budget.UserId = userId;
        budget.CreatedAt = existing.CreatedAt;
        budget.UpdatedAt = DateTime.UtcNow;

        await _context.Budgets.ReplaceOneAsync(
            b => b.Id == budgetId && b.UserId == userId, budget);

        await _cache.RemoveAsync(CacheKeys.UserBudgets(userId, budget.ExpenseBookId, CacheKeys.CurrentMonthKey()));
        return budget;
    }

    public async Task<bool> DeleteBudgetAsync(string userId, string budgetId)
    {
        var existing = await _context.Budgets
            .Find(b => b.Id == budgetId && b.UserId == userId)
            .FirstOrDefaultAsync();
        var result = await _context.Budgets.DeleteOneAsync(
            b => b.Id == budgetId && b.UserId == userId);
        if (result.DeletedCount > 0 && existing != null)
            await _cache.RemoveAsync(CacheKeys.UserBudgets(userId, existing.ExpenseBookId, CacheKeys.CurrentMonthKey()));
        return result.DeletedCount > 0;
    }

    public async Task<Budget> UpsertBudgetVersionAsync(
        string userId,
        string? expenseBookId,
        string category,
        decimal amount,
        DateTime effectiveDate,
        string? effectivePeriod = null)
    {
        var filterBuilder = Builders<Budget>.Filter;
        var filter = filterBuilder.Eq(b => b.UserId, userId)
            & filterBuilder.Eq(b => b.Category, category);

        if (!string.IsNullOrEmpty(expenseBookId))
            filter &= filterBuilder.Eq(b => b.ExpenseBookId, expenseBookId);

        var existing = await _context.Budgets.Find(filter).FirstOrDefaultAsync();

        if (existing == null)
        {
            var firstVersion = new BudgetVersion
            {
                VersionNumber = 1,
                EffectivePeriod = effectivePeriod,
                EffectiveDate = effectiveDate,
                Amount = amount,
                CreatedAt = DateTime.UtcNow
            };
            var newBudget = new Budget
            {
                UserId = userId,
                ExpenseBookId = expenseBookId,
                Category = category,
                Amount = amount,
                LatestVersionNumber = 1,
                Versions = [firstVersion],
                Period = "monthly",
                Currency = "USD",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Budgets.InsertOneAsync(newBudget);
            await _cache.RemoveAsync(CacheKeys.UserBudgets(userId, expenseBookId, effectivePeriod ?? CacheKeys.CurrentMonthKey()));
            return newBudget;
        }
        else
        {
            var nextVersionNumber = existing.LatestVersionNumber + 1;
            var newVersion = new BudgetVersion
            {
                VersionNumber = nextVersionNumber,
                EffectivePeriod = effectivePeriod,
                EffectiveDate = effectiveDate,
                Amount = amount,
                CreatedAt = DateTime.UtcNow
            };

            var update = Builders<Budget>.Update
                .Push(b => b.Versions, newVersion)
                .Set(b => b.LatestVersionNumber, nextVersionNumber)
                .Set(b => b.Amount, amount)
                .Set(b => b.UpdatedAt, DateTime.UtcNow);

            await _context.Budgets.UpdateOneAsync(
                b => b.Id == existing.Id && b.UserId == userId, update);

            existing.Versions.Add(newVersion);
            existing.LatestVersionNumber = nextVersionNumber;
            existing.Amount = amount;
            await _cache.RemoveAsync(CacheKeys.UserBudgets(userId, expenseBookId, effectivePeriod ?? CacheKeys.CurrentMonthKey()));
            return existing;
        }
    }
}
