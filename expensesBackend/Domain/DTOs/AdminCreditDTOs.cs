namespace ExpensesBackend.API.Domain.DTOs;

public class AdminCreditBalanceDto
{
    public string BookId { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public int FreeCreditsLeft { get; set; }
    public int PaidCreditsLeft { get; set; }
    public int FreeCreditsLimit { get; set; }
    public DateTime LastResetDate { get; set; }
}

public class AdminCreditTransactionListDto
{
    public List<AdminCreditTransactionDto> Transactions { get; set; } = [];
    public long Total { get; set; }
}

public class AdminCreditTransactionDto
{
    public string Id { get; set; } = string.Empty;
    public string ExpenseBookId { get; set; } = string.Empty;
    public string TriggeredByUserId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string> ToolsUsed { get; set; } = [];
    public DateTime Timestamp { get; set; }
}

public class AdminGrantCreditsRequest
{
    public int Amount { get; set; }
    public string? Note { get; set; }
}
