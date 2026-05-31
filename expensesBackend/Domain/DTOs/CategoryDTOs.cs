namespace ExpensesBackend.API.Domain.DTOs;

public class CategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "expense";
    public string Icon { get; set; } = "fa-solid fa-tag";
    public string Color { get; set; } = "#6366f1";
    public bool IsDefault { get; set; }
    /// <summary>"need" | "want" | "debt" | null</summary>
    public string? FinancialClass { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCategoryRequest
{
    public string? ExpenseBookId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "expense";
    public string Icon { get; set; } = "fa-solid fa-tag";
    public string Color { get; set; } = "#6366f1";
    /// <summary>"need" | "want" | "debt" | null (auto-detected from name if omitted)</summary>
    public string? FinancialClass { get; set; }
}

public class UpdateCategoryRequest
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    /// <summary>"need" | "want" | "debt" | null (clears classification)</summary>
    public string? FinancialClass { get; set; }
    public bool ClearFinancialClass { get; set; }
}

public class ImportCategoriesRequest
{
    public string? ExpenseBookId { get; set; }
    public List<CreateCategoryRequest> Categories { get; set; } = new();
}

public class ImportCategoriesResponse
{
    public int Imported { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}
