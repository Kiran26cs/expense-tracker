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

    public async Task<List<ExpenseDto>> GetExpensesAsync(string userId, string? expenseBookId, DateTime? startDate, DateTime? endDate, string? category)
    {
        var filterBuilder = Builders<Expense>.Filter;
        var filter = filterBuilder.Eq(e => e.UserId, userId);

        if (!string.IsNullOrEmpty(expenseBookId))
            filter &= filterBuilder.Eq(e => e.ExpenseBookId, expenseBookId);

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
            ExpenseBookId = request.ExpenseBookId,
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
        await UpdateDailyExpenseSummaryAsync(userId, request.ExpenseBookId, expense.Date, expense.Category, expense.Amount, isAdd: true);

        // Handle recurring expense
        if (request.IsRecurring && request.RecurringConfig != null)
        {
            var recurring = new RecurringExpense
            {
                UserId = userId,
                ExpenseBookId = request.ExpenseBookId,
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

            // Generate upcoming payments for the next 30 days
            await GenerateUpcomingPaymentsForRecurringAsync(recurring);
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
        await UpdateDailyExpenseSummaryAsync(userId, expense.ExpenseBookId, oldDate, oldCategory, oldAmount, isAdd: false);
        // Add new amount to new date/category
        await UpdateDailyExpenseSummaryAsync(userId, expense.ExpenseBookId, expense.Date, expense.Category, expense.Amount, isAdd: true);

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
        await UpdateDailyExpenseSummaryAsync(userId, expense.ExpenseBookId, expense.Date, expense.Category, expense.Amount, isAdd: false);

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

    private async Task UpdateDailyExpenseSummaryAsync(string userId, string? expenseBookId, DateTime expenseDate, string category, decimal amount, bool isAdd)
    {
        var dateOnly = expenseDate.Date; // Remove time component

        var filterBuilder = Builders<DailyExpenseSummary>.Filter;
        var filter = filterBuilder.Eq(d => d.UserId, userId) &
                     filterBuilder.Eq(d => d.Date, dateOnly);
        
        if (!string.IsNullOrEmpty(expenseBookId))
        {
            filter &= filterBuilder.Eq(d => d.ExpenseBookId, expenseBookId);
        }

        var summary = await _context.DailyExpenseSummaries.Find(filter).FirstOrDefaultAsync();

        if (summary == null && isAdd)
        {
            // Create new summary for this date
            summary = new DailyExpenseSummary
            {
                UserId = userId,
                ExpenseBookId = expenseBookId,
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
            ExpenseBookId = expense.ExpenseBookId,
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

    private RecurringExpenseDto MapRecurringExpenseToDto(RecurringExpense recurring)
    {
        return new RecurringExpenseDto
        {
            Id = recurring.Id,
            ExpenseBookId = recurring.ExpenseBookId,
            Amount = recurring.Amount,
            Category = recurring.Category,
            PaymentMethod = recurring.PaymentMethod,
            Description = recurring.Description,
            Frequency = recurring.Frequency,
            StartDate = recurring.StartDate,
            EndDate = recurring.EndDate,
            NextOccurrence = recurring.NextOccurrence,
            LastProcessed = recurring.LastProcessed,
            IsActive = recurring.IsActive,
            CreatedAt = recurring.CreatedAt,
            UpdatedAt = recurring.UpdatedAt
        };
    }

    public async Task<List<RecurringExpenseDto>> GetRecurringExpensesAsync(string userId, string? expenseBookId, DateTime? startDate, DateTime? endDate)
    {
        var filterBuilder = Builders<RecurringExpense>.Filter;
        var filter = filterBuilder.Eq(r => r.UserId, userId) & filterBuilder.Eq(r => r.IsActive, true);

        if (!string.IsNullOrEmpty(expenseBookId))
            filter &= filterBuilder.Eq(r => r.ExpenseBookId, expenseBookId);

        // Filter by NextOccurrence falling within the date range
        if (startDate.HasValue)
            filter &= filterBuilder.Gte(r => r.NextOccurrence, startDate.Value);

        if (endDate.HasValue)
            filter &= filterBuilder.Lte(r => r.NextOccurrence, endDate.Value);

        var recurringExpenses = await _context.RecurringExpenses
            .Find(filter)
            .SortBy(r => r.NextOccurrence)
            .ToListAsync();

        return recurringExpenses.Select(MapRecurringExpenseToDto).ToList();
    }

    public async Task<ExpenseDto> MarkRecurringExpenseAsPaidAsync(string userId, string recurringExpenseId, DateTime paidDate)
    {
        var recurring = await _context.RecurringExpenses
            .Find(r => r.Id == recurringExpenseId && r.UserId == userId)
            .FirstOrDefaultAsync();

        if (recurring == null)
            throw new KeyNotFoundException("Recurring expense not found");

        // Create a new expense for this payment
        var expense = new Expense
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            UserId = userId,
            ExpenseBookId = recurring.ExpenseBookId,
            Amount = recurring.Amount,
            Date = paidDate,
            Category = recurring.Category,
            PaymentMethod = recurring.PaymentMethod,
            Description = recurring.Description,
            Notes = $"Payment for recurring: {recurring.Description}",
            IsRecurring = true,
            RecurringId = recurringExpenseId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Expenses.InsertOneAsync(expense);

        // Update the recurring expense's last processed date and next occurrence
        recurring.LastProcessed = paidDate;
        recurring.NextOccurrence = CalculateNextOccurrence(paidDate, recurring.Frequency);
        recurring.UpdatedAt = DateTime.UtcNow;

        await _context.RecurringExpenses.ReplaceOneAsync(r => r.Id == recurringExpenseId, recurring);

        // Update daily expense summary
        await UpdateDailyExpenseSummaryAsync(userId, recurring.ExpenseBookId, paidDate, recurring.Category, recurring.Amount, isAdd: true);

        return MapToExpenseDto(expense);
    }

    /// <summary>
    /// Generate 2 upcoming payment entries for a recurring expense.
    /// </summary>
    public async Task GenerateUpcomingPaymentsForRecurringAsync(RecurringExpense recurring)
    {
        // Check how many upcoming payments already exist for this recurring expense
        var existingCount = await _context.UpcomingPayments
            .Find(u => u.RecurringExpenseId == recurring.Id)
            .CountDocumentsAsync();

        // We want to maintain exactly 2 upcoming payment instances
        var instancesNeeded = 2 - (int)existingCount;

        if (instancesNeeded <= 0)
            return; // Already have 2 or more instances

        var currentDate = recurring.NextOccurrence;

        for (int i = 0; i < instancesNeeded; i++)
        {
            // Check if end date is set and we've passed it
            if (recurring.EndDate.HasValue && currentDate > recurring.EndDate.Value)
                break;

            // Check if this upcoming payment already exists
            var exists = await _context.UpcomingPayments
                .Find(u => u.RecurringExpenseId == recurring.Id && u.DueDate == currentDate)
                .AnyAsync();

            if (!exists)
            {
                var status = GetUpcomingPaymentStatus(currentDate);

                var upcomingPayment = new UpcomingPayment
                {
                    UserId = recurring.UserId,
                    ExpenseBookId = recurring.ExpenseBookId,
                    RecurringExpenseId = recurring.Id,
                    Amount = recurring.Amount,
                    Category = recurring.Category,
                    PaymentMethod = recurring.PaymentMethod,
                    Description = recurring.Description,
                    Frequency = recurring.Frequency,
                    DueDate = currentDate,
                    Status = status
                };

                await _context.UpcomingPayments.InsertOneAsync(upcomingPayment);
            }

            currentDate = CalculateNextOccurrence(currentDate, recurring.Frequency);
        }
    }

    /// <summary>
    /// Generate upcoming payments for ALL active recurring expenses for a user.
    /// </summary>
    public async Task GenerateAllUpcomingPaymentsAsync(string userId)
    {
        var activeRecurring = await _context.RecurringExpenses
            .Find(r => r.UserId == userId && r.IsActive == true)
            .ToListAsync();

        foreach (var recurring in activeRecurring)
        {
            await GenerateUpcomingPaymentsForRecurringAsync(recurring);
        }
    }

    /// <summary>
    /// Refresh statuses of all upcoming payments for a user based on current date.
    /// </summary>
    public async Task RefreshUpcomingPaymentStatusesAsync(string userId)
    {
        var upcomingPayments = await _context.UpcomingPayments
            .Find(u => u.UserId == userId)
            .ToListAsync();

        foreach (var payment in upcomingPayments)
        {
            var newStatus = GetUpcomingPaymentStatus(payment.DueDate);
            if (newStatus != payment.Status)
            {
                payment.Status = newStatus;
                payment.UpdatedAt = DateTime.UtcNow;
                await _context.UpcomingPayments.ReplaceOneAsync(u => u.Id == payment.Id, payment);
            }
        }
    }

    private string GetUpcomingPaymentStatus(DateTime dueDate)
    {
        var today = DateTime.UtcNow.Date;
        var daysUntilDue = (dueDate.Date - today).Days;

        if (daysUntilDue < -7)
            return "pending"; // Unpaid for more than a week
        if (daysUntilDue < 0)
            return "overdue"; // Past due date
        if (daysUntilDue == 0)
            return "due"; // Due today
        return "upcoming"; // Due in the future
    }

    /// <summary>
    /// Delete all upcoming payments for a recurring expense.
    /// </summary>
    public async Task DeleteUpcomingPaymentsForRecurringAsync(string recurringExpenseId)
    {
        await _context.UpcomingPayments.DeleteManyAsync(u => u.RecurringExpenseId == recurringExpenseId);
    }

    /// <summary>
    /// Update a recurring expense and regenerate upcoming payments.
    /// </summary>
    public async Task<RecurringExpenseDto> UpdateRecurringExpenseAsync(
        string userId, 
        string recurringExpenseId, 
        UpdateRecurringExpenseRequest request)
    {
        var recurring = await _context.RecurringExpenses
            .Find(r => r.Id == recurringExpenseId && r.UserId == userId)
            .FirstOrDefaultAsync();

        if (recurring == null)
            throw new KeyNotFoundException("Recurring expense not found");

        // Track if critical fields changed (amount, frequency, startDate)
        bool needRegenerate = false;

        if (request.Amount.HasValue && request.Amount.Value != recurring.Amount)
        {
            recurring.Amount = request.Amount.Value;
            needRegenerate = true;
        }

        if (!string.IsNullOrEmpty(request.Frequency) && request.Frequency != recurring.Frequency)
        {
            recurring.Frequency = request.Frequency;
            needRegenerate = true;
        }

        if (request.StartDate.HasValue && request.StartDate.Value != recurring.StartDate)
        {
            recurring.StartDate = request.StartDate.Value;
            recurring.NextOccurrence = request.StartDate.Value;
            needRegenerate = true;
        }

        if (!string.IsNullOrEmpty(request.Category))
            recurring.Category = request.Category;

        if (!string.IsNullOrEmpty(request.PaymentMethod))
            recurring.PaymentMethod = request.PaymentMethod;

        if (request.Description != null)
            recurring.Description = request.Description;

        if (request.EndDate.HasValue)
            recurring.EndDate = request.EndDate;

        recurring.UpdatedAt = DateTime.UtcNow;

        // Update the recurring expense
        await _context.RecurringExpenses.ReplaceOneAsync(r => r.Id == recurringExpenseId, recurring);

        // If critical fields changed, delete all unpaid upcoming payments and create 2 new ones
        if (needRegenerate)
        {
            await DeleteUpcomingPaymentsForRecurringAsync(recurringExpenseId);
            await GenerateUpcomingPaymentsForRecurringAsync(recurring);
        }

        return MapRecurringExpenseToDto(recurring);
    }

    /// <summary>
    /// Delete a recurring expense (soft delete by marking inactive) and remove upcoming payments.
    /// </summary>
    public async Task<bool> DeleteRecurringExpenseAsync(string userId, string recurringExpenseId)
    {
        var recurring = await _context.RecurringExpenses
            .Find(r => r.Id == recurringExpenseId && r.UserId == userId)
            .FirstOrDefaultAsync();

        if (recurring == null)
            return false;

        // Mark as inactive instead of hard delete
        recurring.IsActive = false;
        recurring.UpdatedAt = DateTime.UtcNow;
        await _context.RecurringExpenses.ReplaceOneAsync(r => r.Id == recurringExpenseId, recurring);

        // Delete all upcoming payments for this recurring expense
        await DeleteUpcomingPaymentsForRecurringAsync(recurringExpenseId);

        return true;
    }
}