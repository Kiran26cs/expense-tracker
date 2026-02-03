using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services;

public class BudgetService : IBudgetService
{
    private readonly MongoDbContext _context;

    public BudgetService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<List<Budget>> GetBudgetsAsync(string userId, string? month = null)
    {
        var filterBuilder = Builders<Budget>.Filter;
        var filter = filterBuilder.Eq(b => b.UserId, userId);

        if (!string.IsNullOrEmpty(month))
        {
            // Filter by month (YYYY-MM format)
            var monthDate = DateTime.Parse(month + "-01");
            var startOfMonth = new DateTime(monthDate.Year, monthDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            filter &= filterBuilder.Gte(b => b.StartDate, startOfMonth) & 
                      filterBuilder.Lte(b => b.StartDate, endOfMonth);
        }

        var budgets = await _context.Budgets
            .Find(filter)
            .SortByDescending(b => b.StartDate)
            .ToListAsync();

        // Calculate spent amount for each budget
        foreach (var budget in budgets)
        {
            var expenseFilter = Builders<Expense>.Filter.Eq(e => e.UserId, userId) &
                               Builders<Expense>.Filter.Eq(e => e.Category, budget.Category) &
                               Builders<Expense>.Filter.Gte(e => e.Date, budget.StartDate) &
                               Builders<Expense>.Filter.Lte(e => e.Date, budget.EndDate);

            var expenses = await _context.Expenses.Find(expenseFilter).ToListAsync();
            budget.Spent = expenses.Sum(e => e.Amount);
            budget.UpdatedAt = DateTime.UtcNow;
        }

        return budgets;
    }

    public async Task<Budget?> GetBudgetByIdAsync(string userId, string budgetId)
    {
        var budget = await _context.Budgets
            .Find(b => b.Id == budgetId && b.UserId == userId)
            .FirstOrDefaultAsync();

        if (budget != null)
        {
            // Calculate spent amount
            var expenseFilter = Builders<Expense>.Filter.Eq(e => e.UserId, userId) &
                               Builders<Expense>.Filter.Eq(e => e.Category, budget.Category) &
                               Builders<Expense>.Filter.Gte(e => e.Date, budget.StartDate) &
                               Builders<Expense>.Filter.Lte(e => e.Date, budget.EndDate);

            var expenses = await _context.Expenses.Find(expenseFilter).ToListAsync();
            budget.Spent = expenses.Sum(e => e.Amount);
        }

        return budget;
    }

    public async Task<Budget> CreateBudgetAsync(Budget budget)
    {
        budget.CreatedAt = DateTime.UtcNow;
        budget.UpdatedAt = DateTime.UtcNow;
        budget.Spent = 0;

        await _context.Budgets.InsertOneAsync(budget);

        return budget;
    }

    public async Task<Budget?> UpdateBudgetAsync(string userId, string budgetId, Budget budget)
    {
        var existingBudget = await _context.Budgets
            .Find(b => b.Id == budgetId && b.UserId == userId)
            .FirstOrDefaultAsync();

        if (existingBudget == null)
            return null;

        budget.Id = budgetId;
        budget.UserId = userId;
        budget.CreatedAt = existingBudget.CreatedAt;
        budget.UpdatedAt = DateTime.UtcNow;

        // Calculate spent amount
        var expenseFilter = Builders<Expense>.Filter.Eq(e => e.UserId, userId) &
                           Builders<Expense>.Filter.Eq(e => e.Category, budget.Category) &
                           Builders<Expense>.Filter.Gte(e => e.Date, budget.StartDate) &
                           Builders<Expense>.Filter.Lte(e => e.Date, budget.EndDate);

        var expenses = await _context.Expenses.Find(expenseFilter).ToListAsync();
        budget.Spent = expenses.Sum(e => e.Amount);

        await _context.Budgets.ReplaceOneAsync(
            b => b.Id == budgetId && b.UserId == userId,
            budget
        );

        return budget;
    }

    public async Task<bool> DeleteBudgetAsync(string userId, string budgetId)
    {
        var result = await _context.Budgets.DeleteOneAsync(
            b => b.Id == budgetId && b.UserId == userId
        );

        return result.DeletedCount > 0;
    }
}
