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

    private const string SmsEndpoint = "https://control.msg91.com/api/v5/flow";
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

    public async Task<bool> SendSmsAsync(string to, string message, Dictionary<string, string>? variables = null)
    {
        try
        {
            var authKey = _configuration["Messaging:MSG91:AuthKey"]
                ?? throw new InvalidOperationException("MSG91 AuthKey not configured");
            var templateId = _configuration["Messaging:MSG91:SmsTemplateId"]
                ?? throw new InvalidOperationException("MSG91 SmsTemplateId not configured");

            // MSG91 expects phone in format 91XXXXXXXXXX (country code + number, no + prefix)
            var mobile = to;

            // Recipient object: mobiles field + all template variables spread at same level
            var recipient = new Dictionary<string, object> { ["mobiles"] = mobile };
            if (variables != null)
                foreach (var kv in variables)
                    recipient[kv.Key] = kv.Value;

            var payload = new
            {
                template_id = templateId,
                recipients = new[] { recipient }
            };

            return await PostAsync(SmsEndpoint, authKey, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MSG91 SMS send failed to {To}", to);
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

    // Strips leading + or 00, keeps digits only. e.g. +919876543210 → 919876543210
    private static string NormalizeMobile(string phone)
    {
        var digits = phone.TrimStart('+').TrimStart('0', '0');
        return new string(digits.Where(char.IsDigit).ToArray());
    }
}
