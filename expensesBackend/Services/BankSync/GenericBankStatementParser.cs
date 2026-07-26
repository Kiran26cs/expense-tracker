using ExpensesBackend.API.Domain.Entities;
using System.Globalization;

namespace ExpensesBackend.API.Services.BankSync;

public class GenericBankStatementParser : IBankStatementParser
{
    private readonly BankColumnMap _map;

    public GenericBankStatementParser(BankColumnMap map)
    {
        _map = map;
    }

    public Task<(List<ParsedBankTransaction> transactions, string detectedFormat)> ParseAsync(
        string[] headers, List<string[]> rows)
    {
        var headerIndex = BuildHeaderIndex(headers);

        var dateIdx  = FindColumn(headerIndex, _map.DateColumnNames);
        var descIdx  = FindColumn(headerIndex, _map.DescriptionColumnNames);
        var debitIdx = FindColumn(headerIndex, _map.DebitColumnNames);
        var creditIdx = FindColumn(headerIndex, _map.CreditColumnNames);

        if (dateIdx < 0 || descIdx < 0)
            throw new InvalidOperationException(
                $"Could not find required columns in {_map.BankName} statement.");

        var transactions = new List<ParsedBankTransaction>();
        int rowNumber = 1;

        foreach (var row in rows)
        {
            if (row.Length <= Math.Max(dateIdx, descIdx)) continue;

            var rawDate = GetCell(row, dateIdx);
            var rawDesc = GetCell(row, descIdx);
            var rawDebit = debitIdx >= 0 ? GetCell(row, debitIdx) : string.Empty;
            var rawCredit = creditIdx >= 0 ? GetCell(row, creditIdx) : string.Empty;

            // Skip blank rows
            if (string.IsNullOrWhiteSpace(rawDate) && string.IsNullOrWhiteSpace(rawDesc)) continue;

            if (!TryParseDate(rawDate, out var date)) continue;

            var debitAmt  = ParseDecimal(rawDebit);
            var creditAmt = ParseDecimal(rawCredit);

            // Skip rows with no monetary value
            if (debitAmt == 0 && creditAmt == 0) continue;

            // Debit wins if both are non-zero (rare edge case)
            decimal amount;
            string type;
            if (debitAmt > 0)
            {
                amount = debitAmt;
                type   = "expense";
            }
            else
            {
                amount = creditAmt;
                type   = "income";
            }

            transactions.Add(new ParsedBankTransaction
            {
                RowNumber   = rowNumber++,
                Date        = date,
                Description = rawDesc.Trim(),
                Amount      = amount,
                Type        = type
            });
        }

        return Task.FromResult((transactions, $"{_map.BankName}_CSV"));
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static Dictionary<string, int> BuildHeaderIndex(string[] headers)
    {
        var idx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length; i++)
        {
            var key = headers[i].Trim();
            if (!string.IsNullOrEmpty(key) && !idx.ContainsKey(key))
                idx[key] = i;
        }
        return idx;
    }

    private static int FindColumn(Dictionary<string, int> index, string[] candidates)
    {
        foreach (var name in candidates)
            if (index.TryGetValue(name, out var i)) return i;
        return -1;
    }

    private static string GetCell(string[] row, int idx) =>
        idx < row.Length ? row[idx].Trim() : string.Empty;

    private bool TryParseDate(string raw, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        raw = raw.Trim();

        foreach (var fmt in _map.DateFormats)
        {
            if (DateTime.TryParseExact(raw, fmt, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date))
            {
                date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                return true;
            }
        }

        // Fallback to generic parse
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
            return true;
        }

        return false;
    }

    private static decimal ParseDecimal(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0m;

        // Remove currency symbols, spaces, commas
        var cleaned = raw.Trim()
            .Replace("₹", "")
            .Replace("Rs.", "")
            .Replace("Rs", "")
            .Replace(",", "")
            .Replace(" ", "");

        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var val)
            ? Math.Abs(val)
            : 0m;
    }
}
