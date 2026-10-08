using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IAuthService
{
    Task<bool> SendOtpAsync(string email, bool isLogin = false);
    Task<bool> VerifyOtpAsync(string email, string otp);
    Task<AuthResponse> SignupAsync(SignupRequest request, string otp);
    Task<LoginLinkResult> LoginAsync(string email, string otp);
    Task<AuthResponse> ConfirmOtpLinkAsync(string email, string otp);
    Task<LoginLinkResult> GoogleLoginAsync(string credential);
    Task<AuthResponse> ConfirmGoogleLinkAsync(string credential);
    Task<AuthResponse> RefreshTokenAsync(string sessionId, string refreshToken);
    Task LogoutAsync(string sessionId, string userId);
    Task LogoutAllAsync(string userId);
    Task<List<SessionDto>> ListSessionsAsync(string userId, string? currentSessionId);
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<UserDto?> UpdateProfileAsync(string userId, UpdateProfileRequest req);
    string GenerateJwtToken(User user);
}
