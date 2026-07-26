using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IAdminDashboardService
{
    Task<AdminUserStatsDto> GetUserStatsAsync();
    Task<AdminSubscriptionStatsDto> GetSubscriptionStatsAsync();
    Task<AdminCreditStatsDto> GetCreditStatsAsync();
    Task<AdminBookStatsDto> GetBookStatsAsync();
    Task<AdminImportStatsDto> GetImportStatsAsync();
    Task<AdminRecentActionsDto> GetRecentActionsAsync(int limit = 20);
}
