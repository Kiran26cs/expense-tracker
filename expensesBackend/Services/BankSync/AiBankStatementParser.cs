using ExpensesBackend.API.Domain.Entities;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ExpensesBackend.API.Services.BankSync;

/// <summary>
/// Fallback parser: sends the first 10 rows to Claude to identify columns,
/// then delegates to GenericBankStatementParser using the detected mapping.
/// </summary>
public class AiBankStatementParser : IBankStatementParser
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<AiBankStatementParser> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AiBankStatementParser(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<AiBankStatementParser> logger)
    {
        _http   = httpClientFactory.CreateClient("Claude");
        _config = config;
        _logger = logger;
    }

    public async Task<(List<ParsedBankTransaction> transactions, string detectedFormat)> ParseAsync(
        string[] headers, List<string[]> rows)
    {
        var map = await DetectColumnsWithAiAsync(headers, rows.Take(10).ToList());

        if (map == null)
            throw new InvalidOperationException(
                "Could not identify column structure from this statement. " +
                "Supported banks: HDFC, ICICI, SBI, Axis, Kotak, IndusInd. " +
                "Please export the statement directly from your bank's net banking portal in XLS/CSV format.");

        var parser = new GenericBankStatementParser(map);
        var (txns, _) = await parser.ParseAsync(headers, rows);
        return (txns, $"AI_{map.BankName}");
    }

    private async Task<BankColumnMap?> DetectColumnsWithAiAsync(string[] headers, List<string[]> sampleRows)
    {
        var apiKey = _config["Claude:ApiKey"];
        if (string.IsNullOrEmpty(apiKey)) return null;

        var sampleText = new StringBuilder();
        sampleText.AppendLine("Headers: " + string.Join(" | ", headers));
        foreach (var row in sampleRows)
            sampleText.AppendLine("Row: " + string.Join(" | ", row));

        var prompt =
            "You are parsing a bank statement. Here are the column headers and sample rows:\n\n" +
            sampleText +
            "\n\nIdentify which column header contains:\n" +
            "1. Transaction date\n" +
            "2. Transaction description or narration\n" +
            "3. Debit/withdrawal amount (money going OUT — may be blank for credits)\n" +
            "4. Credit/deposit amount (money coming IN — may be blank for debits)\n" +
            "5. The date format (e.g. dd/MM/yyyy, dd-MM-yyyy, d MMM yyyy)\n" +
            "6. The bank name if recognizable, else 'Unknown'\n\n" +
            "Respond ONLY with a JSON object like:\n" +
            "{\"dateColumn\":\"...\",\"descriptionColumn\":\"...\",\"debitColumn\":\"...\",\"creditColumn\":\"...\",\"dateFormat\":\"...\",\"bankName\":\"...\"}";

        var body = new
        {
            model      = "claude-haiku-4-5-20251001",
            max_tokens = 300,
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
            if (!response.IsSuccessStatusCode) return null;

            var responseBody = await response.Content.ReadAsStringAsync();
            var text = JsonNode.Parse(responseBody)?["content"]?[0]?["text"]?.GetValue<string>();
            if (string.IsNullOrEmpty(text)) return null;

            // Extract JSON from Claude's response
            var start = text.IndexOf('{');
            var end   = text.LastIndexOf('}');
            if (start < 0 || end < 0) return null;

            var json = JsonNode.Parse(text[start..(end + 1)]);
            if (json == null) return null;

            var dateCol = json["dateColumn"]?.GetValue<string>() ?? string.Empty;
            var descCol = json["descriptionColumn"]?.GetValue<string>() ?? string.Empty;
            var debitCol  = json["debitColumn"]?.GetValue<string>() ?? string.Empty;
            var creditCol = json["creditColumn"]?.GetValue<string>() ?? string.Empty;
            var dateFmt   = json["dateFormat"]?.GetValue<string>() ?? "dd/MM/yyyy";
            var bankName  = json["bankName"]?.GetValue<string>() ?? "Unknown";

            if (string.IsNullOrEmpty(dateCol) || string.IsNullOrEmpty(descCol))
                return null;

            // Verify the AI-detected column names actually exist in the headers
            var headerSet = new HashSet<string>(headers.Select(h => h.Trim()), StringComparer.OrdinalIgnoreCase);
            if (!headerSet.Contains(dateCol) || !headerSet.Contains(descCol))
            {
                _logger.LogWarning(
                    "AI detected columns [{Date}, {Desc}] for {Bank} but they are not in the actual headers: [{Headers}]",
                    dateCol, descCol, bankName, string.Join(", ", headers));
                return null;
            }

            return new BankColumnMap
            {
                BankName               = bankName,
                DateColumnNames        = [dateCol],
                DescriptionColumnNames = [descCol],
                DebitColumnNames       = string.IsNullOrEmpty(debitCol) || !headerSet.Contains(debitCol) ? [] : [debitCol],
                CreditColumnNames      = string.IsNullOrEmpty(creditCol) || !headerSet.Contains(creditCol) ? [] : [creditCol],
                DateFormats            = [dateFmt, "dd/MM/yyyy", "dd-MM-yyyy", "d MMM yyyy"]
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI column detection failed");
            return null;
        }
    }
}
