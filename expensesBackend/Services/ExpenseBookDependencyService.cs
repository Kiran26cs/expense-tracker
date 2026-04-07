using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services;

public class ExpenseBookDependencyService : IExpenseBookDependencyService
{
    private readonly MongoDbContext _context;

    public ExpenseBookDependencyService(MongoDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Copies all system-default categories (expenseBookId == null, isDefault == true)
    /// into the newly created expense book as book-scoped, editable copies.
    /// </summary>
    public async Task CopyDefaultCategoriesToBookAsync(string expenseBookId)
    {
        var systemFilter = Builders<Category>.Filter.Eq(c => c.IsDefault, true);

        var systemCategories = await _context.Categories.Find(systemFilter).ToListAsync();

        if (systemCategories.Count == 0)
            return;

        var bookCategories = systemCategories.Select(sc => new Category
        {
            ExpenseBookId = expenseBookId,
            Name = sc.Name,
            Type = sc.Type,
            Icon = sc.Icon,
            Color = sc.Color,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        await _context.Categories.InsertManyAsync(bookCategories);
    }
}
