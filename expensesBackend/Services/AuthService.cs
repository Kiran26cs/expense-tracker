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
    private readonly IMessagingService _messaging;
    private readonly ISessionService _sessions;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const int OTP_EXPIRY_MINUTES = 5;
    private const int MAX_OTP_ATTEMPTS = 3;

    // Namespaces this service's OTPs against PlatformAdminAuthService's, which uses "platform-admin".
    // Without this, a regular user could clear/replace a platform admin's pending OTP (or vice versa)
    // by sending an OTP request for the same email address.
    private const string OtpPurpose = "user";

    // Date the account-linking confirmation feature shipped. Accounts created before this
    // predate the GoogleId/OtpLinkedAt fields, so a null value on them doesn't mean "never
    // used this method" — see GoogleLoginAsync's and LoginAsync's grandfather branches.
    private static readonly DateTime AccountLinkingFeatureCutoverUtc = new(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc);

    public AuthService(
        MongoDbContext context,
        IConfiguration configuration,
        IMessagingService messaging,
        ISessionService sessions,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _configuration = configuration;
        _messaging = messaging;
        _sessions = sessions;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> SendOtpAsync(string email, bool isLogin = false)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        if (isLogin)
        {
            var userExists = await _context.Users
                .Find(u => u.Email == email)
                .AnyAsync();
            if (!userExists)
                return true; // silently succeed — do not reveal whether account exists
        }

        var otp = GenerateOtp();
        var expiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES);

        var otpRecord = new OtpRecord
        {
            Email = email,
            Otp = otp,
            ExpiresAt = expiresAt,
            Attempts = 0,
            Verified = false,
            Purpose = OtpPurpose
        };

        // Scoped to Purpose so this never clears/interferes with a platform-admin OTP for the same email.
        await _context.OtpRecords.DeleteManyAsync(
            Builders<OtpRecord>.Filter.Eq(o => o.Email, email) & Builders<OtpRecord>.Filter.Eq(o => o.Purpose, OtpPurpose));

        await _context.OtpRecords.InsertOneAsync(otpRecord);
        Console.WriteLine($"your otp is: {otp}");

        var emailVariables = new Dictionary<string, string>
        {
            ["otp"] = otp,
            ["company_name"] = "Expense Tracker"
        };
        return await _messaging.SendEmailAsync(
            email,
            "Your Nidhiwise OTP",
            $"Your Nidhiwise OTP for login is {otp}. Expires in {OTP_EXPIRY_MINUTES} minutes.",
            emailVariables);
    }

    public async Task<bool> VerifyOtpAsync(string email, string otp)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        var filter = Builders<OtpRecord>.Filter.Eq(o => o.Email, email)
                     & Builders<OtpRecord>.Filter.Eq(o => o.Purpose, OtpPurpose)
                     & Builders<OtpRecord>.Filter.Gt(o => o.ExpiresAt, DateTime.UtcNow);

        var otpRecord = await _context.OtpRecords.Find(filter).FirstOrDefaultAsync();

        if (otpRecord == null)
            return false;

        if (otpRecord.Attempts >= MAX_OTP_ATTEMPTS)
        {
            await _context.OtpRecords.DeleteOneAsync(Builders<OtpRecord>.Filter.Eq(o => o.Id, otpRecord.Id));
            return false;
        }

        if (otpRecord.Otp != otp)
        {
            var update = Builders<OtpRecord>.Update.Inc(o => o.Attempts, 1);
            await _context.OtpRecords.UpdateOneAsync(
                Builders<OtpRecord>.Filter.Eq(o => o.Id, otpRecord.Id), update);
            return false;
        }

        var verifyUpdate = Builders<OtpRecord>.Update.Set(o => o.Verified, true);
        await _context.OtpRecords.UpdateOneAsync(
            Builders<OtpRecord>.Filter.Eq(o => o.Id, otpRecord.Id), verifyUpdate);

        return true;
    }

    private async Task<bool> IsOtpVerifiedAsync(string email, string otp)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        var otpRecord = await _context.OtpRecords
            .Find(Builders<OtpRecord>.Filter.Eq(o => o.Email, email) & Builders<OtpRecord>.Filter.Eq(o => o.Purpose, OtpPurpose))
            .FirstOrDefaultAsync();

        return otpRecord != null
            && otpRecord.Otp == otp
            && otpRecord.Verified
            && DateTime.UtcNow <= otpRecord.ExpiresAt;
    }

    public async Task<AuthResponse> SignupAsync(SignupRequest request, string otp)
    {
        if (!await IsOtpVerifiedAsync(request.Email, otp))
            throw new UnauthorizedAccessException("Invalid or expired OTP. Please verify OTP first.");

        var existingUser = await _context.Users
            .Find(u => u.Email == request.Email)
            .FirstOrDefaultAsync();

        if (existingUser != null)
            throw new InvalidOperationException("Email is already registered. Please sign in instead.");

        var user = new User
        {
            Email = request.Email,
            Name = request.Name,
            Currency = request.Currency,
            MonthlyIncome = request.MonthlyIncome,
            // OTP-native signup — this account's origin is known, nothing to ever link.
            OtpLinkedAt = DateTime.UtcNow,
        };

        await _context.Users.InsertOneAsync(user);

        return await BuildAuthResponseAsync(user);
    }

    public async Task<LoginLinkResult> LoginAsync(string email, string otp)
    {
        if (!await IsOtpVerifiedAsync(email, otp))
            throw new UnauthorizedAccessException("Invalid or expired OTP. Please verify OTP first.");

        var user = await _context.Users
            .Find(u => u.Email == email)
            .FirstOrDefaultAsync();

        if (user == null)
            throw new UnauthorizedAccessException("User not found");

        if (user.OtpLinkedAt != null)
        {
            // Already used OTP login on this account before — normal login.
            return new LoginLinkResult { RequiresLinking = false, Auth = await BuildAuthResponseAsync(user) };
        }

        if (user.CreatedAt < AccountLinkingFeatureCutoverUtc)
        {
            // Predates the OtpLinkedAt field — can't tell whether this account originally
            // signed up via Google or OTP, so grandfather it in silently, same as the
            // symmetric case in GoogleLoginAsync.
            await LinkOtpAccountAsync(user.Id);
            return new LoginLinkResult { RequiresLinking = false, Auth = await BuildAuthResponseAsync(user) };
        }

        // Existing Google-created account (created after the cutover, so its history is
        // known), first time seen with an OTP login — require explicit confirmation.
        return new LoginLinkResult
        {
            RequiresLinking = true,
            Preview = new AccountLinkPreviewDto { Name = user.Name, Email = user.Email ?? string.Empty, CreatedAt = user.CreatedAt },
        };
    }

    public async Task<AuthResponse> ConfirmOtpLinkAsync(string email, string otp)
    {
        if (!await IsOtpVerifiedAsync(email, otp))
            throw new UnauthorizedAccessException("Invalid or expired OTP. Please verify OTP first.");

        var user = await _context.Users
            .Find(u => u.Email == email)
            .FirstOrDefaultAsync()
            ?? throw new UnauthorizedAccessException("User not found");

        if (user.OtpLinkedAt == null)
            await LinkOtpAccountAsync(user.Id);

        return await BuildAuthResponseAsync(user);
    }

    private Task LinkOtpAccountAsync(string userId)
        => _context.Users.UpdateOneAsync(
            u => u.Id == userId,
            Builders<User>.Update.Set(u => u.OtpLinkedAt, DateTime.UtcNow));

    private async Task<GoogleJsonWebSignature.Payload> ValidateGoogleCredentialAsync(string credential)
    {
        var googleClientId = _configuration["Google:ClientId"]
            ?? throw new InvalidOperationException("Google ClientId not configured");

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { googleClientId }
        };

        try
        {
            return await GoogleJsonWebSignature.ValidateAsync(credential, settings);
        }
        catch (InvalidJwtException)
        {
            throw new UnauthorizedAccessException("Invalid Google token");
        }
    }

    public async Task<LoginLinkResult> GoogleLoginAsync(string credential)
    {
        var payload = await ValidateGoogleCredentialAsync(credential);

        var user = await _context.Users
            .Find(u => u.Email == payload.Email)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            // Fresh account — no existing account to link to, so this is a normal signup.
            user = new User
            {
                Email = payload.Email,
                Name = payload.Name ?? payload.Email ?? "Google User",
                Currency = "USD",
                MonthlyIncome = 0,
                GoogleId = payload.Subject,
                GoogleLinkedAt = DateTime.UtcNow,
            };
            await _context.Users.InsertOneAsync(user);
            return new LoginLinkResult { RequiresLinking = false, Auth = await BuildAuthResponseAsync(user) };
        }

        if (user.GoogleId == payload.Subject)
        {
            // Already linked to this Google account — normal login.
            return new LoginLinkResult { RequiresLinking = false, Auth = await BuildAuthResponseAsync(user) };
        }

        if (!string.IsNullOrEmpty(user.GoogleId))
        {
            // Shouldn't happen — Google emails are globally unique — but don't silently proceed.
            throw new UnauthorizedAccessException("This email is linked to a different Google account.");
        }

        if (user.CreatedAt < AccountLinkingFeatureCutoverUtc)
        {
            // This account predates the GoogleId field entirely, so a null GoogleId here
            // doesn't mean "never used Google" — it just means we never recorded it. We can't
            // tell whether this account originally signed up via OTP or Google, so grandfather
            // it in silently (one time only) rather than surprising a long-time Google user
            // with a linking prompt that makes no sense from their side.
            await LinkGoogleAccountAsync(user.Id, payload.Subject);
            return new LoginLinkResult { RequiresLinking = false, Auth = await BuildAuthResponseAsync(user) };
        }

        // Existing OTP-created account (created after the cutover, so its history is known),
        // first time seen with a Google credential — require explicit confirmation before
        // linking rather than silently logging in.
        return new LoginLinkResult
        {
            RequiresLinking = true,
            Preview = new AccountLinkPreviewDto { Name = user.Name, Email = user.Email ?? string.Empty, CreatedAt = user.CreatedAt },
        };
    }

    public async Task<AuthResponse> ConfirmGoogleLinkAsync(string credential)
    {
        var payload = await ValidateGoogleCredentialAsync(credential);

        var user = await _context.Users
            .Find(u => u.Email == payload.Email)
            .FirstOrDefaultAsync()
            ?? throw new UnauthorizedAccessException("User not found");

        if (string.IsNullOrEmpty(user.GoogleId))
            await LinkGoogleAccountAsync(user.Id, payload.Subject);

        return await BuildAuthResponseAsync(user);
    }

    private Task LinkGoogleAccountAsync(string userId, string googleSubject)
        => _context.Users.UpdateOneAsync(
            u => u.Id == userId,
            Builders<User>.Update
                .Set(u => u.GoogleId, googleSubject)
                .Set(u => u.GoogleLinkedAt, DateTime.UtcNow));

    public async Task<AuthResponse> RefreshTokenAsync(string sessionId, string refreshToken)
    {
        var (newSessionId, newRefreshToken) = await _sessions.RotateAsync(sessionId, refreshToken, CurrentUserAgent());

        // The session row doesn't carry the user id back out of RotateAsync's tuple, so look
        // it up — cheap single read, and keeps ISessionService free of User/AuthResponse concerns.
        var userId = await _context.Sessions
            .Find(s => s.Id == newSessionId)
            .Project(s => s.UserId)
            .FirstOrDefaultAsync();
        var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync()
            ?? throw new UnauthorizedAccessException("User not found");

        return new AuthResponse
        {
            Token = GenerateJwtToken(user),
            RefreshToken = newRefreshToken,
            SessionId = newSessionId,
            User = MapToUserDto(user)
        };
    }

    public Task LogoutAsync(string sessionId, string userId)
        => _sessions.RevokeAsync(sessionId, "user_logout", requireUserId: userId);

    public Task LogoutAllAsync(string userId)
        => _sessions.RevokeAllForUserAsync(userId, "user_logout_all");

    public Task<List<SessionDto>> ListSessionsAsync(string userId, string? currentSessionId)
        => _sessions.ListActiveForUserAsync(userId, currentSessionId);

    private async Task<AuthResponse> BuildAuthResponseAsync(User user)
    {
        var (sessionId, refreshToken) = await _sessions.CreateSessionAsync(user.Id, CurrentUserAgent());
        return new AuthResponse
        {
            Token = GenerateJwtToken(user),
            RefreshToken = refreshToken,
            SessionId = sessionId,
            User = MapToUserDto(user)
        };
    }

    private string CurrentUserAgent()
        => _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty;

    public string GenerateJwtToken(User user)
    {
        var jwtSecret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret must be configured.");
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var accessMinutes = int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out var minutes) ? minutes : 30;

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
            expires: DateTime.UtcNow.AddMinutes(accessMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateOtp()
        => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Currency = user.Currency,
            MonthlyIncome = user.MonthlyIncome,
            MonthlySavingsGoal = user.MonthlySavingsGoal,
            Plan = user.Plan.ToString(),
            HasPrivacyPin = !string.IsNullOrEmpty(user.PrivacyPinHash)
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        return user == null ? null : MapToUserDto(user);
    }

    public async Task<UserDto?> UpdateProfileAsync(string userId, UpdateProfileRequest req)
    {
        var updates = new List<UpdateDefinition<User>>();
        if (req.Currency != null)
            updates.Add(Builders<User>.Update.Set(u => u.Currency, req.Currency));
        if (req.MonthlySavingsGoal.HasValue)
            updates.Add(Builders<User>.Update.Set(u => u.MonthlySavingsGoal, req.MonthlySavingsGoal.Value));

        if (updates.Count == 0)
        {
            var unchanged = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            return unchanged == null ? null : MapToUserDto(unchanged);
        }

        updates.Add(Builders<User>.Update.Set(u => u.UpdatedAt, DateTime.UtcNow));
        var combined = Builders<User>.Update.Combine(updates);
        var opts = new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After };
        var updated = await _context.Users.FindOneAndUpdateAsync(u => u.Id == userId, combined, opts);
        return updated == null ? null : MapToUserDto(updated);
    }
}
