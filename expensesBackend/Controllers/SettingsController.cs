using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public SettingsController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

    // GET api/settings/categories
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories()
    {
        try
        {
            var userId = GetUserId();
            var categories = await _categoryService.GetCategoriesAsync(userId);
            return Ok(ApiResponse<List<CategoryDto>>.SuccessResponse(categories));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<CategoryDto>>.ErrorResponse(ex.Message));
        }
    }

    // GET api/settings/categories/{id}
    [HttpGet("categories/{id}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategory(string id)
    {
        try
        {
            var userId = GetUserId();
            var category = await _categoryService.GetCategoryByIdAsync(userId, id);
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
            var userId = GetUserId();
            var category = await _categoryService.CreateCategoryAsync(userId, request);
            return CreatedAtAction(
                nameof(GetCategory),
                new { id = category.Id },
                ApiResponse<CategoryDto>.SuccessResponse(category)
            );
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
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(string id, [FromBody] UpdateCategoryRequest request)
    {
        try
        {
            var userId = GetUserId();
            var category = await _categoryService.UpdateCategoryAsync(userId, id, request);
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
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCategory(string id)
    {
        try
        {
            var userId = GetUserId();
            await _categoryService.DeleteCategoryAsync(userId, id);
            return Ok(ApiResponse<bool>.SuccessResponse(true));
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
            var userId = GetUserId();
            var result = await _categoryService.ImportCategoriesAsync(userId, request);
            return Ok(ApiResponse<ImportCategoriesResponse>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ImportCategoriesResponse>.ErrorResponse(ex.Message));
        }
    }
}
