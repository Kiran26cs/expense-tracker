using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class Lending
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("expenseBookId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string ExpenseBookId { get; set; } = string.Empty;

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("borrowerName")]
    public string BorrowerName { get; set; } = string.Empty;

    [BsonElement("borrowerContact")]
    public string? BorrowerContact { get; set; }

    [BsonElement("principalAmount")]
    public decimal PrincipalAmount { get; set; }

    [BsonElement("annualInterestRate")]
    public decimal AnnualInterestRate { get; set; } = 0;

    [BsonElement("startDate")]
    public DateTime StartDate { get; set; }

    [BsonElement("dueDate")]
    public DateTime? DueDate { get; set; }

    // Denormalized summary — updated on every repayment write
    [BsonElement("totalRepaid")]
    public decimal TotalRepaid { get; set; } = 0;

    [BsonElement("outstandingPrincipal")]
    public decimal OutstandingPrincipal { get; set; }

    [BsonElement("repaymentCount")]
    public int RepaymentCount { get; set; } = 0;

    [BsonElement("status")]
    public string Status { get; set; } = "active"; // active | settled

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;

    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; set; }
}
