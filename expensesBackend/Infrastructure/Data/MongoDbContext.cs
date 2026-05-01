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
    public IMongoCollection<ExpenseBook> ExpenseBooks => _database.GetCollection<ExpenseBook>("expenseBooks");
    public IMongoCollection<Expense> Expenses => _database.GetCollection<Expense>("expenses");
    public IMongoCollection<Category> Categories => _database.GetCollection<Category>("categories");
    public IMongoCollection<Budget> Budgets => _database.GetCollection<Budget>("budgets");
    public IMongoCollection<RecurringExpense> RecurringExpenses => 
        _database.GetCollection<RecurringExpense>("recurringExpenses");
    public IMongoCollection<OtpRecord> OtpRecords => 
        _database.GetCollection<OtpRecord>("otpRecords");
    public IMongoCollection<DailyExpenseSummary> DailyExpenseSummaries => 
        _database.GetCollection<DailyExpenseSummary>("dailyExpenseSummaries");
    public IMongoCollection<UpcomingPayment> UpcomingPayments => 
        _database.GetCollection<UpcomingPayment>("upcomingPayments");
    public IMongoCollection<ExpenseBookMember> ExpenseBookMembers =>
        _database.GetCollection<ExpenseBookMember>("expenseBookMembers");

    private void CreateIndexes()
    {
        // User indexes
        var userIndexKeys = Builders<User>.IndexKeys
            .Ascending(u => u.Email)
            .Ascending(u => u.Phone);
        Users.Indexes.CreateOne(new CreateIndexModel<User>(userIndexKeys));

        // ExpenseBook indexes
        var expenseBookIndexKeys = Builders<ExpenseBook>.IndexKeys
            .Ascending(eb => eb.UserId)
            .Descending(eb => eb.CreatedAt);
        ExpenseBooks.Indexes.CreateOne(new CreateIndexModel<ExpenseBook>(expenseBookIndexKeys));

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

        // UpcomingPayment indexes
        var upcomingPaymentIndexKeys = Builders<UpcomingPayment>.IndexKeys
            .Ascending(u => u.UserId)
            .Ascending(u => u.DueDate);
        UpcomingPayments.Indexes.CreateOne(new CreateIndexModel<UpcomingPayment>(upcomingPaymentIndexKeys));

        var upcomingRecurringIndexKeys = Builders<UpcomingPayment>.IndexKeys
            .Ascending(u => u.UserId)
            .Ascending(u => u.RecurringExpenseId);
        UpcomingPayments.Indexes.CreateOne(new CreateIndexModel<UpcomingPayment>(upcomingRecurringIndexKeys));

        // ExpenseBookMember indexes
        // 1. Permission lookup: find a specific user's membership in a book
        var memberBookUserIndex = Builders<ExpenseBookMember>.IndexKeys
            .Ascending(m => m.ExpenseBookId)
            .Ascending(m => m.UserId);
        ExpenseBookMembers.Indexes.CreateOne(new CreateIndexModel<ExpenseBookMember>(
            memberBookUserIndex,
            new CreateIndexOptions { Name = "idx_member_book_user" }));

        // 2. All books for a user (member list view)
        var memberUserIndex = Builders<ExpenseBookMember>.IndexKeys.Ascending(m => m.UserId);
        ExpenseBookMembers.Indexes.CreateOne(new CreateIndexModel<ExpenseBookMember>(
            memberUserIndex,
            new CreateIndexOptions { Name = "idx_member_user" }));

        // 3. Invite token lookup — sparse+unique so null tokens don't conflict
        var memberTokenIndex = Builders<ExpenseBookMember>.IndexKeys.Ascending(m => m.InviteToken);
        ExpenseBookMembers.Indexes.CreateOne(new CreateIndexModel<ExpenseBookMember>(
            memberTokenIndex,
            new CreateIndexOptions { Name = "idx_member_token", Sparse = true, Unique = true }));

        // 4. Prevent duplicate pending invites to the same email per book
        //    Partial index: only applies when inviteStatus == "pending"
        var memberEmailIndex = Builders<ExpenseBookMember>.IndexKeys
            .Ascending(m => m.ExpenseBookId)
            .Ascending(m => m.InvitedEmail);
        var memberEmailFilter = Builders<ExpenseBookMember>.Filter.Eq(m => m.InviteStatus, "pending");
        ExpenseBookMembers.Indexes.CreateOne(new CreateIndexModel<ExpenseBookMember>(
            memberEmailIndex,
            new CreateIndexOptions<ExpenseBookMember>
            {
                Name = "idx_member_book_email_pending",
                Unique = true,
                PartialFilterExpression = memberEmailFilter
            }));
    }
}
