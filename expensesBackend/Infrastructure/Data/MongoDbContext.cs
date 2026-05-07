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
    public IMongoCollection<Lending> Lendings =>
        _database.GetCollection<Lending>("lendings");
    public IMongoCollection<LendingRepayment> LendingRepayments =>
        _database.GetCollection<LendingRepayment>("lendingRepayments");

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

        // Cosmos DB requires an index for every ORDER BY field.
        // Queries filtered by ExpenseBookId (not UserId) need their own indexes.
        var expenseByBookDateIndex = Builders<Expense>.IndexKeys
            .Ascending(e => e.ExpenseBookId)
            .Descending(e => e.Date);
        Expenses.Indexes.CreateOne(new CreateIndexModel<Expense>(
            expenseByBookDateIndex,
            new CreateIndexOptions { Name = "idx_expense_book_date" }));

        // Dynamic sort by Amount (paged expense query) needs amount indexes for both filter paths.
        var expenseByBookAmountIndex = Builders<Expense>.IndexKeys
            .Ascending(e => e.ExpenseBookId)
            .Descending(e => e.Amount);
        Expenses.Indexes.CreateOne(new CreateIndexModel<Expense>(
            expenseByBookAmountIndex,
            new CreateIndexOptions { Name = "idx_expense_book_amount" }));

        var expenseByUserAmountIndex = Builders<Expense>.IndexKeys
            .Ascending(e => e.UserId)
            .Descending(e => e.Amount);
        Expenses.Indexes.CreateOne(new CreateIndexModel<Expense>(
            expenseByUserAmountIndex,
            new CreateIndexOptions { Name = "idx_expense_user_amount" }));

        Expenses.Indexes.CreateOne(new CreateIndexModel<Expense>(
            Builders<Expense>.IndexKeys.Descending(e => e.Date),
            new CreateIndexOptions { Name = "idx_expense_date" }));

        Expenses.Indexes.CreateOne(new CreateIndexModel<Expense>(
            Builders<Expense>.IndexKeys.Descending(e => e.Amount),
            new CreateIndexOptions { Name = "idx_expense_amount" }));

        // Cosmos DB requires ALL ORDER BY fields in a single composite index.
        // The paged query sorts by (date, _id) or (amount, _id) — _id is the keyset tiebreaker.
        Expenses.Indexes.CreateOne(new CreateIndexModel<Expense>(
            Builders<Expense>.IndexKeys.Descending(e => e.Date).Descending(e => e.Id),
            new CreateIndexOptions { Name = "idx_expense_date_id" }));

        Expenses.Indexes.CreateOne(new CreateIndexModel<Expense>(
            Builders<Expense>.IndexKeys.Descending(e => e.Amount).Descending(e => e.Id),
            new CreateIndexOptions { Name = "idx_expense_amount_id" }));

        Expenses.Indexes.CreateOne(new CreateIndexModel<Expense>(
            Builders<Expense>.IndexKeys.Ascending(e => e.ExpenseBookId).Descending(e => e.Date).Descending(e => e.Id),
            new CreateIndexOptions { Name = "idx_expense_book_date_id" }));

        Expenses.Indexes.CreateOne(new CreateIndexModel<Expense>(
            Builders<Expense>.IndexKeys.Ascending(e => e.UserId).Descending(e => e.Date).Descending(e => e.Id),
            new CreateIndexOptions { Name = "idx_expense_user_date_id" }));

        Expenses.Indexes.CreateOne(new CreateIndexModel<Expense>(
            Builders<Expense>.IndexKeys.Ascending(e => e.ExpenseBookId).Descending(e => e.Amount).Descending(e => e.Id),
            new CreateIndexOptions { Name = "idx_expense_book_amount_id" }));

        Expenses.Indexes.CreateOne(new CreateIndexModel<Expense>(
            Builders<Expense>.IndexKeys.Ascending(e => e.UserId).Descending(e => e.Amount).Descending(e => e.Id),
            new CreateIndexOptions { Name = "idx_expense_user_amount_id" }));

        // Category indexes — Cosmos DB requires an index on every ORDER BY field
        var categoryIndexKeys = Builders<Category>.IndexKeys
            .Ascending(c => c.ExpenseBookId)
            .Ascending(c => c.Name);
        Categories.Indexes.CreateOne(new CreateIndexModel<Category>(
            categoryIndexKeys,
            new CreateIndexOptions { Name = "idx_category_book_name" }));

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

        // DashboardService sorts UpcomingPayments by DueDate filtered only by RecurringExpenseId
        // (no UserId in filter), so (UserId, DueDate) doesn't apply — needs its own index.
        var upcomingByRecurringDueDateIndex = Builders<UpcomingPayment>.IndexKeys
            .Ascending(u => u.RecurringExpenseId)
            .Descending(u => u.DueDate);
        UpcomingPayments.Indexes.CreateOne(new CreateIndexModel<UpcomingPayment>(
            upcomingByRecurringDueDateIndex,
            new CreateIndexOptions { Name = "idx_upcoming_recurring_duedate" }));

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

        // 3. Invite token lookup — partial+unique so only documents with a token are indexed
        //    Cosmos DB for MongoDB indexes null values in sparse indexes (unlike native MongoDB),
        //    so we use a partial filter to exclude documents without a token instead.
        var memberTokenIndex = Builders<ExpenseBookMember>.IndexKeys.Ascending(m => m.InviteToken);
        var memberTokenFilter = Builders<ExpenseBookMember>.Filter
            .Type(m => m.InviteToken, MongoDB.Bson.BsonType.String);
        ExpenseBookMembers.Indexes.CreateOne(new CreateIndexModel<ExpenseBookMember>(
            memberTokenIndex,
            new CreateIndexOptions<ExpenseBookMember> { Name = "idx_member_token", PartialFilterExpression = memberTokenFilter }));

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

        // Lending indexes
        var lendingIndexKeys = Builders<Lending>.IndexKeys
            .Ascending(l => l.ExpenseBookId)
            .Ascending(l => l.IsDeleted)
            .Ascending(l => l.Status);
        Lendings.Indexes.CreateOne(new CreateIndexModel<Lending>(
            lendingIndexKeys,
            new CreateIndexOptions { Name = "idx_lending_book_status" }));

        // GetLendingsAsync sorts by CreatedAt DESC — Cosmos DB requires CreatedAt in the index.
        var lendingByCreatedAtIndex = Builders<Lending>.IndexKeys
            .Ascending(l => l.ExpenseBookId)
            .Ascending(l => l.IsDeleted)
            .Descending(l => l.CreatedAt);
        Lendings.Indexes.CreateOne(new CreateIndexModel<Lending>(
            lendingByCreatedAtIndex,
            new CreateIndexOptions { Name = "idx_lending_book_createdat" }));

        // LendingRepayment indexes
        var repaymentIndexKeys = Builders<LendingRepayment>.IndexKeys
            .Ascending(r => r.LendingId)
            .Ascending(r => r.IsDeleted)
            .Descending(r => r.Date);
        LendingRepayments.Indexes.CreateOne(new CreateIndexModel<LendingRepayment>(
            repaymentIndexKeys,
            new CreateIndexOptions { Name = "idx_repayment_lending_date" }));
    }
}
