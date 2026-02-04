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

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummary>>> GetSummary(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var summary = await _dashboardService.GetSummaryAsync(userId, startDate, endDate);
            return Ok(ApiResponse<DashboardSummary>.SuccessResponse(summary));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<DashboardSummary>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("trends")]
    public async Task<ActionResult<ApiResponse<List<MonthlyTrend>>>> GetMonthlyTrends([FromQuery] int months = 6)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var trends = await _dashboardService.GetMonthlyTrendsAsync(userId, months);
            return Ok(ApiResponse<List<MonthlyTrend>>.SuccessResponse(trends));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<MonthlyTrend>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("grouped-transactions")]
    public async Task<ActionResult<ApiResponse<List<DailyTransactionGroup>>>> GetGroupedTransactions(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var transactions = await _dashboardService.GetGroupedTransactionsAsync(userId, startDate, endDate);
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
}
