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
}
