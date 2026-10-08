using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class Session
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string UserId { get; set; } = string.Empty;

    // Shared across a login and every session it's rotated into — lets reuse-detection
    // revoke just this token lineage instead of every session the user has.
    [BsonElement("familyId")]
    public string FamilyId { get; set; } = string.Empty;

    [BsonElement("refreshTokenHash")]
    public string RefreshTokenHash { get; set; } = string.Empty;

    [BsonElement("userAgent")]
    public string UserAgent { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("lastUsedAt")]
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

    // TTL index on this field — Mongo auto-deletes the row once it's past its refresh-token expiry
    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("revokedAt")]
    [BsonIgnoreIfNull]
    public DateTime? RevokedAt { get; set; }

    // "user_logout" | "user_logout_all" | "admin_revoke" | "rotated" | "reuse_detected"
    [BsonElement("revokedReason")]
    [BsonIgnoreIfNull]
    public string? RevokedReason { get; set; }

    // Points to the session row created when this one was rotated during /Auth/refresh
    [BsonElement("replacedBySessionId")]
    [BsonIgnoreIfNull]
    public string? ReplacedBySessionId { get; set; }
}
