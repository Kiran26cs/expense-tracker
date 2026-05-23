using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.AI;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json.Nodes;

namespace ExpensesBackend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/ai")]
public class AiChatController : ControllerBase
{
    private readonly IPermissionService _permissions;
    private readonly ICreditService _credits;
    private readonly IExpenseBookService _bookService;
    private readonly IAuthService _authService;
    private readonly ClaudeOrchestrator _orchestrator;
    private readonly SystemPromptBuilder _promptBuilder;
    private readonly ToolRegistry _toolRegistry;

    public AiChatController(
        IPermissionService permissions,
        ICreditService credits,
        IExpenseBookService bookService,
        IAuthService authService,
        ClaudeOrchestrator orchestrator,
        SystemPromptBuilder promptBuilder,
        ToolRegistry toolRegistry)
    {
        _permissions  = permissions;
        _credits      = credits;
        _bookService  = bookService;
        _authService  = authService;
        _orchestrator = orchestrator;
        _promptBuilder = promptBuilder;
        _toolRegistry = toolRegistry;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ApiResponse<AiChatResponse>>> Chat([FromBody] AiChatRequest request)
    {
        try
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(request.Message))
                return BadRequest(ApiResponse<AiChatResponse>.ErrorResponse("Message cannot be empty."));

            if (string.IsNullOrEmpty(request.BookId))
                return BadRequest(ApiResponse<AiChatResponse>.ErrorResponse("BookId is required."));

            // 1. Load the book and verify AI chat is enabled
            var book = await _bookService.GetExpenseBookByIdAsync(userId, request.BookId);
            if (book == null)
                return NotFound(ApiResponse<AiChatResponse>.ErrorResponse("Expense book not found."));

            if (!book.AiChatEnabled)
                return StatusCode(403, ApiResponse<AiChatResponse>.ErrorResponse(
                    "AI Chat is not enabled for this expense book. The owner can enable it in Settings."));

            // 2. Verify the user is a member
            await _permissions.AssertIsMemberAsync(request.BookId, userId);

            // 3. Credit check (book-level)
            var hasCredits = await _credits.HasCreditsAsync(request.BookId);
            if (!hasCredits)
                return StatusCode(402, ApiResponse<AiChatResponse>.ErrorResponse(
                    "This expense book has no AI credits left. The book owner can add more credits."));

            // 4. Resolve permissions — done once for the entire chat turn
            var resolvedPerms = await _permissions.ResolveAsync(request.BookId, userId);

            // 5. Get user email for system prompt
            var user = await _authService.GetUserByIdAsync(userId);
            var userEmail = user?.Email ?? string.Empty;

            // 6. Build system prompt with full context (BuildAsync accepts ExpenseBookResponse)
            var systemPrompt = await _promptBuilder.BuildAsync(
                book, userId, userEmail, resolvedPerms, request.ReferenceContext);

            // 7. Build tool execution context (carries perms — no re-resolution per tool)
            var execCtx = new ToolExecutionContext
            {
                UserId      = userId,
                BookId      = request.BookId,
                Permissions = resolvedPerms,
            };

            // 8. Get tool definitions filtered by user permissions
            var tools = _toolRegistry.GetDefinitions(resolvedPerms);

            // 9. Run the agentic loop (pass up to last 20 history messages for context)
            var history = request.History.TakeLast(20).ToList();
            (string reply, List<string> toolsUsed) = await _orchestrator.RunAsync(
                systemPrompt,
                request.Message,
                tools,
                (string name, JsonObject args) => _toolRegistry.ExecuteAsync(name, args, execCtx),
                history);

            // 10. Deduct credit and get updated balance
            await _credits.DeductAsync(request.BookId, userId, toolsUsed);
            var balance = await _credits.GetBalanceAsync(request.BookId);

            return Ok(ApiResponse<AiChatResponse>.SuccessResponse(new AiChatResponse
            {
                Reply       = reply,
                CreditsUsed = 1,
                CreditsLeft = balance.TotalCreditsLeft,
                ToolsUsed   = toolsUsed,
            }));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<AiChatResponse>.ErrorResponse(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<AiChatResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AiChatResponse>.ErrorResponse(ex.Message));
        }
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
}
