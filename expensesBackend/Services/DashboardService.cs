using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services;

public class DashboardService : IDashboardService
{
    private readonly MongoDbContext _context;

    public DashboardService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummary> GetSummaryAsync(string userId, DateTime? startDate, DateTime? endDate)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var end = endDate ?? DateTime.UtcNow;

        var filterBuilder = Builders<Domain.Entities.Expense>.Filter;
        var filter = filterBuilder.Eq(e => e.UserId, userId) &
                    filterBuilder.Gte(e => e.Date, start) &
                    filterBuilder.Lte(e => e.Date, end);

        var expenses = await _context.Expenses
            .Find(filter)
            .ToListAsync();

        var totalExpenses = expenses.Sum(e => e.Amount);

        // Get user's monthly income
        var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        var totalIncome = user?.MonthlyIncome ?? 0;

        // Category breakdown
        var categoryBreakdown = expenses
            .GroupBy(e => e.Category)
            .Select(g => new CategoryBreakdown
            {
                Category = g.Key,
                Amount = g.Sum(e => e.Amount),
                Percentage = totalExpenses > 0 ? (double)(g.Sum(e => e.Amount) / totalExpenses * 100) : 0
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        // Recent transactions
        var recentTransactions = expenses
            .OrderByDescending(e => e.Date)
            .Take(10)
            .Select(e => new ExpenseDto
            {
                Id = e.Id,
                Amount = e.Amount,
                Date = e.Date,
                Category = e.Category,
                PaymentMethod = e.PaymentMethod,
                Description = e.Description,
                Notes = e.Notes,
                ReceiptUrl = e.ReceiptUrl,
                IsRecurring = e.IsRecurring,
                CreatedAt = e.CreatedAt
            })
            .ToList();

        return new DashboardSummary
        {
            TotalExpenses = totalExpenses,
            TotalIncome = totalIncome,
            Savings = totalIncome - totalExpenses,
            CategoryBreakdown = categoryBreakdown,
            RecentTransactions = recentTransactions
        };
    }

    public async Task<List<MonthlyTrend>> GetMonthlyTrendsAsync(string userId, int months)
    {
        var startDate = DateTime.UtcNow.AddMonths(-months);
        
        var filterBuilder = Builders<Domain.Entities.Expense>.Filter;
        var filter = filterBuilder.Eq(e => e.UserId, userId) &
                    filterBuilder.Gte(e => e.Date, startDate);

        var expenses = await _context.Expenses
            .Find(filter)
            .ToListAsync();

        var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        var monthlyIncome = user?.MonthlyIncome ?? 0;

        var trends = expenses
            .GroupBy(e => new { e.Date.Year, e.Date.Month })
            .Select(g => new MonthlyTrend
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                TotalExpenses = g.Sum(e => e.Amount),
                TotalIncome = monthlyIncome
            })
            .OrderBy(t => t.Month)
            .ToList();

        return trends;
    }
}
