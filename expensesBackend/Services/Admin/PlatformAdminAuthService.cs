using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ExpensesBackend.API.Services.Admin;

public class PlatformAdminAuthService : IPlatformAdminAuthService
{
    private readonly MongoDbContext _context;
    private readonly IConfiguration _config;
    private readonly IMessagingService _messaging;

    private const int OTP_EXPIRY_MINUTES = 5;
    private const int MAX_OTP_ATTEMPTS   = 3;
    private const string ADMIN_JWT_AUDIENCE = "platform-admin-v1";

    public PlatformAdminAuthService(
        MongoDbContext context,
        IConfiguration config,
        IMessagingService messaging)
    {
        _context   = context;
        _config    = config;
        _messaging = messaging;
    }

    public async Task<bool> SendOtpAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        // Silently succeed if email is not a registered active admin — do not reveal existence
        var admin = await _context.PlatformAdmins
            .Find(a => a.Email == email.ToLowerInvariant() && a.IsActive)
            .FirstOrDefaultAsync();

        if (admin == null)
            return true;

        var otp       = GenerateOtp();
        var expiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES);

        // Reuse the same OtpRecord collection — email is namespaced naturally
        await _context.OtpRecords.DeleteManyAsync(
            Builders<OtpRecord>.Filter.Eq(o => o.Email, email.ToLowerInvariant()));

        await _context.OtpRecords.InsertOneAsync(new OtpRecord
        {
            Email     = email.ToLowerInvariant(),
            Otp       = otp,
            ExpiresAt = expiresAt,
            Attempts  = 0,
            Verified  = false,
        });

        Console.WriteLine($"[Admin OTP] {otp}");

        // Fire email in background — don't block the API response on external delivery latency.
        // IMessagingService is registered as singleton so it is safe to capture here.
        var capturedEmail = email;
        var capturedOtp   = otp;
        _ = Task.Run(async () =>
        {
            try
            {
                await _messaging.SendEmailAsync(
                    capturedEmail,
                    "Nidhiwise Admin — Your OTP",
                    $"Your admin portal OTP is {capturedOtp}. Expires in {OTP_EXPIRY_MINUTES} minutes.",
                    new Dictionary<string, string> { ["otp"] = capturedOtp, ["company_name"] = "Nidhiwise Admin" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin OTP Email Error] {ex.Message}");
            }
        });
        return true;
    }

    public async Task<AdminAuthResponse> LoginAsync(string email, string otp)
    {
        var normalizedEmail = email.ToLowerInvariant();

        if (!await VerifyOtpAsync(normalizedEmail, otp))
            throw new UnauthorizedAccessException("Invalid or expired OTP.");

        var admin = await _context.PlatformAdmins
            .Find(a => a.Email == normalizedEmail && a.IsActive)
            .FirstOrDefaultAsync()
            ?? throw new UnauthorizedAccessException("Admin account not found or inactive.");

        // Update lastLoginAt
        await _context.PlatformAdmins.UpdateOneAsync(
            a => a.Id == admin.Id,
            Builders<PlatformAdmin>.Update
                .Set(a => a.LastLoginAt, DateTime.UtcNow)
                .Set(a => a.UpdatedAt,   DateTime.UtcNow));

        return new AdminAuthResponse
        {
            Token = GenerateAdminJwt(admin),
            Admin = MapToDto(admin),
        };
    }

    public async Task<AdminDto?> GetAdminByIdAsync(string adminId)
    {
        var admin = await _context.PlatformAdmins
            .Find(a => a.Id == adminId)
            .FirstOrDefaultAsync();

        return admin == null ? null : MapToDto(admin);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<bool> VerifyOtpAsync(string email, string otp)
    {
        var filter = Builders<OtpRecord>.Filter.Eq(o => o.Email, email)
                   & Builders<OtpRecord>.Filter.Gt(o => o.ExpiresAt, DateTime.UtcNow);

        var record = await _context.OtpRecords.Find(filter).FirstOrDefaultAsync();
        if (record == null) return false;

        if (record.Attempts >= MAX_OTP_ATTEMPTS)
        {
            await _context.OtpRecords.DeleteOneAsync(
                Builders<OtpRecord>.Filter.Eq(o => o.Id, record.Id));
            return false;
        }

        if (record.Otp != otp)
        {
            await _context.OtpRecords.UpdateOneAsync(
                Builders<OtpRecord>.Filter.Eq(o => o.Id, record.Id),
                Builders<OtpRecord>.Update.Inc(o => o.Attempts, 1));
            return false;
        }

        // Clean up used OTP
        await _context.OtpRecords.DeleteOneAsync(
            Builders<OtpRecord>.Filter.Eq(o => o.Id, record.Id));

        return true;
    }

    private string GenerateAdminJwt(PlatformAdmin admin)
    {
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                              _config["Jwt:Secret"] ?? "your-super-secret-key-min-32-chars-long"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   admin.Id),
            new(JwtRegisteredClaimNames.Email, admin.Email),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new("type", "platform_admin"),
        };

        foreach (var permission in admin.Permissions)
            claims.Add(new Claim("permission", permission));

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"] ?? "ExpensesBackend",
            audience:           ADMIN_JWT_AUDIENCE,
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateOtp()
        => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

    private static AdminDto MapToDto(PlatformAdmin admin) => new()
    {
        Id          = admin.Id,
        Email       = admin.Email,
        Name        = admin.Name,
        Permissions = admin.Permissions,
        LastLoginAt = admin.LastLoginAt,
    };
}
