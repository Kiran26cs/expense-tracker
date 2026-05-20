using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IAuthService
{
    Task<bool> SendOtpAsync(string? email, string? phone, bool isLogin = false);
    Task<bool> VerifyOtpAsync(string? email, string? phone, string otp);
    Task<AuthResponse> SignupAsync(SignupRequest request, string otp);
    Task<AuthResponse> LoginAsync(string? email, string? phone, string otp);
    Task<AuthResponse> GoogleLoginAsync(string credential);
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<UserDto?> UpdateProfileAsync(string userId, UpdateProfileRequest req);
    string GenerateJwtToken(User user);
    string GenerateRefreshToken();
}
