using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetCategoriesAsync(string userId);
    Task<CategoryDto> GetCategoryByIdAsync(string userId, string categoryId);
    Task<CategoryDto> CreateCategoryAsync(string userId, CreateCategoryRequest request);
    Task<CategoryDto> UpdateCategoryAsync(string userId, string categoryId, UpdateCategoryRequest request);
    Task<bool> DeleteCategoryAsync(string userId, string categoryId);
    Task<ImportCategoriesResponse> ImportCategoriesAsync(string userId, ImportCategoriesRequest request);
    Task SeedDefaultCategoriesAsync(string userId);
}
