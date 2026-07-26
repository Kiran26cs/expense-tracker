namespace ExpensesBackend.API.Domain.DTOs;

public class ParsedBankTransactionDto
{
    public int RowNumber { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty; // "expense" | "income"
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
}

public class BankSyncConfirmResultDto
{
    public ImportSessionDto ImportSession { get; set; } = new();
    public int Imported { get; set; }
    public int DuplicatesSkipped { get; set; }
}
