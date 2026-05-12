using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/expensebooks/template")]
public class TemplateBookController : ControllerBase
{
    private readonly ITemplateBookService _templateService;

    public TemplateBookController(ITemplateBookService templateService)
    {
        _templateService = templateService;
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

    // POST api/expensebooks/template
    [HttpPost]
    public async Task<ActionResult<ApiResponse<StartTemplateBookResponse>>> CreateFromTemplate(
        [FromBody] CreateTemplateBookRequest request)
    {
        try
        {
            var userId = GetUserId();
            var result = await _templateService.StartTemplateCreationAsync(
                userId, request.Currency ?? "USD");

            if (result.AlreadyExists)
                return Conflict(ApiResponse<StartTemplateBookResponse>.ErrorResponse(
                    "You already have a demo expense book."));

            return Accepted(ApiResponse<StartTemplateBookResponse>.SuccessResponse(
                new StartTemplateBookResponse
                {
                    BookId    = result.BookId!,
                    SessionId = result.SessionId!
                }));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<StartTemplateBookResponse>.ErrorResponse(ex.Message));
        }
    }
}
