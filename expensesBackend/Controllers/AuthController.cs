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
            var result = await _authService.SendOtpAsync(request.Email, request.Phone);
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
            var result = await _authService.VerifyOtpAsync(request.Email, request.Phone, request.Otp);
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
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request, [FromQuery] string otp)
    {
        try
        {
            var result = await _authService.LoginAsync(request.Email, request.Phone, otp);
            return Ok(ApiResponse<AuthResponse>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AuthResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("google")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> GoogleLogin([FromBody] GoogleAuthRequest request)
    {
        try
        {
            var result = await _authService.GoogleLoginAsync(request.Credential);
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

    [Authorize]
    [HttpGet("me")]
    public ActionResult<ApiResponse<UserDto>> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<UserDto>.ErrorResponse("User not authenticated"));

        // TODO: Fetch user from database
        return Ok(ApiResponse<UserDto>.SuccessResponse(new UserDto { Id = userId }));
    }
}
