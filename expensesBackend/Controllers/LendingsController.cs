using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/expensebooks/{bookId}/lendings")]
public class LendingsController : ControllerBase
{
    private readonly ILendingService _lendingService;

    public LendingsController(ILendingService lendingService)
    {
        _lendingService = lendingService;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

    // GET api/expensebooks/{bookId}/lendings?status=active
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<LendingDto>>>> GetLendings(
        string bookId,
        [FromQuery] string? status = null)
    {
        try
        {
            var userId = GetUserId();
            var lendings = await _lendingService.GetLendingsAsync(userId, bookId, status);
            return Ok(ApiResponse<List<LendingDto>>.SuccessResponse(lendings));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<List<LendingDto>>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<LendingDto>>.ErrorResponse(ex.Message));
        }
    }

    // GET api/expensebooks/{bookId}/lendings/{lendingId}
    [HttpGet("{lendingId}")]
    public async Task<ActionResult<ApiResponse<LendingDto>>> GetLending(string bookId, string lendingId)
    {
        try
        {
            var userId = GetUserId();
            var lending = await _lendingService.GetLendingByIdAsync(userId, bookId, lendingId);
            return Ok(ApiResponse<LendingDto>.SuccessResponse(lending));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<LendingDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<LendingDto>.ErrorResponse(ex.Message));
        }
    }

    // POST api/expensebooks/{bookId}/lendings
    [HttpPost]
    public async Task<ActionResult<ApiResponse<LendingDto>>> CreateLending(
        string bookId,
        [FromBody] CreateLendingRequest request)
    {
        try
        {
            request.ExpenseBookId = bookId;
            var userId = GetUserId();
            var lending = await _lendingService.CreateLendingAsync(userId, request);
            return CreatedAtAction(
                nameof(GetLending),
                new { bookId, lendingId = lending.Id },
                ApiResponse<LendingDto>.SuccessResponse(lending));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<LendingDto>.ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<LendingDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<LendingDto>.ErrorResponse(ex.Message));
        }
    }

    // PUT api/expensebooks/{bookId}/lendings/{lendingId}
    [HttpPut("{lendingId}")]
    public async Task<ActionResult<ApiResponse<LendingDto>>> UpdateLending(
        string bookId,
        string lendingId,
        [FromBody] UpdateLendingRequest request)
    {
        try
        {
            var userId = GetUserId();
            var lending = await _lendingService.UpdateLendingAsync(userId, bookId, lendingId, request);
            return Ok(ApiResponse<LendingDto>.SuccessResponse(lending));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<LendingDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<LendingDto>.ErrorResponse(ex.Message));
        }
    }

    // POST api/expensebooks/{bookId}/lendings/{lendingId}/settle
    [HttpPost("{lendingId}/settle")]
    public async Task<ActionResult<ApiResponse<bool>>> SettleLending(string bookId, string lendingId, [FromBody] SettleLendingRequest? request)
    {
        try
        {
            var userId = GetUserId();
            await _lendingService.SettleLendingAsync(userId, bookId, lendingId, request?.InterestCollected, request?.SettlementDate, request?.Notes);
            return Ok(ApiResponse<bool>.SuccessResponse(true));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    // DELETE api/expensebooks/{bookId}/lendings/{lendingId}
    [HttpDelete("{lendingId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteLending(string bookId, string lendingId)
    {
        try
        {
            var userId = GetUserId();
            await _lendingService.DeleteLendingAsync(userId, bookId, lendingId);
            return Ok(ApiResponse<bool>.SuccessResponse(true));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    // GET api/expensebooks/{bookId}/lendings/{lendingId}/repayments
    [HttpGet("{lendingId}/repayments")]
    public async Task<ActionResult<ApiResponse<LendingRepaymentsResponse>>> GetRepayments(
        string bookId,
        string lendingId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var userId = GetUserId();
            var result = await _lendingService.GetRepaymentsAsync(userId, bookId, lendingId, page, pageSize);
            return Ok(ApiResponse<LendingRepaymentsResponse>.SuccessResponse(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<LendingRepaymentsResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<LendingRepaymentsResponse>.ErrorResponse(ex.Message));
        }
    }

    // POST api/expensebooks/{bookId}/lendings/{lendingId}/repayments
    [HttpPost("{lendingId}/repayments")]
    public async Task<ActionResult<ApiResponse<RepaymentDto>>> AddRepayment(
        string bookId,
        string lendingId,
        [FromBody] CreateRepaymentRequest request)
    {
        try
        {
            var userId = GetUserId();
            var repayment = await _lendingService.AddRepaymentAsync(userId, bookId, lendingId, request);
            return Ok(ApiResponse<RepaymentDto>.SuccessResponse(repayment));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<RepaymentDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<RepaymentDto>.ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<RepaymentDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<RepaymentDto>.ErrorResponse(ex.Message));
        }
    }

    // DELETE api/expensebooks/{bookId}/lendings/{lendingId}/repayments/{repaymentId}
    [HttpDelete("{lendingId}/repayments/{repaymentId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRepayment(
        string bookId,
        string lendingId,
        string repaymentId)
    {
        try
        {
            var userId = GetUserId();
            await _lendingService.DeleteRepaymentAsync(userId, bookId, lendingId, repaymentId);
            return Ok(ApiResponse<bool>.SuccessResponse(true));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }
}
