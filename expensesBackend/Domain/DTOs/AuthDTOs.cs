namespace ExpensesBackend.API.Domain.DTOs;

public class LoginRequest
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class VerifyOtpRequest
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Otp { get; set; } = string.Empty;
}

public class SignupRequest
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public decimal MonthlyIncome { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public decimal MonthlyIncome { get; set; }
}
