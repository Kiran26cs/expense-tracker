using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ExpensesBackend.API.Services.BankSync;

public class BankStatementPdfExtractor
{
    private const int MaxPages = 60;

    /// <summary>
    /// Extracts text from a PDF statement, preserving the tabular layout.
    /// Password is required for encrypted bank statements — typically the
    /// account holder's date of birth in DDMMYYYY format (HDFC, ICICI, Axis, Kotak).
    /// </summary>
    public string ExtractText(Stream stream, string? password = null)
    {
        try
        {
            var options = new ParsingOptions();
            if (!string.IsNullOrWhiteSpace(password))
                options.Password = password;

            using var pdf = PdfDocument.Open(stream, options);
            var sb = new StringBuilder();
            int pageCount = Math.Min(pdf.NumberOfPages, MaxPages);

            for (int p = 1; p <= pageCount; p++)
            {
                var page = pdf.GetPage(p);
                sb.AppendLine($"--- Page {p} ---");
                sb.AppendLine(ExtractPageAsLines(page));
            }

            return sb.ToString();
        }
        catch (Exception ex) when (
            ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("encrypt",  StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("decrypt",  StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "This PDF is password-protected. Please provide the statement password " +
                "(usually your date of birth in DDMMYYYY format, e.g. 15081990).");
        }
    }

    /// <summary>
    /// Groups letters by their Y-position into visual lines, then sorts each line
    /// left-to-right. This reconstructs the tabular layout without requiring
    /// the DocumentLayoutAnalysis package.
    /// </summary>
    private static string ExtractPageAsLines(Page page)
    {
        var letters = page.Letters;
        if (letters == null || !letters.Any()) return string.Empty;

        // Tolerance: letters within 2 points vertically = same line
        const double lineTolerance = 2.0;

        var lineGroups = new List<(double y, List<Letter> letters)>();

        foreach (var letter in letters)
        {
            if (string.IsNullOrWhiteSpace(letter.Value)) continue;

            double letterY = letter.Location.Y;
            var existing = lineGroups.FirstOrDefault(l => Math.Abs(l.y - letterY) <= lineTolerance);

            if (existing.letters != null)
                existing.letters.Add(letter);
            else
                lineGroups.Add((letterY, [letter]));
        }

        // Sort lines top-to-bottom (PDF Y increases upward → descending order)
        var sb = new StringBuilder();
        foreach (var (_, lineLetters) in lineGroups.OrderByDescending(l => l.y))
        {
            // Sort letters left-to-right within each line
            var lineText = string.Concat(lineLetters
                .OrderBy(l => l.Location.X)
                .Select(l => l.Value));

            if (!string.IsNullOrWhiteSpace(lineText))
                sb.AppendLine(lineText);
        }

        return sb.ToString();
    }
}
