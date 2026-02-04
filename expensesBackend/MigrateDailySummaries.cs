using MongoDB.Driver;
using MongoDB.Bson;
using ExpensesBackend.API.Domain.Entities;

namespace ExpensesBackend.API;

/// <summary>
/// One-time migration script to populate DailyExpenseSummaries from existing expenses
/// Run this from Program.cs or as a standalone console command
/// </summary>
public class MigrateDailySummaries
{
    public static async Task RunMigration(string connectionString, string databaseName)
    {
        Console.WriteLine("Starting Daily Summaries Migration...");
        
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        
        var expensesCollection = database.GetCollection<Expense>("expenses");
        var summariesCollection = database.GetCollection<DailyExpenseSummary>("dailyExpenseSummaries");
        
        // Get all expenses
        var allExpenses = await expensesCollection.Find(_ => true).ToListAsync();
        
        if (!allExpenses.Any())
        {
            Console.WriteLine("No expenses found. Nothing to migrate.");
            return;
        }
        
        Console.WriteLine($"Found {allExpenses.Count} expenses to migrate");
        
        // Group by user
        var userGroups = allExpenses.GroupBy(e => e.UserId);
        
        int summariesCreated = 0;
        int summariesUpdated = 0;
        
        foreach (var userGroup in userGroups)
        {
            var userId = userGroup.Key;
            Console.WriteLine($"\nProcessing user: {userId}");
            
            // Group expenses by date
            var dateGroups = userGroup.GroupBy(e => e.Date.Date);
            
            foreach (var dateGroup in dateGroups)
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
                
                // Check if summary already exists
                var existingSummary = await summariesCollection
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
                    
                    await summariesCollection.InsertOneAsync(summary);
                    summariesCreated++;
                    Console.WriteLine($"  Created summary for {date:yyyy-MM-dd} - Total: ₹{totalSpent}");
                }
                else
                {
                    // Update existing summary
                    existingSummary.CategorySpending = categoryGroups;
                    existingSummary.TotalSpent = totalSpent;
                    existingSummary.UpdatedAt = DateTime.UtcNow;
                    
                    await summariesCollection.ReplaceOneAsync(
                        s => s.Id == existingSummary.Id,
                        existingSummary
                    );
                    summariesUpdated++;
                    Console.WriteLine($"  Updated summary for {date:yyyy-MM-dd} - Total: ₹{totalSpent}");
                }
            }
        }
        
        Console.WriteLine($"\n✅ Migration completed!");
        Console.WriteLine($"   - Summaries created: {summariesCreated}");
        Console.WriteLine($"   - Summaries updated: {summariesUpdated}");
        Console.WriteLine($"   - Total: {summariesCreated + summariesUpdated}");
    }
}
