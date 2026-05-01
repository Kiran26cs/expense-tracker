namespace ExpensesBackend.API.Infrastructure.Cache;

/// <summary>
/// Central registry for all Redis cache keys in the application.
/// Prevents magic strings from being scattered across services.
/// </summary>
public static class CacheKeys
{
    // Categories — keyed by expenseBookId since categories belong to a book
    public static string Categories(string expenseBookId) =>
        $"categories:{expenseBookId}";

    public static string CategoryById(string expenseBookId, string categoryId) =>
        $"categories:{expenseBookId}:{categoryId}";

    // Dashboard
    public static string DashboardSummary(string userId, string? bookId) =>
        $"dashboard:summary:{userId}:{bookId ?? "all"}";

    public static string MonthlyTrends(string userId, string? bookId, int months) =>
        $"dashboard:trends:{userId}:{bookId ?? "all"}:{months}";

    public static string UpcomingPayments(string userId, string? bookId, int page) =>
        $"dashboard:upcoming:{userId}:{bookId ?? "all"}:page{page}";

    // Budgets — keyed by month (YYYY-MM) because GetBudgetsAsync aggregates
    // both budget amounts and live spending for that specific month.
    public static string UserBudgets(string userId, string? bookId, string month) =>
        $"budgets:{userId}:{bookId ?? "all"}:{month}";

    // Helper: returns the YYYY-MM string for a given date
    public static string MonthKey(DateTime date) =>
        $"{date.Year}-{date.Month:D2}";

    public static string CurrentMonthKey() => MonthKey(DateTime.UtcNow);

    // Expense Books
    public static string UserExpenseBooks(string userId) =>
        $"expense-books:{userId}";

    // Auth / OTP
    public static string Otp(string identifier) =>
        $"otp:{identifier}";

    // User Settings
    public static string UserSettings(string userId) =>
        $"settings:{userId}";

    // Book-level Settings
    public static string BookSettings(string bookId) =>
        $"settings:book:{bookId}";

    // Member permissions — cached per (bookId, userId) pair
    public static string MemberPermissions(string bookId, string userId) =>
        $"member-perms:{bookId}:{userId}";
}
