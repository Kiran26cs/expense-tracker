using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers.Admin;

[Authorize(Policy = "PlatformAdmin")]
[ApiController]
[Route("api/admin/books")]
public class AdminBooksController : ControllerBase
{
    private readonly IAdminBookService _books;

    public AdminBooksController(IAdminBookService books) => _books = books;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<AdminBookListDto>>> List(
        [FromQuery] string? search,
        [FromQuery] bool? aiEnabled,
        [FromQuery] string? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _books.GetBooksAsync(search, aiEnabled, userId, page, Math.Min(pageSize, 100));
        return Ok(ApiResponse<AdminBookListDto>.SuccessResponse(result));
    }

    [HttpPatch("{bookId}/ai-chat")]
    public async Task<ActionResult<ApiResponse<AdminBookSummaryDto>>> ToggleAiChat(
        string bookId, [FromBody] AdminToggleAiChatRequest req)
    {
        try
        {
            var result = await _books.ToggleAiChatAsync(bookId, req.Enabled, GetAdminId(), GetAdminEmail());
            return Ok(ApiResponse<AdminBookSummaryDto>.SuccessResponse(result));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<AdminBookSummaryDto>.ErrorResponse(ex.Message)); }
    }

    [HttpDelete("{bookId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteBook(string bookId)
    {
        try
        {
            await _books.DeleteBookAsync(bookId, GetAdminId(), GetAdminEmail());
            return Ok(ApiResponse<object>.SuccessResponse(new { message = "Book deleted." }));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.ErrorResponse(ex.Message)); }
    }

    private string GetAdminId()    => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    private string GetAdminEmail() => User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
}
