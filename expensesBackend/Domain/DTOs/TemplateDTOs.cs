using System.Text.Json.Serialization;

namespace ExpensesBackend.API.Domain.DTOs;

// ── JSON template models ────────────────────────────────────────────────────

public class TemplateData
{
    [JsonPropertyName("templateVersion")]
    public string TemplateVersion { get; set; } = "1.0";

    [JsonPropertyName("bookDefaults")]
    public TemplateBookDefaults BookDefaults { get; set; } = new();

    [JsonPropertyName("categories")]
    public List<TemplateCategoryItem> Categories { get; set; } = [];

    [JsonPropertyName("expenses")]
    public List<TemplateExpenseItem> Expenses { get; set; } = [];

    [JsonPropertyName("budgets")]
    public List<TemplateBudgetItem> Budgets { get; set; } = [];

    [JsonPropertyName("upcomingPayments")]
    public List<TemplateUpcomingPaymentItem> UpcomingPayments { get; set; } = [];

    [JsonPropertyName("lendings")]
    public List<TemplateLendingItem> Lendings { get; set; } = [];
}

public class TemplateBookDefaults
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("monthlySavingsGoal")]
    public decimal MonthlySavingsGoal { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "fa-solid fa-wallet";

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#6366f1";
}

public class TemplateCategoryItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "default";

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#6366f1";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "expense";
}

public class TemplateExpenseItem
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("daysAgo")]
    public int DaysAgo { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("paymentMethod")]
    public string PaymentMethod { get; set; } = "Cash";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "expense";

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public class TemplateBudgetItem
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("limitAmount")]
    public decimal LimitAmount { get; set; }

    [JsonPropertyName("alertThreshold")]
    public int AlertThreshold { get; set; } = 80;

    [JsonPropertyName("spentPercent")]
    public decimal SpentPercent { get; set; }
}

public class TemplateUpcomingPaymentItem
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("daysFromNow")]
    public int DaysFromNow { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("paymentMethod")]
    public string PaymentMethod { get; set; } = "Bank Transfer";

    [JsonPropertyName("frequency")]
    public string Frequency { get; set; } = "monthly";
}

public class TemplateLendingItem
{
    [JsonPropertyName("borrowerName")]
    public string BorrowerName { get; set; } = string.Empty;

    [JsonPropertyName("borrowerContact")]
    public string? BorrowerContact { get; set; }

    [JsonPropertyName("principalAmount")]
    public decimal PrincipalAmount { get; set; }

    [JsonPropertyName("annualInterestRate")]
    public decimal AnnualInterestRate { get; set; }

    [JsonPropertyName("daysAgo")]
    public int DaysAgo { get; set; }

    [JsonPropertyName("dueDaysFromNow")]
    public int DueDaysFromNow { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("repayments")]
    public List<TemplateRepaymentItem> Repayments { get; set; } = [];
}

public class TemplateRepaymentItem
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("daysAgo")]
    public int DaysAgo { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

// ── API request / response DTOs ─────────────────────────────────────────────

public class CreateTemplateBookRequest
{
    public string? Currency { get; set; }
}

public class StartTemplateBookResponse
{
    public string BookId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}

public class TemplateCreationJobPayload
{
    public string ImportSessionId { get; set; } = string.Empty;
    public string ExpenseBookId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
}
