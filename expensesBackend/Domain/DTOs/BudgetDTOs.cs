namespace ExpensesBackend.API.Domain.DTOs;

public class UpsertBudgetVersionRequest
{
    public string? ExpenseBookId { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string EffectiveDate { get; set; } = string.Empty;
    /// <summary>YYYY-MM string of the month this budget is being saved for.</summary>
    public string? EffectivePeriod { get; set; }
}
