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

    // The untouched bank narration before BankNarrationParser cleaned Description down to
    // just a payee name. Kept for duplicate-fingerprinting (it carries the bank's own unique
    // transaction ref, which the cleaned name doesn't) and for payment-method detection.
    [BsonElement("rawDescription")]
    [BsonIgnoreIfNull]
    public string? RawDescription { get; set; }

    // Stable payee identifier (UPI VPA or NEFT/RTGS/IMPS beneficiary name) extracted from
    // RawDescription — null when the narration didn't match a recognized bank format.
    [BsonElement("payeeKey")]
    [BsonIgnoreIfNull]
    public string? PayeeKey { get; set; }

    // Suggested category (from payee memory or AI), shown in the preview step for the user to
    // review/edit before confirming. "Uncategorized" if nothing could be suggested.
    [BsonElement("category")]
    public string Category { get; set; } = "Uncategorized";

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    // "expense" (debit) | "income" (credit)
    [BsonElement("type")]
    public string Type { get; set; } = "expense";

    // SHA256(bookId+date+amount+rawDescription) — set at confirm time when bookId is known
    [BsonElement("externalTxnRef")]
    public string ExternalTxnRef { get; set; } = string.Empty;
}
