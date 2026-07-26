using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers.Admin;

[ApiController]
[Route("api/admin/auth")]
public class PlatformAdminAuthController : ControllerBase
{
    private readonly IPlatformAdminAuthService _adminAuth;

    public PlatformAdminAuthController(IPlatformAdminAuthService adminAuth)
    {
        _adminAuth = adminAuth;
    }

    [HttpPost("send-otp")]
    public async Task<ActionResult<ApiResponse<object>>> SendOtp(
        [FromBody] AdminSendOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(ApiResponse<object>.ErrorResponse("Email is required."));

        // Always return success — do not reveal whether the email is a registered admin
        await _adminAuth.SendOtpAsync(request.Email);
        return Ok(ApiResponse<object>.SuccessResponse(new { message = "OTP sent if email is registered." }));
    }

    [HttpPost("verify-otp")]
    public async Task<ActionResult<ApiResponse<AdminAuthResponse>>> VerifyOtp(
        [FromBody] AdminVerifyOtpRequest request)
    {
        try
        {
            var response = await _adminAuth.LoginAsync(request.Email, request.Otp);
            return Ok(ApiResponse<AdminAuthResponse>.SuccessResponse(response));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AdminAuthResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AdminAuthResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("me")]
    [Authorize(Policy = "PlatformAdmin")]
    public async Task<ActionResult<ApiResponse<AdminDto>>> Me()
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return Unauthorized(ApiResponse<AdminDto>.ErrorResponse("Unauthorized."));

        var admin = await _adminAuth.GetAdminByIdAsync(adminId);
        if (admin == null)
            return NotFound(ApiResponse<AdminDto>.ErrorResponse("Admin not found."));

        return Ok(ApiResponse<AdminDto>.SuccessResponse(admin));
    }
}
