using System.ComponentModel.DataAnnotations;

namespace ExpensesBackend.API.Domain.DTOs;

public class PagePermissionsDto
{
    public string? Dashboard { get; set; }
    public string? Expenses { get; set; }
    public string? Budgets { get; set; }
    public string? Settings { get; set; }
    public string? Insights { get; set; }
}

public class ResolvedPermissions
{
    public string Role { get; set; } = "none";           // owner|admin|member|viewer|none
    public string Dashboard { get; set; } = "none";      // view|none
    public string Expenses { get; set; } = "none";       // write|view|none
    public string Budgets { get; set; } = "none";        // write|view|none
    public string Settings { get; set; } = "none";       // write|view|none
    public string Insights { get; set; } = "none";       // view|none
    public bool CanDeleteExpenses { get; set; } = false;
    public bool CanManageMembers { get; set; } = false;
    public bool CanModifyBook { get; set; } = false;
    public bool IsOwner { get; set; } = false;
    public List<string> AllowedCategoryIds { get; set; } = [];
}

public class ExpenseBookMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string ExpenseBookId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string InvitedEmail { get; set; } = string.Empty;
    public string InviteStatus { get; set; } = "pending";
    public string Role { get; set; } = "member";
    public PagePermissionsDto? Permissions { get; set; }
    public List<string> AllowedCategoryIds { get; set; } = [];
    public bool CanDeleteExpenses { get; set; }
    public string AddedBy { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class InviteMemberRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "member";   // admin | member | viewer

    public PagePermissionsDto? Permissions { get; set; }
    public List<string> AllowedCategoryIds { get; set; } = [];
    public bool CanDeleteExpenses { get; set; } = false;
}

public class InviteMemberResponse
{
    public ExpenseBookMemberDto Member { get; set; } = new();
    public string InviteLink { get; set; } = string.Empty;
}

public class UpdateMemberRequest
{
    public string? Role { get; set; }
    public PagePermissionsDto? Permissions { get; set; }
    public List<string>? AllowedCategoryIds { get; set; }
    public bool? CanDeleteExpenses { get; set; }
}

public class AcceptInviteResponse
{
    public ExpenseBookMemberDto Member { get; set; } = new();
    public string ExpenseBookId { get; set; } = string.Empty;
    public string ExpenseBookName { get; set; } = string.Empty;
}

public class PendingInviteDto
{
    public string MemberId { get; set; } = string.Empty;
    public string ExpenseBookId { get; set; } = string.Empty;
    public string ExpenseBookName { get; set; } = string.Empty;
    public string? ExpenseBookIcon { get; set; }
    public string? ExpenseBookColor { get; set; }
    public string? ExpenseBookCurrency { get; set; }
    public string Role { get; set; } = "member";
    public string InviteToken { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}
