using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/expensebooks/{bookId}/members")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;
    private readonly IConfiguration _configuration;

    public MembersController(IMemberService memberService, IConfiguration configuration)
    {
        _memberService   = memberService;
        _configuration   = configuration;
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("User ID not found in token");

    private string GetUserEmail() =>
        User.FindFirst("email")?.Value
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? string.Empty;

    /// <summary>Returns categories accessible to the current user for this book (used to populate invite/edit modal).</summary>
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetAccessibleCategories(string bookId)
    {
        try
        {
            var categories = await _memberService.GetAccessibleCategoriesAsync(bookId, GetUserId());
            return Ok(ApiResponse<List<CategoryDto>>.SuccessResponse(categories));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<List<CategoryDto>>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<CategoryDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>List all members of an expense book.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ExpenseBookMemberDto>>>> GetMembers(string bookId)
    {
        try
        {
            var members = await _memberService.GetMembersAsync(bookId, GetUserId());
            return Ok(ApiResponse<List<ExpenseBookMemberDto>>.SuccessResponse(members));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<List<ExpenseBookMemberDto>>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<ExpenseBookMemberDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>Get the resolved permissions for the current user in this book.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<ResolvedPermissions>>> GetMyPermissions(string bookId)
    {
        try
        {
            var perms = await _memberService.GetResolvedPermissionsAsync(bookId, GetUserId());
            return Ok(ApiResponse<ResolvedPermissions>.SuccessResponse(perms));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ResolvedPermissions>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>Invite a user to the expense book by email.</summary>
    [HttpPost("invite")]
    public async Task<ActionResult<ApiResponse<InviteMemberResponse>>> InviteMember(
        string bookId, [FromBody] InviteMemberRequest request)
    {
        try
        {
            var baseUrl = _configuration["App:FrontendUrl"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}";
            var result  = await _memberService.InviteMemberAsync(bookId, GetUserId(), request, baseUrl);
            return Ok(ApiResponse<InviteMemberResponse>.SuccessResponse(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<InviteMemberResponse>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<InviteMemberResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<InviteMemberResponse>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>Update role/permissions for an existing member.</summary>
    [HttpPut("{memberId}")]
    public async Task<ActionResult<ApiResponse<ExpenseBookMemberDto>>> UpdateMember(
        string bookId, string memberId, [FromBody] UpdateMemberRequest request)
    {
        try
        {
            var member = await _memberService.UpdateMemberAsync(bookId, memberId, GetUserId(), request);
            return Ok(ApiResponse<ExpenseBookMemberDto>.SuccessResponse(member));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<ExpenseBookMemberDto>.ErrorResponse(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<ExpenseBookMemberDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ExpenseBookMemberDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ExpenseBookMemberDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>Remove (revoke) a member from the expense book.</summary>
    [HttpDelete("{memberId}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveMember(string bookId, string memberId)
    {
        try
        {
            await _memberService.RemoveMemberAsync(bookId, memberId, GetUserId());
            return Ok(ApiResponse<object>.SuccessResponse(new { }));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>List all pending invites addressed to the current user's email.</summary>
    [HttpGet("~/api/members/pending")]
    public async Task<ActionResult<ApiResponse<List<PendingInviteDto>>>> GetPendingInvites()
    {
        try
        {
            var email = GetUserEmail();
            if (string.IsNullOrEmpty(email))
                return BadRequest(ApiResponse<List<PendingInviteDto>>.ErrorResponse("User email not found in token."));

            var result = await _memberService.GetPendingInvitesAsync(email);
            return Ok(ApiResponse<List<PendingInviteDto>>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<List<PendingInviteDto>>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>Decline (revoke) a pending invite by token.</summary>
    [HttpPost("~/api/members/decline")]
    public async Task<ActionResult<ApiResponse<object>>> DeclineInvite([FromQuery] string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(ApiResponse<object>.ErrorResponse("Token is required."));

            var email = GetUserEmail();
            if (string.IsNullOrEmpty(email))
                return BadRequest(ApiResponse<object>.ErrorResponse("User email not found in token."));

            await _memberService.DeclineInviteAsync(token, email);
            return Ok(ApiResponse<object>.SuccessResponse(new { }));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Accept an invite token. The user must be authenticated.
    /// Route escapes the parent path prefix using '~/'.
    /// </summary>
    [HttpPost("~/api/members/accept")]
    public async Task<ActionResult<ApiResponse<AcceptInviteResponse>>> AcceptInvite([FromQuery] string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(ApiResponse<AcceptInviteResponse>.ErrorResponse("Token is required."));

            var result = await _memberService.AcceptInviteAsync(token, GetUserId());
            return Ok(ApiResponse<AcceptInviteResponse>.SuccessResponse(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(401, ApiResponse<AcceptInviteResponse>.ErrorResponse(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<AcceptInviteResponse>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<AcceptInviteResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AcceptInviteResponse>.ErrorResponse(ex.Message));
        }
    }
}
