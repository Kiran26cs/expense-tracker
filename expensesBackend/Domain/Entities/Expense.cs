using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class Expense
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

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("date")]
    public DateTime Date { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } = "expense";

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;

    [BsonElement("paymentMethod")]
    public string PaymentMethod { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("receiptUrl")]
    public string? ReceiptUrl { get; set; }

    [BsonElement("isRecurring")]
    public bool IsRecurring { get; set; }

    [BsonElement("recurringId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? RecurringId { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
