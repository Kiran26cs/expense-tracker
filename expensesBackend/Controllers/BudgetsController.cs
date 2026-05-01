using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _budgetService;
    private readonly IMemberService _memberService;

    public BudgetsController(IBudgetService budgetService, IMemberService memberService)
    {
        _budgetService = budgetService;
        _memberService = memberService;
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<Budget>>>> GetBudgets(
        [FromQuery] string? expenseBookId,
        [FromQuery] string? month)
    {
        try
        {
            var userId = GetUserId();

            if (!string.IsNullOrEmpty(expenseBookId))
                await _memberService.EnsureHasAccessAsync(expenseBookId, userId, "budgets:view");

            var budgets = await _budgetService.GetBudgetsAsync(userId, expenseBookId, month);
            return Ok(ApiResponse<List<Budget>>.SuccessResponse(budgets));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<List<Budget>>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<Budget>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<Budget>>> GetBudget(string id)
    {
        try
        {
            var userId = GetUserId();
            var budget = await _budgetService.GetBudgetByIdAsync(userId, id);
            if (budget == null)
                return NotFound(ApiResponse<Budget>.ErrorResponse("Budget not found"));
            return Ok(ApiResponse<Budget>.SuccessResponse(budget));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Budget>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("upsert-version")]
    public async Task<ActionResult<ApiResponse<Budget>>> UpsertBudgetVersion([FromBody] UpsertBudgetVersionRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(request.Category))
                return BadRequest(ApiResponse<Budget>.ErrorResponse("Category is required"));
            if (request.Amount <= 0)
                return BadRequest(ApiResponse<Budget>.ErrorResponse("Amount must be greater than zero"));
            if (!DateTime.TryParse(request.EffectiveDate, out var parsedDate))
                return BadRequest(ApiResponse<Budget>.ErrorResponse("Invalid effective date"));

            var bookId = string.IsNullOrEmpty(request.ExpenseBookId) ? null : request.ExpenseBookId;

            if (!string.IsNullOrEmpty(bookId))
                await _memberService.EnsureHasAccessAsync(bookId, userId, "budgets:write");

            var effectiveDate   = new DateTime(parsedDate.Year, parsedDate.Month, parsedDate.Day, 0, 0, 0, DateTimeKind.Utc);
            var effectivePeriod = string.IsNullOrWhiteSpace(request.EffectivePeriod) ? null : request.EffectivePeriod;

            var budget = await _budgetService.UpsertBudgetVersionAsync(userId, bookId, request.Category, request.Amount, effectiveDate, effectivePeriod);
            return Ok(ApiResponse<Budget>.SuccessResponse(budget));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<Budget>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Budget>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteBudget(string id, [FromQuery] string? expenseBookId)
    {
        try
        {
            var userId = GetUserId();

            if (!string.IsNullOrEmpty(expenseBookId))
                await _memberService.EnsureHasAccessAsync(expenseBookId, userId, "budgets:write");

            var result = await _budgetService.DeleteBudgetAsync(userId, id);
            if (!result)
                return NotFound(ApiResponse<object>.ErrorResponse("Budget not found"));
            return Ok(ApiResponse<object>.SuccessResponse(new { message = "Budget deleted successfully" }));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }
}

