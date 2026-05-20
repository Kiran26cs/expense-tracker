using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class UserSubscription
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("plan")]
    [BsonRepresentation(BsonType.String)]
    public PlanType Plan { get; set; } = PlanType.Free;

    [BsonElement("status")]
    public string Status { get; set; } = "created"; // created | active | cancelled | expired

    [BsonElement("razorpaySubscriptionId")]
    public string RazorpaySubscriptionId { get; set; } = string.Empty;

    [BsonElement("razorpayCustomerId")]
    public string? RazorpayCustomerId { get; set; }

    [BsonElement("currentPeriodStart")]
    public DateTime? CurrentPeriodStart { get; set; }

    [BsonElement("currentPeriodEnd")]
    public DateTime? CurrentPeriodEnd { get; set; }

    [BsonElement("cancelAtPeriodEnd")]
    public bool CancelAtPeriodEnd { get; set; } = false;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
