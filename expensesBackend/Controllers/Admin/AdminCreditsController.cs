using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers.Admin;

[Authorize(Policy = "PlatformAdmin")]
[ApiController]
[Route("api/admin/credits")]
public class AdminCreditsController : ControllerBase
{
    private readonly IAdminCreditService _credits;

    public AdminCreditsController(IAdminCreditService credits) => _credits = credits;

    [HttpGet("{bookId}")]
    public async Task<ActionResult<ApiResponse<AdminCreditBalanceDto>>> GetBalance(string bookId)
    {
        var result = await _credits.GetBalanceAsync(bookId);
        if (result == null) return NotFound(ApiResponse<AdminCreditBalanceDto>.ErrorResponse("Book not found."));
        return Ok(ApiResponse<AdminCreditBalanceDto>.SuccessResponse(result));
    }

    [HttpPost("{bookId}/grant")]
    public async Task<ActionResult<ApiResponse<AdminCreditBalanceDto>>> Grant(
        string bookId, [FromBody] AdminGrantCreditsRequest req)
    {
        try
        {
            var result = await _credits.GrantCreditsAsync(bookId, req.Amount, req.Note, GetAdminId(), GetAdminEmail());
            return Ok(ApiResponse<AdminCreditBalanceDto>.SuccessResponse(result));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<AdminCreditBalanceDto>.ErrorResponse(ex.Message)); }
        catch (ArgumentException ex)    { return BadRequest(ApiResponse<AdminCreditBalanceDto>.ErrorResponse(ex.Message)); }
    }

    [HttpGet("{bookId}/transactions")]
    public async Task<ActionResult<ApiResponse<AdminCreditTransactionListDto>>> Transactions(
        string bookId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _credits.GetTransactionsAsync(bookId, page, Math.Min(pageSize, 100));
        return Ok(ApiResponse<AdminCreditTransactionListDto>.SuccessResponse(result));
    }

    private string GetAdminId()    => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    private string GetAdminEmail() => User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
}
