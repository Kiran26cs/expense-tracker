using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services;

public class PayeeCategoryMemoryService : IPayeeCategoryMemoryService
{
    private readonly MongoDbContext _context;

    public PayeeCategoryMemoryService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<string, (string CategoryId, string CategoryName)>> GetMemoryAsync(
        string expenseBookId, IEnumerable<string> payeeKeys)
    {
        var keys = payeeKeys.Where(k => !string.IsNullOrEmpty(k)).Distinct().ToList();
        if (keys.Count == 0) return new();

        var filter = Builders<PayeeCategoryMemory>.Filter.And(
            Builders<PayeeCategoryMemory>.Filter.Eq(p => p.ExpenseBookId, expenseBookId),
            Builders<PayeeCategoryMemory>.Filter.In(p => p.PayeeKey, keys));

        var docs = await _context.PayeeCategoryMemories.Find(filter).ToListAsync();
        return docs.ToDictionary(d => d.PayeeKey, d => (d.CategoryId, d.CategoryName));
    }

    public async Task RememberAsync(string expenseBookId, string payeeKey, string categoryId, string categoryName)
    {
        if (string.IsNullOrEmpty(payeeKey) || string.IsNullOrEmpty(categoryId)) return;

        var filter = Builders<PayeeCategoryMemory>.Filter.And(
            Builders<PayeeCategoryMemory>.Filter.Eq(p => p.ExpenseBookId, expenseBookId),
            Builders<PayeeCategoryMemory>.Filter.Eq(p => p.PayeeKey, payeeKey));

        var update = Builders<PayeeCategoryMemory>.Update
            .Set(p => p.CategoryId, categoryId)
            .Set(p => p.CategoryName, categoryName)
            .Set(p => p.UpdatedAt, DateTime.UtcNow)
            .Inc(p => p.HitCount, 1)
            .SetOnInsert(p => p.ExpenseBookId, expenseBookId)
            .SetOnInsert(p => p.PayeeKey, payeeKey);

        await _context.PayeeCategoryMemories.UpdateOneAsync(
            filter, update, new UpdateOptions { IsUpsert = true });
    }
}
