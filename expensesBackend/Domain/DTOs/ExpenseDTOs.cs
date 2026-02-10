using System.ComponentModel.DataAnnotations;

namespace ExpensesBackend.API.Domain.DTOs;

public class CreateExpenseRequest
{
    public string? ExpenseBookId { get; set; }
    [Required]
    public decimal Amount { get; set; }
    [Required]
    public DateTime Date { get; set; }
    [Required]
    public string Category { get; set; } = string.Empty;
    [Required]
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
    public string? ExpenseBookId { get; set; }
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

public class RecurringExpenseDto
{
    public string Id { get; set; } = string.Empty;
    public string? ExpenseBookId { get; set; }
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Frequency { get; set; } = "monthly";
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime NextOccurrence { get; set; }
    public DateTime? LastProcessed { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class MarkRecurringPaidRequest
{
    public DateTime PaidDate { get; set; }
}

public class UpdateRecurringExpenseRequest
{
    public decimal? Amount { get; set; }
    public string? Frequency { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Category { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Description { get; set; }
}

