using ExpensesBackend.API.Domain.Entities;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ExpensesBackend.API.Services.BankSync;

/// <summary>
/// Sends PDF-extracted text to Claude and parses the returned transaction JSON.
/// Handles chunking for statements longer than ~30,000 characters.
/// </summary>
public class PdfBankStatementParser
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<PdfBankStatementParser> _logger;

    // ~30K chars ≈ 7,500 tokens — well within Haiku's context window per chunk
    private const int ChunkSize = 30_000;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PdfBankStatementParser(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<PdfBankStatementParser> logger)
    {
        _http   = httpClientFactory.CreateClient("Claude");
        _config = config;
        _logger = logger;
    }

    public async Task<(List<ParsedBankTransaction> transactions, string detectedFormat)> ParseAsync(
        string pdfText, string bankName)
    {
        var chunks = SplitIntoChunks(pdfText, ChunkSize);
        var all = new List<RawPdfTransaction>();
        int chunkIndex = 0;

        foreach (var chunk in chunks)
        {
            var result = await ParseChunkAsync(chunk, bankName, chunkIndex++);
            all.AddRange(result);
        }

        // Renumber rows sequentially and convert to domain entity
        var transactions = all
            .Select((t, i) => MapToTransaction(t, i + 1))
            .Where(t => t != null)
            .Cast<ParsedBankTransaction>()
            .ToList();

        return (transactions, $"{bankName}_PDF");
    }

    // ── Private helpers ──────────────────────────────────────────────────────────

    private async Task<List<RawPdfTransaction>> ParseChunkAsync(
        string chunk, string bankName, int chunkIndex)
    {
        var apiKey = _config["Claude:ApiKey"];
        if (string.IsNullOrEmpty(apiKey)) return [];

        var prompt =
            $"You are extracting transactions from a {bankName} bank statement PDF (chunk {chunkIndex + 1}).\n\n" +
            "Here is the extracted text:\n\n" +
            chunk +
            "\n\n" +
            "Extract ALL financial transactions. For each transaction return:\n" +
            "- date: ISO format YYYY-MM-DD\n" +
            "- description: the merchant/narration text (trim whitespace)\n" +
            "- debit: positive number if money went OUT, or null\n" +
            "- credit: positive number if money came IN, or null\n\n" +
            "Rules:\n" +
            "- Skip rows with no date or no amount\n" +
            "- Skip opening/closing balance rows\n" +
            "- Remove commas from amounts (e.g. 1,350.00 → 1350.00)\n\n" +
            "Return ONLY a valid JSON array. No explanation, no markdown:\n" +
            "[{\"date\":\"2026-07-01\",\"description\":\"UPI/SWIGGY\",\"debit\":350.00,\"credit\":null}]";

        var body = new
        {
            model      = "claude-haiku-4-5-20251001",
            max_tokens = 4096,
            messages   = new[] { new { role = "user", content = prompt } }
        };

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post, "https://api.anthropic.com/v1/messages")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Claude PDF parse failed with {Status}", response.StatusCode);
                return [];
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var text = JsonNode.Parse(responseBody)?["content"]?[0]?["text"]?.GetValue<string>();
            if (string.IsNullOrEmpty(text)) return [];

            return ExtractTransactionsFromResponse(text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PDF chunk {Index} parsing error", chunkIndex);
            return [];
        }
    }

    private static List<RawPdfTransaction> ExtractTransactionsFromResponse(string text)
    {
        try
        {
            var start = text.IndexOf('[');
            var end   = text.LastIndexOf(']');
            if (start < 0 || end < 0 || end <= start) return [];

            var json = text[start..(end + 1)];
            return JsonSerializer.Deserialize<List<RawPdfTransaction>>(json, JsonOpts) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static ParsedBankTransaction? MapToTransaction(RawPdfTransaction raw, int rowNumber)
    {
        if (!DateTime.TryParse(raw.Date, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date)) return null;

        decimal debit  = raw.Debit  ?? 0m;
        decimal credit = raw.Credit ?? 0m;
        if (debit == 0 && credit == 0) return null;

        return new ParsedBankTransaction
        {
            RowNumber   = rowNumber,
            Date        = DateTime.SpecifyKind(date, DateTimeKind.Utc),
            Description = (raw.Description ?? string.Empty).Trim(),
            Amount      = debit > 0 ? debit : credit,
            Type        = debit > 0 ? "expense" : "income"
        };
    }

    private static List<string> SplitIntoChunks(string text, int chunkSize)
    {
        var chunks = new List<string>();
        for (int i = 0; i < text.Length; i += chunkSize)
            chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
        return chunks;
    }

    // Matches Claude's camelCase JSON output
    private class RawPdfTransaction
    {
        public string? Date { get; set; }
        public string? Description { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
    }
}
