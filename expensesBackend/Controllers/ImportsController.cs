using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/{bookId}/imports")]
public class ImportsController : ControllerBase
{
    private readonly IImportService _importService;
    private readonly IMemberService _memberService;

    public ImportsController(IImportService importService, IMemberService memberService)
    {
        _importService = importService;
        _memberService = memberService;
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

    // POST api/{bookId}/imports
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ImportSessionDto>>> StartImport(
        string bookId, [FromBody] StartImportRequest request)
    {
        try
        {
            var userId = GetUserId();
            var perms  = await _memberService.GetResolvedPermissionsAsync(bookId, userId);
            if (perms.Expenses != "write")
                return StatusCode(403, ApiResponse<ImportSessionDto>.ErrorResponse(
                    "You do not have write access to expenses in this book."));

            var session = await _importService.CreateImportSessionAsync(
                bookId, userId, request, perms.AllowedCategoryIds);

            return Accepted(ApiResponse<ImportSessionDto>.SuccessResponse(session));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<ImportSessionDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ImportSessionDto>.ErrorResponse(ex.Message));
        }
    }

    // GET api/{bookId}/imports
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ImportSessionSummaryDto>>>> GetImports(string bookId)
    {
        try
        {
            var userId = GetUserId();
            await _memberService.EnsureHasAccessAsync(bookId, userId, "expenses:view");

            var sessions = await _importService.GetImportSessionsAsync(bookId, userId);
            return Ok(ApiResponse<List<ImportSessionSummaryDto>>.SuccessResponse(sessions));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<List<ImportSessionSummaryDto>>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<ImportSessionSummaryDto>>.ErrorResponse(ex.Message));
        }
    }

    // GET api/{bookId}/imports/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ImportSessionDto>>> GetImport(string bookId, string id)
    {
        try
        {
            var userId = GetUserId();
            await _memberService.EnsureHasAccessAsync(bookId, userId, "expenses:view");

            var session = await _importService.GetImportSessionByIdAsync(id, bookId, userId);
            return Ok(ApiResponse<ImportSessionDto>.SuccessResponse(session));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<ImportSessionDto>.ErrorResponse(ex.Message));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<ImportSessionDto>.ErrorResponse("Import session not found"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ImportSessionDto>.ErrorResponse(ex.Message));
        }
    }

    // POST api/{bookId}/imports/{id}/retry
    [HttpPost("{id}/retry")]
    public async Task<ActionResult<ApiResponse<ImportSessionDto>>> RetryImport(
        string bookId, string id, [FromBody] RetryImportRequest? request)
    {
        try
        {
            var userId = GetUserId();
            var perms  = await _memberService.GetResolvedPermissionsAsync(bookId, userId);
            if (perms.Expenses != "write")
                return StatusCode(403, ApiResponse<ImportSessionDto>.ErrorResponse(
                    "You do not have write access to expenses in this book."));

            var rowNumbers = request?.RowNumbers?.Count > 0 ? request.RowNumbers : null;
            var session = await _importService.RetryFailedAsync(
                id, bookId, userId, perms.AllowedCategoryIds, rowNumbers);

            return Accepted(ApiResponse<ImportSessionDto>.SuccessResponse(session));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<ImportSessionDto>.ErrorResponse("Import session not found"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ImportSessionDto>.ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<ImportSessionDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ImportSessionDto>.ErrorResponse(ex.Message));
        }
    }
}
