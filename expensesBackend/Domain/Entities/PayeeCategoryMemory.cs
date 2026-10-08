using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

/// <summary>
/// Remembers which category a given bank-statement payee (UPI VPA, or NEFT/RTGS/IMPS
/// beneficiary name) was last assigned within a book. Future bank-sync imports of the same
/// recurring payee reuse this instead of asking the AI classifier again, and it's kept up to
/// date whenever the user manually re-categorizes a matching expense.
/// </summary>
public class PayeeCategoryMemory
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("expenseBookId")]
    public string ExpenseBookId { get; set; } = string.Empty;

    [BsonElement("payeeKey")]
    public string PayeeKey { get; set; } = string.Empty;

    [BsonElement("categoryId")]
    public string CategoryId { get; set; } = string.Empty;

    [BsonElement("categoryName")]
    public string CategoryName { get; set; } = string.Empty;

    [BsonElement("hitCount")]
    public int HitCount { get; set; } = 1;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
