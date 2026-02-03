using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(string userId, DateTime? startDate, DateTime? endDate);
    Task<List<MonthlyTrend>> GetMonthlyTrendsAsync(string userId, int months);
}
