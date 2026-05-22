using ExpensesBackend.API.Domain;
using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Cache;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services;

public class CategoryService : ICategoryService
{
    private readonly MongoDbContext _context;
    private readonly ICacheService _cache;
    private static readonly TimeSpan CategoryCacheTtl = TimeSpan.FromMinutes(30);

    public CategoryService(MongoDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync(string expenseBookId)
    {
        var cacheKey = CacheKeys.Categories(expenseBookId);
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var filter = Builders<Category>.Filter.Eq(c => c.ExpenseBookId, expenseBookId);
            var categories = await _context.Categories
                .Find(filter)
                .SortBy(c => c.Name)
                .ToListAsync();
            return categories.Select(MapToDto).ToList();
        }, CategoryCacheTtl) ?? [];
    }

    public async Task<CategoryDto> GetCategoryByIdAsync(string expenseBookId, string categoryId)
    {
        var cacheKey = CacheKeys.CategoryById(expenseBookId, categoryId);
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var filter = Builders<Category>.Filter.And(
                Builders<Category>.Filter.Eq(c => c.Id, categoryId),
                Builders<Category>.Filter.Eq(c => c.ExpenseBookId, expenseBookId)
            );
            var category = await _context.Categories.Find(filter).FirstOrDefaultAsync();
            if (category == null)
                throw new KeyNotFoundException("Category not found");
            return MapToDto(category);
        }, CategoryCacheTtl) ?? throw new KeyNotFoundException("Category not found");
    }

    public async Task<CategoryDto> CreateCategoryAsync(string expenseBookId, string requestingUserId, CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Category name is required");

        var dupFilter = Builders<Category>.Filter.And(
            Builders<Category>.Filter.Eq(c => c.ExpenseBookId, expenseBookId),
            Builders<Category>.Filter.Eq(c => c.Name, request.Name.Trim()));

        if (await _context.Categories.Find(dupFilter).AnyAsync())
            throw new ArgumentException($"Category '{request.Name}' already exists");

        // Enforce plan limit — Pro is unlimited, skip entirely
        var user = await _context.Users.Find(u => u.Id == requestingUserId).FirstOrDefaultAsync();
        var plan = user?.Plan ?? PlanType.Free;

        if (plan == PlanType.Free)
        {
            // Global limit: sum CategoriesUsed across all books
            var memberDocs = await _context.ExpenseBookMembers
                .Find(m => m.UserId == requestingUserId && !m.IsDeleted)
                .ToListAsync();
            var totalUsed = memberDocs.Sum(m => m.CategoriesUsed);
            if (totalUsed >= PlanLimits.MaxCategories(PlanType.Free))
                throw new InvalidOperationException(
                    $"Free plan is limited to {PlanLimits.MaxCategories(PlanType.Free)} categories in total. Upgrade to add more.");
        }
        else if (plan == PlanType.Starter)
        {
            // Per-book limit: check only this book's stored counter
            var memberDoc = await _context.ExpenseBookMembers
                .Find(m => m.UserId == requestingUserId && m.ExpenseBookId == expenseBookId && !m.IsDeleted)
                .FirstOrDefaultAsync();
            var bookUsed = memberDoc?.CategoriesUsed ?? 0;
            if (bookUsed >= PlanLimits.MaxCategories(PlanType.Starter))
                throw new InvalidOperationException(
                    $"Starter plan is limited to {PlanLimits.MaxCategories(PlanType.Starter)} categories per book. Upgrade to add more.");
        }
        // Pro: no check

        var category = new Category
        {
            ExpenseBookId = expenseBookId,
            Name          = request.Name.Trim(),
            Type          = request.Type  ?? "expense",
            Icon          = request.Icon  ?? "fa-solid fa-tag",
            Color         = request.Color ?? "#6366f1",
            IsDefault     = false,
            CreatedAt     = DateTime.UtcNow
        };

        await _context.Categories.InsertOneAsync(category);
        await _cache.RemoveAsync(CacheKeys.Categories(expenseBookId));

        // Increment counter in member doc (skip for Pro)
        if (plan != PlanType.Pro)
            await IncrementMemberCategoryCountAsync(expenseBookId, requestingUserId, +1);

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

        await Task.WhenAll(
            _cache.RemoveAsync(CacheKeys.Categories(expenseBookId)),
            _cache.RemoveAsync(CacheKeys.CategoryById(expenseBookId, categoryId))
        );

        return MapToDto((await _context.Categories.Find(filter).FirstOrDefaultAsync())!);
    }

    public async Task<bool> DeleteCategoryAsync(string expenseBookId, string categoryId, string requestingUserId)
    {
        var filter = Builders<Category>.Filter.And(
            Builders<Category>.Filter.Eq(c => c.Id, categoryId),
            Builders<Category>.Filter.Eq(c => c.ExpenseBookId, expenseBookId),
            Builders<Category>.Filter.Eq(c => c.IsDefault, false));

        var result = await _context.Categories.DeleteOneAsync(filter);
        if (result.DeletedCount == 0)
            throw new InvalidOperationException("Category not found or is a default category");

        await Task.WhenAll(
            _cache.RemoveAsync(CacheKeys.Categories(expenseBookId)),
            _cache.RemoveAsync(CacheKeys.CategoryById(expenseBookId, categoryId))
        );

        // Decrement counter (skip for Pro)
        var user = await _context.Users.Find(u => u.Id == requestingUserId).FirstOrDefaultAsync();
        if (user?.Plan != PlanType.Pro)
            await IncrementMemberCategoryCountAsync(expenseBookId, requestingUserId, -1);

        return true;
    }

    public async Task<ImportCategoriesResponse> ImportCategoriesAsync(string expenseBookId, string requestingUserId, ImportCategoriesRequest request)
    {
        var response = new ImportCategoriesResponse();

        foreach (var catReq in request.Categories)
        {
            try
            {
                await CreateCategoryAsync(expenseBookId, requestingUserId, catReq);
                response.Imported++;
            }
            catch (Exception ex)
            {
                response.Failed++;
                response.Errors.Add($"{catReq.Name}: {ex.Message}");
            }
        }

        await _cache.RemoveAsync(CacheKeys.Categories(expenseBookId));
        return response;
    }

    public Task SeedDefaultCategoriesAsync(string expenseBookId)
    {
        // Seeding is handled by ExpenseBookDependencyService.CopyDefaultCategoriesToBookAsync
        // which is called automatically when an expense book is created.
        return Task.CompletedTask;
    }

    private async Task IncrementMemberCategoryCountAsync(string expenseBookId, string userId, int delta)
    {
        var filter = Builders<ExpenseBookMember>.Filter.And(
            Builders<ExpenseBookMember>.Filter.Eq(m => m.UserId, userId),
            Builders<ExpenseBookMember>.Filter.Eq(m => m.ExpenseBookId, expenseBookId),
            Builders<ExpenseBookMember>.Filter.Eq(m => m.IsDeleted, false));

        var update = Builders<ExpenseBookMember>.Update
            .Inc(m => m.CategoriesUsed, delta)
            .Set(m => m.UpdatedAt, DateTime.UtcNow);

        var result = await _context.ExpenseBookMembers.UpdateOneAsync(filter, update);

        // Owner may not have a member doc yet — create one
        if (result.MatchedCount == 0 && delta > 0)
        {
            await _context.ExpenseBookMembers.InsertOneAsync(new ExpenseBookMember
            {
                ExpenseBookId  = expenseBookId,
                UserId         = userId,
                Role           = "owner",
                InviteStatus   = "accepted",
                AddedBy        = userId,
                CategoriesUsed = 1,
                IsDeleted      = false,
            });
        }
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
