using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class UpcomingPayment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("expenseBookId")]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfDefault]
    public string? ExpenseBookId { get; set; }

    [BsonElement("recurringExpenseId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string RecurringExpenseId { get; set; } = string.Empty;

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;

    [BsonElement("paymentMethod")]
    public string PaymentMethod { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("frequency")]
    public string Frequency { get; set; } = "monthly";

    [BsonElement("dueDate")]
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Status: upcoming, due, overdue, pending
    /// - upcoming: due in the future (more than 1 day away)
    /// - due: due today
    /// - overdue: past due date and unpaid
    /// - pending: unpaid for more than a week
    /// </summary>
    [BsonElement("status")]
    public string Status { get; set; } = "upcoming";

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
