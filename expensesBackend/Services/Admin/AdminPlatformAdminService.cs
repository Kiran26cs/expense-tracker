using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services.Admin;

public class AdminPlatformAdminService : IAdminPlatformAdminService
{
    private readonly MongoDbContext _ctx;

    public AdminPlatformAdminService(MongoDbContext ctx) => _ctx = ctx;

    public async Task<AdminPlatformAdminListDto> GetAdminsAsync()
    {
        // Sort in memory — Cosmos DB cannot guarantee the grantedAt index is present
        // (index creation can fail with "stuck collection metadata" on this collection).
        // The admin list is always small so in-memory sort is fine.
        var admins = await _ctx.PlatformAdmins.Find(_ => true).ToListAsync();
        admins = admins.OrderByDescending(a => a.GrantedAt).ToList();

        return new AdminPlatformAdminListDto
        {
            Admins = admins.Select(MapToDto).ToList(),
            Total  = admins.Count,
        };
    }

    public async Task<AdminPlatformAdminSummaryDto> CreateAdminAsync(
        AdminCreatePlatformAdminRequest request, string grantedByAdminId)
    {
        var existing = await _ctx.PlatformAdmins
            .Find(a => a.Email == request.Email.ToLowerInvariant())
            .FirstOrDefaultAsync();

        if (existing != null)
            throw new InvalidOperationException($"A platform admin with email '{request.Email}' already exists.");

        // Validate permissions
        var invalid = request.Permissions.Except(AdminPermissions.All).ToList();
        if (invalid.Count > 0)
            throw new ArgumentException($"Unknown permissions: {string.Join(", ", invalid)}");

        var admin = new PlatformAdmin
        {
            Email       = request.Email.ToLowerInvariant(),
            Name        = request.Name,
            Permissions = request.Permissions,
            IsActive    = true,
            GrantedBy   = grantedByAdminId,
            GrantedAt   = DateTime.UtcNow,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow,
        };

        await _ctx.PlatformAdmins.InsertOneAsync(admin);
        return MapToDto(admin);
    }

    public async Task<AdminPlatformAdminSummaryDto> UpdateAdminAsync(
        string adminId, AdminUpdatePlatformAdminRequest request)
    {
        var admin = await _ctx.PlatformAdmins.Find(a => a.Id == adminId).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Admin not found.");

        if (request.Permissions != null)
        {
            var invalid = request.Permissions.Except(AdminPermissions.All).ToList();
            if (invalid.Count > 0)
                throw new ArgumentException($"Unknown permissions: {string.Join(", ", invalid)}");
        }

        var updates = new List<UpdateDefinition<PlatformAdmin>>
        {
            Builders<PlatformAdmin>.Update.Set(a => a.UpdatedAt, DateTime.UtcNow),
        };
        if (request.Name        != null) updates.Add(Builders<PlatformAdmin>.Update.Set(a => a.Name,        request.Name));
        if (request.Permissions != null) updates.Add(Builders<PlatformAdmin>.Update.Set(a => a.Permissions, request.Permissions));

        var combined = Builders<PlatformAdmin>.Update.Combine(updates);
        await _ctx.PlatformAdmins.UpdateOneAsync(a => a.Id == adminId, combined);

        var updated = await _ctx.PlatformAdmins.Find(a => a.Id == adminId).FirstOrDefaultAsync();
        return MapToDto(updated!);
    }

    public async Task SetActiveAsync(string adminId, bool isActive)
    {
        await _ctx.PlatformAdmins.UpdateOneAsync(
            a => a.Id == adminId,
            Builders<PlatformAdmin>.Update
                .Set(a => a.IsActive,   isActive)
                .Set(a => a.UpdatedAt,  DateTime.UtcNow));
    }

    private static AdminPlatformAdminSummaryDto MapToDto(PlatformAdmin a) => new()
    {
        Id          = a.Id,
        Email       = a.Email,
        Name        = a.Name,
        Permissions = a.Permissions,
        IsActive    = a.IsActive,
        GrantedBy   = a.GrantedBy,
        GrantedAt   = a.GrantedAt,
        LastLoginAt = a.LastLoginAt,
    };
}
