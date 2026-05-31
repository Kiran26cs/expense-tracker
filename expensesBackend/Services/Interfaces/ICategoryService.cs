using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetCategoriesAsync(string expenseBookId);
    Task<CategoryDto> GetCategoryByIdAsync(string expenseBookId, string categoryId);
    Task<CategoryDto> CreateCategoryAsync(string expenseBookId, string requestingUserId, CreateCategoryRequest request);
    Task<CategoryDto> UpdateCategoryAsync(string expenseBookId, string categoryId, UpdateCategoryRequest request);
    Task<bool> DeleteCategoryAsync(string expenseBookId, string categoryId, string requestingUserId);
    Task<ImportCategoriesResponse> ImportCategoriesAsync(string expenseBookId, string requestingUserId, ImportCategoriesRequest request);
    Task SeedDefaultCategoriesAsync(string expenseBookId);
    /// <summary>
    /// Classifies all untagged categories in the book using the rule table then AI fallback.
    /// Returns the number of categories that were classified.
    /// </summary>
    Task<int> BulkClassifyAsync(string expenseBookId);
}
