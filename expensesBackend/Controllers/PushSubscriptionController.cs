using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/notifications/push")]
public class PushSubscriptionController : ControllerBase
{
    private readonly MongoDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PushSubscriptionController> _logger;

    public PushSubscriptionController(
        MongoDbContext context,
        IConfiguration configuration,
        ILogger<PushSubscriptionController> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

    // GET api/notifications/push/vapid-public-key
    // Returns the VAPID public key the frontend needs to subscribe to push notifications.
    [HttpGet("vapid-public-key")]
    public IActionResult GetVapidPublicKey()
    {
        var publicKey = _configuration["WebPush:VapidPublicKey"];
        if (string.IsNullOrEmpty(publicKey))
            return StatusCode(503, ApiResponse<string>.ErrorResponse("Push notifications not configured"));

        return Ok(ApiResponse<string>.SuccessResponse(publicKey));
    }

    // POST api/notifications/push/subscribe
    // Saves (or updates) a browser push subscription for the authenticated user.
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscribeRequest request)
    {
        var userId = GetUserId();

        var filter = Builders<Domain.Entities.PushSubscription>.Filter
            .Where(s => s.UserId == userId && s.Endpoint == request.Endpoint);

        var existing = await _context.PushSubscriptions.Find(filter).FirstOrDefaultAsync();
        if (existing != null)
        {
            await _context.PushSubscriptions.UpdateOneAsync(filter,
                Builders<Domain.Entities.PushSubscription>.Update
                    .Set(s => s.P256dh, request.P256dh)
                    .Set(s => s.Auth, request.Auth)
                    .Set(s => s.UpdatedAt, DateTime.UtcNow));
        }
        else
        {
            await _context.PushSubscriptions.InsertOneAsync(new Domain.Entities.PushSubscription
            {
                UserId = userId,
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth,
                UserAgent = Request.Headers.UserAgent.ToString()
            });
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    // POST api/notifications/push/unsubscribe
    // Removes a push subscription so the user stops receiving push notifications from this device.
    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeRequest request)
    {
        var userId = GetUserId();
        await _context.PushSubscriptions.DeleteOneAsync(
            s => s.UserId == userId && s.Endpoint == request.Endpoint);
        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    // POST api/notifications/push/test
    // Sends a test push notification to all subscribed devices of the current user.
    [HttpPost("test")]
    public async Task<IActionResult> SendTestNotification(
        [FromServices] IPushNotificationService pushService)
    {
        var userId = GetUserId();
        var subscriptions = await _context.PushSubscriptions
            .Find(s => s.UserId == userId)
            .ToListAsync();

        if (subscriptions.Count == 0)
            return BadRequest(ApiResponse<bool>.ErrorResponse("No push subscription found. Open the app in a supported browser first."));

        var sent = 0;
        foreach (var sub in subscriptions)
        {
            var success = await pushService.SendAsync(
                sub.Endpoint, sub.P256dh, sub.Auth,
                "NidhiWise Test",
                "Push notifications are working!",
                "/app");
            if (success) sent++;
        }

        return Ok(ApiResponse<string>.SuccessResponse($"Sent to {sent}/{subscriptions.Count} device(s)"));
    }
}
