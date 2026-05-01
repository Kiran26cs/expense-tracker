using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class ExpenseBook
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty; // Personal, Work, or custom

    [BsonElement("currency")]
    public string Currency { get; set; } = "USD";

    [BsonElement("color")]
    public string? Color { get; set; }

    [BsonElement("icon")]
    public string? Icon { get; set; }

    [BsonElement("monthlySavingsGoal")]
    public decimal MonthlySavingsGoal { get; set; } = 0;

    [BsonElement("isDefault")]
    public bool IsDefault { get; set; } = false; // One default book per user

    [BsonElement("totalExpenses")]
    public decimal TotalExpenses { get; set; } = 0;

    [BsonElement("expenseCount")]
    public int ExpenseCount { get; set; } = 0;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
