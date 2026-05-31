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

    [BsonElement("originalAmount")]
    [BsonIgnoreIfNull]
    public decimal? OriginalAmount { get; set; }

    [BsonElement("originalCurrency")]
    [BsonIgnoreIfNull]
    public string? OriginalCurrency { get; set; }

    /// <summary>Units of book currency per 1 unit of OriginalCurrency at the time of entry.</summary>
    [BsonElement("fxRate")]
    [BsonIgnoreIfNull]
    public decimal? FxRate { get; set; }

    [BsonElement("isRecurring")]
    public bool IsRecurring { get; set; }

    [BsonElement("recurringId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? RecurringId { get; set; }

    [BsonElement("receiptGroupId")]
    [BsonIgnoreIfNull]
    public string? ReceiptGroupId { get; set; }

    [BsonElement("receiptNumber")]
    [BsonIgnoreIfNull]
    public string? ReceiptNumber { get; set; }

    [BsonElement("isReceiptItem")]
    [BsonIgnoreIfDefault]
    public bool IsReceiptItem { get; set; }

    /// <summary>Tax amount captured from the receipt for reference only. Stored as its own expense entry (Option C).</summary>
    [BsonElement("taxAmount")]
    [BsonIgnoreIfNull]
    public decimal? TaxAmount { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
