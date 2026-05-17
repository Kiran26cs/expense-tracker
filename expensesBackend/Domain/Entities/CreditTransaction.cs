using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class CreditTransaction
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("expenseBookId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string ExpenseBookId { get; set; } = string.Empty;

    [BsonElement("triggeredByUserId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string TriggeredByUserId { get; set; } = string.Empty;

    // Negative = debit (ai_chat usage), Positive = credit (top-up / reset)
    [BsonElement("amount")]
    public int Amount { get; set; }

    // "ai_chat" | "monthly_reset" | "admin_grant"
    [BsonElement("reason")]
    public string Reason { get; set; } = string.Empty;

    [BsonElement("toolsUsed")]
    public List<string> ToolsUsed { get; set; } = [];

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
