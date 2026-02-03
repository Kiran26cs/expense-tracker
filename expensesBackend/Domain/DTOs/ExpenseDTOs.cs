namespace ExpensesBackend.API.Domain.DTOs;

public class CreateExpenseRequest
{
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Category { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public bool IsRecurring { get; set; }
    public RecurringConfig? RecurringConfig { get; set; }
}

public class UpdateExpenseRequest
{
    public decimal? Amount { get; set; }
    public DateTime? Date { get; set; }
    public string? Category { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
}

public class ExpenseDto
{
    public string Id { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Category { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? ReceiptUrl { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RecurringConfig
{
    public string Frequency { get; set; } = "monthly";
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
