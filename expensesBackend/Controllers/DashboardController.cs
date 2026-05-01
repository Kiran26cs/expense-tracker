using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IMemberService    _memberService;

    public DashboardController(IDashboardService dashboardService, IMemberService memberService)
    {
        _dashboardService = dashboardService;
        _memberService    = memberService;
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

    private async Task<List<string>> GetAllowedCategoryIdsAsync(string? expenseBookId)
    {
        if (string.IsNullOrEmpty(expenseBookId)) return [];
        var perms = await _memberService.GetResolvedPermissionsAsync(expenseBookId, GetUserId());
        return perms.AllowedCategoryIds;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummary>>> GetSummary(
        [FromQuery] string? expenseBookId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var normalizedStart = startDate.HasValue ? startDate.Value.Date : (DateTime?)null;
            var normalizedEnd   = endDate.HasValue   ? endDate.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : (DateTime?)null;
            var allowedCategories = await GetAllowedCategoryIdsAsync(expenseBookId);
            var summary = await _dashboardService.GetSummaryAsync(GetUserId(), expenseBookId, normalizedStart, normalizedEnd, allowedCategories);
            return Ok(ApiResponse<DashboardSummary>.SuccessResponse(summary));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<DashboardSummary>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("trends")]
    public async Task<ActionResult<ApiResponse<List<MonthlyTrend>>>> GetMonthlyTrends(
        [FromQuery] string? expenseBookId,
        [FromQuery] int months = 6)
    {
        try
        {
            var allowedCategories = await GetAllowedCategoryIdsAsync(expenseBookId);
            var trends = await _dashboardService.GetMonthlyTrendsAsync(GetUserId(), expenseBookId, months, allowedCategories);
            return Ok(ApiResponse<List<MonthlyTrend>>.SuccessResponse(trends));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<MonthlyTrend>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("grouped-transactions")]
    public async Task<ActionResult<ApiResponse<List<DailyTransactionGroup>>>> GetGroupedTransactions(
        [FromQuery] string? expenseBookId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var allowedCategories = await GetAllowedCategoryIdsAsync(expenseBookId);
            var transactions = await _dashboardService.GetGroupedTransactionsAsync(GetUserId(), expenseBookId, startDate, endDate, allowedCategories);
            return Ok(ApiResponse<List<DailyTransactionGroup>>.SuccessResponse(transactions));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<DailyTransactionGroup>>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("migrate-daily-summaries")]
    public async Task<ActionResult<ApiResponse<string>>> MigrateDailySummaries()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            await _dashboardService.MigrateDailySummariesAsync(userId);
            return Ok(ApiResponse<string>.SuccessResponse("Daily summaries migrated successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("upcoming-payments")]
    public async Task<ActionResult<ApiResponse<UpcomingPaymentsPaginatedResponse>>> GetUpcomingPayments(
        [FromQuery] string? expenseBookId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var result = await _dashboardService.GetUpcomingPaymentsAsync(userId, expenseBookId, page, pageSize);
            return Ok(ApiResponse<UpcomingPaymentsPaginatedResponse>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<UpcomingPaymentsPaginatedResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("upcoming-payments/{id}/mark-paid")]
    public async Task<ActionResult<ApiResponse<UpcomingPaymentDto>>> MarkUpcomingPaymentAsPaid(
        string id,
        [FromBody] MarkUpcomingPaidRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var result = await _dashboardService.MarkUpcomingPaymentAsPaidAsync(userId, id, request.PaidDate, request.RecordAsExpense);
            return Ok(ApiResponse<UpcomingPaymentDto>.SuccessResponse(result));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<UpcomingPaymentDto>.ErrorResponse("Upcoming payment not found"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<UpcomingPaymentDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("generate-upcoming-payments")]
    public async Task<ActionResult<ApiResponse<string>>> GenerateUpcomingPayments()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            await _dashboardService.GenerateUpcomingPaymentsAsync(userId);
            return Ok(ApiResponse<string>.SuccessResponse("Upcoming payments generated successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
    }
}
