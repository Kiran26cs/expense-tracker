using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class NotificationLog
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("upcomingPaymentId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? UpcomingPaymentId { get; set; }

    [BsonElement("expenseBookId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? ExpenseBookId { get; set; }

    /// <summary>
    /// "5day" or "1day" — which reminder window triggered this notification
    /// </summary>
    [BsonElement("notificationType")]
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// "push" or "email" — which channel successfully delivered
    /// </summary>
    [BsonElement("channel")]
    public string Channel { get; set; } = string.Empty;

    [BsonElement("sentAt")]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
