using ExpensesBackend.API.Services.Interfaces;
using WebPush;

namespace ExpensesBackend.API.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(IConfiguration configuration, ILogger<PushNotificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string endpoint, string p256dh, string auth, string title, string body, string url = "/app")
    {
        try
        {
            var publicKey = _configuration["WebPush:VapidPublicKey"]
                ?? throw new InvalidOperationException("VAPID public key not configured");
            var privateKey = _configuration["WebPush:VapidPrivateKey"]
                ?? throw new InvalidOperationException("VAPID private key not configured");
            var subject = _configuration["WebPush:Subject"] ?? "mailto:admin@nidhiwise.com";

            var subscription = new PushSubscription(endpoint, p256dh, auth);
            var vapidDetails = new VapidDetails(subject, publicKey, privateKey);

            // Payload must use NGSW's "notification" envelope so the Angular service worker
            // displays the notification automatically, including when the app is in the background.
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                notification = new
                {
                    title,
                    body,
                    icon = "/icons/icon-192.png",
                    badge = "/icons/icon-192.png",
                    data = new
                    {
                        onActionClick = new
                        {
                            @default = new
                            {
                                operation = "navigateLastFocusedOrOpen",
                                url
                            }
                        }
                    }
                }
            });

            var client = new WebPushClient();
            await client.SendNotificationAsync(subscription, payload, vapidDetails);
            return true;
        }
        catch (WebPushException ex) when (
            ex.StatusCode == System.Net.HttpStatusCode.Gone ||
            ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Re-throw so the scheduler can delete this stale subscription
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Web push failed to endpoint {Endpoint}", endpoint[..Math.Min(60, endpoint.Length)]);
            return false;
        }
    }
}
