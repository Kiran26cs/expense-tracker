namespace ExpensesBackend.API.Services.Interfaces;

public interface IMessagingService
{
    Task<bool> SendEmailAsync(string to, string subject, string body, Dictionary<string, string>? variables = null);
    Task<bool> SendSmsAsync(string to, string message, Dictionary<string, string>? variables = null);
}
