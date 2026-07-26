namespace ExpensesBackend.API.Services.BankSync;

public class BankColumnMap
{
    public string BankName { get; set; } = string.Empty;

    // Column names tried in order (case-insensitive) for each field
    public string[] DateColumnNames { get; set; } = [];
    public string[] DescriptionColumnNames { get; set; } = [];
    public string[] DebitColumnNames { get; set; } = [];   // money out → expense
    public string[] CreditColumnNames { get; set; } = [];  // money in  → income

    // Date formats tried in order (for DateTime.TryParseExact)
    public string[] DateFormats { get; set; } = [];

    // Columns whose presence uniquely identifies this bank — ALL must be present to match
    public string[] IdentifyingColumns { get; set; } = [];
}
