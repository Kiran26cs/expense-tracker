using MongoDB.Driver;
using ExpensesBackend.API.Domain.Entities;

namespace ExpensesBackend.API;

/// <summary>
/// One-time migration: CreateExpenseAsync used to store the Add-Transaction modal's category
/// selection unresolved — a raw category id — instead of the category name, unlike every other
/// entry path (import, batch, edit). This backfills those existing expenses so Category always
/// holds the name, matching what filtering and budget matching expect.
/// Run with: dotnet run migrate-categories
/// </summary>
public class MigrateExpenseCategoryIds
{
    public static async Task RunMigration(string connectionString, string databaseName)
    {
        Console.WriteLine("Starting Expense Category Id -> Name Migration...");

        var client   = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);

        var categoriesCollection = database.GetCollection<Category>("categories");
        var expensesCollection   = database.GetCollection<Expense>("expenses");

        var allCategories = await categoriesCollection.Find(_ => true).ToListAsync();
        if (!allCategories.Any())
        {
            Console.WriteLine("No categories found. Nothing to migrate.");
            return;
        }

        var idToName = allCategories.ToDictionary(c => c.Id, c => c.Name);
        Console.WriteLine($"Loaded {idToName.Count} categories.");

        // Any expense whose Category value happens to equal a known category id (rather than a
        // name) is one that was never resolved at creation time — that's the bug this fixes.
        var filter   = Builders<Expense>.Filter.In(e => e.Category, idToName.Keys);
        var affected = await expensesCollection.Find(filter).ToListAsync();

        if (!affected.Any())
        {
            Console.WriteLine("No affected expenses found. Nothing to migrate.");
            return;
        }

        Console.WriteLine($"Found {affected.Count} expense(s) with a raw category id stored instead of a name.");

        int updated = 0;
        foreach (var exp in affected)
        {
            if (!idToName.TryGetValue(exp.Category, out var name)) continue;

            var update = Builders<Expense>.Update.Set(e => e.Category, name);
            await expensesCollection.UpdateOneAsync(e => e.Id == exp.Id, update);
            updated++;

            if (updated % 50 == 0)
                Console.WriteLine($"  ...{updated} updated so far");
        }

        Console.WriteLine($"\nMigration completed! {updated} expense(s) corrected from id to name.");
    }
}
