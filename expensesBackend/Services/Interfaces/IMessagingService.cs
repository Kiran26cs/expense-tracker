namespace ExpensesBackend.API.Services.Interfaces;

public interface IMessagingService
{
    Task<bool> SendEmailAsync(string to, string subject, string body, Dictionary<string, string>? variables = null);
}
