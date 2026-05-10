using Azure;
using Azure.Communication.Email;
using ExpensesBackend.API.Services.Interfaces;

namespace ExpensesBackend.API.Services.Messaging;

public class AzureCommunicationMessagingService : IMessagingService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureCommunicationMessagingService> _logger;

    public AzureCommunicationMessagingService(
        IConfiguration configuration,
        ILogger<AzureCommunicationMessagingService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, Dictionary<string, string>? variables = null)
    {
        try
        {
            var connectionString = _configuration["Messaging:AzureCommunication:ConnectionString"]
                ?? throw new InvalidOperationException("ACS ConnectionString not configured");
            var senderEmail = _configuration["Messaging:AzureCommunication:SenderEmail"]
                ?? throw new InvalidOperationException("ACS SenderEmail not configured");

            var client = new EmailClient(connectionString);

            var message = new EmailMessage(
                senderAddress: senderEmail,
                recipients: new EmailRecipients(new[] { new EmailAddress(to) }),
                content: new EmailContent(subject)
                {
                    PlainText = body
                }
            );

            var operation = await client.SendAsync(WaitUntil.Completed, message);
            var succeeded = operation.Value.Status == EmailSendStatus.Succeeded;

            if (!succeeded)
                _logger.LogError("ACS email send failed to {To}: status {Status}", to, operation.Value.Status);

            return succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ACS email send failed to {To}", to);
            return false;
        }
    }

    public Task<bool> SendSmsAsync(string to, string message, Dictionary<string, string>? variables = null)
    {
        _logger.LogWarning("SMS is not configured for ACS provider. Use MSG91 for SMS.");
        return Task.FromResult(false);
    }
}
