namespace ExpensesBackend.API.Domain.DTOs;

public class CreditBalanceDto
{
    public string ExpenseBookId { get; set; } = string.Empty;
    public int FreeCreditsLeft { get; set; }
    public int PaidCreditsLeft { get; set; }
    public int TotalCreditsLeft => FreeCreditsLeft + PaidCreditsLeft;
    public int FreeCreditsLimit { get; set; }
    public DateTime LastResetDate { get; set; }
}

public class AdminGrantRequest
{
    public int Amount { get; set; }
}

public class CreditTransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string TriggeredByUserId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string> ToolsUsed { get; set; } = [];
    public DateTime Timestamp { get; set; }
}
