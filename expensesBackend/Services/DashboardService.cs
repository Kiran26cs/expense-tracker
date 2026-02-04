using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
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

    public async Task<List<DailyTransactionGroup>> GetGroupedTransactionsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var filterBuilder = Builders<Domain.Entities.DailyExpenseSummary>.Filter;
        var filter = filterBuilder.Eq(d => d.UserId, userId) &
                     filterBuilder.Gte(d => d.Date, start.Date) &
                     filterBuilder.Lte(d => d.Date, end.Date);

        var summaries = await _context.DailyExpenseSummaries
            .Find(filter)
            .SortByDescending(d => d.Date)
            .Limit(10) // Get 10 most recent days
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        var groupedTransactions = summaries.Select(s => new DailyTransactionGroup
        {
            Date = s.Date,
            DateLabel = s.Date == today ? "Today" :
                       s.Date == yesterday ? "Yesterday" :
                       s.Date.ToString("MMMM dd, yyyy"),
            CategorySpending = s.CategorySpending.Select(c => new CategorySpendingDto
            {
                Category = c.Category,
                Amount = c.Amount,
                Count = c.Count
            }).ToList(),
            TotalSpent = s.TotalSpent
        }).ToList();

        return groupedTransactions;
    }

    public async Task MigrateDailySummariesAsync(string userId)
    {
        // Get all expenses for the user
        var expenses = await _context.Expenses
            .Find(e => e.UserId == userId)
            .ToListAsync();

        if (!expenses.Any())
            return;

        // Group expenses by date
        var groupedByDate = expenses
            .GroupBy(e => e.Date.Date)
            .ToList();

        foreach (var dateGroup in groupedByDate)
        {
            var date = dateGroup.Key;

            // Group by category within each date
            var categoryGroups = dateGroup
                .GroupBy(e => e.Category)
                .Select(g => new CategorySpending
                {
                    Category = g.Key,
                    Amount = g.Sum(e => e.Amount),
                    Count = g.Count()
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            var totalSpent = categoryGroups.Sum(c => c.Amount);

            // Check if summary already exists for this date
            var existingSummary = await _context.DailyExpenseSummaries
                .Find(s => s.UserId == userId && s.Date == date)
                .FirstOrDefaultAsync();

            if (existingSummary == null)
            {
                // Create new summary
                var summary = new DailyExpenseSummary
                {
                    UserId = userId,
                    Date = date,
                    CategorySpending = categoryGroups,
                    TotalSpent = totalSpent,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.DailyExpenseSummaries.InsertOneAsync(summary);
            }
            else
            {
                // Update existing summary
                existingSummary.CategorySpending = categoryGroups;
                existingSummary.TotalSpent = totalSpent;
                existingSummary.UpdatedAt = DateTime.UtcNow;

                await _context.DailyExpenseSummaries.ReplaceOneAsync(
                    s => s.Id == existingSummary.Id,
                    existingSummary
                );
            }
        }
    }
}
