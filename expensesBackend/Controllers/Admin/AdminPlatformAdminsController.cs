using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers.Admin;

[Authorize(Policy = "PlatformAdmin")]
[ApiController]
[Route("api/admin/platform-admins")]
public class AdminPlatformAdminsController : ControllerBase
{
    private readonly IAdminPlatformAdminService _adminMgmt;
    private readonly MongoDbContext _ctx;

    public AdminPlatformAdminsController(IAdminPlatformAdminService adminMgmt, MongoDbContext ctx)
    {
        _adminMgmt = adminMgmt;
        _ctx       = ctx;
    }

    private string CurrentAdminId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    private string CurrentAdminEmail => User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
    private bool HasAdminsPermission => User.Claims.Any(c => c.Type == "permission" && c.Value == AdminPermissions.Admins);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<AdminPlatformAdminListDto>>> List()
    {
        if (!HasAdminsPermission)
            return Forbid();

        var result = await _adminMgmt.GetAdminsAsync();
        return Ok(ApiResponse<AdminPlatformAdminListDto>.SuccessResponse(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AdminPlatformAdminSummaryDto>>> Create(
        [FromBody] AdminCreatePlatformAdminRequest request)
    {
        if (!HasAdminsPermission)
            return Forbid();

        try
        {
            var created = await _adminMgmt.CreateAdminAsync(request, CurrentAdminId);

            await LogAudit(AdminActions.CreateAdmin, "platform_admin", created.Id,
                $"Created platform admin {created.Email} with permissions: {string.Join(", ", created.Permissions)}");

            return Ok(ApiResponse<AdminPlatformAdminSummaryDto>.SuccessResponse(created));
        }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<AdminPlatformAdminSummaryDto>.ErrorResponse(ex.Message)); }
        catch (ArgumentException ex)         { return BadRequest(ApiResponse<AdminPlatformAdminSummaryDto>.ErrorResponse(ex.Message)); }
    }

    [HttpPatch("{adminId}")]
    public async Task<ActionResult<ApiResponse<AdminPlatformAdminSummaryDto>>> Update(
        string adminId, [FromBody] AdminUpdatePlatformAdminRequest request)
    {
        if (!HasAdminsPermission)
            return Forbid();

        try
        {
            var updated = await _adminMgmt.UpdateAdminAsync(adminId, request);

            await LogAudit(AdminActions.UpdateAdmin, "platform_admin", adminId,
                $"Updated platform admin {updated.Email}");

            return Ok(ApiResponse<AdminPlatformAdminSummaryDto>.SuccessResponse(updated));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<AdminPlatformAdminSummaryDto>.ErrorResponse(ex.Message)); }
        catch (ArgumentException ex)    { return BadRequest(ApiResponse<AdminPlatformAdminSummaryDto>.ErrorResponse(ex.Message)); }
    }

    [HttpPatch("{adminId}/active")]
    public async Task<ActionResult<ApiResponse<object>>> SetActive(
        string adminId, [FromBody] AdminSetAdminActiveRequest request)
    {
        if (!HasAdminsPermission)
            return Forbid();

        // Prevent an admin from deactivating themselves
        if (adminId == CurrentAdminId && !request.IsActive)
            return BadRequest(ApiResponse<object>.ErrorResponse("You cannot deactivate your own account."));

        try
        {
            await _adminMgmt.SetActiveAsync(adminId, request.IsActive);

            var action = request.IsActive ? "Activated" : "Deactivated";
            await LogAudit(AdminActions.DeactivateAdmin, "platform_admin", adminId,
                $"{action} platform admin {adminId}");

            return Ok(ApiResponse<object>.SuccessResponse(new { message = $"Admin {(request.IsActive ? "activated" : "deactivated")}." }));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.ErrorResponse(ex.Message)); }
    }

    private async Task LogAudit(string action, string targetType, string? targetId, string summary)
    {
        await _ctx.AdminAuditLogs.InsertOneAsync(new AdminAuditLog
        {
            AdminId    = CurrentAdminId,
            AdminEmail = CurrentAdminEmail,
            Action     = action,
            TargetType = targetType,
            TargetId   = targetId,
            Summary    = summary,
            Timestamp  = DateTime.UtcNow,
        });
    }
}
