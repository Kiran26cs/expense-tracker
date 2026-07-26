using ExpensesBackend.API.Domain.Entities;

namespace ExpensesBackend.API.Services.BankSync;

public interface IBankStatementParser
{
    // headers: the detected header row; rows: data rows beneath it
    Task<(List<ParsedBankTransaction> transactions, string detectedFormat)> ParseAsync(
        string[] headers, List<string[]> rows);
}
