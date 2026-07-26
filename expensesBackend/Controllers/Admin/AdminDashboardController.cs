using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpensesBackend.API.Controllers.Admin;

[Authorize(Policy = "PlatformAdmin")]
[ApiController]
[Route("api/admin/dashboard")]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboard;

    public AdminDashboardController(IAdminDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet("user-stats")]
    public async Task<ActionResult<ApiResponse<AdminUserStatsDto>>> UserStats()
    {
        var data = await _dashboard.GetUserStatsAsync();
        return Ok(ApiResponse<AdminUserStatsDto>.SuccessResponse(data));
    }

    [HttpGet("subscription-stats")]
    public async Task<ActionResult<ApiResponse<AdminSubscriptionStatsDto>>> SubscriptionStats()
    {
        var data = await _dashboard.GetSubscriptionStatsAsync();
        return Ok(ApiResponse<AdminSubscriptionStatsDto>.SuccessResponse(data));
    }

    [HttpGet("credit-stats")]
    public async Task<ActionResult<ApiResponse<AdminCreditStatsDto>>> CreditStats()
    {
        var data = await _dashboard.GetCreditStatsAsync();
        return Ok(ApiResponse<AdminCreditStatsDto>.SuccessResponse(data));
    }

    [HttpGet("book-stats")]
    public async Task<ActionResult<ApiResponse<AdminBookStatsDto>>> BookStats()
    {
        var data = await _dashboard.GetBookStatsAsync();
        return Ok(ApiResponse<AdminBookStatsDto>.SuccessResponse(data));
    }

    [HttpGet("import-stats")]
    public async Task<ActionResult<ApiResponse<AdminImportStatsDto>>> ImportStats()
    {
        var data = await _dashboard.GetImportStatsAsync();
        return Ok(ApiResponse<AdminImportStatsDto>.SuccessResponse(data));
    }

    [HttpGet("recent-actions")]
    public async Task<ActionResult<ApiResponse<AdminRecentActionsDto>>> RecentActions()
    {
        var data = await _dashboard.GetRecentActionsAsync();
        return Ok(ApiResponse<AdminRecentActionsDto>.SuccessResponse(data));
    }
}
