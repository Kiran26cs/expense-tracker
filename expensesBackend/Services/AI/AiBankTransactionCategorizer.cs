using ExpensesBackend.API.Services.Interfaces;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ExpensesBackend.API.Services.AI;

public class AiBankTransactionCategorizer : IBankTransactionCategorizer
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<AiBankTransactionCategorizer> _logger;

    private const int BatchSize = 50;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AiBankTransactionCategorizer(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<AiBankTransactionCategorizer> logger)
    {
        _http   = httpClientFactory.CreateClient("Claude");
        _config = config;
        _logger = logger;
    }

    public async Task<Dictionary<int, string>> ClassifyAsync(
        List<(int RowNumber, string Description)> transactions,
        List<string> availableCategoryNames)
    {
        var result = new Dictionary<int, string>();

        if (transactions.Count == 0)
            return result;

        // If no categories exist in the book yet, everything goes to Uncategorized
        if (availableCategoryNames.Count == 0)
        {
            foreach (var (row, _) in transactions)
                result[row] = "Uncategorized";
            return result;
        }

        // Process in batches so we stay within token limits
        for (int start = 0; start < transactions.Count; start += BatchSize)
        {
            var batch = transactions.Skip(start).Take(BatchSize).ToList();
            try
            {
                var assignments = await ClassifyBatchAsync(batch, availableCategoryNames);
                foreach (var kv in assignments)
                    result[kv.Key] = kv.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Batch categorization failed for rows {Start}-{End}; using Uncategorized",
                    start, start + batch.Count);
                foreach (var (row, _) in batch)
                    result[row] = "Uncategorized";
            }
        }

        return result;
    }

    private async Task<Dictionary<int, string>> ClassifyBatchAsync(
        List<(int RowNumber, string Description)> batch,
        List<string> categoryNames)
    {
        var apiKey = _config["Claude:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            return batch.ToDictionary(t => t.RowNumber, _ => "Uncategorized");
        }

        var catList  = string.Join(", ", categoryNames.Select(n => $"\"{n}\""));
        var txnLines = string.Join("\n", batch.Select((t, i) => $"{i + 1}. {t.Description}"));

        var prompt =
            $"You are categorizing bank transactions for a personal finance app.\n" +
            $"Available categories: {catList}\n\n" +
            $"For each transaction description below, pick the single best matching category from the list above.\n" +
            $"Rules:\n" +
            $"- Reply with ONLY a JSON array of {batch.Count} strings, one per transaction, in the same order.\n" +
            $"- Each string must be exactly one of the available category names.\n" +
            $"- If no category fits, use \"Uncategorized\".\n" +
            $"- No explanation, no extra text — only the JSON array.\n\n" +
            $"Transactions:\n{txnLines}";

        var body = new
        {
            model      = "claude-haiku-4-5-20251001",
            max_tokens = 1024,
            messages   = new[] { new { role = "user", content = prompt } }
        };

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
            _logger.LogWarning("Claude returned {Status} for batch categorization", response.StatusCode);
            return batch.ToDictionary(t => t.RowNumber, _ => "Uncategorized");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var text = JsonNode.Parse(responseBody)?["content"]?[0]?["text"]?.GetValue<string>();

        if (string.IsNullOrEmpty(text))
            return batch.ToDictionary(t => t.RowNumber, _ => "Uncategorized");

        // Extract the JSON array from Claude's response
        var startIdx = text.IndexOf('[');
        var endIdx   = text.LastIndexOf(']');
        if (startIdx < 0 || endIdx < 0)
            return batch.ToDictionary(t => t.RowNumber, _ => "Uncategorized");

        var json = JsonNode.Parse(text[startIdx..(endIdx + 1)]);
        if (json is not JsonArray arr)
            return batch.ToDictionary(t => t.RowNumber, _ => "Uncategorized");

        // Build a case-insensitive lookup of valid category names
        var validNames = new HashSet<string>(categoryNames, StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<int, string>();
        for (int i = 0; i < batch.Count; i++)
        {
            var assigned = i < arr.Count ? arr[i]?.GetValue<string>()?.Trim() : null;

            // Only accept the assignment if it's a real category in this book
            result[batch[i].RowNumber] = (!string.IsNullOrEmpty(assigned) && validNames.Contains(assigned))
                ? assigned
                : "Uncategorized";
        }

        return result;
    }
}
