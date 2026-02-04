using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services;

public class ExpenseService : IExpenseService
{
    private readonly MongoDbContext _context;

    public ExpenseService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<List<ExpenseDto>> GetExpensesAsync(string userId, DateTime? startDate, DateTime? endDate, string? category)
    {
        var filterBuilder = Builders<Expense>.Filter;
        var filter = filterBuilder.Eq(e => e.UserId, userId);

        if (startDate.HasValue)
            filter &= filterBuilder.Gte(e => e.Date, startDate.Value);

        if (endDate.HasValue)
            filter &= filterBuilder.Lte(e => e.Date, endDate.Value);

        if (!string.IsNullOrEmpty(category))
            filter &= filterBuilder.Eq(e => e.Category, category);

        var expenses = await _context.Expenses
            .Find(filter)
            .SortByDescending(e => e.Date)
            .ToListAsync();

        return expenses.Select(MapToExpenseDto).ToList();
    }

    public async Task<ExpenseDto> GetExpenseByIdAsync(string userId, string expenseId)
    {
        var expense = await _context.Expenses
            .Find(e => e.Id == expenseId && e.UserId == userId)
            .FirstOrDefaultAsync();

        if (expense == null)
            throw new KeyNotFoundException("Expense not found");

        return MapToExpenseDto(expense);
    }

    public async Task<ExpenseDto> CreateExpenseAsync(string userId, CreateExpenseRequest request)
    {
        var expense = new Expense
        {
            UserId = userId,
            Amount = request.Amount,
            Date = request.Date,
            Category = request.Category,
            PaymentMethod = request.PaymentMethod,
            Description = request.Description,
            Notes = request.Notes,
            IsRecurring = request.IsRecurring
        };

        await _context.Expenses.InsertOneAsync(expense);

        // Update daily expense summary
        await UpdateDailyExpenseSummaryAsync(userId, expense.Date, expense.Category, expense.Amount, isAdd: true);

        // Handle recurring expense
        if (request.IsRecurring && request.RecurringConfig != null)
        {
            var recurring = new RecurringExpense
            {
                UserId = userId,
                Amount = request.Amount,
                Category = request.Category,
                PaymentMethod = request.PaymentMethod,
                Description = request.Description,
                Frequency = request.RecurringConfig.Frequency,
                StartDate = request.RecurringConfig.StartDate,
                EndDate = request.RecurringConfig.EndDate,
                NextOccurrence = CalculateNextOccurrence(request.RecurringConfig.StartDate, request.RecurringConfig.Frequency)
            };

            await _context.RecurringExpenses.InsertOneAsync(recurring);
            
            expense.RecurringId = recurring.Id;
            await _context.Expenses.ReplaceOneAsync(e => e.Id == expense.Id, expense);
        }

        return MapToExpenseDto(expense);
    }

    public async Task<ExpenseDto> UpdateExpenseAsync(string userId, string expenseId, UpdateExpenseRequest request)
    {
        var expense = await _context.Expenses
            .Find(e => e.Id == expenseId && e.UserId == userId)
            .FirstOrDefaultAsync();

        if (expense == null)
            throw new KeyNotFoundException("Expense not found");

        var oldAmount = expense.Amount;
        var oldDate = expense.Date;
        var oldCategory = expense.Category;

        if (request.Amount.HasValue)
            expense.Amount = request.Amount.Value;
        if (request.Date.HasValue)
            expense.Date = request.Date.Value;
        if (!string.IsNullOrEmpty(request.Category))
            expense.Category = request.Category;
        if (!string.IsNullOrEmpty(request.PaymentMethod))
            expense.PaymentMethod = request.PaymentMethod;
        if (request.Description != null)
            expense.Description = request.Description;
        if (request.Notes != null)
            expense.Notes = request.Notes;

        expense.UpdatedAt = DateTime.UtcNow;

        await _context.Expenses.ReplaceOneAsync(e => e.Id == expenseId, expense);

        // Update daily expense summaries
        // Remove old amount from old date/category
        await UpdateDailyExpenseSummaryAsync(userId, oldDate, oldCategory, oldAmount, isAdd: false);
        // Add new amount to new date/category
        await UpdateDailyExpenseSummaryAsync(userId, expense.Date, expense.Category, expense.Amount, isAdd: true);

        return MapToExpenseDto(expense);
    }

    public async Task<bool> DeleteExpenseAsync(string userId, string expenseId)
    {
        var expense = await _context.Expenses
            .Find(e => e.Id == expenseId && e.UserId == userId)
            .FirstOrDefaultAsync();

        if (expense == null)
            return false;

        // Update daily expense summary before deleting
        await UpdateDailyExpenseSummaryAsync(userId, expense.Date, expense.Category, expense.Amount, isAdd: false);

        var result = await _context.Expenses
            .DeleteOneAsync(e => e.Id == expenseId && e.UserId == userId);

        return result.DeletedCount > 0;
    }

    public async Task<string> UploadReceiptAsync(string userId, string expenseId, Stream fileStream, string fileName)
    {
        var expense = await _context.Expenses
            .Find(e => e.Id == expenseId && e.UserId == userId)
            .FirstOrDefaultAsync();

        if (expense == null)
            throw new KeyNotFoundException("Expense not found");

        // TODO: Implement actual file upload to cloud storage (Azure Blob, AWS S3, etc.)
        var receiptUrl = $"/uploads/receipts/{expenseId}_{fileName}";

        expense.ReceiptUrl = receiptUrl;
        expense.UpdatedAt = DateTime.UtcNow;

        await _context.Expenses.ReplaceOneAsync(e => e.Id == expenseId, expense);

        return receiptUrl;
    }

    private DateTime CalculateNextOccurrence(DateTime startDate, string frequency)
    {
        return frequency.ToLower() switch
        {
            "daily" => startDate.AddDays(1),
            "weekly" => startDate.AddDays(7),
            "monthly" => startDate.AddMonths(1),
            "yearly" => startDate.AddYears(1),
            _ => startDate.AddMonths(1)
        };
    }

    private async Task UpdateDailyExpenseSummaryAsync(string userId, DateTime expenseDate, string category, decimal amount, bool isAdd)
    {
        var dateOnly = expenseDate.Date; // Remove time component

        var filter = Builders<DailyExpenseSummary>.Filter.Eq(d => d.UserId, userId) &
                     Builders<DailyExpenseSummary>.Filter.Eq(d => d.Date, dateOnly);

        var summary = await _context.DailyExpenseSummaries.Find(filter).FirstOrDefaultAsync();

        if (summary == null && isAdd)
        {
            // Create new summary for this date
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
        else if (summary != null)
        {
            // Update existing summary
            var categorySpending = summary.CategorySpending.FirstOrDefault(c => c.Category == category);

            if (categorySpending != null)
            {
                if (isAdd)
                {
                    categorySpending.Amount += amount;
                    categorySpending.Count++;
                }
                else
                {
                    categorySpending.Amount -= amount;
                    categorySpending.Count--;

                    // Remove category if count is 0
                    if (categorySpending.Count <= 0)
                    {
                        summary.CategorySpending.Remove(categorySpending);
                    }
                }
            }
            else if (isAdd)
            {
                // Add new category spending
                summary.CategorySpending.Add(new CategorySpending
                {
                    Category = category,
                    Amount = amount,
                    Count = 1
                });
            }

            // Update total spent
            summary.TotalSpent = isAdd ? summary.TotalSpent + amount : summary.TotalSpent - amount;
            summary.UpdatedAt = DateTime.UtcNow;

            // Sort category spending by amount descending
            summary.CategorySpending = summary.CategorySpending.OrderByDescending(c => c.Amount).ToList();

            if (summary.CategorySpending.Count == 0 && summary.TotalSpent <= 0)
            {
                // Delete summary if no more expenses
                await _context.DailyExpenseSummaries.DeleteOneAsync(filter);
            }
            else
            {
                await _context.DailyExpenseSummaries.ReplaceOneAsync(filter, summary);
            }
        }
    }

    private ExpenseDto MapToExpenseDto(Expense expense)
    {
        return new ExpenseDto
        {
            Id = expense.Id,
            Amount = expense.Amount,
            Date = expense.Date,
            Category = expense.Category,
            PaymentMethod = expense.PaymentMethod,
            Description = expense.Description,
            Notes = expense.Notes,
            ReceiptUrl = expense.ReceiptUrl,
            IsRecurring = expense.IsRecurring,
            CreatedAt = expense.CreatedAt
        };
    }
}
