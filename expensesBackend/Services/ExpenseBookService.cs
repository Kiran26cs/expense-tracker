using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services;

public class ExpenseBookService : IExpenseBookService
{
    private readonly MongoDbContext _context;
    private readonly IExpenseBookDependencyService _dependencyService;

    public ExpenseBookService(MongoDbContext context, IExpenseBookDependencyService dependencyService)
    {
        _context = context;
        _dependencyService = dependencyService;
    }

    public async Task<List<ExpenseBookResponse>> GetExpenseBooksAsync(string userId)
    {
        // Books the user owns
        var ownedBooks = await _context.ExpenseBooks
            .Find(eb => eb.UserId == userId)
            .ToListAsync();

        // Member records for accepted memberships (not owner)
        var memberRecords = await _context.ExpenseBookMembers
            .Find(m => m.UserId == userId && m.InviteStatus == "accepted" && !m.IsDeleted)
            .ToListAsync();

        var memberBookIds = memberRecords.Select(m => m.ExpenseBookId).ToList();
        var roleByBookId  = memberRecords.ToDictionary(m => m.ExpenseBookId, m => m.Role);

        var memberBooks = memberBookIds.Count > 0
            ? await _context.ExpenseBooks
                .Find(eb => memberBookIds.Contains(eb.Id) && eb.UserId != userId)
                .ToListAsync()
            : [];

        var allBooks = ownedBooks
            .Select(b => MapToExpenseBookResponse(b, memberRole: null))
            .Concat(memberBooks.Select(b =>
                MapToExpenseBookResponse(b, roleByBookId.TryGetValue(b.Id, out var r) ? r : "member")))
            .OrderByDescending(eb => eb.IsDefault)
            .ThenByDescending(eb => eb.CreatedAt)
            .ToList();

        return allBooks;
    }

    public async Task<ExpenseBookResponse> GetExpenseBookByIdAsync(string userId, string expenseBookId)
    {
        var expenseBook = await _context.ExpenseBooks
            .Find(eb => eb.Id == expenseBookId)
            .FirstOrDefaultAsync();

        if (expenseBook == null)
            throw new KeyNotFoundException("Expense book not found");

        // Allow owner or accepted member
        string? memberRole = null;
        if (expenseBook.UserId != userId)
        {
            var member = await _context.ExpenseBookMembers
                .Find(m => m.ExpenseBookId == expenseBookId
                        && m.UserId == userId
                        && m.InviteStatus == "accepted"
                        && !m.IsDeleted)
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("Expense book not found");

            memberRole = member.Role;
        }

        return MapToExpenseBookResponse(expenseBook, memberRole);
    }

    public async Task<ExpenseBookResponse> CreateExpenseBookAsync(string userId, CreateExpenseBookRequest request)
    {
        // If this is marked as default, unset any existing default
        if (request.IsDefault)
        {
            var update = Builders<ExpenseBook>.Update.Set(eb => eb.IsDefault, false);
            await _context.ExpenseBooks.UpdateManyAsync(eb => eb.UserId == userId && eb.IsDefault, update);
        }

        // If this is the first expense book for the user, make it default
        var existingCount = await _context.ExpenseBooks.CountDocumentsAsync(eb => eb.UserId == userId);
        var isDefault = existingCount == 0 || request.IsDefault;

        var expenseBook = new ExpenseBook
        {
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Currency = request.Currency,
            Color = request.Color,
            Icon = request.Icon,
            IsDefault = isDefault
        };

        await _context.ExpenseBooks.InsertOneAsync(expenseBook);
        await _dependencyService.CopyDefaultCategoriesToBookAsync(expenseBook.Id);

        return MapToExpenseBookResponse(expenseBook);
    }

    public async Task<ExpenseBookResponse> UpdateExpenseBookAsync(string userId, string expenseBookId, UpdateExpenseBookRequest request)
    {
        // Find by book ID only — caller is trusted to have been ACL-checked at controller level
        var expenseBook = await _context.ExpenseBooks
            .Find(eb => eb.Id == expenseBookId)
            .FirstOrDefaultAsync();

        if (expenseBook == null)
            throw new KeyNotFoundException("Expense book not found");

        // If setting as default, unset any existing default for the book's owner
        if (request.IsDefault && !expenseBook.IsDefault)
        {
            var update = Builders<ExpenseBook>.Update.Set(eb => eb.IsDefault, false);
            await _context.ExpenseBooks.UpdateManyAsync(eb => eb.UserId == expenseBook.UserId && eb.IsDefault, update);
        }

        expenseBook.Name = request.Name;
        expenseBook.Description = request.Description;
        expenseBook.Category = request.Category;
        expenseBook.Currency = request.Currency;
        expenseBook.Color = request.Color;
        expenseBook.Icon = request.Icon;
        expenseBook.IsDefault = request.IsDefault;
        expenseBook.UpdatedAt = DateTime.UtcNow;

        await _context.ExpenseBooks.ReplaceOneAsync(
            eb => eb.Id == expenseBookId,
            expenseBook
        );

        return MapToExpenseBookResponse(expenseBook);
    }

    public async Task DeleteExpenseBookAsync(string userId, string expenseBookId)
    {
        var expenseBook = await _context.ExpenseBooks
            .Find(eb => eb.Id == expenseBookId && eb.UserId == userId)
            .FirstOrDefaultAsync();

        if (expenseBook == null)
            throw new KeyNotFoundException("Expense book not found");

        // Check if there are any expenses linked to this book
        var hasExpenses = await _context.Expenses.Find(e => e.ExpenseBookId == expenseBookId).AnyAsync();
        if (hasExpenses)
            throw new InvalidOperationException("Cannot delete expense book with existing expenses. Please delete all expenses first.");

        await _context.ExpenseBooks.DeleteOneAsync(eb => eb.Id == expenseBookId && eb.UserId == userId);

        // If deleted book was default, set another book as default
        if (expenseBook.IsDefault)
        {
            var firstBook = await _context.ExpenseBooks
                .Find(eb => eb.UserId == userId)
                .FirstOrDefaultAsync();

            if (firstBook != null)
            {
                var update = Builders<ExpenseBook>.Update.Set(eb => eb.IsDefault, true);
                await _context.ExpenseBooks.UpdateOneAsync(eb => eb.Id == firstBook.Id, update);
            }
        }
    }

    public async Task<List<string>> GetExpenseBookCategoriesAsync(string userId)
    {
        var categories = await _context.ExpenseBooks
            .Distinct(eb => eb.Category, eb => eb.UserId == userId)
            .ToListAsync();

        // Always include default categories
        var defaultCategories = new List<string> { "Personal", "Work" };
        
        var allCategories = defaultCategories
            .Concat(categories.Where(c => !defaultCategories.Contains(c)))
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        return allCategories;
    }

    public async Task<ExpenseBookResponse?> GetDefaultExpenseBookAsync(string userId)
    {
        var expenseBook = await _context.ExpenseBooks
            .Find(eb => eb.UserId == userId && eb.IsDefault)
            .FirstOrDefaultAsync();

        return expenseBook != null ? MapToExpenseBookResponse(expenseBook) : null;
    }

    public async Task UpdateExpenseBookStatsAsync(string expenseBookId)
    {
        // Calculate total expenses and count for this book
        var expenses = await _context.Expenses
            .Find(e => e.ExpenseBookId == expenseBookId)
            .ToListAsync();

        var totalExpenses = expenses.Sum(e => e.Amount);
        var expenseCount = expenses.Count;

        var update = Builders<ExpenseBook>.Update
            .Set(eb => eb.TotalExpenses, totalExpenses)
            .Set(eb => eb.ExpenseCount, expenseCount)
            .Set(eb => eb.UpdatedAt, DateTime.UtcNow);

        await _context.ExpenseBooks.UpdateOneAsync(eb => eb.Id == expenseBookId, update);
    }

    private static ExpenseBookResponse MapToExpenseBookResponse(ExpenseBook expenseBook, string? memberRole = null)
    {
        return new ExpenseBookResponse
        {
            Id = expenseBook.Id,
            UserId = expenseBook.UserId,
            Name = expenseBook.Name,
            Description = expenseBook.Description,
            Category = expenseBook.Category,
            Currency = expenseBook.Currency,
            Color = expenseBook.Color,
            Icon = expenseBook.Icon,
            IsDefault = expenseBook.IsDefault,
            TotalExpenses = expenseBook.TotalExpenses,
            ExpenseCount = expenseBook.ExpenseCount,
            CreatedAt = expenseBook.CreatedAt,
            UpdatedAt = expenseBook.UpdatedAt,
            MemberRole = memberRole
        };
    }
}
