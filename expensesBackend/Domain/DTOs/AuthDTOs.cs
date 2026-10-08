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
    public string SessionId { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
}

public class RefreshRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequest
{
    public string SessionId { get; set; } = string.Empty;
}

public class SessionDto
{
    public string Id { get; set; } = string.Empty;
    public string DeviceLabel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public bool IsCurrent { get; set; }
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
    public bool HasPrivacyPin { get; set; }
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

public class AccountLinkPreviewDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// Shared by both GoogleLoginAsync and LoginAsync — either flow can discover an existing
// account that was created via the other method and needs one-time link confirmation.
public class LoginLinkResult
{
    public bool RequiresLinking { get; set; }
    public AccountLinkPreviewDto? Preview { get; set; }
    public AuthResponse? Auth { get; set; }
}
