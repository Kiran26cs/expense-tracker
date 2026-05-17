using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/expensebooks/{bookId}/credits")]
public class CreditsController : ControllerBase
{
    private readonly ICreditService _credits;
    private readonly IPermissionService _permissions;

    public CreditsController(ICreditService credits, IPermissionService permissions)
    {
        _credits     = credits;
        _permissions = permissions;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<CreditBalanceDto>>> GetBalance(string bookId)
    {
        try
        {
            await _permissions.AssertIsMemberAsync(bookId, GetUserId());
            var balance = await _credits.GetBalanceAsync(bookId);
            return Ok(ApiResponse<CreditBalanceDto>.SuccessResponse(balance));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<CreditBalanceDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<CreditBalanceDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>Owner-only: manually grant paid credits to a book (phase-1 stand-in for Razorpay).</summary>
    [HttpPost("admin-grant")]
    public async Task<ActionResult<ApiResponse<CreditBalanceDto>>> AdminGrant(
        string bookId, [FromBody] AdminGrantRequest request)
    {
        try
        {
            await _permissions.AssertIsOwnerAsync(bookId, GetUserId());

            if (request.Amount <= 0)
                return BadRequest(ApiResponse<CreditBalanceDto>.ErrorResponse("Amount must be greater than zero."));

            await _credits.AdminGrantAsync(bookId, request.Amount, GetUserId());
            var balance = await _credits.GetBalanceAsync(bookId);
            return Ok(ApiResponse<CreditBalanceDto>.SuccessResponse(balance));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<CreditBalanceDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<CreditBalanceDto>.ErrorResponse(ex.Message));
        }
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
}
