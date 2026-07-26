namespace ExpensesBackend.API.Domain.DTOs;

public class AdminBookListDto
{
    public List<AdminBookSummaryDto> Books { get; set; } = [];
    public long Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class AdminBookSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerPlan { get; set; } = string.Empty;
    public bool AiChatEnabled { get; set; }
    public bool IsTemplate { get; set; }
    public int ExpenseCount { get; set; }
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminToggleAiChatRequest
{
    public bool Enabled { get; set; }
}
