using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class BookCredits
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("expenseBookId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string ExpenseBookId { get; set; } = string.Empty;

    [BsonElement("freeCreditsLeft")]
    public int FreeCreditsLeft { get; set; } = 50;

    [BsonElement("paidCreditsLeft")]
    public int PaidCreditsLeft { get; set; } = 0;

    [BsonElement("freeCreditsLimit")]
    public int FreeCreditsLimit { get; set; } = 50;

    [BsonElement("lastResetDate")]
    public DateTime LastResetDate { get; set; } = DateTime.UtcNow;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
