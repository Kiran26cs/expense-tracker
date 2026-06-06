namespace ExpensesBackend.API.Services.Interfaces;

public interface IPushNotificationService
{
    /// <summary>
    /// Sends a Web Push notification to a single browser subscription.
    /// Throws WebPushException with Gone/NotFound status when the subscription is expired — caller should delete it.
    /// Returns false on other delivery failures.
    /// </summary>
    Task<bool> SendAsync(string endpoint, string p256dh, string auth, string title, string body, string url = "/app");
}
