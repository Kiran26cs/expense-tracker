namespace ExpensesBackend.API.Domain.DTOs;

public class ParsedBankTransactionDto
{
    public int RowNumber { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty; // "expense" | "income"
    // Suggested category (from payee memory or AI) — editable by the user before confirming.
    public string Category { get; set; } = "Uncategorized";
    // Stable payee identifier (UPI VPA, or NEFT/RTGS/IMPS name) — lets the frontend recognize
    // repeat payees within the same statement and propagate a category across them. Null when
    // the narration didn't match a recognized format.
    public string? PayeeKey { get; set; }
}

public class BankStatementPreviewDto
{
    public string SessionId { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string DetectedFormat { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public List<ParsedBankTransactionDto> Transactions { get; set; } = [];
}

public class ConfirmBankSyncRequest
{
    public string ExpenseBookId { get; set; } = string.Empty;
    // Applied to all rows — can be overridden per-bank in BankSyncService
    public string DefaultPaymentMethod { get; set; } = "Bank Transfer";
    // Row numbers to exclude from this sync
    public List<int> ExcludeRowNumbers { get; set; } = [];
    // Final category per row (rowNumber -> category name), as reviewed/edited by the user in
    // the preview step. Falls back to the session's suggested category for any row not present.
    public Dictionary<int, string>? CategoryOverrides { get; set; }
}

public class BankSyncConfirmResultDto
{
    public ImportSessionDto ImportSession { get; set; } = new();
    public int Imported { get; set; }
    public int DuplicatesSkipped { get; set; }
}
