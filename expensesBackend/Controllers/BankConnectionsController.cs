using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/bank-connections")]
public class BankConnectionsController : ControllerBase
{
    private readonly IBankConnectionService _bankConnectionService;
    private readonly IMemberService _memberService;

    public BankConnectionsController(
        IBankConnectionService bankConnectionService,
        IMemberService memberService)
    {
        _bankConnectionService = bankConnectionService;
        _memberService         = memberService;
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

    // GET api/bank-connections
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<BankConnectionDto>>>> GetConnections()
    {
        try
        {
            var result = await _bankConnectionService.GetConnectionsAsync(GetUserId());
            return Ok(ApiResponse<List<BankConnectionDto>>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<BankConnectionDto>>.ErrorResponse(ex.Message));
        }
    }

    // GET api/bank-connections/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<BankConnectionDto>>> GetConnection(string id)
    {
        try
        {
            var result = await _bankConnectionService.GetConnectionAsync(id, GetUserId());
            return Ok(ApiResponse<BankConnectionDto>.SuccessResponse(result));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<BankConnectionDto>.ErrorResponse("Bank connection not found"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<BankConnectionDto>.ErrorResponse(ex.Message));
        }
    }

    // POST api/bank-connections
    [HttpPost]
    public async Task<ActionResult<ApiResponse<BankConnectionDto>>> CreateConnection(
        [FromBody] CreateBankConnectionRequest request)
    {
        try
        {
            var result = await _bankConnectionService.CreateConnectionAsync(GetUserId(), request);
            return CreatedAtAction(nameof(GetConnection), new { id = result.Id },
                ApiResponse<BankConnectionDto>.SuccessResponse(result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<BankConnectionDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<BankConnectionDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<BankConnectionDto>.ErrorResponse(ex.Message));
        }
    }

    // PUT api/bank-connections/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<BankConnectionDto>>> UpdateConnection(
        string id, [FromBody] UpdateBankConnectionRequest request)
    {
        try
        {
            var result = await _bankConnectionService.UpdateConnectionAsync(id, GetUserId(), request);
            return Ok(ApiResponse<BankConnectionDto>.SuccessResponse(result));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<BankConnectionDto>.ErrorResponse("Bank connection not found"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<BankConnectionDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<BankConnectionDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<BankConnectionDto>.ErrorResponse(ex.Message));
        }
    }

    // DELETE api/bank-connections/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteConnection(string id)
    {
        try
        {
            await _bankConnectionService.DeleteConnectionAsync(id, GetUserId());
            return Ok(ApiResponse<object>.SuccessResponse(new { deleted = true }));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Bank connection not found"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    // GET api/bank-connections/books/{bookId}
    [HttpGet("books/{bookId}")]
    public async Task<ActionResult<ApiResponse<BookBankConnectionDto>>> GetBookConnection(string bookId)
    {
        try
        {
            var userId = GetUserId();
            await _memberService.EnsureHasAccessAsync(bookId, userId, "expenses:view");

            var result = await _bankConnectionService.GetBookConnectionAsync(bookId, userId);
            return Ok(ApiResponse<BookBankConnectionDto>.SuccessResponse(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<BookBankConnectionDto>.ErrorResponse(ex.Message));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<BookBankConnectionDto>.ErrorResponse("Expense book not found"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<BookBankConnectionDto>.ErrorResponse(ex.Message));
        }
    }

    // PUT api/bank-connections/books/{bookId}
    [HttpPut("books/{bookId}")]
    public async Task<ActionResult<ApiResponse<object>>> MapToBook(
        string bookId, [FromBody] MapBankConnectionRequest request)
    {
        try
        {
            var userId = GetUserId();
            // Only the book owner can map a bank connection
            var perms = await _memberService.GetResolvedPermissionsAsync(bookId, userId);
            if (perms.Expenses != "write")
                return StatusCode(403, ApiResponse<object>.ErrorResponse(
                    "You do not have write access to this book."));

            await _bankConnectionService.MapToBookAsync(bookId, userId, request.BankConnectionId);
            return Ok(ApiResponse<object>.SuccessResponse(new { mapped = true }));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    // DELETE api/bank-connections/books/{bookId}
    [HttpDelete("books/{bookId}")]
    public async Task<ActionResult<ApiResponse<object>>> UnmapFromBook(string bookId)
    {
        try
        {
            var userId = GetUserId();
            var perms  = await _memberService.GetResolvedPermissionsAsync(bookId, userId);
            if (perms.Expenses != "write")
                return StatusCode(403, ApiResponse<object>.ErrorResponse(
                    "You do not have write access to this book."));

            await _bankConnectionService.MapToBookAsync(bookId, userId, null);
            return Ok(ApiResponse<object>.SuccessResponse(new { unmapped = true }));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }
}
