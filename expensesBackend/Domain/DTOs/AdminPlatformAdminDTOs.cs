namespace ExpensesBackend.API.Domain.DTOs;

public class AdminPlatformAdminListDto
{
    public List<AdminPlatformAdminSummaryDto> Admins { get; set; } = [];
    public int Total { get; set; }
}

public class AdminPlatformAdminSummaryDto
{
    public string Id          { get; set; } = string.Empty;
    public string Email       { get; set; } = string.Empty;
    public string Name        { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = [];
    public bool IsActive      { get; set; }
    public string GrantedBy   { get; set; } = string.Empty;
    public DateTime GrantedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class AdminCreatePlatformAdminRequest
{
    public string Email             { get; set; } = string.Empty;
    public string Name              { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = [];
}

public class AdminUpdatePlatformAdminRequest
{
    public string? Name              { get; set; }
    public List<string>? Permissions { get; set; }
}

public class AdminSetAdminActiveRequest
{
    public bool IsActive { get; set; }
}
