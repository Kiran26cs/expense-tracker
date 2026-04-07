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

    public async Task<List<CategoryDto>> GetCategoriesAsync(string expenseBookId)
    {
        var filter = Builders<Category>.Filter.Eq(c => c.ExpenseBookId, expenseBookId);
        var categories = await _context.Categories
            .Find(filter)
            .SortBy(c => c.Name)
            .ToListAsync();
        return categories.Select(MapToDto).ToList();
    }

    public async Task<CategoryDto> GetCategoryByIdAsync(string expenseBookId, string categoryId)
    {
        var filter = Builders<Category>.Filter.And(
            Builders<Category>.Filter.Eq(c => c.Id, categoryId),
            Builders<Category>.Filter.Eq(c => c.ExpenseBookId, expenseBookId)
        );
        var category = await _context.Categories.Find(filter).FirstOrDefaultAsync();
        if (category == null)
            throw new KeyNotFoundException("Category not found");
        return MapToDto(category);
    }

    public async Task<CategoryDto> CreateCategoryAsync(string expenseBookId, CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Category name is required");

        var dupFilter = Builders<Category>.Filter.And(
            Builders<Category>.Filter.Eq(c => c.ExpenseBookId, expenseBookId),
            Builders<Category>.Filter.Eq(c => c.Name, request.Name.Trim()));

        if (await _context.Categories.Find(dupFilter).AnyAsync())
            throw new ArgumentException($"Category '{request.Name}' already exists");

        var category = new Category
        {
            ExpenseBookId = expenseBookId,
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

    public async Task<CategoryDto> UpdateCategoryAsync(string expenseBookId, string categoryId, UpdateCategoryRequest request)
    {
        var filter = Builders<Category>.Filter.And(
            Builders<Category>.Filter.Eq(c => c.Id, categoryId),
            Builders<Category>.Filter.Eq(c => c.ExpenseBookId, expenseBookId));

        var category = await _context.Categories.Find(filter).FirstOrDefaultAsync();
        if (category == null)
            throw new KeyNotFoundException("Category not found or cannot be modified");

        var updates = new List<UpdateDefinition<Category>>();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var dupFilter = Builders<Category>.Filter.And(
                Builders<Category>.Filter.Eq(c => c.ExpenseBookId, expenseBookId),
                Builders<Category>.Filter.Eq(c => c.Name, request.Name.Trim()),
                Builders<Category>.Filter.Ne(c => c.Id, categoryId));

            if (await _context.Categories.Find(dupFilter).AnyAsync())
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
            await _context.Categories.UpdateOneAsync(filter, Builders<Category>.Update.Combine(updates));

        return MapToDto((await _context.Categories.Find(filter).FirstOrDefaultAsync())!);
    }

    public async Task<bool> DeleteCategoryAsync(string expenseBookId, string categoryId)
    {
        var filter = Builders<Category>.Filter.And(
            Builders<Category>.Filter.Eq(c => c.Id, categoryId),
            Builders<Category>.Filter.Eq(c => c.ExpenseBookId, expenseBookId),
            Builders<Category>.Filter.Eq(c => c.IsDefault, false));

        var result = await _context.Categories.DeleteOneAsync(filter);
        if (result.DeletedCount == 0)
            throw new InvalidOperationException("Category not found or is a default category");

        return true;
    }

    public async Task<ImportCategoriesResponse> ImportCategoriesAsync(string expenseBookId, ImportCategoriesRequest request)
    {
        var response = new ImportCategoriesResponse();

        foreach (var catReq in request.Categories)
        {
            try
            {
                await CreateCategoryAsync(expenseBookId, catReq);
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

    public Task SeedDefaultCategoriesAsync(string expenseBookId)
    {
        // Seeding is handled by ExpenseBookDependencyService.CopyDefaultCategoriesToBookAsync
        // which is called automatically when an expense book is created.
        return Task.CompletedTask;
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
