using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Channels;

namespace ExpensesBackend.API.Controllers.Admin;

[Authorize(Policy = "PlatformAdmin")]
[ApiController]
[Route("api/admin/jobs")]
public class AdminJobsController : ControllerBase
{
    private readonly ICreditService _credits;
    private readonly IAdminJobService _jobs;
    private readonly MongoDbContext _ctx;
    private readonly Channel<ImportJobPayload> _importChannel;

    public AdminJobsController(
        ICreditService credits,
        IAdminJobService jobs,
        MongoDbContext ctx,
        Channel<ImportJobPayload> importChannel)
    {
        _credits       = credits;
        _jobs          = jobs;
        _ctx           = ctx;
        _importChannel = importChannel;
    }

    /// <summary>Manually runs the monthly free credit reset for all eligible books.</summary>
    [HttpPost("trigger/credit-reset")]
    public async Task<ActionResult<ApiResponse<object>>> TriggerCreditReset()
    {
        await _credits.ResetMonthlyFreeCreditsAsync();

        await LogAudit(AdminActions.TriggerCreditReset, "job", null,
            "Manually triggered monthly credit reset");

        return Ok(ApiResponse<object>.SuccessResponse(new { message = "Monthly credit reset completed." }));
    }

    /// <summary>Lists recent import sessions for monitoring.</summary>
    [HttpGet("imports")]
    public async Task<ActionResult<ApiResponse<List<AdminImportSessionDto>>>> ListImports(
        [FromQuery] string? status)
    {
        var sessions = await _jobs.GetImportSessionsAsync(status);
        return Ok(ApiResponse<List<AdminImportSessionDto>>.SuccessResponse(sessions));
    }

    /// <summary>Retries a failed import session.</summary>
    [HttpPost("imports/{sessionId}/retry")]
    public async Task<ActionResult<ApiResponse<object>>> RetryImport(string sessionId)
    {
        try
        {
            var session = await _jobs.GetSessionAsync(sessionId);
            if (session == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Import session not found."));

            await _jobs.RetryImportAsync(sessionId, _importChannel);

            await LogAudit(AdminActions.RetryImport, "job", sessionId,
                $"Retried import session {sessionId} for book {session.ExpenseBookId}");

            return Ok(ApiResponse<object>.SuccessResponse(new { message = "Import queued for retry." }));
        }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message)); }
        catch (KeyNotFoundException ex)      { return NotFound(ApiResponse<object>.ErrorResponse(ex.Message)); }
    }

    private async Task LogAudit(string action, string targetType, string? targetId, string summary)
    {
        await _ctx.AdminAuditLogs.InsertOneAsync(new AdminAuditLog
        {
            AdminId    = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
            AdminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
            Action     = action,
            TargetType = targetType,
            TargetId   = targetId,
            Summary    = summary,
            Timestamp  = DateTime.UtcNow,
        });
    }
}
