using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExpensesBackend.API.Services.Interfaces;

namespace ExpensesBackend.API.Services.Messaging;

public class Msg91MessagingService : IMessagingService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<Msg91MessagingService> _logger;

    private const string EmailEndpoint = "https://control.msg91.com/api/v5/email/send";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Msg91MessagingService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<Msg91MessagingService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, Dictionary<string, string>? variables = null)
    {
        try
        {
            var authKey = _configuration["Messaging:MSG91:AuthKey"]
                ?? throw new InvalidOperationException("MSG91 AuthKey not configured");
            var templateId = _configuration["Messaging:MSG91:EmailTemplateId"]
                ?? throw new InvalidOperationException("MSG91 EmailTemplateId not configured");
            var domain = _configuration["Messaging:MSG91:EmailDomain"]
                ?? throw new InvalidOperationException("MSG91 EmailDomain not configured");
            var fromEmail = _configuration["Messaging:MSG91:FromEmail"]
                ?? throw new InvalidOperationException("MSG91 FromEmail not configured");
            var fromName = _configuration["Messaging:MSG91:FromName"] ?? "Expense Tracker";

            var payload = new
            {
                recipients = new[]
                {
                    new
                    {
                        to = new[] { new { name = to, email = to } },
                        variables = variables ?? new Dictionary<string, string>()
                    }
                },
                from = new { name = fromName, email = fromEmail },
                domain,
                template_id = templateId,
                validate_before_send = false
            };

            return await PostAsync(EmailEndpoint, authKey, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MSG91 email send failed to {To}", to);
            return false;
        }
    }

    private async Task<bool> PostAsync(string url, string authKey, object payload)
    {
        var client = _httpClientFactory.CreateClient("MSG91");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("authkey", authKey);
        client.DefaultRequestHeaders.Add("accept", "application/json");

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("MSG91 API error {StatusCode}: {Error}", (int)response.StatusCode, error);
        }

        return response.IsSuccessStatusCode;
    }

}
