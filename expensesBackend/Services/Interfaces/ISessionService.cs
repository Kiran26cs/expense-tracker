using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface ISessionService
{
    Task<(string SessionId, string RefreshToken)> CreateSessionAsync(string userId, string userAgent);

    /// <summary>
    /// Validates and rotates a refresh token. Throws UnauthorizedAccessException if the
    /// session/token pair is invalid, expired, or already revoked. A revoked token being
    /// presented again is treated as theft — the whole rotation lineage is revoked.
    /// </summary>
    Task<(string SessionId, string RefreshToken)> RotateAsync(string sessionId, string presentedRefreshToken, string userAgent);

    /// <summary>
    /// Revokes a session. When <paramref name="requireUserId"/> is supplied, the session must
    /// belong to that user or the revoke is a no-op — prevents one user (or a mistargeted admin
    /// action) from revoking a session that isn't theirs by guessing/supplying its id.
    /// </summary>
    Task RevokeAsync(string sessionId, string reason, string? requireUserId = null);
    Task RevokeAllForUserAsync(string userId, string reason);
    Task<List<SessionDto>> ListActiveForUserAsync(string userId, string? currentSessionId);
}
