using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class BankSyncSession
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("bankConnectionId")]
    public string BankConnectionId { get; set; } = string.Empty;

    [BsonElement("bankName")]
    public string BankName { get; set; } = string.Empty;

    [BsonElement("detectedFormat")]
    public string DetectedFormat { get; set; } = string.Empty;

    [BsonElement("transactions")]
    public List<ParsedBankTransaction> Transactions { get; set; } = [];

    // "preview" | "confirmed"
    [BsonElement("status")]
    public string Status { get; set; } = "preview";

    [BsonElement("importSessionId")]
    [BsonIgnoreIfNull]
    public string? ImportSessionId { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // TTL index on this field — MongoDB auto-deletes 2h after creation
    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(2);
}

// Stored inside BankSyncSession — lightweight, no BsonId needed
public class ParsedBankTransaction
{
    [BsonElement("rowNumber")]
    public int RowNumber { get; set; }

    [BsonElement("date")]
    public DateTime Date { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    // "expense" (debit) | "income" (credit)
    [BsonElement("type")]
    public string Type { get; set; } = "expense";

    // SHA256(bookId+date+amount+description) — set at confirm time when bookId is known
    [BsonElement("externalTxnRef")]
    public string ExternalTxnRef { get; set; } = string.Empty;
}
