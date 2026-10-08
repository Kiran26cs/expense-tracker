using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

[BsonIgnoreExtraElements]
public class User
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("email")]
    public string? Email { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("currency")]
    public string Currency { get; set; } = "USD";

    [BsonElement("monthlyIncome")]
    public decimal MonthlyIncome { get; set; }

    [BsonElement("monthlySavingsGoal")]
    public decimal MonthlySavingsGoal { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("plan")]
    [BsonRepresentation(BsonType.String)]
    public PlanType Plan { get; set; } = PlanType.Free;

    [BsonElement("privacyPinHash")]
    public string? PrivacyPinHash { get; set; }

    [BsonElement("googleId")]
    [BsonIgnoreIfNull]
    public string? GoogleId { get; set; }

    [BsonElement("googleLinkedAt")]
    [BsonIgnoreIfNull]
    public DateTime? GoogleLinkedAt { get; set; }

    // Set the moment this account's owner completes an OTP-based login/signup — immediately
    // at creation for OTP-native signups, or after confirmation for a Google-native account's
    // first OTP login. Mirrors GoogleLinkedAt for the reverse linking direction.
    [BsonElement("otpLinkedAt")]
    [BsonIgnoreIfNull]
    public DateTime? OtpLinkedAt { get; set; }
}
