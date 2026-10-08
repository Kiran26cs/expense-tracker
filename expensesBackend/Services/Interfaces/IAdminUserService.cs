using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IAdminUserService
{
    Task<AdminUserListDto> GetUsersAsync(string? search, int page, int pageSize);
    Task<AdminUserDetailDto?> GetUserDetailAsync(string userId);
    Task<AdminUserDetailDto> ChangePlanAsync(string userId, string plan, string adminId, string adminEmail);
    Task<AdminUserDetailDto> SetActiveAsync(string userId, bool isActive, string adminId, string adminEmail);
    Task<List<SessionDto>> GetSessionsAsync(string userId);
    Task RevokeSessionAsync(string userId, string sessionId, string adminId, string adminEmail);
    Task RevokeAllSessionsAsync(string userId, string adminId, string adminEmail);
}
