using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Infrastructure.Cache;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IExpenseBookService _expenseBookService;
    private readonly MongoDbContext _context;
    private readonly ICacheService _cache;
    private static readonly TimeSpan SettingsCacheTtl = TimeSpan.FromMinutes(30);

    public SettingsController(ICategoryService categoryService, IExpenseBookService expenseBookService, MongoDbContext context, ICacheService cache)
    {
        _categoryService = categoryService;
        _expenseBookService = expenseBookService;
        _context = context;
        _cache = cache;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

    private async Task VerifyBookOwnershipAsync(string userId, string expenseBookId)
    {
        // Throws KeyNotFoundException if not found or not owned by this user
        await _expenseBookService.GetExpenseBookByIdAsync(userId, expenseBookId);
    }

    // GET api/settings?expenseBookId=...
    [HttpGet("")]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> GetSettings([FromQuery] string? expenseBookId)
    {
        var userId = GetUserId();

        if (!string.IsNullOrEmpty(expenseBookId))
        {
            var cacheKey = CacheKeys.BookSettings(expenseBookId);
            var cached = await _cache.GetAsync<UserSettingsDto>(cacheKey);
            if (cached is not null)
                return Ok(ApiResponse<UserSettingsDto>.SuccessResponse(cached));

            var book = await _context.ExpenseBooks.Find(b => b.Id == expenseBookId).FirstOrDefaultAsync();
            if (book == null)
                return NotFound(ApiResponse<UserSettingsDto>.ErrorResponse("Expense book not found"));

            var dto = new UserSettingsDto
            {
                DefaultCurrency = book.Currency,
                MonthlySavingsGoal = book.MonthlySavingsGoal
            };
            await _cache.SetAsync(cacheKey, dto, SettingsCacheTtl);
            return Ok(ApiResponse<UserSettingsDto>.SuccessResponse(dto));
        }
        else
        {
            var cacheKey = CacheKeys.UserSettings(userId);
            var cached = await _cache.GetAsync<UserSettingsDto>(cacheKey);
            if (cached is not null)
                return Ok(ApiResponse<UserSettingsDto>.SuccessResponse(cached));

            var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
                return NotFound(ApiResponse<UserSettingsDto>.ErrorResponse("User not found"));

            var dto = new UserSettingsDto
            {
                DefaultCurrency = user.Currency,
                MonthlySavingsGoal = user.MonthlySavingsGoal
            };
            await _cache.SetAsync(cacheKey, dto, SettingsCacheTtl);
            return Ok(ApiResponse<UserSettingsDto>.SuccessResponse(dto));
        }
    }

    // PUT api/settings
    [HttpPut("")]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> UpdateSettings([FromBody] UpdateUserSettingsRequest request)
    {
        var userId = GetUserId();

        if (!string.IsNullOrEmpty(request.ExpenseBookId))
        {
            var book = await _context.ExpenseBooks.Find(b => b.Id == request.ExpenseBookId).FirstOrDefaultAsync();
            if (book == null)
                return NotFound(ApiResponse<UserSettingsDto>.ErrorResponse("Expense book not found"));

            var updateDef = Builders<Domain.Entities.ExpenseBook>.Update
                .Set(b => b.UpdatedAt, DateTime.UtcNow);

            if (!string.IsNullOrEmpty(request.DefaultCurrency))
                updateDef = updateDef.Set(b => b.Currency, request.DefaultCurrency);

            if (request.MonthlySavingsGoal.HasValue)
                updateDef = updateDef.Set(b => b.MonthlySavingsGoal, request.MonthlySavingsGoal.Value);

            await _context.ExpenseBooks.UpdateOneAsync(
                Builders<Domain.Entities.ExpenseBook>.Filter.Eq(b => b.Id, request.ExpenseBookId),
                updateDef
            );

            var updated = await _context.ExpenseBooks.Find(b => b.Id == request.ExpenseBookId).FirstOrDefaultAsync();
            var dto = new UserSettingsDto
            {
                DefaultCurrency = updated!.Currency,
                MonthlySavingsGoal = updated.MonthlySavingsGoal
            };
            await _cache.RemoveAsync(CacheKeys.BookSettings(request.ExpenseBookId));
            await _cache.RemoveAsync(CacheKeys.UserExpenseBooks(userId));
            return Ok(ApiResponse<UserSettingsDto>.SuccessResponse(dto));
        }
        else
        {
            var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
                return NotFound(ApiResponse<UserSettingsDto>.ErrorResponse("User not found"));

            var updateDef = Builders<Domain.Entities.User>.Update
                .Set(u => u.UpdatedAt, DateTime.UtcNow);

            if (!string.IsNullOrEmpty(request.DefaultCurrency))
                updateDef = updateDef.Set(u => u.Currency, request.DefaultCurrency);

            if (request.MonthlySavingsGoal.HasValue)
                updateDef = updateDef.Set(u => u.MonthlySavingsGoal, request.MonthlySavingsGoal.Value);

            await _context.Users.UpdateOneAsync(
                Builders<Domain.Entities.User>.Filter.Eq(u => u.Id, userId),
                updateDef
            );

            var updated = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            var dto = new UserSettingsDto
            {
                DefaultCurrency = updated!.Currency,
                MonthlySavingsGoal = updated.MonthlySavingsGoal
            };
            await _cache.RemoveAsync(CacheKeys.UserSettings(userId));
            return Ok(ApiResponse<UserSettingsDto>.SuccessResponse(dto));
        }
    }

    // GET api/settings/payment-methods
    [HttpGet("payment-methods")]
    public ActionResult<ApiResponse<List<PaymentMethodDto>>> GetPaymentMethods()
    {
        var methods = new Dictionary<PaymentMethodType, string>
        {
            { PaymentMethodType.Cash,         "Cash" },
            { PaymentMethodType.CreditCard,   "Credit Card" },
            { PaymentMethodType.DebitCard,    "Debit Card" },
            { PaymentMethodType.BankTransfer, "Bank Transfer" },
            { PaymentMethodType.UPI,          "UPI" },
            { PaymentMethodType.Cheque,       "Cheque" },
            { PaymentMethodType.Other,        "Other" },
        };

        var result = methods.Select(kv => new PaymentMethodDto
        {
            Id   = (int)kv.Key,
            Name = kv.Value
        }).ToList();

        return Ok(ApiResponse<List<PaymentMethodDto>>.SuccessResponse(result));
    }

    // GET api/settings/categories
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories([FromQuery] string? expenseBookId)
    {
        try
        {
            if (string.IsNullOrEmpty(expenseBookId))
                return BadRequest(ApiResponse<List<CategoryDto>>.ErrorResponse("expenseBookId is required"));

            var userId = GetUserId();
            await VerifyBookOwnershipAsync(userId, expenseBookId);

            var categories = await _categoryService.GetCategoriesAsync(expenseBookId);
            var result = ApiResponse<List<CategoryDto>>.SuccessResponse(categories);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<CategoryDto>>.ErrorResponse(ex.Message));
        }
    }

    // GET api/settings/categories/{id}
    [HttpGet("categories/{id}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategory(string id, [FromQuery] string? expenseBookId)
    {
        try
        {
            if (string.IsNullOrEmpty(expenseBookId))
                return BadRequest(ApiResponse<CategoryDto>.ErrorResponse("expenseBookId is required"));

            var userId = GetUserId();
            await VerifyBookOwnershipAsync(userId, expenseBookId);

            var category = await _categoryService.GetCategoryByIdAsync(expenseBookId, id);
            return Ok(ApiResponse<CategoryDto>.SuccessResponse(category));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<CategoryDto>.ErrorResponse("Category not found"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<CategoryDto>.ErrorResponse(ex.Message));
        }
    }

    // POST api/settings/categories
    [HttpPost("categories")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ExpenseBookId))
                return BadRequest(ApiResponse<CategoryDto>.ErrorResponse("ExpenseBookId is required"));

            var userId = GetUserId();
            await VerifyBookOwnershipAsync(userId, request.ExpenseBookId);

            var category = await _categoryService.CreateCategoryAsync(request.ExpenseBookId, request);
            return CreatedAtAction(
                nameof(GetCategory),
                new { id = category.Id },
                ApiResponse<CategoryDto>.SuccessResponse(category)
            );
        }
        catch (KeyNotFoundException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CategoryDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<CategoryDto>.ErrorResponse(ex.Message));
        }
    }

    // PUT api/settings/categories/{id}
    [HttpPut("categories/{id}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(string id, [FromQuery] string? expenseBookId, [FromBody] UpdateCategoryRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(expenseBookId))
                return BadRequest(ApiResponse<CategoryDto>.ErrorResponse("expenseBookId is required"));

            var userId = GetUserId();
            await VerifyBookOwnershipAsync(userId, expenseBookId);

            var category = await _categoryService.UpdateCategoryAsync(expenseBookId, id, request);
            return Ok(ApiResponse<CategoryDto>.SuccessResponse(category));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<CategoryDto>.ErrorResponse("Category not found"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CategoryDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<CategoryDto>.ErrorResponse(ex.Message));
        }
    }

    // DELETE api/settings/categories/{id}
    [HttpDelete("categories/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCategory(string id, [FromQuery] string? expenseBookId)
    {
        try
        {
            if (string.IsNullOrEmpty(expenseBookId))
                return BadRequest(ApiResponse<bool>.ErrorResponse("expenseBookId is required"));

            var userId = GetUserId();
            await VerifyBookOwnershipAsync(userId, expenseBookId);

            await _categoryService.DeleteCategoryAsync(expenseBookId, id);
            return Ok(ApiResponse<bool>.SuccessResponse(true));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse("Category not found"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    // POST api/settings/categories/import
    [HttpPost("categories/import")]
    public async Task<ActionResult<ApiResponse<ImportCategoriesResponse>>> ImportCategories([FromBody] ImportCategoriesRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ExpenseBookId))
                return BadRequest(ApiResponse<ImportCategoriesResponse>.ErrorResponse("ExpenseBookId is required"));

            var userId = GetUserId();
            await VerifyBookOwnershipAsync(userId, request.ExpenseBookId);

            var result = await _categoryService.ImportCategoriesAsync(request.ExpenseBookId, request);
            return Ok(ApiResponse<ImportCategoriesResponse>.SuccessResponse(result));
        }
        catch (KeyNotFoundException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ImportCategoriesResponse>.ErrorResponse(ex.Message));
        }
    }
}
