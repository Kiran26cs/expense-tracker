using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class PagePermissions
{
    [BsonElement("dashboard")]
    public string? Dashboard { get; set; }   // "view" | "none" | null (inherit role default)

    [BsonElement("expenses")]
    public string? Expenses { get; set; }    // "write" | "view" | "none" | null

    [BsonElement("budgets")]
    public string? Budgets { get; set; }     // "write" | "view" | "none" | null

    [BsonElement("settings")]
    public string? Settings { get; set; }    // "write" | "view" | "none" | null

    [BsonElement("insights")]
    public string? Insights { get; set; }    // "view" | "none" | null
}

public class ExpenseBookMember
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("expenseBookId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string ExpenseBookId { get; set; } = string.Empty;

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? UserId { get; set; }   // null until invite is accepted

    [BsonElement("invitedEmail")]
    public string InvitedEmail { get; set; } = string.Empty;

    [BsonElement("inviteToken")]
    public string? InviteToken { get; set; }   // nulled after acceptance

    [BsonElement("inviteStatus")]
    public string InviteStatus { get; set; } = "pending";  // pending | accepted | revoked

    [BsonElement("role")]
    public string Role { get; set; } = "member";  // owner | admin | member | viewer

    [BsonElement("permissions")]
    public PagePermissions? Permissions { get; set; }  // null = use role defaults

    [BsonElement("allowedCategoryIds")]
    public List<string> AllowedCategoryIds { get; set; } = [];

    [BsonElement("canDeleteExpenses")]
    public bool CanDeleteExpenses { get; set; } = false;

    [BsonElement("addedBy")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string AddedBy { get; set; } = string.Empty;

    [BsonElement("addedAt")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;

    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; set; }
}
