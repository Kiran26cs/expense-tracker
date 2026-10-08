using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class OtpRecord
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    // Namespaces this OTP to its auth flow ("user" or "platform-admin") so the public user-facing
    // send-otp endpoint can never clear/consume an OTP that belongs to the admin login flow (or
    // vice versa), even when both flows share an email address.
    [BsonElement("purpose")]
    public string Purpose { get; set; } = "user";

    [BsonElement("otp")]
    public string Otp { get; set; } = string.Empty;

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("attempts")]
    public int Attempts { get; set; } = 0;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("verified")]
    public bool Verified { get; set; } = false;
}
