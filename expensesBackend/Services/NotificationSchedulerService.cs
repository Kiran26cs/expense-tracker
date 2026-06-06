using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;
using WebPush;

namespace ExpensesBackend.API.Services;

public class NotificationSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationSchedulerService> _logger;

    public NotificationSchedulerService(IServiceScopeFactory scopeFactory, ILogger<NotificationSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun();
            _logger.LogInformation("Next payment notification run scheduled in {Minutes:F0} minutes", delay.TotalMinutes);
            await Task.Delay(delay, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
                await RunNotificationsAsync(stoppingToken);
        }
    }

    private static TimeSpan GetDelayUntilNextRun()
    {
        var now = DateTime.UtcNow;
        var nextRun = now.Date.AddHours(8);
        if (now >= nextRun)
            nextRun = nextRun.AddDays(1);
        return nextRun - now;
    }

    private async Task RunNotificationsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Running payment notification scheduler");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var pushService = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
        var messagingService = scope.ServiceProvider.GetRequiredService<IMessagingService>();

        var today = DateTime.UtcNow.Date;

        var fiveDayPayments = await GetPaymentsInWindowAsync(db, today.AddDays(5), today.AddDays(6), ct);
        var oneDayPayments  = await GetPaymentsInWindowAsync(db, today.AddDays(1), today.AddDays(2), ct);

        _logger.LogInformation("Found {FiveDay} 5-day and {OneDay} 1-day upcoming payments across all users",
            fiveDayPayments.Count, oneDayPayments.Count);

        // Group by (userId, expenseBookId) — one notification per user per book per window.
        // Payments with no book (ExpenseBookId is null) are grouped together under a null key.
        foreach (var group in fiveDayPayments.GroupBy(p => (p.UserId, p.ExpenseBookId)))
            await NotifyBookAsync(db, pushService, messagingService, group.Key.UserId, group.Key.ExpenseBookId, group.ToList(), "5day", ct);

        foreach (var group in oneDayPayments.GroupBy(p => (p.UserId, p.ExpenseBookId)))
            await NotifyBookAsync(db, pushService, messagingService, group.Key.UserId, group.Key.ExpenseBookId, group.ToList(), "1day", ct);
    }

    private static async Task<List<UpcomingPayment>> GetPaymentsInWindowAsync(
        MongoDbContext db, DateTime from, DateTime to, CancellationToken ct)
    {
        return await db.UpcomingPayments
            .Find(p => p.DueDate >= from && p.DueDate < to)
            .ToListAsync(ct);
    }

    private async Task NotifyBookAsync(
        MongoDbContext db,
        IPushNotificationService pushService,
        IMessagingService messagingService,
        string userId,
        string? expenseBookId,
        List<UpcomingPayment> payments,
        string notificationType,
        CancellationToken ct)
    {
        // Dedup: already sent this window type to this user+book today?
        var today = DateTime.UtcNow.Date;
        var alreadySent = await db.NotificationLogs
            .Find(l => l.UserId == userId
                    && l.ExpenseBookId == expenseBookId
                    && l.NotificationType == notificationType
                    && l.SentAt >= today)
            .AnyAsync(ct);
        if (alreadySent) return;

        var user = await db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync(ct);
        if (user == null) return;

        // Resolve book name and deep-link URL
        string bookLabel = string.Empty;
        string deepLinkUrl = "/app";

        if (!string.IsNullOrEmpty(expenseBookId))
        {
            var book = await db.ExpenseBooks.Find(b => b.Id == expenseBookId).FirstOrDefaultAsync(ct);
            if (book == null)
            {
                _logger.LogInformation("Skipping notification — expense book {BookId} no longer exists", expenseBookId);
                return;
            }
            bookLabel   = book.Name;
            deepLinkUrl = $"/{expenseBookId}/dashboard";
        }

        var daysLabel = notificationType == "5day" ? "5 days" : "tomorrow";

        string title, body;
        if (payments.Count == 1)
        {
            var p    = payments[0];
            var desc = !string.IsNullOrWhiteSpace(p.Description) ? p.Description : p.Category;
            title = "Upcoming Payment Reminder";
            body  = string.IsNullOrEmpty(bookLabel)
                ? $"{desc} — {p.Amount:F2} due in {daysLabel}."
                : $"{desc} — {p.Amount:F2} due in {daysLabel} · {bookLabel}";
        }
        else
        {
            var total = payments.Sum(p => p.Amount);
            title = "Upcoming Payment Reminders";
            body  = string.IsNullOrEmpty(bookLabel)
                ? $"{payments.Count} payments due in {daysLabel} — total {total:F2}."
                : $"{payments.Count} payments due in {daysLabel} · {bookLabel} — total {total:F2}.";
        }

        var subscriptions = await db.PushSubscriptions
            .Find(s => s.UserId == userId)
            .ToListAsync(ct);

        bool notified = false;
        var channel = "email";

        if (subscriptions.Count > 0)
        {
            foreach (var sub in subscriptions)
            {
                try
                {
                    var success = await pushService.SendAsync(sub.Endpoint, sub.P256dh, sub.Auth, title, body, deepLinkUrl);
                    if (success)
                    {
                        notified = true;
                        channel = "push";
                    }
                }
                catch (WebPushException ex) when (
                    ex.StatusCode == System.Net.HttpStatusCode.Gone ||
                    ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("Removing stale push subscription for user {UserId}", userId);
                    await db.PushSubscriptions.DeleteOneAsync(s => s.Id == sub.Id, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Push failed for user {UserId}", userId);
                }
            }
        }

        if (!notified && !string.IsNullOrEmpty(user.Email))
        {
            var emailBody = BuildEmailBody(user.Name, bookLabel, payments, daysLabel);
            var sent = await messagingService.SendEmailAsync(user.Email, title, emailBody);
            if (sent)
            {
                notified = true;
                channel = "email";
            }
        }

        if (notified)
        {
            await db.NotificationLogs.InsertOneAsync(new NotificationLog
            {
                UserId           = userId,
                ExpenseBookId    = expenseBookId,
                NotificationType = notificationType,
                Channel          = channel,
                SentAt           = DateTime.UtcNow
            }, cancellationToken: ct);

            _logger.LogInformation(
                "Notified user {UserId} via {Channel} — {Count} payment(s) in {Type} window for book {BookId}",
                userId, channel, payments.Count, notificationType, expenseBookId ?? "none");
        }
        else
        {
            _logger.LogWarning(
                "Could not notify user {UserId} for book {BookId} — no push subscription and no email",
                userId, expenseBookId ?? "none");
        }
    }

    private static string BuildEmailBody(string userName, string bookLabel, List<UpcomingPayment> payments, string daysLabel)
    {
        var bookLine = string.IsNullOrEmpty(bookLabel) ? string.Empty : $" in {bookLabel}";

        var paymentLines = payments.Select(p =>
        {
            var desc = !string.IsNullOrWhiteSpace(p.Description) ? p.Description : p.Category;
            return $"  • {desc} ({p.Category}) — {p.Amount:F2}  |  Due {p.DueDate:MMMM dd, yyyy}";
        });

        var total = payments.Sum(p => p.Amount);
        var totalLine = payments.Count > 1 ? $"\n  Total: {total:F2}\n" : string.Empty;

        return $"""
            Hi {userName},

            You have {payments.Count} payment{(payments.Count > 1 ? "s" : string.Empty)} due in {daysLabel}{bookLine}:

            {string.Join("\n", paymentLines)}
            {totalLine}
            Log in to NidhiWise to mark payments as paid or manage your recurring payments.

              https://app.nidhiwise.com/app

            — The NidhiWise Team
            """;
    }
}
