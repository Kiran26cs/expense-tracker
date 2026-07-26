using ExcelDataReader;
using ExpensesBackend.API.Domain.Entities;
using System.Data;
using System.Text;

namespace ExpensesBackend.API.Services.BankSync;

/// <summary>
/// Unified entry point for all statement formats.
/// Routes .pdf to PdfBankStatementParser; .csv/.xls/.xlsx to the column-based parsers.
/// </summary>
public class BankStatementParserFactory
{
    private readonly AiBankStatementParser _aiParser;
    private readonly PdfBankStatementParser _pdfParser;
    private readonly BankStatementPdfExtractor _pdfExtractor;

    public BankStatementParserFactory(
        AiBankStatementParser aiParser,
        PdfBankStatementParser pdfParser,
        BankStatementPdfExtractor pdfExtractor)
    {
        _aiParser     = aiParser;
        _pdfParser    = pdfParser;
        _pdfExtractor = pdfExtractor;
    }

    /// <summary>
    /// Parses any supported file type and returns transactions + detected format label.
    /// password is only used for encrypted PDF statements.
    /// </summary>
    public async Task<(List<ParsedBankTransaction> transactions, string detectedFormat)> ParseFileAsync(
        Stream stream, string fileName, string bankName, string? password = null)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        if (ext == ".pdf")
        {
            var text = _pdfExtractor.ExtractText(stream, password);

            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException(
                    "No text could be extracted from this PDF. " +
                    "Please ensure the file is a text-based PDF (not a scanned image).");

            return await _pdfParser.ParseAsync(text, bankName);
        }

        // CSV / Excel path
        var (headers, rows) = ReadFile(stream, fileName, password);
        var parser = GetParser(headers, bankName);
        return await parser.ParseAsync(headers, rows);
    }

    // ── Internal helpers ─────────────────────────────────────────────────────────

    private IBankStatementParser GetParser(string[] headers, string? bankNameHint = null)
    {
        var map = KnownBankFormats.DetectBankWithHint(headers, bankNameHint);
        return map != null
            ? new GenericBankStatementParser(map)
            : _aiParser;
    }

    /// <summary>
    /// Reads the file stream into (headers, dataRows).
    /// Supports .csv, .xls, .xlsx. password is used for password-protected Excel files.
    /// </summary>
    public static (string[] headers, List<string[]> rows) ReadFile(Stream stream, string fileName, string? password = null)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".xls" or ".xlsx" ? ReadExcel(stream, password) : ReadCsv(stream);
    }

    // ── CSV reader ───────────────────────────────────────────────────────────────

    private static (string[] headers, List<string[]> rows) ReadCsv(Stream stream)
    {
        var allRows = new List<string[]>();

        using var reader = new StreamReader(stream, Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line != null) allRows.Add(ParseCsvLine(line));
        }

        return SplitHeadersAndRows(allRows);
    }

    // Minimal RFC-4180 CSV parser — handles quoted fields containing commas
    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                { sb.Append('"'); i++; }
                else
                { inQuotes = !inQuotes; }
            }
            else if (c == ',' && !inQuotes)
            { fields.Add(sb.ToString()); sb.Clear(); }
            else
            { sb.Append(c); }
        }

        fields.Add(sb.ToString());
        return fields.Select(f => f.Trim()).ToArray();
    }

    // ── Excel reader ─────────────────────────────────────────────────────────────

    private static (string[] headers, List<string[]> rows) ReadExcel(Stream stream, string? password = null)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var config = string.IsNullOrEmpty(password)
            ? null
            : new ExcelReaderConfiguration { Password = password };

        using var reader = config != null
            ? ExcelReaderFactory.CreateReader(stream, config)
            : ExcelReaderFactory.CreateReader(stream);

        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
        });

        var allRows = new List<string[]>();
        var table = dataSet.Tables[0];

        foreach (DataRow row in table.Rows)
        {
            allRows.Add(Enumerable.Range(0, table.Columns.Count)
                .Select(i => NormalizeCellValue(row[i]?.ToString()))
                .ToArray());
        }

        return SplitHeadersAndRows(allRows);
    }

    // ── Shared ───────────────────────────────────────────────────────────────────

    private static (string[] headers, List<string[]> rows) SplitHeadersAndRows(List<string[]> allRows)
    {
        const int minColumns  = 4;
        const int maxPreamble = 20;

        // Pick the row with the MOST non-empty cells in the preamble window.
        // This correctly skips partial-data preamble rows (e.g. "Transaction Date from | 01/06 | to | 14/06")
        // and lands on the actual column-header row which always has the highest cell count.
        int bestIdx   = -1;
        int bestCount = 0;

        for (int i = 0; i < Math.Min(allRows.Count, maxPreamble); i++)
        {
            var count = allRows[i].Count(c => !string.IsNullOrWhiteSpace(c));
            if (count > bestCount)
            {
                bestCount = count;
                bestIdx   = i;
            }
        }

        if (bestIdx >= 0 && bestCount >= minColumns)
            return (allRows[bestIdx], allRows.Skip(bestIdx + 1).ToList());

        throw new InvalidOperationException(
            "Could not find a header row with at least 4 columns. " +
            "Please upload a bank statement export file.");
    }

    // Normalize Excel cell values: collapse internal line breaks (Alt+Enter in Excel)
    // and multiple spaces into a single space, then trim.
    private static string NormalizeCellValue(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        // Replace \r\n, \r, \n with a single space
        var normalized = System.Text.RegularExpressions.Regex.Replace(raw, @"[\r\n]+", " ");
        // Collapse multiple spaces
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @" {2,}", " ");
        return normalized.Trim();
    }
}
