using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ExpenseBooksController : ControllerBase
{
    private readonly IExpenseBookService _expenseBookService;
    private readonly ILogger<ExpenseBooksController> _logger;

    public ExpenseBooksController(IExpenseBookService expenseBookService, ILogger<ExpenseBooksController> logger)
    {
        _expenseBookService = expenseBookService;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException("User ID not found in token");
    }

    /// <summary>
    /// Get all expense books for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ExpenseBookResponse>>>> GetExpenseBooks()
    {
        try
        {
            var userId = GetUserId();
            var expenseBooks = await _expenseBookService.GetExpenseBooksAsync(userId);
            
            return Ok(ApiResponse<List<ExpenseBookResponse>>.SuccessResponse(expenseBooks));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expense books");
            return StatusCode(500, ApiResponse<List<ExpenseBookResponse>>.ErrorResponse("Error retrieving expense books"));
        }
    }

    /// <summary>
    /// Get default expense book for the current user
    /// </summary>
    [HttpGet("default")]
    public async Task<ActionResult<ApiResponse<ExpenseBookResponse>>> GetDefaultExpenseBook()
    {
        try
        {
            var userId = GetUserId();
            var expenseBook = await _expenseBookService.GetDefaultExpenseBookAsync(userId);
            
            if (expenseBook == null)
            {
                return NotFound(ApiResponse<ExpenseBookResponse>.ErrorResponse("No default expense book found"));
            }
            
            return Ok(ApiResponse<ExpenseBookResponse>.SuccessResponse(expenseBook));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving default expense book");
            return StatusCode(500, ApiResponse<ExpenseBookResponse>.ErrorResponse("Error retrieving default expense book"));
        }
    }

    /// <summary>
    /// Get a specific expense book by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ExpenseBookResponse>>> GetExpenseBook(string id)
    {
        try
        {
            var userId = GetUserId();
            var expenseBook = await _expenseBookService.GetExpenseBookByIdAsync(userId, id);
            
            return Ok(ApiResponse<ExpenseBookResponse>.SuccessResponse(expenseBook));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<ExpenseBookResponse>.ErrorResponse("Expense book not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expense book");
            return StatusCode(500, ApiResponse<ExpenseBookResponse>.ErrorResponse("Error retrieving expense book"));
        }
    }

    /// <summary>
    /// Create a new expense book
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ExpenseBookResponse>>> CreateExpenseBook([FromBody] CreateExpenseBookRequest request)
    {
        try
        {
            var userId = GetUserId();
            var expenseBook = await _expenseBookService.CreateExpenseBookAsync(userId, request);
            
            return CreatedAtAction(
                nameof(GetExpenseBook),
                new { id = expenseBook.Id },
                ApiResponse<ExpenseBookResponse>.SuccessResponse(expenseBook));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense book");
            return StatusCode(500, ApiResponse<ExpenseBookResponse>.ErrorResponse("Error creating expense book"));
        }
    }

    /// <summary>
    /// Update an existing expense book
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ExpenseBookResponse>>> UpdateExpenseBook(string id, [FromBody] UpdateExpenseBookRequest request)
    {
        try
        {
            var userId = GetUserId();
            var expenseBook = await _expenseBookService.UpdateExpenseBookAsync(userId, id, request);
            
            return Ok(ApiResponse<ExpenseBookResponse>.SuccessResponse(expenseBook));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<ExpenseBookResponse>.ErrorResponse("Expense book not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense book");
            return StatusCode(500, ApiResponse<ExpenseBookResponse>.ErrorResponse("Error updating expense book"));
        }
    }

    /// <summary>
    /// Delete an expense book
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteExpenseBook(string id)
    {
        try
        {
            var userId = GetUserId();
            await _expenseBookService.DeleteExpenseBookAsync(userId, id);
            
            return Ok(ApiResponse<object>.SuccessResponse(new { }));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Expense book not found"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting expense book");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Error deleting expense book"));
        }
    }

    /// <summary>
    /// Get all unique categories used in expense books
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetCategories()
    {
        try
        {
            var userId = GetUserId();
            var categories = await _expenseBookService.GetExpenseBookCategoriesAsync(userId);
            
            return Ok(ApiResponse<List<string>>.SuccessResponse(categories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return StatusCode(500, ApiResponse<List<string>>.ErrorResponse("Error retrieving categories"));
        }
    }

    /// <summary>
    /// Refresh expense book statistics
    /// </summary>
    [HttpPost("{id}/refresh-stats")]
    public async Task<ActionResult<ApiResponse<object>>> RefreshStats(string id)
    {
        try
        {
            var userId = GetUserId();
            // Verify ownership
            await _expenseBookService.GetExpenseBookByIdAsync(userId, id);
            
            await _expenseBookService.UpdateExpenseBookStatsAsync(id);
            
            return Ok(ApiResponse<object>.SuccessResponse(new { }));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Expense book not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing statistics");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Error refreshing statistics"));
        }
    }
}