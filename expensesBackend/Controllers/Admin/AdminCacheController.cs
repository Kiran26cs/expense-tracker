using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers.Admin;

[Authorize(Policy = "PlatformAdmin")]
[ApiController]
[Route("api/admin/cache")]
public class AdminCacheController : ControllerBase
{
    private readonly IAdminCacheService _cacheAdmin;
    private readonly MongoDbContext _ctx;

    public AdminCacheController(IAdminCacheService cacheAdmin, MongoDbContext ctx)
    {
        _cacheAdmin = cacheAdmin;
        _ctx        = ctx;
    }

    /// <summary>Invalidates all cache entries for a specific user.</summary>
    [HttpPost("invalidate/user/{userId}")]
    public async Task<ActionResult<ApiResponse<object>>> InvalidateUser(string userId)
    {
        var user = await _ctx.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("User not found."));

        await _cacheAdmin.InvalidateUserAsync(userId);

        await LogAudit(AdminActions.CacheInvalidate, "user", userId,
            $"Invalidated cache for user {user.Email}");

        return Ok(ApiResponse<object>.SuccessResponse(new { message = $"Cache cleared for user {user.Email}." }));
    }

    /// <summary>Invalidates all cache entries for a specific book.</summary>
    [HttpPost("invalidate/book/{bookId}")]
    public async Task<ActionResult<ApiResponse<object>>> InvalidateBook(string bookId)
    {
        var book = await _ctx.ExpenseBooks.Find(b => b.Id == bookId).FirstOrDefaultAsync();
        if (book == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Book not found."));

        await _cacheAdmin.InvalidateBookAsync(bookId);

        await LogAudit(AdminActions.CacheInvalidate, "book", bookId,
            $"Invalidated cache for book \"{book.Name}\"");

        return Ok(ApiResponse<object>.SuccessResponse(new { message = $"Cache cleared for book \"{book.Name}\"." }));
    }

    private async Task LogAudit(string action, string targetType, string targetId, string summary)
    {
        await _ctx.AdminAuditLogs.InsertOneAsync(new AdminAuditLog
        {
            AdminId    = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
            AdminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
            Action     = action,
            TargetType = targetType,
            TargetId   = targetId,
            Summary    = summary,
            Timestamp  = DateTime.UtcNow,
        });
    }
}
