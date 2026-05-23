using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.AI;

public class ClaudeOrchestrator
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    private static readonly JsonSerializerOptions SerializeOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public ClaudeOrchestrator(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _http   = httpClientFactory.CreateClient("Claude");
        _config = config;
    }

    /// <summary>
    /// Runs the agentic loop: sends user message (plus prior history), executes any tool calls,
    /// feeds results back, repeats until Claude returns a final text response.
    /// </summary>
    public async Task<(string Reply, List<string> ToolsUsed)> RunAsync(
        string systemPrompt,
        string userMessage,
        IReadOnlyList<ToolDefinition> tools,
        Func<string, JsonObject, Task<string>> toolExecutor,
        IReadOnlyList<ChatHistoryMessage>? history = null)
    {
        // Prepend prior conversation turns so Claude has full context
        var messages = new List<object>();
        if (history is { Count: > 0 })
        {
            foreach (var h in history)
                messages.Add(new { role = h.Role, content = h.Content });
        }
        messages.Add(new { role = "user", content = userMessage });
        var toolsUsed = new List<string>();
        var maxRounds = 10;

        for (var round = 0; round < maxRounds; round++)
        {
            var responseJson = await PostAsync(BuildRequestJson(systemPrompt, messages, tools));
            var response     = JsonNode.Parse(responseJson)!.AsObject();

            var stopReason = response["stop_reason"]?.GetValue<string>();
            var content    = response["content"]?.AsArray() ?? [];

            // Rebuild assistant content using only known fields — strips "caller" and other extras
            // so the second API call doesn't get rejected with unknown properties
            var assistantBlocks = new List<object>();
            foreach (var block in content)
            {
                var type = block?["type"]?.GetValue<string>();
                if (type == "text")
                {
                    assistantBlocks.Add(new
                    {
                        type = "text",
                        text = block!["text"]?.GetValue<string>() ?? string.Empty
                    });
                }
                else if (type == "tool_use")
                {
                    assistantBlocks.Add(new
                    {
                        type  = "tool_use",
                        id    = block!["id"]?.GetValue<string>() ?? string.Empty,
                        name  = block!["name"]?.GetValue<string>() ?? string.Empty,
                        input = JsonSerializer.Deserialize<object>(
                                    block!["input"]?.ToJsonString() ?? "{}")
                    });
                }
            }

            messages.Add(new { role = "assistant", content = assistantBlocks });

            if (stopReason != "tool_use")
            {
                var text = string.Concat(
                    assistantBlocks
                        .OfType<object>()
                        .Select(b => JsonSerializer.Serialize(b, SerializeOpts))
                        .Select(s => JsonNode.Parse(s))
                        .Where(n => n?["type"]?.GetValue<string>() == "text")
                        .Select(n => n!["text"]?.GetValue<string>() ?? string.Empty));

                return (text.Trim(), toolsUsed);
            }

            // Execute each tool call and collect results
            var toolResults = new List<object>();
            foreach (var block in content)
            {
                if (block?["type"]?.GetValue<string>() != "tool_use") continue;

                var toolName = block["name"]!.GetValue<string>();
                var toolId   = block["id"]!.GetValue<string>();

                // Parse input fresh — detached from the response JsonNode tree
                var toolInput = JsonNode.Parse(
                    block["input"]?.ToJsonString() ?? "{}")?.AsObject() ?? [];

                toolsUsed.Add(toolName);

                string resultContent;
                try
                {
                    resultContent = await toolExecutor(toolName, toolInput);
                }
                catch (UnauthorizedAccessException ex)
                {
                    resultContent = $"Permission denied: {ex.Message}";
                }
                catch (KeyNotFoundException ex)
                {
                    resultContent = $"Not found: {ex.Message}";
                }
                catch (Exception ex)
                {
                    resultContent = $"Error: {ex.Message}";
                }

                toolResults.Add(new
                {
                    type        = "tool_result",
                    tool_use_id = toolId,
                    content     = resultContent,
                });
            }

            messages.Add(new { role = "user", content = toolResults });
        }

        return ("I was unable to complete your request. Please try again.", toolsUsed);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private string BuildRequestJson(
        string systemPrompt,
        List<object> messages,
        IReadOnlyList<ToolDefinition> tools)
    {
        var toolList = tools.Select(t => new
        {
            name         = t.Name,
            description  = t.Description,
            input_schema = JsonSerializer.Deserialize<object>(t.InputSchemaJson),
        });

        var body = new
        {
            model      = _config["Claude:Model"] ?? "claude-sonnet-4-6",
            max_tokens = 4096,
            system     = systemPrompt,
            tools      = toolList,
            messages,
        };

        return JsonSerializer.Serialize(body, SerializeOpts);
    }

    private async Task<string> PostAsync(string jsonBody)
    {
        var apiKey = _config["Claude:ApiKey"]
            ?? throw new InvalidOperationException("Claude:ApiKey is not configured.");

        using var request = new HttpRequestMessage(
            HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var response     = await _http.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Claude API error {(int)response.StatusCode}: {responseBody}");

        return responseBody;
    }
}

public record ToolDefinition(string Name, string Description, string InputSchemaJson);
