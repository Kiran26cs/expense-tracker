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
}
