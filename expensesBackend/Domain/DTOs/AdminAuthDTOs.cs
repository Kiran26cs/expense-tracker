namespace ExpensesBackend.API.Domain.DTOs;

public class AdminSendOtpRequest
{
    public string Email { get; set; } = string.Empty;
}

public class AdminVerifyOtpRequest
{
    public string Email { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}

public class AdminAuthResponse
{
    public string Token { get; set; } = string.Empty;
    public AdminDto Admin { get; set; } = new();
}

public class AdminDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = [];
    public DateTime? LastLoginAt { get; set; }
}
