using SendGrid;
using SendGrid.Helpers.Mail;
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
}
