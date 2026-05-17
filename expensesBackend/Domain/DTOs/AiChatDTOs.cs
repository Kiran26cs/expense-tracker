namespace ExpensesBackend.API.Domain.DTOs;

public class AiChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string BookId { get; set; } = string.Empty;
    public ReferenceContext? ReferenceContext { get; set; }
}

public class ReferenceContext
{
    // "expense" | "budget" | "category" | "member"
    public string Type { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
}

public class AiChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public int CreditsUsed { get; set; }
    public int CreditsLeft { get; set; }
    public List<string> ToolsUsed { get; set; } = [];
}
