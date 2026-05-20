using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExpensesBackend.API.Domain;
using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using ExpensesBackend.API.Services.Messaging;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services;

public class PaymentService : IPaymentService
{
    private readonly MongoDbContext _context;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly IMessagingService _messaging;

    private string KeyId     => _config["Razorpay:KeyId"]     ?? "";
    private string KeySecret => _config["Razorpay:KeySecret"] ?? "";
    private string WebhookSecret => _config["Razorpay:WebhookSecret"] ?? "";

    private static readonly Dictionary<string, (decimal Amount, string Description)> PlanMeta = new()
    {
        ["Starter"] = (399,  "Starter Plan – 1,000 expenses/month, 100 AI credits"),
        ["Pro"]      = (799,  "Pro Plan – Unlimited expenses, 300 AI credits"),
    };

    public PaymentService(MongoDbContext context, IHttpClientFactory httpFactory, IConfiguration config, IMessagingService messaging)
    {
        _context     = context;
        _httpFactory = httpFactory;
        _config      = config;
        _messaging   = messaging;
    }

    // ── Create subscription ───────────────────────────────────────────────────

    public async Task<CreateSubscriptionResponseDto> CreateSubscriptionAsync(string userId, string plan)
    {
        if (!PlanMeta.TryGetValue(plan, out var meta))
            throw new ArgumentException($"Unknown plan: {plan}");

        var planId = _config[$"Razorpay:Plans:{plan}"]
            ?? throw new InvalidOperationException($"Razorpay plan ID not configured for {plan}");

        var body = new
        {
            plan_id       = planId,
            total_count   = 120,   // 10 years max; Razorpay requires a finite number
            quantity      = 1,
            customer_notify = 1
        };

        var response = await RazorpayPostAsync("/subscriptions", body);
        var subscriptionId = response.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Razorpay did not return a subscription ID");

        // Persist the pending subscription
        await _context.UserSubscriptions.InsertOneAsync(new UserSubscription
        {
            UserId                = userId,
            Plan                  = Enum.Parse<PlanType>(plan),
            Status                = "created",
            RazorpaySubscriptionId = subscriptionId,
        });

        return new CreateSubscriptionResponseDto(
            SubscriptionId: subscriptionId,
            RazorpayKeyId:  KeyId,
            PlanName:       plan,
            Amount:         meta.Amount / 100m,
            Currency:       "INR",
            Description:    meta.Description
        );
    }

    // ── Verify & activate ────────────────────────────────────────────────────

    public async Task<SubscriptionStatusDto> VerifyAndActivateAsync(string userId, VerifyPaymentRequestDto req)
    {
        // HMAC-SHA256 of "payment_id|subscription_id"
        var payload  = $"{req.RazorpayPaymentId}|{req.RazorpaySubscriptionId}";
        var computed = ComputeHmac(payload, KeySecret);

        if (!string.Equals(computed, req.RazorpaySignature, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Payment signature verification failed");

        // Fetch subscription details from Razorpay for the billing period
        var details       = await RazorpayGetAsync($"/subscriptions/{req.RazorpaySubscriptionId}");
        var periodStart   = DateTimeOffset.FromUnixTimeSeconds(details.GetProperty("current_start").GetInt64()).UtcDateTime;
        var periodEnd     = DateTimeOffset.FromUnixTimeSeconds(details.GetProperty("current_end").GetInt64()).UtcDateTime;
        var rzpCustomerId = details.TryGetProperty("customer_id", out var cid) ? cid.GetString() : null;

        var planStr = details.GetProperty("plan_id").GetString() ?? "";
        var plan    = ResolvePlanFromRazorpayPlanId(planStr);

        // Update subscription record
        var subFilter = Builders<UserSubscription>.Filter.Eq(s => s.RazorpaySubscriptionId, req.RazorpaySubscriptionId);
        var subUpdate  = Builders<UserSubscription>.Update
            .Set(s => s.Status,              "active")
            .Set(s => s.Plan,                plan)
            .Set(s => s.CurrentPeriodStart,  periodStart)
            .Set(s => s.CurrentPeriodEnd,    periodEnd)
            .Set(s => s.RazorpayCustomerId,  rzpCustomerId)
            .Set(s => s.UpdatedAt,           DateTime.UtcNow);
        await _context.UserSubscriptions.UpdateOneAsync(subFilter, subUpdate, new UpdateOptions { IsUpsert = true });

        // Upgrade user plan
        await ActivatePlanForUserAsync(userId, plan);

        // Send activation confirmation email
        _ = SendPaymentEmailAsync(userId, "activated", plan, periodEnd);

        return new SubscriptionStatusDto(plan.ToString(), "active", periodEnd, false);
    }

    // ── Status ───────────────────────────────────────────────────────────────

    public async Task<SubscriptionStatusDto?> GetStatusAsync(string userId)
    {
        var sub = await _context.UserSubscriptions
            .Find(s => s.UserId == userId && s.Status == "active")
            .SortByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (sub == null) return null;

        return new SubscriptionStatusDto(
            sub.Plan.ToString(), sub.Status,
            sub.CurrentPeriodEnd, sub.CancelAtPeriodEnd);
    }

    // ── Cancel ───────────────────────────────────────────────────────────────

    public async Task CancelAsync(string userId, bool immediately)
    {
        var sub = await _context.UserSubscriptions
            .Find(s => s.UserId == userId && s.Status == "active")
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("No active subscription found");

        var body = new { cancel_at_cycle_end = immediately ? 0 : 1 };
        await RazorpayPostAsync($"/subscriptions/{sub.RazorpaySubscriptionId}/cancel", body);

        var update = Builders<UserSubscription>.Update
            .Set(s => s.CancelAtPeriodEnd, !immediately)
            .Set(s => s.Status,            immediately ? "cancelled" : "active")
            .Set(s => s.UpdatedAt,         DateTime.UtcNow);
        await _context.UserSubscriptions.UpdateOneAsync(s => s.Id == sub.Id, update);

        if (immediately)
        {
            await DowngradeUserToFreeAsync(userId);
            _ = SendPaymentEmailAsync(userId, "cancelled", sub.Plan, sub.CurrentPeriodEnd);
        }
        else
        {
            _ = SendPaymentEmailAsync(userId, "cancel_scheduled", sub.Plan, sub.CurrentPeriodEnd);
        }
    }

    // ── Webhook ──────────────────────────────────────────────────────────────

    public async Task HandleWebhookAsync(string payload, string signature)
    {
        var computed = ComputeHmac(payload, WebhookSecret);
        if (!string.Equals(computed, signature, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Webhook signature invalid");

        using var doc   = JsonDocument.Parse(payload);
        var eventName   = doc.RootElement.GetProperty("event").GetString();
        var entity      = doc.RootElement.GetProperty("payload").GetProperty("subscription").GetProperty("entity");
        var subId       = entity.GetProperty("id").GetString() ?? "";

        var sub = await _context.UserSubscriptions
            .Find(s => s.RazorpaySubscriptionId == subId)
            .FirstOrDefaultAsync();
        if (sub == null) return;

        switch (eventName)
        {
            case "subscription.charged":
            {
                var periodEnd = DateTimeOffset.FromUnixTimeSeconds(entity.GetProperty("current_end").GetInt64()).UtcDateTime;
                await _context.UserSubscriptions.UpdateOneAsync(s => s.Id == sub.Id,
                    Builders<UserSubscription>.Update
                        .Set(s => s.Status,           "active")
                        .Set(s => s.CurrentPeriodEnd, periodEnd)
                        .Set(s => s.UpdatedAt,        DateTime.UtcNow));
                await ResetCreditsForUserAsync(sub.UserId, sub.Plan);
                _ = SendPaymentEmailAsync(sub.UserId, "renewed", sub.Plan, periodEnd);
                break;
            }
            case "subscription.cancelled":
            case "subscription.completed":
            {
                await _context.UserSubscriptions.UpdateOneAsync(s => s.Id == sub.Id,
                    Builders<UserSubscription>.Update
                        .Set(s => s.Status,    "cancelled")
                        .Set(s => s.UpdatedAt, DateTime.UtcNow));
                await DowngradeUserToFreeAsync(sub.UserId);
                _ = SendPaymentEmailAsync(sub.UserId, "cancelled", sub.Plan, sub.CurrentPeriodEnd);
                break;
            }
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task ActivatePlanForUserAsync(string userId, PlanType plan)
    {
        // Update user plan
        await _context.Users.UpdateOneAsync(u => u.Id == userId,
            Builders<User>.Update.Set(u => u.Plan, plan).Set(u => u.UpdatedAt, DateTime.UtcNow));

        // Update all BookCredits owned by this user
        var books = await _context.ExpenseBooks.Find(b => b.UserId == userId).ToListAsync();
        var monthlyCredits = PlanLimits.MonthlyCredits(plan);

        foreach (var book in books)
        {
            var update = Builders<BookCredits>.Update
                .Set(bc => bc.PlanType,          plan)
                .Set(bc => bc.FreeCreditsLimit,  monthlyCredits)
                .Set(bc => bc.FreeCreditsLeft,   monthlyCredits)
                .Set(bc => bc.UpdatedAt,         DateTime.UtcNow);
            await _context.BookCredits.UpdateOneAsync(bc => bc.ExpenseBookId == book.Id, update);
        }
    }

    private async Task DowngradeUserToFreeAsync(string userId)
    {
        await _context.Users.UpdateOneAsync(u => u.Id == userId,
            Builders<User>.Update.Set(u => u.Plan, PlanType.Free).Set(u => u.UpdatedAt, DateTime.UtcNow));

        var books = await _context.ExpenseBooks.Find(b => b.UserId == userId).ToListAsync();
        foreach (var book in books)
        {
            var update = Builders<BookCredits>.Update
                .Set(bc => bc.PlanType,         PlanType.Free)
                .Set(bc => bc.FreeCreditsLimit, 0)
                .Set(bc => bc.UpdatedAt,        DateTime.UtcNow);
            await _context.BookCredits.UpdateOneAsync(bc => bc.ExpenseBookId == book.Id, update);
        }
    }

    private async Task ResetCreditsForUserAsync(string userId, PlanType plan)
    {
        var monthlyCredits = PlanLimits.MonthlyCredits(plan);
        var books          = await _context.ExpenseBooks.Find(b => b.UserId == userId).ToListAsync();
        var now            = DateTime.UtcNow;

        foreach (var book in books)
        {
            var update = Builders<BookCredits>.Update
                .Set(bc => bc.FreeCreditsLeft, monthlyCredits)
                .Set(bc => bc.LastResetDate,   now)
                .Set(bc => bc.UpdatedAt,       now);
            await _context.BookCredits.UpdateOneAsync(bc => bc.ExpenseBookId == book.Id, update);
        }
    }

    private PlanType ResolvePlanFromRazorpayPlanId(string razorpayPlanId)
    {
        var starterPlanId = _config["Razorpay:Plans:Starter"] ?? "";
        var proPlanId     = _config["Razorpay:Plans:Pro"]     ?? "";

        if (razorpayPlanId == proPlanId)     return PlanType.Pro;
        if (razorpayPlanId == starterPlanId) return PlanType.Starter;
        return PlanType.Free;
    }

    // ── Email notifications ───────────────────────────────────────────────────

    private async Task SendPaymentEmailAsync(string userId, string eventType, PlanType plan, DateTime? periodEnd)
    {
        try
        {
            var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user?.Email == null) return;

            var planName   = plan.ToString();
            var periodStr  = periodEnd.HasValue ? periodEnd.Value.ToString("dd MMM yyyy") : "N/A";
            var appName    = "NidhiWise";

            var (subject, body) = eventType switch
            {
                "activated" => (
                    $"Welcome to {planName} Plan – {appName}",
                    $"Hi {user.Name},\n\nYour {planName} plan is now active. Your subscription renews on {periodStr}.\n\nEnjoy your upgraded features!\n\nThe {appName} Team"
                ),
                "renewed" => (
                    $"Subscription Renewed – {appName}",
                    $"Hi {user.Name},\n\nYour {planName} plan has been renewed. Your next billing date is {periodStr}.\n\nThank you for continuing with {appName}!\n\nThe {appName} Team"
                ),
                "cancel_scheduled" => (
                    $"Subscription Cancellation Scheduled – {appName}",
                    $"Hi {user.Name},\n\nYour {planName} subscription is scheduled to cancel at the end of the current period on {periodStr}. You will continue to have access until then.\n\nIf you change your mind, you can re-subscribe at any time.\n\nThe {appName} Team"
                ),
                "cancelled" => (
                    $"Subscription Cancelled – {appName}",
                    $"Hi {user.Name},\n\nYour {planName} subscription has been cancelled. Your account has been moved to the Free plan.\n\nWe hope to see you again soon.\n\nThe {appName} Team"
                ),
                _ => (null, null)
            };

            if (subject == null || body == null) return;

            var variables = new Dictionary<string, string>
            {
                ["user_name"]    = user.Name ?? "there",
                ["plan_name"]    = planName,
                ["period_end"]   = periodStr,
                ["company_name"] = appName,
            };

            await _messaging.SendEmailAsync(user.Email, subject, body, variables);
        }
        catch
        {
            // Email failure must never break the payment flow
        }
    }

    // ── Razorpay HTTP helpers ─────────────────────────────────────────────────

    private HttpClient CreateRazorpayClient()
    {
        var client     = _httpFactory.CreateClient("Razorpay");
        var authValue  = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{KeyId}:{KeySecret}"));
        client.BaseAddress = new Uri("https://api.razorpay.com/v1/");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
        return client;
    }

    private async Task<JsonElement> RazorpayPostAsync(string path, object body)
    {
        var client   = CreateRazorpayClient();
        var json     = JsonSerializer.Serialize(body);
        var content  = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(path.TrimStart('/'), content);
        var raw      = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Razorpay error ({response.StatusCode}): {raw}");
        return JsonDocument.Parse(raw).RootElement;
    }

    private async Task<JsonElement> RazorpayGetAsync(string path)
    {
        var client   = CreateRazorpayClient();
        var response = await client.GetAsync(path.TrimStart('/'));
        var raw      = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Razorpay error ({response.StatusCode}): {raw}");
        return JsonDocument.Parse(raw).RootElement;
    }

    private static string ComputeHmac(string payload, string secret)
    {
        var key  = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(key);
        return BitConverter.ToString(hmac.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
    }
}
