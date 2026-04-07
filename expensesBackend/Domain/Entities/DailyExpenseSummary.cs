using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class DailyExpenseSummary
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("expenseBookId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    [BsonIgnoreIfDefault]
    public string? ExpenseBookId { get; set; }

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
