using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class BudgetVersion
{
    [BsonElement("versionNumber")]
    public int VersionNumber { get; set; }

    /// <summary>YYYY-MM string tracking which month this version was saved for (e.g. "2026-05").</summary>
    [BsonElement("effectivePeriod")]
    [BsonIgnoreIfDefault]
    public string? EffectivePeriod { get; set; }

    [BsonElement("effectiveDate")]
    public DateTime EffectiveDate { get; set; }

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Budget
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

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;

    // Stored as "limit" in MongoDB for backward compat; serializes to JSON as "amount" via camelCase policy
    [BsonElement("limit")]
    public decimal Amount { get; set; }

    [BsonElement("latestVersionNumber")]
    public int LatestVersionNumber { get; set; } = 0;

    [BsonElement("versions")]
    public List<BudgetVersion> Versions { get; set; } = [];

    [BsonElement("spent")]
    public decimal Spent { get; set; }

    [BsonElement("period")]
    public string Period { get; set; } = "monthly";

    [BsonElement("currency")]
    public string Currency { get; set; } = "USD";

    [BsonElement("alertThreshold")]
    public int AlertThreshold { get; set; } = 80;

    [BsonElement("startDate")]
    public DateTime StartDate { get; set; }

    [BsonElement("endDate")]
    public DateTime EndDate { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
