namespace ExpensesBackend.API.Domain.DTOs;

public class CreateExpenseBookRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "Personal"; // Default to Personal
    public string Currency { get; set; } = "USD";
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool IsDefault { get; set; } = false;
}

public class UpdateExpenseBookRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool IsDefault { get; set; } = false;
}

public class ExpenseBookResponse
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool IsDefault { get; set; }
    public decimal TotalExpenses { get; set; }
    public int ExpenseCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ExpenseBookCategoryResponse
{
    public List<string> Categories { get; set; } = new();
}
