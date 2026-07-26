using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IAdminPlatformAdminService
{
    Task<AdminPlatformAdminListDto> GetAdminsAsync();
    Task<AdminPlatformAdminSummaryDto> CreateAdminAsync(AdminCreatePlatformAdminRequest request, string grantedByAdminId);
    Task<AdminPlatformAdminSummaryDto> UpdateAdminAsync(string adminId, AdminUpdatePlatformAdminRequest request);
    Task SetActiveAsync(string adminId, bool isActive);
}
