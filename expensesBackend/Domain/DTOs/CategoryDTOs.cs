namespace ExpensesBackend.API.Domain.DTOs;

public class CategoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "expense";
    public string Icon { get; set; } = "fa-solid fa-tag";
    public string Color { get; set; } = "#6366f1";
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "expense";
    public string Icon { get; set; } = "fa-solid fa-tag";
    public string Color { get; set; } = "#6366f1";
}

public class UpdateCategoryRequest
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
}

public class ImportCategoriesRequest
{
    public List<CreateCategoryRequest> Categories { get; set; } = new();
}

public class ImportCategoriesResponse
{
    public int Imported { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}
