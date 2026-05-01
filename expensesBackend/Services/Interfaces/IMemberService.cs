using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IMemberService
{
    /// <summary>Returns all non-deleted members of a book. Caller must have at least read access.</summary>
    Task<List<ExpenseBookMemberDto>> GetMembersAsync(string bookId, string requestingUserId);

    /// <summary>Creates a pending invite and returns the invite link.</summary>
    Task<InviteMemberResponse> InviteMemberAsync(string bookId, string requestingUserId, InviteMemberRequest request, string baseUrl);

    /// <summary>Updates role/permissions of an existing member.</summary>
    Task<ExpenseBookMemberDto> UpdateMemberAsync(string bookId, string memberId, string requestingUserId, UpdateMemberRequest request);

    /// <summary>Soft-deletes (revokes) a member record.</summary>
    Task RemoveMemberAsync(string bookId, string memberId, string requestingUserId);

    /// <summary>Accepts an invite token and links the user to the book.</summary>
    Task<AcceptInviteResponse> AcceptInviteAsync(string token, string userId);

    /// <summary>Resolves the effective permissions for a user in a book (Redis-cached).</summary>
    Task<ResolvedPermissions> GetResolvedPermissionsAsync(string bookId, string userId);

    /// <summary>Throws UnauthorizedAccessException if the user cannot manage members.</summary>
    Task EnsureCanManageMembersAsync(string bookId, string userId);

    /// <summary>
    /// Throws UnauthorizedAccessException if the user doesn't satisfy the required access level.
    /// <paramref name="requiredLevel"/> format: "page:level", e.g. "expenses:write", "budgets:view".
    /// </summary>
    Task EnsureHasAccessAsync(string bookId, string userId, string requiredLevel);

    /// <summary>Removes the cached permission entry for a user+book.</summary>
    Task InvalidatePermissionsCacheAsync(string bookId, string userId);

    /// <summary>Returns all pending invites addressed to the given email.</summary>
    Task<List<PendingInviteDto>> GetPendingInvitesAsync(string userEmail);

    /// <summary>Declines (revokes) a pending invite by token. Validates the token belongs to userEmail.</summary>
    Task DeclineInviteAsync(string token, string userEmail);

    /// <summary>
    /// Returns categories for the book filtered by the requesting user's allowed category access.
    /// Owners receive all categories; other roles receive only their explicitly allowed categories.
    /// </summary>
    Task<List<CategoryDto>> GetAccessibleCategoriesAsync(string bookId, string requestingUserId);
}
