using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/bank-sync")]
public class BankSyncController : ControllerBase
{
    private readonly IBankSyncService _bankSyncService;
    private readonly IMemberService _memberService;

    public BankSyncController(IBankSyncService bankSyncService, IMemberService memberService)
    {
        _bankSyncService = bankSyncService;
        _memberService   = memberService;
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

    /// <summary>
    /// Upload a bank statement (CSV / XLS / XLSX / PDF).
    /// For encrypted PDFs (HDFC, ICICI, Axis, Kotak), pass 'password' as a form field —
    /// typically the account holder's date of birth in DDMMYYYY format.
    /// Returns a session ID and a preview of parsed transactions.
    /// The user reviews the preview and calls /confirm to import.
    /// </summary>
    // POST api/bank-sync/{connectionId}/parse
    [HttpPost("{connectionId}/parse")]
    [RequestSizeLimit(15 * 1024 * 1024)] // 15 MB
    public async Task<ActionResult<ApiResponse<BankStatementPreviewDto>>> ParseStatement(
        string connectionId, IFormFile file, [FromForm] string? password = null)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse<BankStatementPreviewDto>.ErrorResponse("No file uploaded"));

            var result = await _bankSyncService.ParseStatementAsync(
                connectionId, GetUserId(), file, password);
            return Ok(ApiResponse<BankStatementPreviewDto>.SuccessResponse(result));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<BankStatementPreviewDto>.ErrorResponse("Bank connection not found"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<BankStatementPreviewDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<BankStatementPreviewDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<BankStatementPreviewDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Confirm a parsed session.
    /// Runs duplicate detection against the target book,
    /// then queues new transactions for import.
    /// </summary>
    // POST api/bank-sync/sessions/{sessionId}/confirm
    [HttpPost("sessions/{sessionId}/confirm")]
    public async Task<ActionResult<ApiResponse<BankSyncConfirmResultDto>>> ConfirmSync(
        string sessionId, [FromBody] ConfirmBankSyncRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ExpenseBookId))
                return BadRequest(ApiResponse<BankSyncConfirmResultDto>.ErrorResponse(
                    "ExpenseBookId is required"));

            var userId = GetUserId();
            var perms  = await _memberService.GetResolvedPermissionsAsync(request.ExpenseBookId, userId);
            if (perms.Expenses != "write")
                return StatusCode(403, ApiResponse<BankSyncConfirmResultDto>.ErrorResponse(
                    "You do not have write access to this book"));

            var result = await _bankSyncService.ConfirmSyncAsync(
                sessionId, userId, request, perms.AllowedCategoryIds);

            return Ok(ApiResponse<BankSyncConfirmResultDto>.SuccessResponse(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<BankSyncConfirmResultDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<BankSyncConfirmResultDto>.ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<BankSyncConfirmResultDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<BankSyncConfirmResultDto>.ErrorResponse(ex.Message));
        }
    }
}
