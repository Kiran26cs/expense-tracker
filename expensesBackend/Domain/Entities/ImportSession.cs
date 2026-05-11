using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class ImportSession
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("expenseBookId")]
    public string ExpenseBookId { get; set; } = string.Empty;

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("fileName")]
    public string FileName { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = ImportStatus.Queued;

    [BsonElement("totalRecords")]
    public int TotalRecords { get; set; }

    [BsonElement("processedCount")]
    public int ProcessedCount { get; set; }

    [BsonElement("successCount")]
    public int SuccessCount { get; set; }

    [BsonElement("failedCount")]
    public int FailedCount { get; set; }

    [BsonElement("records")]
    public List<ImportRecord> Records { get; set; } = [];

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // TTL index is set on this field — MongoDB auto-deletes 24h after it is set
    [BsonElement("completedAt")]
    [BsonIgnoreIfNull]
    public DateTime? CompletedAt { get; set; }
}

public static class ImportStatus
{
    public const string Queued             = "queued";
    public const string Processing         = "processing";
    public const string Completed          = "completed";
    public const string CompletedWithErrors = "completedWithErrors";
    public const string Failed             = "failed";
}

public class ImportRecord
{
    [BsonElement("rowNumber")]
    public int RowNumber { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;

    // Full row snapshot — retained for retry without re-uploading the CSV
    [BsonElement("date")]
    public string Date { get; set; } = string.Empty;

    [BsonElement("paymentMethod")]
    public string PaymentMethod { get; set; } = string.Empty;

    [BsonElement("notes")]
    [BsonIgnoreIfNull]
    public string? Notes { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } = "expense";

    [BsonElement("currency")]
    [BsonIgnoreIfNull]
    public string? Currency { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = ImportRecordStatus.Pending;

    [BsonElement("errorMessage")]
    [BsonIgnoreIfNull]
    public string? ErrorMessage { get; set; }
}

public static class ImportRecordStatus
{
    public const string Pending = "pending";
    public const string Success = "success";
    public const string Failed  = "failed";
}
