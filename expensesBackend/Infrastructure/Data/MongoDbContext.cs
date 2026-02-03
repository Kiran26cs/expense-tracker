using ExpensesBackend.API.Domain.Entities;
using MongoDB.Driver;

namespace ExpensesBackend.API.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDB") 
            ?? "mongodb://localhost:27017";
        var databaseName = configuration["MongoDB:DatabaseName"] ?? "ExpenseTrackerDB";

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);

        // Create indexes
        CreateIndexes();
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<Expense> Expenses => _database.GetCollection<Expense>("expenses");
    public IMongoCollection<Category> Categories => _database.GetCollection<Category>("categories");
    public IMongoCollection<Budget> Budgets => _database.GetCollection<Budget>("budgets");
    public IMongoCollection<RecurringExpense> RecurringExpenses => 
        _database.GetCollection<RecurringExpense>("recurringExpenses");
    public IMongoCollection<OtpRecord> OtpRecords => 
        _database.GetCollection<OtpRecord>("otpRecords");

    private void CreateIndexes()
    {
        // User indexes
        var userIndexKeys = Builders<User>.IndexKeys
            .Ascending(u => u.Email)
            .Ascending(u => u.Phone);
        Users.Indexes.CreateOne(new CreateIndexModel<User>(userIndexKeys));

        // Expense indexes
        var expenseIndexKeys = Builders<Expense>.IndexKeys
            .Ascending(e => e.UserId)
            .Descending(e => e.Date);
        Expenses.Indexes.CreateOne(new CreateIndexModel<Expense>(expenseIndexKeys));

        // Budget indexes
        var budgetIndexKeys = Builders<Budget>.IndexKeys
            .Ascending(b => b.UserId)
            .Ascending(b => b.Period);
        Budgets.Indexes.CreateOne(new CreateIndexModel<Budget>(budgetIndexKeys));

        // RecurringExpense indexes
        var recurringIndexKeys = Builders<RecurringExpense>.IndexKeys
            .Ascending(r => r.UserId)
            .Ascending(r => r.NextOccurrence);
        RecurringExpenses.Indexes.CreateOne(new CreateIndexModel<RecurringExpense>(recurringIndexKeys));

        // OTP indexes - auto-delete expired OTPs after 5 minutes
        var otpIndexKeys = Builders<OtpRecord>.IndexKeys
            .Ascending(o => o.Email)
            .Ascending(o => o.Phone);
        var otpIndexOptions = new CreateIndexModel<OtpRecord>(otpIndexKeys);
        OtpRecords.Indexes.CreateOne(otpIndexOptions);

        // TTL index to auto-delete expired OTPs
        var ttlIndexKeys = Builders<OtpRecord>.IndexKeys.Ascending(o => o.ExpiresAt);
        var ttlIndexOptions = new CreateIndexModel<OtpRecord>(
            ttlIndexKeys,
            new CreateIndexOptions { ExpireAfter = TimeSpan.Zero }
        );
        OtpRecords.Indexes.CreateOne(ttlIndexOptions);
    }
}
