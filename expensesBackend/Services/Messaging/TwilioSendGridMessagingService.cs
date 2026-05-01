using SendGrid;
using SendGrid.Helpers.Mail;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using ExpensesBackend.API.Services.Interfaces;

namespace ExpensesBackend.API.Services.Messaging;

public class TwilioSendGridMessagingService : IMessagingService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TwilioSendGridMessagingService> _logger;

    public TwilioSendGridMessagingService(
        IConfiguration configuration,
        ILogger<TwilioSendGridMessagingService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        TwilioClient.Init(
            _configuration["Messaging:Twilio:AccountSid"]
                ?? throw new InvalidOperationException("Twilio AccountSid not configured"),
            _configuration["Messaging:Twilio:AuthToken"]
                ?? throw new InvalidOperationException("Twilio AuthToken not configured")
        );
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, Dictionary<string, string>? variables = null)
    {
        try
        {
            var apiKey = _configuration["Messaging:SendGrid:ApiKey"]
                ?? throw new InvalidOperationException("SendGrid ApiKey not configured");
            var fromEmail = _configuration["Messaging:SendGrid:FromEmail"]
                ?? throw new InvalidOperationException("SendGrid FromEmail not configured");
            var fromName = _configuration["Messaging:SendGrid:FromName"] ?? "Expense Tracker";

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, fromName);
            var recipient = new EmailAddress(to);

            // SendGrid: use body as both plain-text and HTML content
            var msg = MailHelper.CreateSingleEmail(from, recipient, subject, body, $"<p>{body}</p>");
            var response = await client.SendEmailAsync(msg);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Body.ReadAsStringAsync();
                _logger.LogError("SendGrid error {StatusCode}: {Error}", (int)response.StatusCode, error);
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendGrid email send failed to {To}", to);
            return false;
        }
    }

    public async Task<bool> SendSmsAsync(string to, string message, Dictionary<string, string>? variables = null)
    {
        try
        {
            var fromPhone = _configuration["Messaging:Twilio:FromPhone"]
                ?? throw new InvalidOperationException("Twilio FromPhone not configured");

            // Twilio expects E.164 format: +919876543210
            var toPhone = NormalizeE164(to);

            var result = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(fromPhone),
                to: new PhoneNumber(toPhone)
            );

            if (result.ErrorCode != null)
                _logger.LogError("Twilio SMS error {Code}: {Message}", result.ErrorCode, result.ErrorMessage);

            return result.ErrorCode == null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twilio SMS send failed to {To}", to);
            return false;
        }
    }

    // Ensures phone has + prefix for E.164. e.g. 919876543210 → +919876543210
    private static string NormalizeE164(string phone)
        => phone.StartsWith('+') ? phone : $"+{phone.TrimStart('0')}";
}
