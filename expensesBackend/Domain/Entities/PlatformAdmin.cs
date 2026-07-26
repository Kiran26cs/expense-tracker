using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class PlatformAdmin
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    // Fine-grained permissions: "credits" | "users" | "books" | "cache" | "jobs" | "analytics" | "admins"
    [BsonElement("permissions")]
    public List<string> Permissions { get; set; } = [];

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    // adminId of the creator, or "system" for the bootstrapped first admin
    [BsonElement("grantedBy")]
    public string GrantedBy { get; set; } = string.Empty;

    [BsonElement("grantedAt")]
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("lastLoginAt")]
    [BsonIgnoreIfNull]
    public DateTime? LastLoginAt { get; set; }

    // Optional link to a regular User account (e.g. for the platform owner)
    [BsonElement("linkedUserId")]
    [BsonIgnoreIfNull]
    public string? LinkedUserId { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public static class AdminPermissions
{
    public const string Credits   = "credits";
    public const string Users     = "users";
    public const string Books     = "books";
    public const string Cache     = "cache";
    public const string Jobs      = "jobs";
    public const string Analytics = "analytics";
    public const string Admins    = "admins";

    public static readonly IReadOnlyList<string> All =
        [Credits, Users, Books, Cache, Jobs, Analytics, Admins];
}
