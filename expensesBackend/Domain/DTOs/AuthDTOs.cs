namespace ExpensesBackend.API.Domain.DTOs;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public bool IsLogin { get; set; } = false;
}

public class VerifyOtpRequest
{
    public string Email { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}

public class SignupRequest
{
    public string Email { get; set; } = string.Empty;
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
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlySavingsGoal { get; set; }
    public string Plan { get; set; } = "Free";
}

public class UpdateProfileRequest
{
    public string? Currency { get; set; }
    public decimal? MonthlySavingsGoal { get; set; }
}

public class GoogleAuthRequest
{
    public string Credential { get; set; } = string.Empty;
}
