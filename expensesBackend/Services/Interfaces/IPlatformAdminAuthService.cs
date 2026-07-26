using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IPlatformAdminAuthService
{
    Task<bool> SendOtpAsync(string email);
    Task<AdminAuthResponse> LoginAsync(string email, string otp);
    Task<AdminDto?> GetAdminByIdAsync(string adminId);
}
