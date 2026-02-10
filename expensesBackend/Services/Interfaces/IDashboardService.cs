using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(string userId, string? expenseBookId, DateTime? startDate, DateTime? endDate);
    Task<List<MonthlyTrend>> GetMonthlyTrendsAsync(string userId, string? expenseBookId, int months);    Task<List<DailyTransactionGroup>> GetGroupedTransactionsAsync(string userId, string? expenseBookId, DateTime? startDate = null, DateTime? endDate = null);
    Task MigrateDailySummariesAsync(string userId);
    Task<UpcomingPaymentsPaginatedResponse> GetUpcomingPaymentsAsync(string userId, string? expenseBookId, int page = 1, int pageSize = 10);
    Task<UpcomingPaymentDto> MarkUpcomingPaymentAsPaidAsync(string userId, string upcomingPaymentId, DateTime paidDate, bool recordAsExpense);
    Task GenerateUpcomingPaymentsAsync(string userId);
}
