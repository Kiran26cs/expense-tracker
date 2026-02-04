using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class DailyExpenseSummary
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("date")]
    public DateTime Date { get; set; } // Date only, time set to 00:00:00

    [BsonElement("categorySpending")]
    public List<CategorySpending> CategorySpending { get; set; } = new();

    [BsonElement("totalSpent")]
    public decimal TotalSpent { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CategorySpending
{
    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("count")]
    public int Count { get; set; } // Number of transactions
}
