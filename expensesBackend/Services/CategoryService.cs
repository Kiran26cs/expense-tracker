using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services;

public class CategoryService : ICategoryService
{
    private readonly MongoDbContext _context;

    public CategoryService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync(string userId)
    {
        var filter = Builders<Category>.Filter.Or(
            Builders<Category>.Filter.Eq(c => c.UserId, userId),
            Builders<Category>.Filter.Eq(c => c.IsDefault, true)
        );

        var categories = await _context.Categories
            .Find(filter)
            .SortBy(c => c.Name)
            .ToListAsync();

        // If user has no categories at all, seed defaults
        if (categories.Count == 0)
        {
            await SeedDefaultCategoriesAsync(userId);
            categories = await _context.Categories
                .Find(filter)
                .SortBy(c => c.Name)
                .ToListAsync();
        }

        return categories.Select(MapToDto).ToList();
    }

    public async Task<CategoryDto> GetCategoryByIdAsync(string userId, string categoryId)
    {
        var filter = Builders<Category>.Filter.And(
            Builders<Category>.Filter.Eq(c => c.Id, categoryId),
            Builders<Category>.Filter.Or(
                Builders<Category>.Filter.Eq(c => c.UserId, userId),
                Builders<Category>.Filter.Eq(c => c.IsDefault, true)
            )
        );

        var category = await _context.Categories.Find(filter).FirstOrDefaultAsync();
        if (category == null)
            throw new KeyNotFoundException("Category not found");

        return MapToDto(category);
    }

    public async Task<CategoryDto> CreateCategoryAsync(string userId, CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Category name is required");

        // Check for duplicate name
        var existing = await _context.Categories.Find(c =>
            c.UserId == userId && c.Name.ToLower() == request.Name.ToLower()
        ).FirstOrDefaultAsync();

        if (existing != null)
            throw new ArgumentException($"Category '{request.Name}' already exists");

        var category = new Category
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Type = request.Type ?? "expense",
            Icon = request.Icon ?? "fa-solid fa-tag",
            Color = request.Color ?? "#6366f1",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Categories.InsertOneAsync(category);
        return MapToDto(category);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(string userId, string categoryId, UpdateCategoryRequest request)
    {
        var filter = Builders<Category>.Filter.And(
            Builders<Category>.Filter.Eq(c => c.Id, categoryId),
            Builders<Category>.Filter.Eq(c => c.UserId, userId)
        );

        var category = await _context.Categories.Find(filter).FirstOrDefaultAsync();
        if (category == null)
            throw new KeyNotFoundException("Category not found or cannot be modified");

        var updates = new List<UpdateDefinition<Category>>();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            // Check for duplicate name
            var existing = await _context.Categories.Find(c =>
                c.UserId == userId && c.Name.ToLower() == request.Name.ToLower() && c.Id != categoryId
            ).FirstOrDefaultAsync();

            if (existing != null)
                throw new ArgumentException($"Category '{request.Name}' already exists");

            updates.Add(Builders<Category>.Update.Set(c => c.Name, request.Name.Trim()));
        }

        if (request.Type != null)
            updates.Add(Builders<Category>.Update.Set(c => c.Type, request.Type));

        if (request.Icon != null)
            updates.Add(Builders<Category>.Update.Set(c => c.Icon, request.Icon));

        if (request.Color != null)
            updates.Add(Builders<Category>.Update.Set(c => c.Color, request.Color));

        if (updates.Count > 0)
        {
            var combinedUpdate = Builders<Category>.Update.Combine(updates);
            await _context.Categories.UpdateOneAsync(filter, combinedUpdate);
        }

        var updated = await _context.Categories.Find(filter).FirstOrDefaultAsync();
        return MapToDto(updated!);
    }

    public async Task<bool> DeleteCategoryAsync(string userId, string categoryId)
    {
        var filter = Builders<Category>.Filter.And(
            Builders<Category>.Filter.Eq(c => c.Id, categoryId),
            Builders<Category>.Filter.Eq(c => c.UserId, userId),
            Builders<Category>.Filter.Eq(c => c.IsDefault, false)
        );

        var result = await _context.Categories.DeleteOneAsync(filter);
        if (result.DeletedCount == 0)
            throw new InvalidOperationException("Category not found or is a default category");

        return true;
    }

    public async Task<ImportCategoriesResponse> ImportCategoriesAsync(string userId, ImportCategoriesRequest request)
    {
        var response = new ImportCategoriesResponse();

        foreach (var catReq in request.Categories)
        {
            try
            {
                await CreateCategoryAsync(userId, catReq);
                response.Imported++;
            }
            catch (Exception ex)
            {
                response.Failed++;
                response.Errors.Add($"{catReq.Name}: {ex.Message}");
            }
        }

        return response;
    }

    public async Task SeedDefaultCategoriesAsync(string userId)
    {
        var defaults = new List<Category>
        {
            new() { UserId = userId, Name = "Food & Dining", Icon = "fa-solid fa-utensils", Color = "#ef4444", IsDefault = false },
            new() { UserId = userId, Name = "Transport", Icon = "fa-solid fa-car", Color = "#3b82f6", IsDefault = false },
            new() { UserId = userId, Name = "Shopping", Icon = "fa-solid fa-bag-shopping", Color = "#8b5cf6", IsDefault = false },
            new() { UserId = userId, Name = "Bills & Utilities", Icon = "fa-solid fa-bolt", Color = "#f59e0b", IsDefault = false },
            new() { UserId = userId, Name = "Entertainment", Icon = "fa-solid fa-film", Color = "#ec4899", IsDefault = false },
            new() { UserId = userId, Name = "Health", Icon = "fa-solid fa-heart-pulse", Color = "#10b981", IsDefault = false },
            new() { UserId = userId, Name = "Education", Icon = "fa-solid fa-graduation-cap", Color = "#6366f1", IsDefault = false },
            new() { UserId = userId, Name = "Rent", Icon = "fa-solid fa-house", Color = "#14b8a6", IsDefault = false },
            new() { UserId = userId, Name = "Groceries", Icon = "fa-solid fa-cart-shopping", Color = "#22c55e", IsDefault = false },
            new() { UserId = userId, Name = "Other", Icon = "fa-solid fa-ellipsis", Color = "#64748b", IsDefault = false },
        };

        await _context.Categories.InsertManyAsync(defaults);
    }

    private static CategoryDto MapToDto(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Type = category.Type,
        Icon = category.Icon,
        Color = category.Color,
        IsDefault = category.IsDefault,
        CreatedAt = category.CreatedAt
    };
}
