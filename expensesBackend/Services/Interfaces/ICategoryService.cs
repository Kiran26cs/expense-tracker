using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetCategoriesAsync(string expenseBookId);
    Task<CategoryDto> GetCategoryByIdAsync(string expenseBookId, string categoryId);
    Task<CategoryDto> CreateCategoryAsync(string expenseBookId, CreateCategoryRequest request);
    Task<CategoryDto> UpdateCategoryAsync(string expenseBookId, string categoryId, UpdateCategoryRequest request);
    Task<bool> DeleteCategoryAsync(string expenseBookId, string categoryId);
    Task<ImportCategoriesResponse> ImportCategoriesAsync(string expenseBookId, ImportCategoriesRequest request);
    Task SeedDefaultCategoriesAsync(string expenseBookId);
}
