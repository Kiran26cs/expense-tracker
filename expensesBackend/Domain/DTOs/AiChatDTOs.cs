namespace ExpensesBackend.API.Domain.DTOs;

public class AiChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string BookId { get; set; } = string.Empty;
    public ReferenceContext? ReferenceContext { get; set; }
    /// <summary>
    /// Prior turns sent by the client — oldest first, max 20 entries.
    /// Each entry is a user or assistant message from the visible chat history.
    /// </summary>
    public List<ChatHistoryMessage> History { get; set; } = [];
}

public class ChatHistoryMessage
{
    /// <summary>"user" or "assistant"</summary>
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
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

public class ReceiptExtractRequest
{
    public string BookId { get; set; } = string.Empty;
    /// <summary>Base64-encoded image or PDF data (no data URL prefix).</summary>
    public string FileBase64 { get; set; } = string.Empty;
    /// <summary>MIME type: image/jpeg, image/png, image/webp, application/pdf.</summary>
    public string MimeType { get; set; } = "image/jpeg";
}

public class ReceiptExtractResponse
{
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? Date { get; set; }
    public string? Category { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Type { get; set; }
    public string? Notes { get; set; }
    public int Confidence { get; set; }
    public List<string> MissingFields { get; set; } = [];
}
