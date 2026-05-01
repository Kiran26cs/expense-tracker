using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Cache;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;
using System.Security.Cryptography;

namespace ExpensesBackend.API.Services;

public class MemberService : IMemberService
{
    private readonly MongoDbContext _context;
    private readonly ICacheService _cache;
    private static readonly TimeSpan PermissionCacheTtl = TimeSpan.FromMinutes(10);

    // Role defaults: defines what each role can do without per-member overrides
    private static readonly Dictionary<string, ResolvedPermissions> RoleDefaults = new(StringComparer.OrdinalIgnoreCase)
    {
        ["owner"]  = new() { Role = "owner",  Dashboard = "view", Expenses = "write", Budgets = "write", Settings = "write", Insights = "view", CanDeleteExpenses = true,  CanManageMembers = true,  CanModifyBook = true,  IsOwner = true  },
        ["admin"]  = new() { Role = "admin",  Dashboard = "view", Expenses = "write", Budgets = "write", Settings = "view",  Insights = "view", CanDeleteExpenses = true,  CanManageMembers = true,  CanModifyBook = true,  IsOwner = false },
        ["member"] = new() { Role = "member", Dashboard = "view", Expenses = "write", Budgets = "none",  Settings = "none",  Insights = "view", CanDeleteExpenses = false, CanManageMembers = false, CanModifyBook = false, IsOwner = false },
        ["viewer"] = new() { Role = "viewer", Dashboard = "view", Expenses = "view",  Budgets = "none",  Settings = "none",  Insights = "view", CanDeleteExpenses = false, CanManageMembers = false, CanModifyBook = false, IsOwner = false },
    };

    public MemberService(MongoDbContext context, ICacheService cache)
    {
        _context = context;
        _cache   = cache;
    }

    // ── Permission resolution ─────────────────────────────────────────────────

    public async Task<ResolvedPermissions> GetResolvedPermissionsAsync(string bookId, string userId)
    {
        var cacheKey = CacheKeys.MemberPermissions(bookId, userId);
        var cached = await _cache.GetAsync<ResolvedPermissions>(cacheKey);
        if (cached != null) return cached;

        var resolved = await ResolveInternalAsync(bookId, userId);
        await _cache.SetAsync(cacheKey, resolved, PermissionCacheTtl);
        return resolved;
    }

    private async Task<ResolvedPermissions> ResolveInternalAsync(string bookId, string userId)
    {
        var book = await _context.ExpenseBooks
            .Find(eb => eb.Id == bookId)
            .FirstOrDefaultAsync();

        if (book == null) return NonePermissions();

        // Owner gets full permissions
        if (book.UserId == userId)
            return Clone(RoleDefaults["owner"]);

        // Find accepted non-deleted membership
        var member = await _context.ExpenseBookMembers
            .Find(m => m.ExpenseBookId == bookId
                    && m.UserId == userId
                    && m.InviteStatus == "accepted"
                    && !m.IsDeleted)
            .FirstOrDefaultAsync();

        if (member == null) return NonePermissions();

        // Start from role defaults
        var role = member.Role.ToLowerInvariant();
        var defaults = RoleDefaults.TryGetValue(role, out var rd) ? rd : RoleDefaults["viewer"];
        var resolved = Clone(defaults);

        // Apply per-member page overrides
        if (member.Permissions != null)
        {
            if (member.Permissions.Dashboard != null) resolved.Dashboard = member.Permissions.Dashboard;
            if (member.Permissions.Expenses  != null) resolved.Expenses  = member.Permissions.Expenses;
            if (member.Permissions.Budgets   != null) resolved.Budgets   = member.Permissions.Budgets;
            if (member.Permissions.Settings  != null) resolved.Settings  = member.Permissions.Settings;
            if (member.Permissions.Insights  != null) resolved.Insights  = member.Permissions.Insights;
            // Custom page permissions strip management rights (design spec §3)
            resolved.CanModifyBook    = false;
            resolved.CanManageMembers = false;
        }

        // Explicit overrides (never derived from role)
        resolved.CanDeleteExpenses = member.CanDeleteExpenses;
        resolved.AllowedCategoryIds = member.AllowedCategoryIds ?? [];

        return resolved;
    }

    public async Task EnsureCanManageMembersAsync(string bookId, string userId)
    {
        var perms = await GetResolvedPermissionsAsync(bookId, userId);
        if (!perms.CanManageMembers)
            throw new UnauthorizedAccessException("You do not have permission to manage members of this book.");
    }

    public async Task EnsureHasAccessAsync(string bookId, string userId, string requiredLevel)
    {
        var perms = await GetResolvedPermissionsAsync(bookId, userId);
        if (perms.Role == "none")
            throw new UnauthorizedAccessException("You do not have access to this expense book.");

        var parts = requiredLevel.Split(':');
        var page  = parts[0];
        var level = parts.Length > 1 ? parts[1] : "view";

        var pagePerm = page switch
        {
            "expenses"  => perms.Expenses,
            "budgets"   => perms.Budgets,
            "settings"  => perms.Settings,
            "insights"  => perms.Insights,
            "dashboard" => perms.Dashboard,
            _           => "none"
        };

        if (level == "write" && pagePerm != "write")
            throw new UnauthorizedAccessException($"You do not have write access to {page}.");
        if (level == "view" && pagePerm == "none")
            throw new UnauthorizedAccessException($"You do not have access to {page}.");
    }

    // ── Member CRUD ────────────────────────────────────────────────────────────

    public async Task<List<ExpenseBookMemberDto>> GetMembersAsync(string bookId, string requestingUserId)
    {
        await EnsureHasAccessAsync(bookId, requestingUserId, "expenses:view");

        var members = await _context.ExpenseBookMembers
            .Find(m => m.ExpenseBookId == bookId && !m.IsDeleted)
            .ToListAsync();

        return members.Select(MapToDto).ToList();
    }

    public async Task<InviteMemberResponse> InviteMemberAsync(
        string bookId, string requestingUserId, InviteMemberRequest request, string baseUrl)
    {
        await EnsureCanManageMembersAsync(bookId, requestingUserId);

        var email = request.Email.Trim().ToLowerInvariant();

        // Check if this email belongs to an already accepted member
        var existingUser = await _context.Users
            .Find(u => u.Email == email)
            .FirstOrDefaultAsync();

        if (existingUser != null)
        {
            var alreadyMember = await _context.ExpenseBookMembers.Find(
                m => m.ExpenseBookId == bookId
                  && m.UserId == existingUser.Id
                  && m.InviteStatus == "accepted"
                  && !m.IsDeleted
            ).AnyAsync();

            if (alreadyMember)
                throw new InvalidOperationException("This user is already a member of the book.");
        }

        // Check for an existing pending invite (application-level check before DB partial index fires)
        var existingPending = await _context.ExpenseBookMembers.Find(
            m => m.ExpenseBookId == bookId
              && m.InvitedEmail == email
              && m.InviteStatus == "pending"
              && !m.IsDeleted
        ).AnyAsync();

        if (existingPending)
            throw new InvalidOperationException("A pending invite already exists for this email.");

        // Cryptographically secure URL-safe token
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        var member = new ExpenseBookMember
        {
            ExpenseBookId     = bookId,
            InvitedEmail      = email,
            InviteToken       = token,
            InviteStatus      = "pending",
            Role              = request.Role,
            AllowedCategoryIds = request.AllowedCategoryIds,
            CanDeleteExpenses = request.CanDeleteExpenses,
            AddedBy           = requestingUserId,
            Permissions       = request.Permissions == null ? null : new PagePermissions
            {
                Dashboard = request.Permissions.Dashboard,
                Expenses  = request.Permissions.Expenses,
                Budgets   = request.Permissions.Budgets,
                Settings  = request.Permissions.Settings,
                Insights  = request.Permissions.Insights,
            }
        };

        await _context.ExpenseBookMembers.InsertOneAsync(member);

        var inviteLink = $"{baseUrl}/accept-invite?token={Uri.EscapeDataString(token)}";
        return new InviteMemberResponse { Member = MapToDto(member), InviteLink = inviteLink };
    }

    public async Task<ExpenseBookMemberDto> UpdateMemberAsync(
        string bookId, string memberId, string requestingUserId, UpdateMemberRequest request)
    {
        await EnsureCanManageMembersAsync(bookId, requestingUserId);

        var member = await _context.ExpenseBookMembers
            .Find(m => m.Id == memberId && m.ExpenseBookId == bookId && !m.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Member not found.");

        if (member.Role == "owner")
            throw new InvalidOperationException("Cannot change the owner's role or permissions.");

        var update = Builders<ExpenseBookMember>.Update
            .Set(m => m.UpdatedAt, DateTime.UtcNow);

        if (request.Role != null)
            update = update.Set(m => m.Role, request.Role);

        if (request.AllowedCategoryIds != null)
            update = update.Set(m => m.AllowedCategoryIds, request.AllowedCategoryIds);

        if (request.CanDeleteExpenses.HasValue)
            update = update.Set(m => m.CanDeleteExpenses, request.CanDeleteExpenses.Value);

        if (request.Permissions != null)
            update = update.Set(m => m.Permissions, new PagePermissions
            {
                Dashboard = request.Permissions.Dashboard,
                Expenses  = request.Permissions.Expenses,
                Budgets   = request.Permissions.Budgets,
                Settings  = request.Permissions.Settings,
                Insights  = request.Permissions.Insights,
            });

        await _context.ExpenseBookMembers.UpdateOneAsync(m => m.Id == memberId, update);

        if (member.UserId != null)
            await InvalidatePermissionsCacheAsync(bookId, member.UserId);

        var updated = await _context.ExpenseBookMembers
            .Find(m => m.Id == memberId)
            .FirstOrDefaultAsync();

        return MapToDto(updated!);
    }

    public async Task RemoveMemberAsync(string bookId, string memberId, string requestingUserId)
    {
        await EnsureCanManageMembersAsync(bookId, requestingUserId);

        var member = await _context.ExpenseBookMembers
            .Find(m => m.Id == memberId && m.ExpenseBookId == bookId && !m.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Member not found.");

        if (member.Role == "owner")
            throw new InvalidOperationException("Cannot remove the book owner.");

        var update = Builders<ExpenseBookMember>.Update
            .Set(m => m.IsDeleted,    true)
            .Set(m => m.DeletedAt,    DateTime.UtcNow)
            .Set(m => m.InviteStatus, "revoked")
            .Set(m => m.UpdatedAt,    DateTime.UtcNow);

        await _context.ExpenseBookMembers.UpdateOneAsync(m => m.Id == memberId, update);

        if (member.UserId != null)
            await InvalidatePermissionsCacheAsync(bookId, member.UserId);
    }

    public async Task<AcceptInviteResponse> AcceptInviteAsync(string token, string userId)
    {
        var member = await _context.ExpenseBookMembers
            .Find(m => m.InviteToken == token && m.InviteStatus == "pending" && !m.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Invite not found or already used.");

        // Prevent double-join
        var alreadyAccepted = await _context.ExpenseBookMembers.Find(
            m => m.ExpenseBookId == member.ExpenseBookId
              && m.UserId == userId
              && m.InviteStatus == "accepted"
              && !m.IsDeleted
        ).AnyAsync();

        if (alreadyAccepted)
            throw new InvalidOperationException("You are already a member of this expense book.");

        var update = Builders<ExpenseBookMember>.Update
            .Set(m => m.UserId,       userId)
            .Set(m => m.InviteStatus, "accepted")
            .Unset(m => m.InviteToken)   // Unset (remove field) so sparse unique index ignores it
            .Set(m => m.UpdatedAt,    DateTime.UtcNow);

        await _context.ExpenseBookMembers.UpdateOneAsync(m => m.Id == member.Id, update);
        await InvalidatePermissionsCacheAsync(member.ExpenseBookId, userId);

        var book = await _context.ExpenseBooks
            .Find(eb => eb.Id == member.ExpenseBookId)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Expense book not found.");

        member.UserId = userId;
        member.InviteStatus = "accepted";
        member.InviteToken = null;

        return new AcceptInviteResponse
        {
            Member          = MapToDto(member),
            ExpenseBookId   = member.ExpenseBookId,
            ExpenseBookName = book.Name,
        };
    }

    public async Task InvalidatePermissionsCacheAsync(string bookId, string userId)
        => await _cache.RemoveAsync(CacheKeys.MemberPermissions(bookId, userId));

    public async Task<List<PendingInviteDto>> GetPendingInvitesAsync(string userEmail)
    {
        var email = userEmail.Trim().ToLowerInvariant();

        var pendingMembers = await _context.ExpenseBookMembers
            .Find(m => m.InvitedEmail == email && m.InviteStatus == "pending" && !m.IsDeleted)
            .ToListAsync();

        if (pendingMembers.Count == 0) return [];

        var bookIds = pendingMembers.Select(m => m.ExpenseBookId).Distinct().ToList();
        var books   = await _context.ExpenseBooks
            .Find(eb => bookIds.Contains(eb.Id))
            .ToListAsync();
        var bookById = books.ToDictionary(b => b.Id);

        return pendingMembers
            .Where(m => bookById.ContainsKey(m.ExpenseBookId))
            .Select(m =>
            {
                var book = bookById[m.ExpenseBookId];
                return new PendingInviteDto
                {
                    MemberId          = m.Id,
                    ExpenseBookId     = m.ExpenseBookId,
                    ExpenseBookName   = book.Name,
                    ExpenseBookIcon   = book.Icon,
                    ExpenseBookColor  = book.Color,
                    ExpenseBookCurrency = book.Currency,
                    Role              = m.Role,
                    InviteToken       = m.InviteToken ?? string.Empty,
                    AddedAt           = m.AddedAt,
                };
            })
            .ToList();
    }

    public async Task DeclineInviteAsync(string token, string userEmail)
    {
        var email  = userEmail.Trim().ToLowerInvariant();
        var member = await _context.ExpenseBookMembers
            .Find(m => m.InviteToken == token && m.InviteStatus == "pending" && !m.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Invite not found or already used.");

        if (!string.Equals(member.InvitedEmail, email, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("This invite was not sent to your email address.");

        var update = Builders<ExpenseBookMember>.Update
            .Set(m => m.IsDeleted,    true)
            .Set(m => m.DeletedAt,    DateTime.UtcNow)
            .Set(m => m.InviteStatus, "revoked")
            .Set(m => m.UpdatedAt,    DateTime.UtcNow)
            .Unset(m => m.InviteToken);

        await _context.ExpenseBookMembers.UpdateOneAsync(m => m.Id == member.Id, update);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ResolvedPermissions NonePermissions() => new()
    {
        Role = "none", Dashboard = "none", Expenses = "none",
        Budgets = "none", Settings = "none", Insights = "none",
        CanDeleteExpenses = false, CanManageMembers = false, CanModifyBook = false
    };

    private static ResolvedPermissions Clone(ResolvedPermissions src) => new()
    {
        Role              = src.Role,
        Dashboard         = src.Dashboard,
        Expenses          = src.Expenses,
        Budgets           = src.Budgets,
        Settings          = src.Settings,
        Insights          = src.Insights,
        CanDeleteExpenses = src.CanDeleteExpenses,
        CanManageMembers  = src.CanManageMembers,
        CanModifyBook     = src.CanModifyBook,
        IsOwner           = src.IsOwner,
    };

    private static ExpenseBookMemberDto MapToDto(ExpenseBookMember m) => new()
    {
        Id                 = m.Id,
        ExpenseBookId      = m.ExpenseBookId,
        UserId             = m.UserId,
        InvitedEmail       = m.InvitedEmail,
        InviteStatus       = m.InviteStatus,
        Role               = m.Role,
        AllowedCategoryIds = m.AllowedCategoryIds,
        CanDeleteExpenses  = m.CanDeleteExpenses,
        AddedBy            = m.AddedBy,
        AddedAt            = m.AddedAt,
        UpdatedAt          = m.UpdatedAt,
        Permissions        = m.Permissions == null ? null : new PagePermissionsDto
        {
            Dashboard = m.Permissions.Dashboard,
            Expenses  = m.Permissions.Expenses,
            Budgets   = m.Permissions.Budgets,
            Settings  = m.Permissions.Settings,
            Insights  = m.Permissions.Insights,
        }
    };
}
