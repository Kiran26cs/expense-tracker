using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("send-otp")]
    public async Task<ActionResult<ApiResponse<bool>>> SendOtp([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.SendOtpAsync(request.Email, request.IsLogin);
            return Ok(ApiResponse<bool>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("verify-otp")]
    public async Task<ActionResult<ApiResponse<bool>>> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        try
        {
            var result = await _authService.VerifyOtpAsync(request.Email, request.Otp);
            return Ok(ApiResponse<bool>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("signup")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Signup([FromBody] SignupRequest request, [FromQuery] string otp)
    {
        try
        {
            var result = await _authService.SignupAsync(request, otp);
            return Ok(ApiResponse<AuthResponse>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AuthResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginLinkResult>>> Login([FromBody] LoginRequest request, [FromQuery] string otp)
    {
        try
        {
            var result = await _authService.LoginAsync(request.Email, otp);
            return Ok(ApiResponse<LoginLinkResult>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            // Kept as a plain 400 (not 401) — /Auth/login is in the frontend interceptor's
            // auth-bootstrap exclusion list, where a 401 is swallowed and treated as "session
            // expired" instead of surfacing the real error inline on the login form.
            return BadRequest(ApiResponse<LoginLinkResult>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("login/confirm-link")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> ConfirmOtpLink([FromBody] VerifyOtpRequest request)
    {
        try
        {
            var result = await _authService.ConfirmOtpLinkAsync(request.Email, request.Otp);
            return Ok(ApiResponse<AuthResponse>.SuccessResponse(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AuthResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AuthResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("google")]
    public async Task<ActionResult<ApiResponse<LoginLinkResult>>> GoogleLogin([FromBody] GoogleAuthRequest request)
    {
        try
        {
            var result = await _authService.GoogleLoginAsync(request.Credential);
            return Ok(ApiResponse<LoginLinkResult>.SuccessResponse(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<LoginLinkResult>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<LoginLinkResult>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("google/confirm-link")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> ConfirmGoogleLink([FromBody] GoogleAuthRequest request)
    {
        try
        {
            var result = await _authService.ConfirmGoogleLinkAsync(request.Credential);
            return Ok(ApiResponse<AuthResponse>.SuccessResponse(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AuthResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AuthResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh([FromBody] RefreshRequest request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request.SessionId, request.RefreshToken);
            return Ok(ApiResponse<AuthResponse>.SuccessResponse(result));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AuthResponse>.ErrorResponse(ex.Message));
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<bool>>> Logout([FromBody] LogoutRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<bool>.ErrorResponse("User not authenticated"));

        await _authService.LogoutAsync(request.SessionId, userId);
        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<ActionResult<ApiResponse<bool>>> LogoutAll()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<bool>.ErrorResponse("User not authenticated"));

        await _authService.LogoutAllAsync(userId);
        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    [Authorize]
    [HttpGet("sessions")]
    public async Task<ActionResult<ApiResponse<List<SessionDto>>>> GetSessions([FromQuery] string? currentSessionId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<List<SessionDto>>.ErrorResponse("User not authenticated"));

        var sessions = await _authService.ListSessionsAsync(userId, currentSessionId);
        return Ok(ApiResponse<List<SessionDto>>.SuccessResponse(sessions));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<UserDto>.ErrorResponse("User not authenticated"));

        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null)
            return NotFound(ApiResponse<UserDto>.ErrorResponse("User not found"));

        return Ok(ApiResponse<UserDto>.SuccessResponse(user));
    }

    [Authorize]
    [HttpPatch("profile")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<UserDto>.ErrorResponse("User not authenticated"));

        var updated = await _authService.UpdateProfileAsync(userId, req);
        if (updated == null)
            return NotFound(ApiResponse<UserDto>.ErrorResponse("User not found"));

        return Ok(ApiResponse<UserDto>.SuccessResponse(updated));
    }
}
