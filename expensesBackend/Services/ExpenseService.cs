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

        return MapToExpenseDto(expense);
    }

    public async Task<bool> DeleteExpenseAsync(string userId, string expenseId)
    {
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
