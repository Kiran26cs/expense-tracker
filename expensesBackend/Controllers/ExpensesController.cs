using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ExpenseDto>>>> GetExpenses(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? category)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var expenses = await _expenseService.GetExpensesAsync(userId, startDate, endDate, category);
            return Ok(ApiResponse<List<ExpenseDto>>.SuccessResponse(expenses));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<ExpenseDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> GetExpense(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var expense = await _expenseService.GetExpenseByIdAsync(userId, id);
            return Ok(ApiResponse<ExpenseDto>.SuccessResponse(expense));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<ExpenseDto>.ErrorResponse("Expense not found"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ExpenseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var expense = await _expenseService.CreateExpenseAsync(userId, request);
            return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, 
                ApiResponse<ExpenseDto>.SuccessResponse(expense));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ExpenseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> UpdateExpense(string id, [FromBody] UpdateExpenseRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var expense = await _expenseService.UpdateExpenseAsync(userId, id, request);
            return Ok(ApiResponse<ExpenseDto>.SuccessResponse(expense));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<ExpenseDto>.ErrorResponse("Expense not found"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ExpenseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteExpense(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var result = await _expenseService.DeleteExpenseAsync(userId, id);
            if (!result)
                return NotFound(ApiResponse<bool>.ErrorResponse("Expense not found"));
            
            return Ok(ApiResponse<bool>.SuccessResponse(true));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("recurring")]
    public async Task<ActionResult<ApiResponse<List<RecurringExpenseDto>>>> GetRecurringExpenses(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var recurringExpenses = await _expenseService.GetRecurringExpensesAsync(userId, startDate, endDate);
            return Ok(ApiResponse<List<RecurringExpenseDto>>.SuccessResponse(recurringExpenses));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<RecurringExpenseDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("recurring/{id}/mark-paid")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> MarkRecurringExpenseAsPaid(string id, [FromBody] MarkRecurringPaidRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var expense = await _expenseService.MarkRecurringExpenseAsPaidAsync(userId, id, request.PaidDate);
            return Ok(ApiResponse<ExpenseDto>.SuccessResponse(expense));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<ExpenseDto>.ErrorResponse("Recurring expense not found"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ExpenseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("{id}/receipt")]
    public async Task<ActionResult<ApiResponse<string>>> UploadReceipt(string id, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse<string>.ErrorResponse("No file provided"));

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            using var stream = file.OpenReadStream();
            var receiptUrl = await _expenseService.UploadReceiptAsync(userId, id, stream, file.FileName);
            
            return Ok(ApiResponse<string>.SuccessResponse(receiptUrl));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<string>.ErrorResponse("Expense not found"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }
}
