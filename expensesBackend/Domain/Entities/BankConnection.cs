using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class BankConnection
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    // HDFC | ICICI | SBI | Axis | Kotak | Other
    [BsonElement("bankName")]
    public string BankName { get; set; } = string.Empty;

    [BsonElement("accountMask")]
    [BsonIgnoreIfNull]
    public string? AccountMask { get; set; }

    // "manual" | "auto" | "disabled"
    [BsonElement("mode")]
    public string Mode { get; set; } = "manual";

    // "manual" | "setu" | "finvu"
    [BsonElement("provider")]
    public string Provider { get; set; } = "manual";

    // AA consent token — populated only in auto mode
    [BsonElement("consentHandle")]
    [BsonIgnoreIfNull]
    public string? ConsentHandle { get; set; }

    [BsonElement("consentExpiry")]
    [BsonIgnoreIfNull]
    public DateTime? ConsentExpiry { get; set; }

    [BsonElement("lastSyncedAt")]
    [BsonIgnoreIfNull]
    public DateTime? LastSyncedAt { get; set; }

    // "daily" | "weekly" | "on_demand"
    [BsonElement("autoSyncSchedule")]
    public string AutoSyncSchedule { get; set; } = "on_demand";

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
