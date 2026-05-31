using System.ComponentModel.DataAnnotations;

namespace ExpensesBackend.API.Domain.DTOs;

public class CreateExpenseRequest
{
    public string? ExpenseBookId { get; set; }
    public string Type { get; set; } = "expense";
    [Required]
    public decimal Amount { get; set; }
    [Required]
    public DateTime Date { get; set; }
    [Required]
    public string Category { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public bool IsRecurring { get; set; }
    public RecurringConfig? RecurringConfig { get; set; }
    /// <summary>Original amount in <see cref="OriginalCurrency"/> before conversion. Omit when same as book currency.</summary>
    public decimal? OriginalAmount { get; set; }
    /// <summary>ISO 4217 code of the original currency. Omit when same as book currency.</summary>
    public string? OriginalCurrency { get; set; }
    public string? ReceiptGroupId { get; set; }
    public string? ReceiptNumber { get; set; }
    public bool IsReceiptItem { get; set; }
    public decimal? TaxAmount { get; set; }
}

public class ReceiptItemRequest
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required] public decimal Amount { get; set; }
    [Required] public string Category { get; set; } = string.Empty;
}

public class CreateExpenseBatchRequest
{
    public string? ExpenseBookId { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Merchant { get; set; }
    [Required] public string PaymentMethod { get; set; } = string.Empty;
    [Required] public DateTime Date { get; set; }
    /// <summary>Regular line items. Each becomes its own expense document.</summary>
    [Required] public List<ReceiptItemRequest> Items { get; set; } = [];
    /// <summary>Optional tax — becomes a separate expense entry with category "Tax &amp; Fees".</summary>
    public decimal? TaxAmount { get; set; }
    public string? TaxLabel { get; set; }
}

public class UpdateExpenseRequest
{
    public string? Type { get; set; }
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
    public string Type { get; set; } = "expense";
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Category { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? ReceiptUrl { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal? OriginalAmount { get; set; }
    public string? OriginalCurrency { get; set; }
    public decimal? FxRate { get; set; }
    public string? ReceiptGroupId { get; set; }
    public string? ReceiptNumber { get; set; }
    public bool IsReceiptItem { get; set; }
    public decimal? TaxAmount { get; set; }
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

// ── Paged expense query ───────────────────────────────────────────────────────

public class ExpensePagedRequest
{
    public string? ExpenseBookId { get; set; }
    public string? Search { get; set; }
    public string? Type { get; set; }
    public string? Category { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>"date" or "amount". Defaults to "date".</summary>
    public string SortField { get; set; } = "date";

    /// <summary>"asc" or "desc". Defaults to "desc" (newest first).</summary>
    public string SortDir { get; set; } = "desc";

    /// <summary>Base64-encoded cursor from the previous response.</summary>
    public string? Cursor { get; set; }

    public int PageSize { get; set; } = 50;

    /// <summary>
    /// When non-empty, restricts results to expenses whose category is in this list.
    /// Populated by the controller from the caller's ACL-resolved category restrictions.
    /// </summary>
    public List<string> AllowedCategoryIds { get; set; } = [];
}

public class ExpensePagedResponse
{
    public List<ExpenseDto> Items { get; set; } = [];
    public long Total { get; set; }
    public string? NextCursor { get; set; }
    public string? PrevCursor { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrev { get; set; }
}

/// <summary>Cursor model — serialised as Base64 JSON on the wire.</summary>
public class ExpenseCursor
{
    public string Field { get; set; } = "date";
    public string Dir { get; set; } = "desc";
    /// <summary>ISO-8601 for date field, decimal string for amount field.</summary>
    public string Value { get; set; } = "";
    public string Id { get; set; } = "";
}

