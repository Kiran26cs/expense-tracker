using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IPermissionService
{
    Task<ResolvedPermissions> ResolveAsync(string bookId, string userId);

    /// <summary>Throws UnauthorizedAccessException if user doesn't have the required page access.</summary>
    /// <param name="requiredLevel">"view" or "write"</param>
    Task AssertCanAsync(string bookId, string userId, string page, string requiredLevel = "view");

    Task AssertCanDeleteExpensesAsync(string bookId, string userId);
    Task AssertCanManageMembersAsync(string bookId, string userId);
    Task AssertIsOwnerAsync(string bookId, string userId);
    Task AssertIsMemberAsync(string bookId, string userId);
}
