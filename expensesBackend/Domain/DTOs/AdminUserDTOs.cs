namespace ExpensesBackend.API.Domain.DTOs;

public class AdminUserListDto
{
    public List<AdminUserSummaryDto> Users { get; set; } = [];
    public long Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class AdminUserSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int BookCount { get; set; }
}

public class AdminUserDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AdminUserBookDto> Books { get; set; } = [];
    public AdminUserSubscriptionDto? Subscription { get; set; }
    public AdminUserCreditSummaryDto? Credits { get; set; }
}

public class AdminUserBookDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool AiChatEnabled { get; set; }
    public int ExpenseCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminUserSubscriptionDto
{
    public string Status { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public bool CancelAtPeriodEnd { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
}

public class AdminUserCreditSummaryDto
{
    public int TotalFreeLeft { get; set; }
    public int TotalPaidLeft { get; set; }
}

public class AdminChangePlanRequest
{
    public string Plan { get; set; } = string.Empty;
}

public class AdminSetActiveRequest
{
    public bool IsActive { get; set; }
}
