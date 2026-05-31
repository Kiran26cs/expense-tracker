using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ExpensesBackend.API.Services.Interfaces;

namespace ExpensesBackend.API.Services.AI;

public class AiCategoryClassifier : ICategoryClassifier
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    private static readonly HashSet<string> ValidClasses =
        new(StringComparer.OrdinalIgnoreCase) { "need", "want", "debt" };

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public AiCategoryClassifier(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _http   = httpClientFactory.CreateClient("Claude");
        _config = config;
    }

    public async Task<string?> ClassifyAsync(string categoryName)
    {
        try
        {
            var apiKey = _config["Claude:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return null;

            var body = new
            {
                model      = "claude-haiku-4-5-20251001",
                max_tokens = 10,
                system     = "You classify personal finance expense categories for the 50-30-20 budgeting rule. " +
                             "Reply with exactly one word: need, want, debt, or unclassified. No explanation, no punctuation.",
                messages   = new[]
                {
                    new { role = "user", content = $"Classify: \"{categoryName}\"" }
                }
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
            if (!response.IsSuccessStatusCode) return null;

            var responseBody = await response.Content.ReadAsStringAsync();
            var text = JsonNode.Parse(responseBody)?["content"]?[0]?["text"]
                           ?.GetValue<string>()
                           ?.Trim()
                           .ToLowerInvariant();

            return text is not null && ValidClasses.Contains(text) ? text : null;
        }
        catch
        {
            // Never propagate — classification is best-effort
            return null;
        }
    }
}
