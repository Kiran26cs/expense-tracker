using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;
using System.Security.Cryptography;

namespace ExpensesBackend.API.Services;

public class SessionService : ISessionService
{
    private readonly MongoDbContext _context;
    private readonly TimeSpan _refreshTokenLifetime;

    public SessionService(MongoDbContext context, IConfiguration configuration)
    {
        _context = context;

        _refreshTokenLifetime = TimeSpan.FromDays(
            int.TryParse(configuration["Jwt:RefreshTokenDays"], out var days) ? days : 30);
    }

    public async Task<(string SessionId, string RefreshToken)> CreateSessionAsync(string userId, string userAgent)
    {
        var token = GenerateToken();
        var session = new Session
        {
            UserId = userId,
            FamilyId = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            RefreshTokenHash = Hash(token),
            UserAgent = Truncate(userAgent),
            ExpiresAt = DateTime.UtcNow.Add(_refreshTokenLifetime),
        };

        await _context.Sessions.InsertOneAsync(session);
        return (session.Id, token);
    }

    public async Task<(string SessionId, string RefreshToken)> RotateAsync(string sessionId, string presentedRefreshToken, string userAgent)
    {
        var session = await _context.Sessions.Find(s => s.Id == sessionId).FirstOrDefaultAsync();
        if (session == null)
            throw new UnauthorizedAccessException("Invalid session");

        var presentedHash = Hash(presentedRefreshToken);

        if (session.RevokedAt != null)
        {
            // A revoked token being presented again means it was stolen and used after the
            // legitimate device already rotated past it — kill the whole lineage.
            if (session.RefreshTokenHash == presentedHash)
                await RevokeFamilyAsync(session.FamilyId, "reuse_detected");

            throw new UnauthorizedAccessException("Session revoked");
        }

        if (session.RefreshTokenHash != presentedHash)
            throw new UnauthorizedAccessException("Invalid refresh token");

        if (DateTime.UtcNow > session.ExpiresAt)
            throw new UnauthorizedAccessException("Session expired");

        var newToken = GenerateToken();
        var newSession = new Session
        {
            UserId = session.UserId,
            FamilyId = session.FamilyId,
            RefreshTokenHash = Hash(newToken),
            UserAgent = Truncate(userAgent),
            ExpiresAt = DateTime.UtcNow.Add(_refreshTokenLifetime),
        };
        await _context.Sessions.InsertOneAsync(newSession);

        var update = Builders<Session>.Update
            .Set(s => s.RevokedAt, DateTime.UtcNow)
            .Set(s => s.RevokedReason, "rotated")
            .Set(s => s.ReplacedBySessionId, newSession.Id)
            .Set(s => s.LastUsedAt, DateTime.UtcNow);
        await _context.Sessions.UpdateOneAsync(s => s.Id == session.Id, update);

        return (newSession.Id, newToken);
    }

    public async Task RevokeAsync(string sessionId, string reason, string? requireUserId = null)
    {
        var filter = Builders<Session>.Filter.Eq(s => s.Id, sessionId)
                   & Builders<Session>.Filter.Eq(s => s.RevokedAt, null);
        if (!string.IsNullOrEmpty(requireUserId))
            filter &= Builders<Session>.Filter.Eq(s => s.UserId, requireUserId);

        var update = Builders<Session>.Update
            .Set(s => s.RevokedAt, DateTime.UtcNow)
            .Set(s => s.RevokedReason, reason);
        await _context.Sessions.UpdateOneAsync(filter, update);
    }

    public async Task RevokeAllForUserAsync(string userId, string reason)
    {
        var update = Builders<Session>.Update
            .Set(s => s.RevokedAt, DateTime.UtcNow)
            .Set(s => s.RevokedReason, reason);
        await _context.Sessions.UpdateManyAsync(
            s => s.UserId == userId && s.RevokedAt == null, update);
    }

    private async Task RevokeFamilyAsync(string familyId, string reason)
    {
        var update = Builders<Session>.Update
            .Set(s => s.RevokedAt, DateTime.UtcNow)
            .Set(s => s.RevokedReason, reason);
        await _context.Sessions.UpdateManyAsync(
            s => s.FamilyId == familyId && s.RevokedAt == null, update);
    }

    public async Task<List<SessionDto>> ListActiveForUserAsync(string userId, string? currentSessionId)
    {
        var now = DateTime.UtcNow;
        var sessions = await _context.Sessions
            .Find(s => s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > now)
            .SortByDescending(s => s.LastUsedAt)
            .ToListAsync();

        return sessions.Select(s => new SessionDto
        {
            Id = s.Id,
            DeviceLabel = DescribeDevice(s.UserAgent),
            CreatedAt = s.CreatedAt,
            LastUsedAt = s.LastUsedAt,
            IsCurrent = s.Id == currentSessionId,
        }).ToList();
    }

    private static string GenerateToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    private static string Truncate(string userAgent)
        => string.IsNullOrEmpty(userAgent) ? string.Empty
            : userAgent.Length > 200 ? userAgent[..200] : userAgent;

    // Best-effort, dependency-free label for display in "My Sessions" / admin — not used for
    // any security decision, just so a user can tell their sessions apart.
    private static string DescribeDevice(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown device";

        var browser = userAgent.Contains("Edg/") ? "Edge"
            : userAgent.Contains("Chrome/") ? "Chrome"
            : userAgent.Contains("Firefox/") ? "Firefox"
            : userAgent.Contains("Safari/") ? "Safari"
            : "Browser";

        var os = userAgent.Contains("iPhone") ? "iPhone"
            : userAgent.Contains("iPad") ? "iPad"
            : userAgent.Contains("Android") ? "Android"
            : userAgent.Contains("Windows") ? "Windows"
            : userAgent.Contains("Macintosh") ? "Mac"
            : userAgent.Contains("Linux") ? "Linux"
            : "Unknown OS";

        return $"{browser} on {os}";
    }
}
