using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;

namespace ExpensesBackend.API.Services;

public class PermissionService : IPermissionService
{
    private readonly IMemberService _memberService;

    public PermissionService(IMemberService memberService)
    {
        _memberService = memberService;
    }

    public Task<ResolvedPermissions> ResolveAsync(string bookId, string userId)
        => _memberService.GetResolvedPermissionsAsync(bookId, userId);

    public async Task AssertCanAsync(string bookId, string userId, string page, string requiredLevel = "view")
    {
        var perms = await ResolveAsync(bookId, userId);

        if (perms.Role == "none")
            throw new UnauthorizedAccessException("You do not have access to this expense book.");

        var pagePerm = page.ToLowerInvariant() switch
        {
            "expenses"  => perms.Expenses,
            "budgets"   => perms.Budgets,
            "settings"  => perms.Settings,
            "insights"  => perms.Insights,
            "dashboard" => perms.Dashboard,
            _           => "none"
        };

        if (requiredLevel == "write" && pagePerm != "write")
            throw new UnauthorizedAccessException($"You do not have write access to {page}.");
        if (requiredLevel == "view" && pagePerm == "none")
            throw new UnauthorizedAccessException($"You do not have access to {page}.");
    }

    public async Task AssertCanDeleteExpensesAsync(string bookId, string userId)
    {
        var perms = await ResolveAsync(bookId, userId);
        if (!perms.CanDeleteExpenses)
            throw new UnauthorizedAccessException("You do not have permission to delete expenses in this book.");
    }

    public async Task AssertCanManageMembersAsync(string bookId, string userId)
    {
        var perms = await ResolveAsync(bookId, userId);
        if (!perms.CanManageMembers)
            throw new UnauthorizedAccessException("You do not have permission to manage members of this book.");
    }

    public async Task AssertIsOwnerAsync(string bookId, string userId)
    {
        var perms = await ResolveAsync(bookId, userId);
        if (!perms.IsOwner)
            throw new UnauthorizedAccessException("Only the book owner can perform this action.");
    }

    public async Task AssertIsMemberAsync(string bookId, string userId)
    {
        var perms = await ResolveAsync(bookId, userId);
        if (perms.Role == "none")
            throw new UnauthorizedAccessException("You are not a member of this expense book.");
    }
}
