using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services;

public class DashboardService : IDashboardService
{
    private readonly MongoDbContext _context;
    private readonly IExpenseService _expenseService;

    public DashboardService(MongoDbContext context, IExpenseService expenseService)
    {
        _context = context;
        _expenseService = expenseService;
    }

    public async Task<DashboardSummary> GetSummaryAsync(string userId, string? expenseBookId, DateTime? startDate, DateTime? endDate)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var end = endDate ?? DateTime.UtcNow;

        var filterBuilder = Builders<Domain.Entities.Expense>.Filter;
        var filter = filterBuilder.Eq(e => e.UserId, userId) &
                    filterBuilder.Gte(e => e.Date, start) &
                    filterBuilder.Lte(e => e.Date, end);
        
        if (!string.IsNullOrEmpty(expenseBookId))
        {
            filter &= filterBuilder.Eq(e => e.ExpenseBookId, expenseBookId);
        }

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

    public async Task<List<MonthlyTrend>> GetMonthlyTrendsAsync(string userId, string? expenseBookId, int months)
    {
        var startDate = DateTime.UtcNow.AddMonths(-months);
        
        var filterBuilder = Builders<Domain.Entities.Expense>.Filter;
        var filter = filterBuilder.Eq(e => e.UserId, userId) &
                    filterBuilder.Gte(e => e.Date, startDate);
        
        if (!string.IsNullOrEmpty(expenseBookId))
        {
            filter &= filterBuilder.Eq(e => e.ExpenseBookId, expenseBookId);
        }

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

    public async Task<List<DailyTransactionGroup>> GetGroupedTransactionsAsync(string userId, string? expenseBookId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var filterBuilder = Builders<Domain.Entities.DailyExpenseSummary>.Filter;
        var filter = filterBuilder.Eq(d => d.UserId, userId) &
                     filterBuilder.Gte(d => d.Date, start.Date) &
                     filterBuilder.Lte(d => d.Date, end.Date);
        
        if (!string.IsNullOrEmpty(expenseBookId))
        {
            filter &= filterBuilder.Eq(d => d.ExpenseBookId, expenseBookId);
        }

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

    public async Task<UpcomingPaymentsPaginatedResponse> GetUpcomingPaymentsAsync(string userId, string? expenseBookId, int page = 1, int pageSize = 10)
    {
        // First, generate any missing upcoming payments and refresh statuses
        await GenerateUpcomingPaymentsAsync(userId);

        var expenseService = _expenseService as ExpenseService;
        if (expenseService != null)
        {
            await expenseService.RefreshUpcomingPaymentStatusesAsync(userId);
        }

        var filterBuilder = Builders<UpcomingPayment>.Filter;
        var filter = filterBuilder.Eq(u => u.UserId, userId);
        
        if (!string.IsNullOrEmpty(expenseBookId))
        {
            filter &= filterBuilder.Eq(u => u.ExpenseBookId, expenseBookId);
        }

        var total = (int)await _context.UpcomingPayments.CountDocumentsAsync(filter);

        // Sort: overdue/pending first, then due, then upcoming (by dueDate ascending)
        var upcomingPayments = await _context.UpcomingPayments
            .Find(filter)
            .SortBy(u => u.DueDate)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;

        var items = upcomingPayments.Select(u => new UpcomingPaymentDto
        {
            Id = u.Id,
            RecurringExpenseId = u.RecurringExpenseId,
            Amount = u.Amount,
            Category = u.Category,
            PaymentMethod = u.PaymentMethod,
            Description = u.Description,
            Frequency = u.Frequency,
            DueDate = u.DueDate,
            Status = u.Status,
            DueDateLabel = GetDueDateLabel(u.DueDate, today),
            CreatedAt = u.CreatedAt
        }).ToList();

        return new UpcomingPaymentsPaginatedResponse
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            HasMore = (page * pageSize) < total
        };
    }

    public async Task<UpcomingPaymentDto> MarkUpcomingPaymentAsPaidAsync(string userId, string upcomingPaymentId, DateTime paidDate, bool recordAsExpense)
    {
        var upcomingPayment = await _context.UpcomingPayments
            .Find(u => u.Id == upcomingPaymentId && u.UserId == userId)
            .FirstOrDefaultAsync();

        if (upcomingPayment == null)
            throw new KeyNotFoundException("Upcoming payment not found");

        // Get the related recurring expense
        var recurring = await _context.RecurringExpenses
            .Find(r => r.Id == upcomingPayment.RecurringExpenseId && r.UserId == userId)
            .FirstOrDefaultAsync();

        if (recurring == null)
            throw new KeyNotFoundException("Related recurring expense not found");

        // Optionally record as expense
        if (recordAsExpense)
        {
            var expense = new Expense
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                UserId = userId,
                Amount = upcomingPayment.Amount,
                Date = paidDate,
                Category = upcomingPayment.Category,
                PaymentMethod = upcomingPayment.PaymentMethod,
                Description = upcomingPayment.Description,
                Notes = $"Payment for recurring: {upcomingPayment.Description}",
                IsRecurring = true,
                RecurringId = upcomingPayment.RecurringExpenseId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Expenses.InsertOneAsync(expense);

            // Update daily expense summary
            var expenseService = _expenseService as ExpenseService;
            // Use reflection or add a public method - for now, we'll insert into DailyExpenseSummary directly
            await UpdateDailyExpenseSummaryForPaymentAsync(userId, paidDate, upcomingPayment.Category, upcomingPayment.Amount);
        }

        // Build the DTO to return before deleting
        var today = DateTime.UtcNow.Date;
        var dto = new UpcomingPaymentDto
        {
            Id = upcomingPayment.Id,
            RecurringExpenseId = upcomingPayment.RecurringExpenseId,
            Amount = upcomingPayment.Amount,
            Category = upcomingPayment.Category,
            PaymentMethod = upcomingPayment.PaymentMethod,
            Description = upcomingPayment.Description,
            Frequency = upcomingPayment.Frequency,
            DueDate = upcomingPayment.DueDate,
            Status = "paid",
            DueDateLabel = GetDueDateLabel(upcomingPayment.DueDate, today),
            CreatedAt = upcomingPayment.CreatedAt
        };

        // Delete the upcoming payment FIRST
        await _context.UpcomingPayments.DeleteOneAsync(u => u.Id == upcomingPaymentId);

        // Find the latest upcoming payment date for this recurring expense
        var latestUpcomingPayment = await _context.UpcomingPayments
            .Find(u => u.RecurringExpenseId == recurring.Id)
            .SortByDescending(u => u.DueDate)
            .FirstOrDefaultAsync();

        // Update recurring expense tracking
        recurring.LastProcessed = paidDate;
        
        // Set NextOccurrence to be after the latest existing upcoming payment, or from paidDate if none exist
        if (latestUpcomingPayment != null)
        {
            recurring.NextOccurrence = CalculateNextOccurrence(latestUpcomingPayment.DueDate, recurring.Frequency);
        }
        else
        {
            recurring.NextOccurrence = CalculateNextOccurrence(paidDate, recurring.Frequency);
        }
        
        recurring.UpdatedAt = DateTime.UtcNow;
        await _context.RecurringExpenses.ReplaceOneAsync(r => r.Id == recurring.Id, recurring);

        // Generate next upcoming payment entries to maintain 2 instances
        var expService = _expenseService as ExpenseService;
        if (expService != null)
        {
            await expService.GenerateUpcomingPaymentsForRecurringAsync(recurring);
        }

        return dto;
    }

    public async Task GenerateUpcomingPaymentsAsync(string userId)
    {
        var expenseService = _expenseService as ExpenseService;
        if (expenseService != null)
        {
            await expenseService.GenerateAllUpcomingPaymentsAsync(userId);
        }
    }

    private string GetDueDateLabel(DateTime dueDate, DateTime today)
    {
        var daysUntilDue = (dueDate.Date - today).Days;

        if (daysUntilDue < -7)
            return $"Pending ({Math.Abs(daysUntilDue)} days overdue)";
        if (daysUntilDue < -1)
            return $"Overdue by {Math.Abs(daysUntilDue)} days";
        if (daysUntilDue == -1)
            return "Overdue by 1 day";
        if (daysUntilDue == 0)
            return "Due today";
        if (daysUntilDue == 1)
            return "Due tomorrow";
        return $"Due in {daysUntilDue} days";
    }

    private DateTime CalculateNextOccurrence(DateTime fromDate, string frequency)
    {
        return frequency.ToLower() switch
        {
            "daily" => fromDate.AddDays(1),
            "weekly" => fromDate.AddDays(7),
            "monthly" => fromDate.AddMonths(1),
            "yearly" => fromDate.AddYears(1),
            _ => fromDate.AddMonths(1)
        };
    }

    private async Task UpdateDailyExpenseSummaryForPaymentAsync(string userId, DateTime expenseDate, string category, decimal amount)
    {
        var dateOnly = expenseDate.Date;

        var filter = Builders<DailyExpenseSummary>.Filter.Eq(d => d.UserId, userId) &
                     Builders<DailyExpenseSummary>.Filter.Eq(d => d.Date, dateOnly);

        var summary = await _context.DailyExpenseSummaries.Find(filter).FirstOrDefaultAsync();

        if (summary == null)
        {
            summary = new DailyExpenseSummary
            {
                UserId = userId,
                Date = dateOnly,
                CategorySpending = new List<CategorySpending>
                {
                    new CategorySpending
                    {
                        Category = category,
                        Amount = amount,
                        Count = 1
                    }
                },
                TotalSpent = amount
            };

            await _context.DailyExpenseSummaries.InsertOneAsync(summary);
        }
        else
        {
            var categorySpending = summary.CategorySpending.FirstOrDefault(c => c.Category == category);

            if (categorySpending != null)
            {
                categorySpending.Amount += amount;
                categorySpending.Count++;
            }
            else
            {
                summary.CategorySpending.Add(new CategorySpending
                {
                    Category = category,
                    Amount = amount,
                    Count = 1
                });
            }

            summary.TotalSpent += amount;
            summary.UpdatedAt = DateTime.UtcNow;
            summary.CategorySpending = summary.CategorySpending.OrderByDescending(c => c.Amount).ToList();

            await _context.DailyExpenseSummaries.ReplaceOneAsync(filter, summary);
        }
    }
}
