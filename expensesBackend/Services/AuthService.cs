using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using Google.Apis.Auth;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ExpensesBackend.API.Services;

public class AuthService : IAuthService
{
    private readonly MongoDbContext _context;
    private readonly IConfiguration _configuration;
    private const int OTP_LENGTH = 6;
    private const int OTP_EXPIRY_MINUTES = 5;
    private const int MAX_OTP_ATTEMPTS = 3;

    public AuthService(MongoDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<bool> SendOtpAsync(string? email, string? phone)
    {
        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phone))
            return false;

        // Generate 6-digit OTP
        var otp = GenerateOtp();
        var expiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES);

        // Create OTP record
        var otpRecord = new OtpRecord
        {
            Email = email,
            Phone = phone,
            Otp = otp,
            ExpiresAt = expiresAt,
            Attempts = 0,
            Verified = false
        };

        // Delete any existing OTP for this email/phone
        var filter = Builders<OtpRecord>.Filter.Or(
            Builders<OtpRecord>.Filter.Eq(o => o.Email, email),
            Builders<OtpRecord>.Filter.Eq(o => o.Phone, phone)
        );
        await _context.OtpRecords.DeleteManyAsync(filter);

        // Insert new OTP
        await _context.OtpRecords.InsertOneAsync(otpRecord);

        // TODO: Replace with actual Email/SMS service
        Console.WriteLine($"✓ OTP sent to {email ?? phone}: {otp} (Expires in {OTP_EXPIRY_MINUTES} minutes)");

        return await Task.FromResult(true);
    }

    public async Task<bool> VerifyOtpAsync(string? email, string? phone, string otp)
    {
        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phone))
            return false;

        // Find OTP record
        var filter = Builders<OtpRecord>.Filter.Or(
            Builders<OtpRecord>.Filter.Eq(o => o.Email, email),
            Builders<OtpRecord>.Filter.Eq(o => o.Phone, phone)
        );
        // Also check that OTP is not expired
        filter = filter & Builders<OtpRecord>.Filter.Gt(o => o.ExpiresAt, DateTime.UtcNow);

        var otpRecord = await _context.OtpRecords
            .Find(filter)
            .FirstOrDefaultAsync();

        if (otpRecord == null)
            return false;

        // Check attempts
        if (otpRecord.Attempts >= MAX_OTP_ATTEMPTS)
        {
            await _context.OtpRecords.DeleteOneAsync(Builders<OtpRecord>.Filter.Eq(o => o.Id, otpRecord.Id));
            return false;
        }

        // Verify OTP
        if (otpRecord.Otp != otp)
        {
            // Increment attempts
            var update = Builders<OtpRecord>.Update.Inc(o => o.Attempts, 1);
            await _context.OtpRecords.UpdateOneAsync(
                Builders<OtpRecord>.Filter.Eq(o => o.Id, otpRecord.Id),
                update
            );
            return false;
        }

        // Mark as verified
        var verifyUpdate = Builders<OtpRecord>.Update.Set(o => o.Verified, true);
        await _context.OtpRecords.UpdateOneAsync(
            Builders<OtpRecord>.Filter.Eq(o => o.Id, otpRecord.Id),
            verifyUpdate
        );

        return true;
    }

    /// <summary>
    /// Check if OTP has already been verified (used for signup/login, not for initial verification)
    /// </summary>
    private async Task<bool> IsOtpVerifiedAsync(string? email, string? phone, string otp)
    {
        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(phone))
            return false;

        // Find OTP record by email or phone
        var filter = Builders<OtpRecord>.Filter.Or(
            Builders<OtpRecord>.Filter.Eq(o => o.Email, email),
            Builders<OtpRecord>.Filter.Eq(o => o.Phone, phone)
        );
        
        var otpRecord = await _context.OtpRecords.Find(filter).FirstOrDefaultAsync();
        
        if (otpRecord == null)
            return false;
        
        // Verify OTP code matches
        if (otpRecord.Otp != otp)
            return false;
        
        // Check it's marked as verified
        if (!otpRecord.Verified)
            return false;
        
        // Check not expired
        if (DateTime.UtcNow > otpRecord.ExpiresAt)
            return false;
        
        return true;
    }

    public async Task<AuthResponse> SignupAsync(SignupRequest request, string otp)
    {
        if (!await IsOtpVerifiedAsync(request.Email, request.Phone, otp))
            throw new UnauthorizedAccessException("Invalid or expired OTP. Please verify OTP first.");

        var existingUser = await _context.Users
            .Find(u => u.Email == request.Email || u.Phone == request.Phone)
            .FirstOrDefaultAsync();

        if (existingUser != null)
            throw new InvalidOperationException("User already exists");

        var user = new User
        {
            Email = request.Email,
            Phone = request.Phone,
            Name = request.Name,
            Currency = request.Currency,
            MonthlyIncome = request.MonthlyIncome
        };

        await _context.Users.InsertOneAsync(user);

        return new AuthResponse
        {
            Token = GenerateJwtToken(user),
            RefreshToken = GenerateRefreshToken(),
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponse> LoginAsync(string? email, string? phone, string otp)
    {
        if (!await IsOtpVerifiedAsync(email, phone, otp))
            throw new UnauthorizedAccessException("Invalid or expired OTP. Please verify OTP first.");

        var user = await _context.Users
            .Find(u => u.Email == email || u.Phone == phone)
            .FirstOrDefaultAsync();

        if (user == null)
            throw new UnauthorizedAccessException("User not found");

        return new AuthResponse
        {
            Token = GenerateJwtToken(user),
            RefreshToken = GenerateRefreshToken(),
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponse> GoogleLoginAsync(string credential)
    {
        var googleClientId = _configuration["Google:ClientId"] 
            ?? throw new InvalidOperationException("Google ClientId not configured");

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { googleClientId }
        };

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(credential, settings);
        }
        catch (InvalidJwtException)
        {
            throw new UnauthorizedAccessException("Invalid Google token");
        }

        // Find existing user by email
        var user = await _context.Users
            .Find(u => u.Email == payload.Email)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            // Auto-create user from Google profile
            user = new User
            {
                Email = payload.Email,
                Name = payload.Name ?? payload.Email ?? "Google User",
                Currency = "INR",
                MonthlyIncome = 0
            };
            await _context.Users.InsertOneAsync(user);
        }

        return new AuthResponse
        {
            Token = GenerateJwtToken(user),
            RefreshToken = GenerateRefreshToken(),
            User = MapToUserDto(user)
        };
    }

    public string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] ?? "your-super-secret-key-min-32-chars-long"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "ExpensesBackend",
            audience: _configuration["Jwt:Audience"] ?? "ExpensesBackend",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private string GenerateOtp()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }

    private UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Phone = user.Phone,
            Name = user.Name,
            Currency = user.Currency,
            MonthlyIncome = user.MonthlyIncome
        };
    }
}
