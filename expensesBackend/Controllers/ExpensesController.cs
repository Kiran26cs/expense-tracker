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
    private readonly IMemberService _memberService;

    public ExpensesController(IExpenseService expenseService, IMemberService memberService)
    {
        _expenseService = expenseService;
        _memberService  = memberService;
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ExpensePagedResponse>>> GetExpenses(
        [FromQuery] string? expenseBookId,
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] string? category,
        [FromQuery] string? paymentMethod,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? sortField,
        [FromQuery] string? sortDir,
        [FromQuery] string? cursor,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var userId = GetUserId();

            // ACL: check book access and resolve category restrictions
            var allowedCategoryIds = new List<string>();
            if (!string.IsNullOrEmpty(expenseBookId))
            {
                var perms = await _memberService.GetResolvedPermissionsAsync(expenseBookId, userId);
                if (perms.Expenses == "none")
                    return StatusCode(403, ApiResponse<ExpensePagedResponse>.ErrorResponse("You do not have access to expenses in this book."));

                allowedCategoryIds = perms.AllowedCategoryIds;

                // If the caller has a category whitelist AND is also filtering by a specific category,
                // that category must be in the allowed list
                if (allowedCategoryIds.Count > 0 && !string.IsNullOrEmpty(category)
                    && !allowedCategoryIds.Contains(category))
                {
                    return StatusCode(403, ApiResponse<ExpensePagedResponse>.ErrorResponse("You do not have access to this category."));
                }
            }

            var req = new ExpensePagedRequest
            {
                ExpenseBookId      = expenseBookId,
                Search             = search,
                Type               = type,
                Category           = category,
                PaymentMethod      = paymentMethod,
                StartDate          = startDate,
                EndDate            = endDate,
                SortField          = sortField ?? "date",
                SortDir            = sortDir ?? "desc",
                Cursor             = cursor,
                PageSize           = pageSize > 0 && pageSize <= 200 ? pageSize : 50,
                AllowedCategoryIds = allowedCategoryIds,
            };

            var result = await _expenseService.GetExpensesPagedAsync(userId, req);
            return Ok(ApiResponse<ExpensePagedResponse>.SuccessResponse(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<ExpensePagedResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ExpensePagedResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> GetExpense(string id, [FromQuery] string? expenseBookId)
    {
        try
        {
            var userId = GetUserId();

            if (!string.IsNullOrEmpty(expenseBookId))
                await _memberService.EnsureHasAccessAsync(expenseBookId, userId, "expenses:view");

            var expense = await _expenseService.GetExpenseByIdAsync(userId, id);
            return Ok(ApiResponse<ExpenseDto>.SuccessResponse(expense));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<ExpenseDto>.ErrorResponse(ex.Message));
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
            var userId = GetUserId();

            if (!string.IsNullOrEmpty(request.ExpenseBookId))
            {
                var perms = await _memberService.GetResolvedPermissionsAsync(request.ExpenseBookId, userId);
                if (perms.Expenses != "write")
                    return StatusCode(403, ApiResponse<ExpenseDto>.ErrorResponse("You do not have write access to expenses in this book."));

                // Validate category is within allowed list
                if (perms.AllowedCategoryIds.Count > 0
                    && !string.IsNullOrEmpty(request.Category)
                    && !perms.AllowedCategoryIds.Contains(request.Category))
                {
                    return StatusCode(403, ApiResponse<ExpenseDto>.ErrorResponse("You are not allowed to use this category."));
                }
            }

            var expense = await _expenseService.CreateExpenseAsync(userId, request);
            return CreatedAtAction(nameof(GetExpense), new { id = expense.Id },
                ApiResponse<ExpenseDto>.SuccessResponse(expense));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<ExpenseDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ExpenseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> UpdateExpense(
        string id, [FromBody] UpdateExpenseRequest request, [FromQuery] string? expenseBookId)
    {
        try
        {
            var userId = GetUserId();

            if (!string.IsNullOrEmpty(expenseBookId))
            {
                var perms = await _memberService.GetResolvedPermissionsAsync(expenseBookId, userId);
                if (perms.Expenses != "write")
                    return StatusCode(403, ApiResponse<ExpenseDto>.ErrorResponse("You do not have write access to expenses in this book."));

                // Validate updated category is within allowed list
                if (perms.AllowedCategoryIds.Count > 0
                    && !string.IsNullOrEmpty(request.Category)
                    && !perms.AllowedCategoryIds.Contains(request.Category))
                {
                    return StatusCode(403, ApiResponse<ExpenseDto>.ErrorResponse("You are not allowed to use this category."));
                }
            }

            var expense = await _expenseService.UpdateExpenseAsync(userId, id, request);
            return Ok(ApiResponse<ExpenseDto>.SuccessResponse(expense));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<ExpenseDto>.ErrorResponse(ex.Message));
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
    public async Task<ActionResult<ApiResponse<bool>>> DeleteExpense(string id, [FromQuery] string? expenseBookId)
    {
        try
        {
            var userId = GetUserId();

            if (!string.IsNullOrEmpty(expenseBookId))
            {
                var perms = await _memberService.GetResolvedPermissionsAsync(expenseBookId, userId);
                if (perms.Role == "none")
                    return StatusCode(403, ApiResponse<bool>.ErrorResponse("You do not have access to this book."));
                if (!perms.CanDeleteExpenses)
                    return StatusCode(403, ApiResponse<bool>.ErrorResponse("You do not have permission to delete expenses in this book."));
            }

            var result = await _expenseService.DeleteExpenseAsync(userId, id);
            if (!result)
                return NotFound(ApiResponse<bool>.ErrorResponse("Expense not found"));

            return Ok(ApiResponse<bool>.SuccessResponse(true));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<bool>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("recurring")]
    public async Task<ActionResult<ApiResponse<List<RecurringExpenseDto>>>> GetRecurringExpenses(
        [FromQuery] string? expenseBookId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var userId = GetUserId();

            if (!string.IsNullOrEmpty(expenseBookId))
                await _memberService.EnsureHasAccessAsync(expenseBookId, userId, "expenses:view");

            var recurringExpenses = await _expenseService.GetRecurringExpensesAsync(userId, expenseBookId, startDate, endDate);
            return Ok(ApiResponse<List<RecurringExpenseDto>>.SuccessResponse(recurringExpenses));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<List<RecurringExpenseDto>>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<RecurringExpenseDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("recurring/{id}/mark-paid")]
    public async Task<ActionResult<ApiResponse<ExpenseDto>>> MarkRecurringExpenseAsPaid(
        string id, [FromBody] MarkRecurringPaidRequest request, [FromQuery] string? expenseBookId)
    {
        try
        {
            var userId = GetUserId();

            if (!string.IsNullOrEmpty(expenseBookId))
                await _memberService.EnsureHasAccessAsync(expenseBookId, userId, "expenses:write");

            var expense = await _expenseService.MarkRecurringExpenseAsPaidAsync(userId, id, request.PaidDate);
            return Ok(ApiResponse<ExpenseDto>.SuccessResponse(expense));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<ExpenseDto>.ErrorResponse(ex.Message));
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

    [HttpPut("recurring/{id}")]
    public async Task<ActionResult<ApiResponse<RecurringExpenseDto>>> UpdateRecurringExpense(
        string id, [FromBody] UpdateRecurringExpenseRequest request, [FromQuery] string? expenseBookId)
    {
        try
        {
            var userId = GetUserId();

            if (!string.IsNullOrEmpty(expenseBookId))
                await _memberService.EnsureHasAccessAsync(expenseBookId, userId, "expenses:write");

            var recurringExpense = await _expenseService.UpdateRecurringExpenseAsync(userId, id, request);
            return Ok(ApiResponse<RecurringExpenseDto>.SuccessResponse(recurringExpense));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<RecurringExpenseDto>.ErrorResponse(ex.Message));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<RecurringExpenseDto>.ErrorResponse("Recurring expense not found"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<RecurringExpenseDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("recurring/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRecurringExpense(
        string id, [FromQuery] string? expenseBookId)
    {
        try
        {
            var userId = GetUserId();

            if (!string.IsNullOrEmpty(expenseBookId))
                await _memberService.EnsureHasAccessAsync(expenseBookId, userId, "expenses:write");

            var result = await _expenseService.DeleteRecurringExpenseAsync(userId, id);
            if (!result)
                return NotFound(ApiResponse<bool>.ErrorResponse("Recurring expense not found"));

            return Ok(ApiResponse<bool>.SuccessResponse(true));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<bool>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("{id}/receipt")]
    public async Task<ActionResult<ApiResponse<string>>> UploadReceipt(string id, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse<string>.ErrorResponse("No file provided"));

            var userId = GetUserId();
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

