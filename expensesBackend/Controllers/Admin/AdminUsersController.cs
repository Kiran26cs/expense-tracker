using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers.Admin;

[Authorize(Policy = "PlatformAdmin")]
[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _users;

    public AdminUsersController(IAdminUserService users) => _users = users;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<AdminUserListDto>>> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _users.GetUsersAsync(search, page, Math.Min(pageSize, 100));
        return Ok(ApiResponse<AdminUserListDto>.SuccessResponse(result));
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<ApiResponse<AdminUserDetailDto>>> Detail(string userId)
    {
        var result = await _users.GetUserDetailAsync(userId);
        if (result == null) return NotFound(ApiResponse<AdminUserDetailDto>.ErrorResponse("User not found."));
        return Ok(ApiResponse<AdminUserDetailDto>.SuccessResponse(result));
    }

    [HttpPatch("{userId}/plan")]
    public async Task<ActionResult<ApiResponse<AdminUserDetailDto>>> ChangePlan(
        string userId, [FromBody] AdminChangePlanRequest req)
    {
        try
        {
            var result = await _users.ChangePlanAsync(userId, req.Plan, GetAdminId(), GetAdminEmail());
            return Ok(ApiResponse<AdminUserDetailDto>.SuccessResponse(result));
        }
        catch (KeyNotFoundException ex)  { return NotFound(ApiResponse<AdminUserDetailDto>.ErrorResponse(ex.Message)); }
        catch (ArgumentException ex)     { return BadRequest(ApiResponse<AdminUserDetailDto>.ErrorResponse(ex.Message)); }
    }

    [HttpPatch("{userId}/active")]
    public async Task<ActionResult<ApiResponse<AdminUserDetailDto>>> SetActive(
        string userId, [FromBody] AdminSetActiveRequest req)
    {
        try
        {
            var result = await _users.SetActiveAsync(userId, req.IsActive, GetAdminId(), GetAdminEmail());
            return Ok(ApiResponse<AdminUserDetailDto>.SuccessResponse(result));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<AdminUserDetailDto>.ErrorResponse(ex.Message)); }
    }

    private string GetAdminId()    => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    private string GetAdminEmail() => User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
}
