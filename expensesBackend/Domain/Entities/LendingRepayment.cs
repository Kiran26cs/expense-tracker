using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class LendingRepayment
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("lendingId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string LendingId { get; set; } = string.Empty;

    [BsonElement("expenseBookId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string ExpenseBookId { get; set; } = string.Empty;

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("date")]
    public DateTime Date { get; set; }

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("recordedAt")]
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;

    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; set; }
}
