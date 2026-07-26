using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class AdminAuditLog
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("adminId")]
    public string AdminId { get; set; } = string.Empty;

    [BsonElement("adminEmail")]
    public string AdminEmail { get; set; } = string.Empty;

    [BsonElement("action")]
    public string Action { get; set; } = string.Empty;

    // "book" | "user" | "cache" | "job" | "admin"
    [BsonElement("targetType")]
    public string TargetType { get; set; } = string.Empty;

    [BsonElement("targetId")]
    [BsonIgnoreIfNull]
    public string? TargetId { get; set; }

    // Freeform payload — stores what changed (amounts, old/new values, etc.)
    [BsonElement("payload")]
    [BsonIgnoreIfNull]
    public Dictionary<string, object>? Payload { get; set; }

    // Human-readable summary shown in the audit feed
    [BsonElement("summary")]
    public string Summary { get; set; } = string.Empty;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public static class AdminActions
{
    public const string GrantCredits      = "grant_credits";
    public const string ChangePlan        = "change_plan";
    public const string DeactivateUser    = "deactivate_user";
    public const string ReactivateUser    = "reactivate_user";
    public const string ToggleAiChat      = "toggle_ai_chat";
    public const string DeleteBook        = "delete_book";
    public const string CacheInvalidate   = "cache_invalidate";
    public const string CacheFlush        = "cache_flush";
    public const string TriggerCreditReset = "trigger_credit_reset";
    public const string TriggerNotifications = "trigger_notifications";
    public const string RetryImport       = "retry_import";
    public const string CreateAdmin       = "create_admin";
    public const string UpdateAdmin       = "update_admin";
    public const string DeactivateAdmin   = "deactivate_admin";
}
