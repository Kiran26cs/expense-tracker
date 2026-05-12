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
    /// Cascade-deletes every document in every collection that belongs to this expense book.
    /// Run this before deleting the ExpenseBook document itself.
    /// </summary>
    public async Task DeleteAllDependenciesAsync(string expenseBookId)
    {
        var f = expenseBookId;
        await Task.WhenAll(
            _context.Expenses            .DeleteManyAsync(e => e.ExpenseBookId == f),
            _context.Categories          .DeleteManyAsync(c => c.ExpenseBookId == f),
            _context.Budgets             .DeleteManyAsync(b => b.ExpenseBookId == f),
            _context.RecurringExpenses   .DeleteManyAsync(r => r.ExpenseBookId == f),
            _context.UpcomingPayments    .DeleteManyAsync(u => u.ExpenseBookId == f),
            _context.Lendings            .DeleteManyAsync(l => l.ExpenseBookId == f),
            _context.LendingRepayments   .DeleteManyAsync(r => r.ExpenseBookId == f),
            _context.ImportSessions      .DeleteManyAsync(s => s.ExpenseBookId == f),
            _context.DailyExpenseSummaries.DeleteManyAsync(d => d.ExpenseBookId == f),
            _context.ExpenseBookMembers  .DeleteManyAsync(m => m.ExpenseBookId == f)
        );
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
