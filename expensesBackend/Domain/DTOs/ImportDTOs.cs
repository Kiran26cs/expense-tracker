namespace ExpensesBackend.API.Domain.DTOs;

public class StartImportRequest
{
    public string FileName { get; set; } = string.Empty;
    public List<CsvExpenseRow> Rows { get; set; } = [];
}

public class CsvExpenseRow
{
    public int RowNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Type { get; set; } = "expense";
    public string Currency { get; set; } = string.Empty;
}

public class ImportSessionDto
{
    public string Id { get; set; } = string.Empty;
    public string ExpenseBookId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<ImportRecordDto> Records { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class ImportSessionSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class ImportRecordDto
{
    public int RowNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public class ImportJobPayload
{
    public string ImportSessionId { get; set; } = string.Empty;
    public string ExpenseBookId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public List<CsvExpenseRow> Rows { get; set; } = [];
    public List<string> AllowedCategoryIds { get; set; } = [];
    public bool IsRetry { get; set; } = false;
}

public class RetryImportRequest
{
    // Empty = retry all failed records; otherwise retry only the specified row numbers.
    public List<int> RowNumbers { get; set; } = [];
}
