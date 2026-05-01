# Code Citations

## License: MIT
https://github.com/AdrianoAE/EntityFrameworkCore.Translations/blob/c57ff4089c2d940a173fff704223ae5f0597d2fd/src/EntityFrameworkCore.Translations/AllTranslationsQuery.cs

```
I'm in Ask mode so I can't create new files directly. Let me provide the complete implementation — all code ready to apply. Switch to Agent mode to have these created, or use the code blocks below directly.

---

## Full ACL Implementation

### Files to Create (Backend)

---

#### `expensesBackend/Domain/Entities/ExpenseBookMember.cs`

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class PagePermissions
{
    [BsonElement("dashboard")]
    public string? Dashboard { get; set; }  // "view" | "none" | null = role default

    [BsonElement("expenses")]
    public string? Expenses { get; set; }   // "view" | "write" | "none" | null

    [BsonElement("budgets")]
    public string? Budgets { get; set; }    // "view" | "write" | "none" | null

    [BsonElement("settings")]
    public string? Settings { get; set; }   // "view" | "write" | "none" | null

    [BsonElement("insights")]
    public string? Insights { get; set; }   // "view" | "none" | null
}

public class ExpenseBookMember
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("expenseBookId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string ExpenseBookId { get; set; } = string.Empty;

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? UserId { get; set; }

    [BsonElement("invitedEmail")]
    public string? InvitedEmail { get; set; }

    /// <summary>One-time token in invite link. Nulled after acceptance.</summary>
    [BsonElement("inviteToken")]
    public string? InviteToken { get; set; }

    /// <summary>pending | accepted | revoked</summary>
    [BsonElement("inviteStatus")]
    public string InviteStatus { get; set; } = "pending";

    /// <summary>owner | admin | member | viewer</summary>
    [BsonElement("role")]
    public string Role { get; set; } = "viewer";

    /// <summary>
    /// Null = use role defaults (full access for that role).
    /// Non-null = custom overrides; CanModifyBook + CanManageMembers become false.
    /// </summary>
    [BsonElement("permissions")]
    public PagePermissions? Permissions { get; set; }

    /// <summary>Empty = all categories visible.</summary>
    [BsonElement("allowedCategoryIds")]
    public List<string> AllowedCategoryIds { get; set; } = new();

    [BsonElement("canDeleteExpenses")]
    public bool CanDeleteExpenses { get; set; } = false;

    [BsonElement("addedBy")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string AddedBy { get; set; } = string.Empty;

    [BsonElement("addedAt")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;

    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; set; }
}
```

---

#### `expensesBackend/Domain/DTOs/MemberDTOs.cs`

```csharp
namespace ExpensesBackend.API.Domain.DTOs;

public class PagePermissionsDto
{
    public string? Dashboard { get; set; }
    public string? Expenses { get; set; }
    public string? Budgets { get; set; }
    public string? Settings { get; set; }
    public string? Insights { get; set; }
}

/// <summary>Fully resolved permissions for a user on a book — ready for UI/enforcement use.</summary>
public class ResolvedPermissions
{
    public string Role { get; set; } = "none";
    public string Dashboard { get; set; } = "none";
    public string Expenses { get; set; } = "none";
    public string Budgets { get; set; } = "none";
    public string Settings { get; set; } = "none";
    public string Insights { get; set; } = "none";
    public bool CanDeleteExpenses { get; set; } = false;
    public bool CanManageMembers { get; set; } = false;
    public bool CanModifyBook { get; set; } = false;
    public bool IsOwner { get; set; } = false;
    public List<string> AllowedCategoryIds { get; set; } = new();
}

public class ExpenseBookMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string ExpenseBookId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? InvitedEmail { get; set; }
    public string? UserName { get; set; }
    public string InviteStatus { get; set; } = "pending";
    public string Role { get; set; } = "viewer";
    public PagePermissionsDto? Permissions { get; set; }
    public List<string> AllowedCategoryIds { get; set; } = new();
    public bool CanDeleteExpenses { get; set; }
    public DateTime AddedAt { get; set; }
}

public class InviteMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "viewer";
    public PagePermissionsDto? Permissions { get; set; }
    public List<string> AllowedCategoryIds { get; set; } = new();
    public bool CanDeleteExpenses { get; set; } = false;
}

public class InviteMemberResponse
{
    public ExpenseBookMemberDto Member { get; set; } = null!;
    public string InviteLink { get; set; } = string.Empty;
}

public class UpdateMemberPermissionsRequest
{
    public string? Role { get; set; }
    public PagePermissionsDto? Permissions { get; set; }
    public List<string>? AllowedCategoryIds { get; set; }
    public bool? CanDeleteExpenses { get; set; }
}

public class AcceptInviteResponse
{
    public string ExpenseBookId { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```

---

#### `expensesBackend/Services/Interfaces/IMemberService.cs`

```csharp
using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IMemberService
{
    Task<List<ExpenseBookMemberDto>> GetMembersAsync(string bookId, string requestingUserId);
    Task<InviteMemberResponse> InviteMemberAsync(string bookId, string inviterUserId, InviteMemberRequest request, string baseUrl);
    Task<ExpenseBookMemberDto> UpdateMemberAsync(string bookId, string memberId, string requestingUserId, UpdateMemberPermissionsRequest request);
    Task RemoveMemberAsync(string bookId, string memberId, string requestingUserId);
    Task<AcceptInviteResponse> AcceptInviteAsync(string token, string userId);
    Task<ResolvedPermissions> GetResolvedPermissionsAsync(string bookId, string userId);
    Task EnsureMemberAccessAsync(string bookId, string userId);
    Task<bool> IsMemberAsync(string bookId, string userId);
    Task InvalidatePermissionsCacheAsync(string bookId, string userId);
}
```

---

#### `expensesBackend/Services/MemberService.cs`

```csharp
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
    private static readonly TimeSpan PermissionsTtl = TimeSpan.FromMinutes(5);

    public MemberService(MongoDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    // ─── Permission Resolution ────────────────────────────────────────────────

    public async Task<ResolvedPermissions> GetResolvedPermissionsAsync(string bookId, string userId)
    {
        var key = CacheKeys.MemberPermissions(bookId, userId);
        return await _cache.GetOrSetAsync(key, () => ResolveFromDbAsync(bookId, userId), PermissionsTtl)
               ?? new ResolvedPermissions { Role = "none" };
    }

    private async Task<ResolvedPermissions> ResolveFromDbAsync(string bookId, string userId)
    {
        // Owner check first (authoritative)
        var isOwner = await _context.ExpenseBooks
            .Find(b => b.Id == bookId && b.UserId == userId)
            .AnyAsync();

        if (isOwner)
            return RoleDefaults("owner");

        // Member check
        var member = await _context.ExpenseBookMembers
            .Find(m => m.ExpenseBookId == bookId && m.UserId == userId
                    && m.InviteStatus == "accepted" && !m.IsDeleted)
            .FirstOrDefaultAsync();

        return member == null ? new ResolvedPermissions { Role = "none" } : ResolveFromMember(member);
    }

    private static ResolvedPermissions ResolveFromMember(ExpenseBookMember m)
    {
        var r = RoleDefaults(m.Role);

        if (m.Permissions != null)
        {
            // Custom permissions = limited admin, loses modify/manage rights
            r.Dashboard = m.Permissions.Dashboard ?? r.Dashboard;
            r.Expenses  = m.Permissions.Expenses  ?? r.Expenses;
            r.Budgets   = m.Permissions.Budgets   ?? r.Budgets;
            r.Settings  = m.Permissions.Settings  ?? r.Settings;
            r.Insights  = m.Permissions.Insights  ?? r.Insights;
            r.CanModifyBook    = false;
            r.CanManageMembers = false;
        }

        r.CanDeleteExpenses  = m.CanDeleteExpenses;
        r.AllowedCategoryIds = m.AllowedCategoryIds;
        return r;
    }

    private static ResolvedPermissions RoleDefaults(string role) => role switch
    {
        "owner"  => new() { Role="owner",  Dashboard="view", Expenses="write", Budgets="write", Settings="write", Insights="view", CanDeleteExpenses=true,  CanManageMembers=true,  CanModifyBook=true,  IsOwner=true  },
        "admin"  => new() { Role="admin",  Dashboard="view", Expenses="write", Budgets="write", Settings="view",  Insights="view", CanDeleteExpenses=true,  CanManageMembers=true,  CanModifyBook=true,  IsOwner=false },
        "member" => new() { Role="member", Dashboard="view", Expenses="write", Budgets="none",  Settings="none",  Insights="view", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false },
        "viewer" => new() { Role="viewer", Dashboard="view", Expenses="view",  Budgets="none",  Settings="none",  Insights="view", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false },
        _        => new() { Role="none",   Dashboard="none", Expenses="none",  Budgets="none",  Settings="none",  Insights="none", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false }
    };

    public async Task EnsureMemberAccessAsync(string bookId, string userId)
    {
        var p = await GetResolvedPermissionsAsync(bookId, userId);
        if (p.Role == "none")
            throw new UnauthorizedAccessException("You do not have access to this expense book");
    }

    public async Task<bool> IsMemberAsync(string bookId, string userId)
    {
        var p = await GetResolvedPermissionsAsync(bookId, userId);
        return p.Role != "none";
    }

    public async Task InvalidatePermissionsCacheAsync(string bookId, string userId)
        => await _cache.RemoveAsync(CacheKeys.MemberPermissions(bookId, userId));

    // ─── Member CRUD ──────────────────────────────────────────────────────────

    public async Task<List<ExpenseBookMemberDto>> GetMembersAsync(
```


## License: MIT
https://github.com/AdrianoAE/EntityFrameworkCore.Translations/blob/c57ff4089c2d940a173fff704223ae5f0597d2fd/src/EntityFrameworkCore.Translations/AllTranslationsQuery.cs

```
I'm in Ask mode so I can't create new files directly. Let me provide the complete implementation — all code ready to apply. Switch to Agent mode to have these created, or use the code blocks below directly.

---

## Full ACL Implementation

### Files to Create (Backend)

---

#### `expensesBackend/Domain/Entities/ExpenseBookMember.cs`

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class PagePermissions
{
    [BsonElement("dashboard")]
    public string? Dashboard { get; set; }  // "view" | "none" | null = role default

    [BsonElement("expenses")]
    public string? Expenses { get; set; }   // "view" | "write" | "none" | null

    [BsonElement("budgets")]
    public string? Budgets { get; set; }    // "view" | "write" | "none" | null

    [BsonElement("settings")]
    public string? Settings { get; set; }   // "view" | "write" | "none" | null

    [BsonElement("insights")]
    public string? Insights { get; set; }   // "view" | "none" | null
}

public class ExpenseBookMember
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("expenseBookId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string ExpenseBookId { get; set; } = string.Empty;

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? UserId { get; set; }

    [BsonElement("invitedEmail")]
    public string? InvitedEmail { get; set; }

    /// <summary>One-time token in invite link. Nulled after acceptance.</summary>
    [BsonElement("inviteToken")]
    public string? InviteToken { get; set; }

    /// <summary>pending | accepted | revoked</summary>
    [BsonElement("inviteStatus")]
    public string InviteStatus { get; set; } = "pending";

    /// <summary>owner | admin | member | viewer</summary>
    [BsonElement("role")]
    public string Role { get; set; } = "viewer";

    /// <summary>
    /// Null = use role defaults (full access for that role).
    /// Non-null = custom overrides; CanModifyBook + CanManageMembers become false.
    /// </summary>
    [BsonElement("permissions")]
    public PagePermissions? Permissions { get; set; }

    /// <summary>Empty = all categories visible.</summary>
    [BsonElement("allowedCategoryIds")]
    public List<string> AllowedCategoryIds { get; set; } = new();

    [BsonElement("canDeleteExpenses")]
    public bool CanDeleteExpenses { get; set; } = false;

    [BsonElement("addedBy")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string AddedBy { get; set; } = string.Empty;

    [BsonElement("addedAt")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;

    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; set; }
}
```

---

#### `expensesBackend/Domain/DTOs/MemberDTOs.cs`

```csharp
namespace ExpensesBackend.API.Domain.DTOs;

public class PagePermissionsDto
{
    public string? Dashboard { get; set; }
    public string? Expenses { get; set; }
    public string? Budgets { get; set; }
    public string? Settings { get; set; }
    public string? Insights { get; set; }
}

/// <summary>Fully resolved permissions for a user on a book — ready for UI/enforcement use.</summary>
public class ResolvedPermissions
{
    public string Role { get; set; } = "none";
    public string Dashboard { get; set; } = "none";
    public string Expenses { get; set; } = "none";
    public string Budgets { get; set; } = "none";
    public string Settings { get; set; } = "none";
    public string Insights { get; set; } = "none";
    public bool CanDeleteExpenses { get; set; } = false;
    public bool CanManageMembers { get; set; } = false;
    public bool CanModifyBook { get; set; } = false;
    public bool IsOwner { get; set; } = false;
    public List<string> AllowedCategoryIds { get; set; } = new();
}

public class ExpenseBookMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string ExpenseBookId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? InvitedEmail { get; set; }
    public string? UserName { get; set; }
    public string InviteStatus { get; set; } = "pending";
    public string Role { get; set; } = "viewer";
    public PagePermissionsDto? Permissions { get; set; }
    public List<string> AllowedCategoryIds { get; set; } = new();
    public bool CanDeleteExpenses { get; set; }
    public DateTime AddedAt { get; set; }
}

public class InviteMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "viewer";
    public PagePermissionsDto? Permissions { get; set; }
    public List<string> AllowedCategoryIds { get; set; } = new();
    public bool CanDeleteExpenses { get; set; } = false;
}

public class InviteMemberResponse
{
    public ExpenseBookMemberDto Member { get; set; } = null!;
    public string InviteLink { get; set; } = string.Empty;
}

public class UpdateMemberPermissionsRequest
{
    public string? Role { get; set; }
    public PagePermissionsDto? Permissions { get; set; }
    public List<string>? AllowedCategoryIds { get; set; }
    public bool? CanDeleteExpenses { get; set; }
}

public class AcceptInviteResponse
{
    public string ExpenseBookId { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```

---

#### `expensesBackend/Services/Interfaces/IMemberService.cs`

```csharp
using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IMemberService
{
    Task<List<ExpenseBookMemberDto>> GetMembersAsync(string bookId, string requestingUserId);
    Task<InviteMemberResponse> InviteMemberAsync(string bookId, string inviterUserId, InviteMemberRequest request, string baseUrl);
    Task<ExpenseBookMemberDto> UpdateMemberAsync(string bookId, string memberId, string requestingUserId, UpdateMemberPermissionsRequest request);
    Task RemoveMemberAsync(string bookId, string memberId, string requestingUserId);
    Task<AcceptInviteResponse> AcceptInviteAsync(string token, string userId);
    Task<ResolvedPermissions> GetResolvedPermissionsAsync(string bookId, string userId);
    Task EnsureMemberAccessAsync(string bookId, string userId);
    Task<bool> IsMemberAsync(string bookId, string userId);
    Task InvalidatePermissionsCacheAsync(string bookId, string userId);
}
```

---

#### `expensesBackend/Services/MemberService.cs`

```csharp
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
    private static readonly TimeSpan PermissionsTtl = TimeSpan.FromMinutes(5);

    public MemberService(MongoDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    // ─── Permission Resolution ────────────────────────────────────────────────

    public async Task<ResolvedPermissions> GetResolvedPermissionsAsync(string bookId, string userId)
    {
        var key = CacheKeys.MemberPermissions(bookId, userId);
        return await _cache.GetOrSetAsync(key, () => ResolveFromDbAsync(bookId, userId), PermissionsTtl)
               ?? new ResolvedPermissions { Role = "none" };
    }

    private async Task<ResolvedPermissions> ResolveFromDbAsync(string bookId, string userId)
    {
        // Owner check first (authoritative)
        var isOwner = await _context.ExpenseBooks
            .Find(b => b.Id == bookId && b.UserId == userId)
            .AnyAsync();

        if (isOwner)
            return RoleDefaults("owner");

        // Member check
        var member = await _context.ExpenseBookMembers
            .Find(m => m.ExpenseBookId == bookId && m.UserId == userId
                    && m.InviteStatus == "accepted" && !m.IsDeleted)
            .FirstOrDefaultAsync();

        return member == null ? new ResolvedPermissions { Role = "none" } : ResolveFromMember(member);
    }

    private static ResolvedPermissions ResolveFromMember(ExpenseBookMember m)
    {
        var r = RoleDefaults(m.Role);

        if (m.Permissions != null)
        {
            // Custom permissions = limited admin, loses modify/manage rights
            r.Dashboard = m.Permissions.Dashboard ?? r.Dashboard;
            r.Expenses  = m.Permissions.Expenses  ?? r.Expenses;
            r.Budgets   = m.Permissions.Budgets   ?? r.Budgets;
            r.Settings  = m.Permissions.Settings  ?? r.Settings;
            r.Insights  = m.Permissions.Insights  ?? r.Insights;
            r.CanModifyBook    = false;
            r.CanManageMembers = false;
        }

        r.CanDeleteExpenses  = m.CanDeleteExpenses;
        r.AllowedCategoryIds = m.AllowedCategoryIds;
        return r;
    }

    private static ResolvedPermissions RoleDefaults(string role) => role switch
    {
        "owner"  => new() { Role="owner",  Dashboard="view", Expenses="write", Budgets="write", Settings="write", Insights="view", CanDeleteExpenses=true,  CanManageMembers=true,  CanModifyBook=true,  IsOwner=true  },
        "admin"  => new() { Role="admin",  Dashboard="view", Expenses="write", Budgets="write", Settings="view",  Insights="view", CanDeleteExpenses=true,  CanManageMembers=true,  CanModifyBook=true,  IsOwner=false },
        "member" => new() { Role="member", Dashboard="view", Expenses="write", Budgets="none",  Settings="none",  Insights="view", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false },
        "viewer" => new() { Role="viewer", Dashboard="view", Expenses="view",  Budgets="none",  Settings="none",  Insights="view", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false },
        _        => new() { Role="none",   Dashboard="none", Expenses="none",  Budgets="none",  Settings="none",  Insights="none", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false }
    };

    public async Task EnsureMemberAccessAsync(string bookId, string userId)
    {
        var p = await GetResolvedPermissionsAsync(bookId, userId);
        if (p.Role == "none")
            throw new UnauthorizedAccessException("You do not have access to this expense book");
    }

    public async Task<bool> IsMemberAsync(string bookId, string userId)
    {
        var p = await GetResolvedPermissionsAsync(bookId, userId);
        return p.Role != "none";
    }

    public async Task InvalidatePermissionsCacheAsync(string bookId, string userId)
        => await _cache.RemoveAsync(CacheKeys.MemberPermissions(bookId, userId));

    // ─── Member CRUD ──────────────────────────────────────────────────────────

    public async Task<List<ExpenseBookMemberDto>> GetMembersAsync(
```


## License: MIT
https://github.com/AdrianoAE/EntityFrameworkCore.Translations/blob/c57ff4089c2d940a173fff704223ae5f0597d2fd/src/EntityFrameworkCore.Translations/AllTranslationsQuery.cs

```
I'm in Ask mode so I can't create new files directly. Let me provide the complete implementation — all code ready to apply. Switch to Agent mode to have these created, or use the code blocks below directly.

---

## Full ACL Implementation

### Files to Create (Backend)

---

#### `expensesBackend/Domain/Entities/ExpenseBookMember.cs`

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class PagePermissions
{
    [BsonElement("dashboard")]
    public string? Dashboard { get; set; }  // "view" | "none" | null = role default

    [BsonElement("expenses")]
    public string? Expenses { get; set; }   // "view" | "write" | "none" | null

    [BsonElement("budgets")]
    public string? Budgets { get; set; }    // "view" | "write" | "none" | null

    [BsonElement("settings")]
    public string? Settings { get; set; }   // "view" | "write" | "none" | null

    [BsonElement("insights")]
    public string? Insights { get; set; }   // "view" | "none" | null
}

public class ExpenseBookMember
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("expenseBookId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string ExpenseBookId { get; set; } = string.Empty;

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? UserId { get; set; }

    [BsonElement("invitedEmail")]
    public string? InvitedEmail { get; set; }

    /// <summary>One-time token in invite link. Nulled after acceptance.</summary>
    [BsonElement("inviteToken")]
    public string? InviteToken { get; set; }

    /// <summary>pending | accepted | revoked</summary>
    [BsonElement("inviteStatus")]
    public string InviteStatus { get; set; } = "pending";

    /// <summary>owner | admin | member | viewer</summary>
    [BsonElement("role")]
    public string Role { get; set; } = "viewer";

    /// <summary>
    /// Null = use role defaults (full access for that role).
    /// Non-null = custom overrides; CanModifyBook + CanManageMembers become false.
    /// </summary>
    [BsonElement("permissions")]
    public PagePermissions? Permissions { get; set; }

    /// <summary>Empty = all categories visible.</summary>
    [BsonElement("allowedCategoryIds")]
    public List<string> AllowedCategoryIds { get; set; } = new();

    [BsonElement("canDeleteExpenses")]
    public bool CanDeleteExpenses { get; set; } = false;

    [BsonElement("addedBy")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string AddedBy { get; set; } = string.Empty;

    [BsonElement("addedAt")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;

    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; set; }
}
```

---

#### `expensesBackend/Domain/DTOs/MemberDTOs.cs`

```csharp
namespace ExpensesBackend.API.Domain.DTOs;

public class PagePermissionsDto
{
    public string? Dashboard { get; set; }
    public string? Expenses { get; set; }
    public string? Budgets { get; set; }
    public string? Settings { get; set; }
    public string? Insights { get; set; }
}

/// <summary>Fully resolved permissions for a user on a book — ready for UI/enforcement use.</summary>
public class ResolvedPermissions
{
    public string Role { get; set; } = "none";
    public string Dashboard { get; set; } = "none";
    public string Expenses { get; set; } = "none";
    public string Budgets { get; set; } = "none";
    public string Settings { get; set; } = "none";
    public string Insights { get; set; } = "none";
    public bool CanDeleteExpenses { get; set; } = false;
    public bool CanManageMembers { get; set; } = false;
    public bool CanModifyBook { get; set; } = false;
    public bool IsOwner { get; set; } = false;
    public List<string> AllowedCategoryIds { get; set; } = new();
}

public class ExpenseBookMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string ExpenseBookId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? InvitedEmail { get; set; }
    public string? UserName { get; set; }
    public string InviteStatus { get; set; } = "pending";
    public string Role { get; set; } = "viewer";
    public PagePermissionsDto? Permissions { get; set; }
    public List<string> AllowedCategoryIds { get; set; } = new();
    public bool CanDeleteExpenses { get; set; }
    public DateTime AddedAt { get; set; }
}

public class InviteMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "viewer";
    public PagePermissionsDto? Permissions { get; set; }
    public List<string> AllowedCategoryIds { get; set; } = new();
    public bool CanDeleteExpenses { get; set; } = false;
}

public class InviteMemberResponse
{
    public ExpenseBookMemberDto Member { get; set; } = null!;
    public string InviteLink { get; set; } = string.Empty;
}

public class UpdateMemberPermissionsRequest
{
    public string? Role { get; set; }
    public PagePermissionsDto? Permissions { get; set; }
    public List<string>? AllowedCategoryIds { get; set; }
    public bool? CanDeleteExpenses { get; set; }
}

public class AcceptInviteResponse
{
    public string ExpenseBookId { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```

---

#### `expensesBackend/Services/Interfaces/IMemberService.cs`

```csharp
using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IMemberService
{
    Task<List<ExpenseBookMemberDto>> GetMembersAsync(string bookId, string requestingUserId);
    Task<InviteMemberResponse> InviteMemberAsync(string bookId, string inviterUserId, InviteMemberRequest request, string baseUrl);
    Task<ExpenseBookMemberDto> UpdateMemberAsync(string bookId, string memberId, string requestingUserId, UpdateMemberPermissionsRequest request);
    Task RemoveMemberAsync(string bookId, string memberId, string requestingUserId);
    Task<AcceptInviteResponse> AcceptInviteAsync(string token, string userId);
    Task<ResolvedPermissions> GetResolvedPermissionsAsync(string bookId, string userId);
    Task EnsureMemberAccessAsync(string bookId, string userId);
    Task<bool> IsMemberAsync(string bookId, string userId);
    Task InvalidatePermissionsCacheAsync(string bookId, string userId);
}
```

---

#### `expensesBackend/Services/MemberService.cs`

```csharp
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
    private static readonly TimeSpan PermissionsTtl = TimeSpan.FromMinutes(5);

    public MemberService(MongoDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    // ─── Permission Resolution ────────────────────────────────────────────────

    public async Task<ResolvedPermissions> GetResolvedPermissionsAsync(string bookId, string userId)
    {
        var key = CacheKeys.MemberPermissions(bookId, userId);
        return await _cache.GetOrSetAsync(key, () => ResolveFromDbAsync(bookId, userId), PermissionsTtl)
               ?? new ResolvedPermissions { Role = "none" };
    }

    private async Task<ResolvedPermissions> ResolveFromDbAsync(string bookId, string userId)
    {
        // Owner check first (authoritative)
        var isOwner = await _context.ExpenseBooks
            .Find(b => b.Id == bookId && b.UserId == userId)
            .AnyAsync();

        if (isOwner)
            return RoleDefaults("owner");

        // Member check
        var member = await _context.ExpenseBookMembers
            .Find(m => m.ExpenseBookId == bookId && m.UserId == userId
                    && m.InviteStatus == "accepted" && !m.IsDeleted)
            .FirstOrDefaultAsync();

        return member == null ? new ResolvedPermissions { Role = "none" } : ResolveFromMember(member);
    }

    private static ResolvedPermissions ResolveFromMember(ExpenseBookMember m)
    {
        var r = RoleDefaults(m.Role);

        if (m.Permissions != null)
        {
            // Custom permissions = limited admin, loses modify/manage rights
            r.Dashboard = m.Permissions.Dashboard ?? r.Dashboard;
            r.Expenses  = m.Permissions.Expenses  ?? r.Expenses;
            r.Budgets   = m.Permissions.Budgets   ?? r.Budgets;
            r.Settings  = m.Permissions.Settings  ?? r.Settings;
            r.Insights  = m.Permissions.Insights  ?? r.Insights;
            r.CanModifyBook    = false;
            r.CanManageMembers = false;
        }

        r.CanDeleteExpenses  = m.CanDeleteExpenses;
        r.AllowedCategoryIds = m.AllowedCategoryIds;
        return r;
    }

    private static ResolvedPermissions RoleDefaults(string role) => role switch
    {
        "owner"  => new() { Role="owner",  Dashboard="view", Expenses="write", Budgets="write", Settings="write", Insights="view", CanDeleteExpenses=true,  CanManageMembers=true,  CanModifyBook=true,  IsOwner=true  },
        "admin"  => new() { Role="admin",  Dashboard="view", Expenses="write", Budgets="write", Settings="view",  Insights="view", CanDeleteExpenses=true,  CanManageMembers=true,  CanModifyBook=true,  IsOwner=false },
        "member" => new() { Role="member", Dashboard="view", Expenses="write", Budgets="none",  Settings="none",  Insights="view", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false },
        "viewer" => new() { Role="viewer", Dashboard="view", Expenses="view",  Budgets="none",  Settings="none",  Insights="view", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false },
        _        => new() { Role="none",   Dashboard="none", Expenses="none",  Budgets="none",  Settings="none",  Insights="none", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false }
    };

    public async Task EnsureMemberAccessAsync(string bookId, string userId)
    {
        var p = await GetResolvedPermissionsAsync(bookId, userId);
        if (p.Role == "none")
            throw new UnauthorizedAccessException("You do not have access to this expense book");
    }

    public async Task<bool> IsMemberAsync(string bookId, string userId)
    {
        var p = await GetResolvedPermissionsAsync(bookId, userId);
        return p.Role != "none";
    }

    public async Task InvalidatePermissionsCacheAsync(string bookId, string userId)
        => await _cache.RemoveAsync(CacheKeys.MemberPermissions(bookId, userId));

    // ─── Member CRUD ──────────────────────────────────────────────────────────

    public async Task<List<ExpenseBookMemberDto>> GetMembersAsync(
```


## License: MIT
https://github.com/AdrianoAE/EntityFrameworkCore.Translations/blob/c57ff4089c2d940a173fff704223ae5f0597d2fd/src/EntityFrameworkCore.Translations/AllTranslationsQuery.cs

```
I'm in Ask mode so I can't create new files directly. Let me provide the complete implementation — all code ready to apply. Switch to Agent mode to have these created, or use the code blocks below directly.

---

## Full ACL Implementation

### Files to Create (Backend)

---

#### `expensesBackend/Domain/Entities/ExpenseBookMember.cs`

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class PagePermissions
{
    [BsonElement("dashboard")]
    public string? Dashboard { get; set; }  // "view" | "none" | null = role default

    [BsonElement("expenses")]
    public string? Expenses { get; set; }   // "view" | "write" | "none" | null

    [BsonElement("budgets")]
    public string? Budgets { get; set; }    // "view" | "write" | "none" | null

    [BsonElement("settings")]
    public string? Settings { get; set; }   // "view" | "write" | "none" | null

    [BsonElement("insights")]
    public string? Insights { get; set; }   // "view" | "none" | null
}

public class ExpenseBookMember
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("expenseBookId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string ExpenseBookId { get; set; } = string.Empty;

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? UserId { get; set; }

    [BsonElement("invitedEmail")]
    public string? InvitedEmail { get; set; }

    /// <summary>One-time token in invite link. Nulled after acceptance.</summary>
    [BsonElement("inviteToken")]
    public string? InviteToken { get; set; }

    /// <summary>pending | accepted | revoked</summary>
    [BsonElement("inviteStatus")]
    public string InviteStatus { get; set; } = "pending";

    /// <summary>owner | admin | member | viewer</summary>
    [BsonElement("role")]
    public string Role { get; set; } = "viewer";

    /// <summary>
    /// Null = use role defaults (full access for that role).
    /// Non-null = custom overrides; CanModifyBook + CanManageMembers become false.
    /// </summary>
    [BsonElement("permissions")]
    public PagePermissions? Permissions { get; set; }

    /// <summary>Empty = all categories visible.</summary>
    [BsonElement("allowedCategoryIds")]
    public List<string> AllowedCategoryIds { get; set; } = new();

    [BsonElement("canDeleteExpenses")]
    public bool CanDeleteExpenses { get; set; } = false;

    [BsonElement("addedBy")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string AddedBy { get; set; } = string.Empty;

    [BsonElement("addedAt")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;

    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; set; }
}
```

---

#### `expensesBackend/Domain/DTOs/MemberDTOs.cs`

```csharp
namespace ExpensesBackend.API.Domain.DTOs;

public class PagePermissionsDto
{
    public string? Dashboard { get; set; }
    public string? Expenses { get; set; }
    public string? Budgets { get; set; }
    public string? Settings { get; set; }
    public string? Insights { get; set; }
}

/// <summary>Fully resolved permissions for a user on a book — ready for UI/enforcement use.</summary>
public class ResolvedPermissions
{
    public string Role { get; set; } = "none";
    public string Dashboard { get; set; } = "none";
    public string Expenses { get; set; } = "none";
    public string Budgets { get; set; } = "none";
    public string Settings { get; set; } = "none";
    public string Insights { get; set; } = "none";
    public bool CanDeleteExpenses { get; set; } = false;
    public bool CanManageMembers { get; set; } = false;
    public bool CanModifyBook { get; set; } = false;
    public bool IsOwner { get; set; } = false;
    public List<string> AllowedCategoryIds { get; set; } = new();
}

public class ExpenseBookMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string ExpenseBookId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? InvitedEmail { get; set; }
    public string? UserName { get; set; }
    public string InviteStatus { get; set; } = "pending";
    public string Role { get; set; } = "viewer";
    public PagePermissionsDto? Permissions { get; set; }
    public List<string> AllowedCategoryIds { get; set; } = new();
    public bool CanDeleteExpenses { get; set; }
    public DateTime AddedAt { get; set; }
}

public class InviteMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "viewer";
    public PagePermissionsDto? Permissions { get; set; }
    public List<string> AllowedCategoryIds { get; set; } = new();
    public bool CanDeleteExpenses { get; set; } = false;
}

public class InviteMemberResponse
{
    public ExpenseBookMemberDto Member { get; set; } = null!;
    public string InviteLink { get; set; } = string.Empty;
}

public class UpdateMemberPermissionsRequest
{
    public string? Role { get; set; }
    public PagePermissionsDto? Permissions { get; set; }
    public List<string>? AllowedCategoryIds { get; set; }
    public bool? CanDeleteExpenses { get; set; }
}

public class AcceptInviteResponse
{
    public string ExpenseBookId { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```

---

#### `expensesBackend/Services/Interfaces/IMemberService.cs`

```csharp
using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IMemberService
{
    Task<List<ExpenseBookMemberDto>> GetMembersAsync(string bookId, string requestingUserId);
    Task<InviteMemberResponse> InviteMemberAsync(string bookId, string inviterUserId, InviteMemberRequest request, string baseUrl);
    Task<ExpenseBookMemberDto> UpdateMemberAsync(string bookId, string memberId, string requestingUserId, UpdateMemberPermissionsRequest request);
    Task RemoveMemberAsync(string bookId, string memberId, string requestingUserId);
    Task<AcceptInviteResponse> AcceptInviteAsync(string token, string userId);
    Task<ResolvedPermissions> GetResolvedPermissionsAsync(string bookId, string userId);
    Task EnsureMemberAccessAsync(string bookId, string userId);
    Task<bool> IsMemberAsync(string bookId, string userId);
    Task InvalidatePermissionsCacheAsync(string bookId, string userId);
}
```

---

#### `expensesBackend/Services/MemberService.cs`

```csharp
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
    private static readonly TimeSpan PermissionsTtl = TimeSpan.FromMinutes(5);

    public MemberService(MongoDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    // ─── Permission Resolution ────────────────────────────────────────────────

    public async Task<ResolvedPermissions> GetResolvedPermissionsAsync(string bookId, string userId)
    {
        var key = CacheKeys.MemberPermissions(bookId, userId);
        return await _cache.GetOrSetAsync(key, () => ResolveFromDbAsync(bookId, userId), PermissionsTtl)
               ?? new ResolvedPermissions { Role = "none" };
    }

    private async Task<ResolvedPermissions> ResolveFromDbAsync(string bookId, string userId)
    {
        // Owner check first (authoritative)
        var isOwner = await _context.ExpenseBooks
            .Find(b => b.Id == bookId && b.UserId == userId)
            .AnyAsync();

        if (isOwner)
            return RoleDefaults("owner");

        // Member check
        var member = await _context.ExpenseBookMembers
            .Find(m => m.ExpenseBookId == bookId && m.UserId == userId
                    && m.InviteStatus == "accepted" && !m.IsDeleted)
            .FirstOrDefaultAsync();

        return member == null ? new ResolvedPermissions { Role = "none" } : ResolveFromMember(member);
    }

    private static ResolvedPermissions ResolveFromMember(ExpenseBookMember m)
    {
        var r = RoleDefaults(m.Role);

        if (m.Permissions != null)
        {
            // Custom permissions = limited admin, loses modify/manage rights
            r.Dashboard = m.Permissions.Dashboard ?? r.Dashboard;
            r.Expenses  = m.Permissions.Expenses  ?? r.Expenses;
            r.Budgets   = m.Permissions.Budgets   ?? r.Budgets;
            r.Settings  = m.Permissions.Settings  ?? r.Settings;
            r.Insights  = m.Permissions.Insights  ?? r.Insights;
            r.CanModifyBook    = false;
            r.CanManageMembers = false;
        }

        r.CanDeleteExpenses  = m.CanDeleteExpenses;
        r.AllowedCategoryIds = m.AllowedCategoryIds;
        return r;
    }

    private static ResolvedPermissions RoleDefaults(string role) => role switch
    {
        "owner"  => new() { Role="owner",  Dashboard="view", Expenses="write", Budgets="write", Settings="write", Insights="view", CanDeleteExpenses=true,  CanManageMembers=true,  CanModifyBook=true,  IsOwner=true  },
        "admin"  => new() { Role="admin",  Dashboard="view", Expenses="write", Budgets="write", Settings="view",  Insights="view", CanDeleteExpenses=true,  CanManageMembers=true,  CanModifyBook=true,  IsOwner=false },
        "member" => new() { Role="member", Dashboard="view", Expenses="write", Budgets="none",  Settings="none",  Insights="view", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false },
        "viewer" => new() { Role="viewer", Dashboard="view", Expenses="view",  Budgets="none",  Settings="none",  Insights="view", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false },
        _        => new() { Role="none",   Dashboard="none", Expenses="none",  Budgets="none",  Settings="none",  Insights="none", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false }
    };

    public async Task EnsureMemberAccessAsync(string bookId, string userId)
    {
        var p = await GetResolvedPermissionsAsync(bookId, userId);
        if (p.Role == "none")
            throw new UnauthorizedAccessException("You do not have access to this expense book");
    }

    public async Task<bool> IsMemberAsync(string bookId, string userId)
    {
        var p = await GetResolvedPermissionsAsync(bookId, userId);
        return p.Role != "none";
    }

    public async Task InvalidatePermissionsCacheAsync(string bookId, string userId)
        => await _cache.RemoveAsync(CacheKeys.MemberPermissions(bookId, userId));

    // ─── Member CRUD ──────────────────────────────────────────────────────────

    public async Task<List<ExpenseBookMemberDto>> GetMembersAsync(
```


## License: MIT
https://github.com/AdrianoAE/EntityFrameworkCore.Translations/blob/c57ff4089c2d940a173fff704223ae5f0597d2fd/src/EntityFrameworkCore.Translations/AllTranslationsQuery.cs

```
I'm in Ask mode so I can't create new files directly. Let me provide the complete implementation — all code ready to apply. Switch to Agent mode to have these created, or use the code blocks below directly.

---

## Full ACL Implementation

### Files to Create (Backend)

---

#### `expensesBackend/Domain/Entities/ExpenseBookMember.cs`

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class PagePermissions
{
    [BsonElement("dashboard")]
    public string? Dashboard { get; set; }  // "view" | "none" | null = role default

    [BsonElement("expenses")]
    public string? Expenses { get; set; }   // "view" | "write" | "none" | null

    [BsonElement("budgets")]
    public string? Budgets { get; set; }    // "view" | "write" | "none" | null

    [BsonElement("settings")]
    public string? Settings { get; set; }   // "view" | "write" | "none" | null

    [BsonElement("insights")]
    public string? Insights { get; set; }   // "view" | "none" | null
}

public class ExpenseBookMember
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("expenseBookId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string ExpenseBookId { get; set; } = string.Empty;

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? UserId { get; set; }

    [BsonElement("invitedEmail")]
    public string? InvitedEmail { get; set; }

    /// <summary>One-time token in invite link. Nulled after acceptance.</summary>
    [BsonElement("inviteToken")]
    public string? InviteToken { get; set; }

    /// <summary>pending | accepted | revoked</summary>
    [BsonElement("inviteStatus")]
    public string InviteStatus { get; set; } = "pending";

    /// <summary>owner | admin | member | viewer</summary>
    [BsonElement("role")]
    public string Role { get; set; } = "viewer";

    /// <summary>
    /// Null = use role defaults (full access for that role).
    /// Non-null = custom overrides; CanModifyBook + CanManageMembers become false.
    /// </summary>
    [BsonElement("permissions")]
    public PagePermissions? Permissions { get; set; }

    /// <summary>Empty = all categories visible.</summary>
    [BsonElement("allowedCategoryIds")]
    public List<string> AllowedCategoryIds { get; set; } = new();

    [BsonElement("canDeleteExpenses")]
    public bool CanDeleteExpenses { get; set; } = false;

    [BsonElement("addedBy")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string AddedBy { get; set; } = string.Empty;

    [BsonElement("addedAt")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;

    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; set; }
}
```

---

#### `expensesBackend/Domain/DTOs/MemberDTOs.cs`

```csharp
namespace ExpensesBackend.API.Domain.DTOs;

public class PagePermissionsDto
{
    public string? Dashboard { get; set; }
    public string? Expenses { get; set; }
    public string? Budgets { get; set; }
    public string? Settings { get; set; }
    public string? Insights { get; set; }
}

/// <summary>Fully resolved permissions for a user on a book — ready for UI/enforcement use.</summary>
public class ResolvedPermissions
{
    public string Role { get; set; } = "none";
    public string Dashboard { get; set; } = "none";
    public string Expenses { get; set; } = "none";
    public string Budgets { get; set; } = "none";
    public string Settings { get; set; } = "none";
    public string Insights { get; set; } = "none";
    public bool CanDeleteExpenses { get; set; } = false;
    public bool CanManageMembers { get; set; } = false;
    public bool CanModifyBook { get; set; } = false;
    public bool IsOwner { get; set; } = false;
    public List<string> AllowedCategoryIds { get; set; } = new();
}

public class ExpenseBookMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string ExpenseBookId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? InvitedEmail { get; set; }
    public string? UserName { get; set; }
    public string InviteStatus { get; set; } = "pending";
    public string Role { get; set; } = "viewer";
    public PagePermissionsDto? Permissions { get; set; }
    public List<string> AllowedCategoryIds { get; set; } = new();
    public bool CanDeleteExpenses { get; set; }
    public DateTime AddedAt { get; set; }
}

public class InviteMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "viewer";
    public PagePermissionsDto? Permissions { get; set; }
    public List<string> AllowedCategoryIds { get; set; } = new();
    public bool CanDeleteExpenses { get; set; } = false;
}

public class InviteMemberResponse
{
    public ExpenseBookMemberDto Member { get; set; } = null!;
    public string InviteLink { get; set; } = string.Empty;
}

public class UpdateMemberPermissionsRequest
{
    public string? Role { get; set; }
    public PagePermissionsDto? Permissions { get; set; }
    public List<string>? AllowedCategoryIds { get; set; }
    public bool? CanDeleteExpenses { get; set; }
}

public class AcceptInviteResponse
{
    public string ExpenseBookId { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```

---

#### `expensesBackend/Services/Interfaces/IMemberService.cs`

```csharp
using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IMemberService
{
    Task<List<ExpenseBookMemberDto>> GetMembersAsync(string bookId, string requestingUserId);
    Task<InviteMemberResponse> InviteMemberAsync(string bookId, string inviterUserId, InviteMemberRequest request, string baseUrl);
    Task<ExpenseBookMemberDto> UpdateMemberAsync(string bookId, string memberId, string requestingUserId, UpdateMemberPermissionsRequest request);
    Task RemoveMemberAsync(string bookId, string memberId, string requestingUserId);
    Task<AcceptInviteResponse> AcceptInviteAsync(string token, string userId);
    Task<ResolvedPermissions> GetResolvedPermissionsAsync(string bookId, string userId);
    Task EnsureMemberAccessAsync(string bookId, string userId);
    Task<bool> IsMemberAsync(string bookId, string userId);
    Task InvalidatePermissionsCacheAsync(string bookId, string userId);
}
```

---

#### `expensesBackend/Services/MemberService.cs`

```csharp
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
    private static readonly TimeSpan PermissionsTtl = TimeSpan.FromMinutes(5);

    public MemberService(MongoDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    // ─── Permission Resolution ────────────────────────────────────────────────

    public async Task<ResolvedPermissions> GetResolvedPermissionsAsync(string bookId, string userId)
    {
        var key = CacheKeys.MemberPermissions(bookId, userId);
        return await _cache.GetOrSetAsync(key, () => ResolveFromDbAsync(bookId, userId), PermissionsTtl)
               ?? new ResolvedPermissions { Role = "none" };
    }

    private async Task<ResolvedPermissions> ResolveFromDbAsync(string bookId, string userId)
    {
        // Owner check first (authoritative)
        var isOwner = await _context.ExpenseBooks
            .Find(b => b.Id == bookId && b.UserId == userId)
            .AnyAsync();

        if (isOwner)
            return RoleDefaults("owner");

        // Member check
        var member = await _context.ExpenseBookMembers
            .Find(m => m.ExpenseBookId == bookId && m.UserId == userId
                    && m.InviteStatus == "accepted" && !m.IsDeleted)
            .FirstOrDefaultAsync();

        return member == null ? new ResolvedPermissions { Role = "none" } : ResolveFromMember(member);
    }

    private static ResolvedPermissions ResolveFromMember(ExpenseBookMember m)
    {
        var r = RoleDefaults(m.Role);

        if (m.Permissions != null)
        {
            // Custom permissions = limited admin, loses modify/manage rights
            r.Dashboard = m.Permissions.Dashboard ?? r.Dashboard;
            r.Expenses  = m.Permissions.Expenses  ?? r.Expenses;
            r.Budgets   = m.Permissions.Budgets   ?? r.Budgets;
            r.Settings  = m.Permissions.Settings  ?? r.Settings;
            r.Insights  = m.Permissions.Insights  ?? r.Insights;
            r.CanModifyBook    = false;
            r.CanManageMembers = false;
        }

        r.CanDeleteExpenses  = m.CanDeleteExpenses;
        r.AllowedCategoryIds = m.AllowedCategoryIds;
        return r;
    }

    private static ResolvedPermissions RoleDefaults(string role) => role switch
    {
        "owner"  => new() { Role="owner",  Dashboard="view", Expenses="write", Budgets="write", Settings="write", Insights="view", CanDeleteExpenses=true,  CanManageMembers=true,  CanModifyBook=true,  IsOwner=true  },
        "admin"  => new() { Role="admin",  Dashboard="view", Expenses="write", Budgets="write", Settings="view",  Insights="view", CanDeleteExpenses=true,  CanManageMembers=true,  CanModifyBook=true,  IsOwner=false },
        "member" => new() { Role="member", Dashboard="view", Expenses="write", Budgets="none",  Settings="none",  Insights="view", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false },
        "viewer" => new() { Role="viewer", Dashboard="view", Expenses="view",  Budgets="none",  Settings="none",  Insights="view", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false },
        _        => new() { Role="none",   Dashboard="none", Expenses="none",  Budgets="none",  Settings="none",  Insights="none", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false }
    };

    public async Task EnsureMemberAccessAsync(string bookId, string userId)
    {
        var p = await GetResolvedPermissionsAsync(bookId, userId);
        if (p.Role == "none")
            throw new UnauthorizedAccessException("You do not have access to this expense book");
    }

    public async Task<bool> IsMemberAsync(string bookId, string userId)
    {
        var p = await GetResolvedPermissionsAsync(bookId, userId);
        return p.Role != "none";
    }

    public async Task InvalidatePermissionsCacheAsync(string bookId, string userId)
        => await _cache.RemoveAsync(CacheKeys.MemberPermissions(bookId, userId));

    // ─── Member CRUD ──────────────────────────────────────────────────────────

    public async Task<List<ExpenseBookMemberDto>> GetMembersAsync(
```


## License: unknown
https://github.com/vankhoektcn/vK2G8CC3YZhAySO/blob/061d38bf62c7ea30ec2695f3631533966d37bfaf/App_Code/KB_Class/HS_BANGGIATHUOC.cs

```
I'm in Ask mode so I can't create new files directly. Let me provide the complete implementation — all code ready to apply. Switch to Agent mode to have these created, or use the code blocks below directly.

---

## Full ACL Implementation

### Files to Create (Backend)

---

#### `expensesBackend/Domain/Entities/ExpenseBookMember.cs`

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class PagePermissions
{
    [BsonElement("dashboard")]
    public string? Dashboard { get; set; }  // "view" | "none" | null = role default

    [BsonElement("expenses")]
    public string? Expenses { get; set; }   // "view" | "write" | "none" | null

    [BsonElement("budgets")]
    public string? Budgets { get; set; }    // "view" | "write" | "none" | null

    [BsonElement("settings")]
    public string? Settings { get; set; }   // "view" | "write" | "none" | null

    [BsonElement("insights")]
    public string? Insights { get; set; }   // "view" | "none" | null
}

public class ExpenseBookMember
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("expenseBookId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string ExpenseBookId { get; set; } = string.Empty;

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? UserId { get; set; }

    [BsonElement("invitedEmail")]
    public string? InvitedEmail { get; set; }

    /// <summary>One-time token in invite link. Nulled after acceptance.</summary>
    [BsonElement("inviteToken")]
    public string? InviteToken { get; set; }

    /// <summary>pending | accepted | revoked</summary>
    [BsonElement("inviteStatus")]
    public string InviteStatus { get; set; } = "pending";

    /// <summary>owner | admin | member | viewer</summary>
    [BsonElement("role")]
    public string Role { get; set; } = "viewer";

    /// <summary>
    /// Null = use role defaults (full access for that role).
    /// Non-null = custom overrides; CanModifyBook + CanManageMembers become false.
    /// </summary>
    [BsonElement("permissions")]
    public PagePermissions? Permissions { get; set; }

    /// <summary>Empty = all categories visible.</summary>
    [BsonElement("allowedCategoryIds")]
    public List<string> AllowedCategoryIds { get; set; } = new();

    [BsonElement("canDeleteExpenses")]
    public bool CanDeleteExpenses { get; set; } = false;

    [BsonElement("addedBy")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string AddedBy { get; set; } = string.Empty;

    [BsonElement("addedAt")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;

    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; set; }
}
```

---

#### `expensesBackend/Domain/DTOs/MemberDTOs.cs`

```csharp
namespace ExpensesBackend.API.Domain.DTOs;

public class PagePermissionsDto
{
    public string? Dashboard { get; set; }
    public string? Expenses { get; set; }
    public string? Budgets { get; set; }
    public string? Settings { get; set; }
    public string? Insights { get; set; }
}

/// <summary>Fully resolved permissions for a user on a book — ready for UI/enforcement use.</summary>
public class ResolvedPermissions
{
    public string Role { get; set; } = "none";
    public string Dashboard { get; set; } = "none";
    public string Expenses { get; set; } = "none";
    public string Budgets { get; set; } = "none";
    public string Settings { get; set; } = "none";
    public string Insights { get; set; } = "none";
    public bool CanDeleteExpenses { get; set; } = false;
    public bool CanManageMembers { get; set; } = false;
    public bool CanModifyBook { get; set; } = false;
    public bool IsOwner { get; set; } = false;
    public List<string> AllowedCategoryIds { get; set; } = new();
}

public class ExpenseBookMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string ExpenseBookId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? InvitedEmail { get; set; }
    public string? UserName { get; set; }
    public string InviteStatus { get; set; } = "pending";
    public string Role { get; set; } = "viewer";
    public PagePermissionsDto? Permissions { get; set; }
    public List<string> AllowedCategoryIds { get; set; } = new();
    public bool CanDeleteExpenses { get; set; }
    public DateTime AddedAt { get; set; }
}

public class InviteMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "viewer";
    public PagePermissionsDto? Permissions { get; set; }
    public List<string> AllowedCategoryIds { get; set; } = new();
    public bool CanDeleteExpenses { get; set; } = false;
}

public class InviteMemberResponse
{
    public ExpenseBookMemberDto Member { get; set; } = null!;
    public string InviteLink { get; set; } = string.Empty;
}

public class UpdateMemberPermissionsRequest
{
    public string? Role { get; set; }
    public PagePermissionsDto? Permissions { get; set; }
    public List<string>? AllowedCategoryIds { get; set; }
    public bool? CanDeleteExpenses { get; set; }
}

public class AcceptInviteResponse
{
    public string ExpenseBookId { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```

---

#### `expensesBackend/Services/Interfaces/IMemberService.cs`

```csharp
using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IMemberService
{
    Task<List<ExpenseBookMemberDto>> GetMembersAsync(string bookId, string requestingUserId);
    Task<InviteMemberResponse> InviteMemberAsync(string bookId, string inviterUserId, InviteMemberRequest request, string baseUrl);
    Task<ExpenseBookMemberDto> UpdateMemberAsync(string bookId, string memberId, string requestingUserId, UpdateMemberPermissionsRequest request);
    Task RemoveMemberAsync(string bookId, string memberId, string requestingUserId);
    Task<AcceptInviteResponse> AcceptInviteAsync(string token, string userId);
    Task<ResolvedPermissions> GetResolvedPermissionsAsync(string bookId, string userId);
    Task EnsureMemberAccessAsync(string bookId, string userId);
    Task<bool> IsMemberAsync(string bookId, string userId);
    Task InvalidatePermissionsCacheAsync(string bookId, string userId);
}
```

---

#### `expensesBackend/Services/MemberService.cs`

```csharp
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
    private static readonly TimeSpan PermissionsTtl = TimeSpan.FromMinutes(5);

    public MemberService(MongoDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    // ─── Permission Resolution ────────────────────────────────────────────────

    public async Task<ResolvedPermissions> GetResolvedPermissionsAsync(string bookId, string userId)
    {
        var key = CacheKeys.MemberPermissions(bookId, userId);
        return await _cache.GetOrSetAsync(key, () => ResolveFromDbAsync(bookId, userId), PermissionsTtl)
               ?? new ResolvedPermissions { Role = "none" };
    }

    private async Task<ResolvedPermissions> ResolveFromDbAsync(string bookId, string userId)
    {
        // Owner check first (authoritative)
        var isOwner = await _context.ExpenseBooks
            .Find(b => b.Id == bookId && b.UserId == userId)
            .AnyAsync();

        if (isOwner)
            return RoleDefaults("owner");

        // Member check
        var member = await _context.ExpenseBookMembers
            .Find(m => m.ExpenseBookId == bookId && m.UserId == userId
                    && m.InviteStatus == "accepted" && !m.IsDeleted)
            .FirstOrDefaultAsync();

        return member == null ? new ResolvedPermissions { Role = "none" } : ResolveFromMember(member);
    }

    private static ResolvedPermissions ResolveFromMember(ExpenseBookMember m)
    {
        var r = RoleDefaults(m.Role);

        if (m.Permissions != null)
        {
            // Custom permissions = limited admin, loses modify/manage rights
            r.Dashboard = m.Permissions.Dashboard ?? r.Dashboard;
            r.Expenses  = m.Permissions.Expenses  ?? r.Expenses;
            r.Budgets   = m.Permissions.Budgets   ?? r.Budgets;
            r.Settings  = m.Permissions.Settings  ?? r.Settings;
            r.Insights  = m.Permissions.Insights  ?? r.Insights;
            r.CanModifyBook    = false;
            r.CanManageMembers = false;
        }

        r.CanDeleteExpenses  = m.CanDeleteExpenses;
        r.AllowedCategoryIds = m.AllowedCategoryIds;
        return r;
    }

    private static ResolvedPermissions RoleDefaults(string role) => role switch
    {
        "owner"  => new() { Role="owner",  Dashboard="view", Expenses="write", Budgets="write", Settings="write", Insights="view", CanDeleteExpenses=true,  CanManageMembers=true,  CanModifyBook=true,  IsOwner=true  },
        "admin"  => new() { Role="admin",  Dashboard="view", Expenses="write", Budgets="write", Settings="view",  Insights="view", CanDeleteExpenses=true,  CanManageMembers=true,  CanModifyBook=true,  IsOwner=false },
        "member" => new() { Role="member", Dashboard="view", Expenses="write", Budgets="none",  Settings="none",  Insights="view", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false },
        "viewer" => new() { Role="viewer", Dashboard="view", Expenses="view",  Budgets="none",  Settings="none",  Insights="view", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false },
        _        => new() { Role="none",   Dashboard="none", Expenses="none",  Budgets="none",  Settings="none",  Insights="none", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false }
    };

    public async Task EnsureMemberAccessAsync(string bookId, string userId)
    {
        var p = await GetResolvedPermissionsAsync(bookId, userId);
        if (p.Role == "none")
            throw new UnauthorizedAccessException("You do not have access to this expense book");
    }

    public async Task<bool> IsMemberAsync(string bookId, string userId)
    {
        var p = await GetResolvedPermissionsAsync(bookId, userId);
        return p.Role != "none";
    }

    public async Task InvalidatePermissionsCacheAsync(string bookId, string userId)
        => await _cache.RemoveAsync(CacheKeys.MemberPermissions(bookId, userId));

    // ─── Member CRUD ──────────────────────────────────────────────────────────

    public async Task<List<ExpenseBookMemberDto>> GetMembersAsync(string bookId, string requestingUserId)
    {
        await EnsureMemberAccessAsync(bookId, requestingUserId);

        var members = await _context.ExpenseBookMembers
            .Find(m => m.ExpenseBookId == bookId && !m.IsDeleted)
            .ToListAsync();

        // Enrich with user info
        var userIds = members.Where(m => m.UserId != null).Select(m => m.UserId!).Distinct().ToList();
        var userMap = userIds.Any()
            ? (await _context.Users.Find(u => userIds.Contains(u.Id)).ToListAsync()).ToDictionary(u => u.Id)
            : new Dictionary<string, User>();

        // Add synthetic owner record if not already in member collection
        var book = await _context.ExpenseBooks.Find(b => b.Id == bookId).FirstOrDefaultAsync();
        var dtos = new List<ExpenseBookMemberDto>();

        if (book != null && members.All(m => m.UserId != book.UserId || m.Role != "owner"))
        {
            userMap.TryGetValue(book.UserId, out var ownerUser);
            dtos.Add(new ExpenseBookMemberDto
            {
                Id            = $"owner-{book.UserId}",
                ExpenseBookId = bookId,
                UserId        = book.UserId,
                InvitedEmail  = ownerUser?.Email,
                UserName      = ownerUser?.Name,
                InviteStatus  = "accepted",
                Role          = "owner",
                CanDeleteExpenses = true,
                AddedAt       = book.CreatedAt
            });
        }

        foreach (var m in members)
        {
            userMap.TryGetValue(m.UserId ?? "", out var u);
            dtos.Add(MapToDto(m, u));
        }

        return dtos;
    }

    public async Task<InviteMemberResponse> InviteMemberAsync(
        string bookId, string inviterUserId, InviteMemberRequest request, string baseUrl)
    {
        var perms = await GetResolvedPermissionsAsync(bookId, inviterUserId);
        if (!perms.CanManageMembers)
            throw new UnauthorizedAccessException("You do not have permission to manage members");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required");

        if (!IsValidNonOwnerRole(request.Role))
            throw new ArgumentException("Invalid role. Valid: admin, member, viewer");

        var email = request.Email.Trim().ToLowerInvariant();

        // Check for duplicate active membership
        var existingUser = await _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
        if (existingUser != null)
        {
            var alreadyMember = await _context.ExpenseBookMembers
                .Find(m => m.ExpenseBookId == bookId && m.UserId == existingUser.Id
                        && m.InviteStatus == "accepted" && !m.IsDeleted)
                .AnyAsync();
            if (alreadyMember)
                throw new ArgumentException("This user is already a member");

            // Check if they are the owner
            var isOwner = await _context.ExpenseBooks
                .Find(b => b.Id == bookId && b.UserId == existingUser.Id)
                .AnyAsync();
            if (isOwner)
                throw new ArgumentException("This user is the owner of this expense book");
        }

        // Check for duplicate pending invite
        var hasPending = await _context.ExpenseBookMembers
            .Find(m => m.ExpenseBookId == bookId && m.InvitedEmail == email
                    && m.InviteStatus == "pending" && !m.IsDeleted)
            .AnyAsync();
        if (hasPending)
            throw new ArgumentException("A pending invite already exists for this email");

        var token = GenerateToken();
        var member = new ExpenseBookMember
        {
            ExpenseBookId = bookId,
            UserId        = existingUser?.Id,
            InvitedEmail  = email,
            InviteToken   = token,
            InviteStatus  = "pending",
            Role          = request.Role,
            Permissions   = request.Permissions == null ? null : new PagePermissions
            {
                Dashboard = request.Permissions.Dashboard,
                Expenses  = request.Permissions.Expenses,
                Budgets   = request.Permissions.Budgets,
                Settings  = request.Permissions.Settings,
                Insights  = request.Permissions.Insights
            },
            AllowedCategoryIds = request.AllowedCategoryIds ?? new(),
            CanDeleteExpenses  = request.CanDeleteExpenses,
            AddedBy            = inviterUserId,
            AddedAt            = DateTime.UtcNow,
            UpdatedAt          = DateTime.UtcNow
        };

        await _context.ExpenseBookMembers.InsertOneAsync(member);

        var inviteLink = $"{baseUrl.TrimEnd('/')}/accept-invite?token={token}";
        return new InviteMemberResponse { Member = MapToDto(member, existingUser), InviteLink = inviteLink };
    }

    public async Task<ExpenseBookMemberDto> UpdateMemberAsync(
        string bookId, string memberId, string requestingUserId, UpdateMemberPermissionsRequest request)
    {
        var perms = await GetResolvedPermissionsAsync(bookId, requestingUserId);
        if (!perms.CanManageMembers)
            throw new UnauthorizedAccessException("You do not have permission to manage members");

        var member = await _context.ExpenseBookMembers
            .Find(m => m.Id == memberId && m.ExpenseBookId == bookId && !m.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Member not found");

        if (member.Role == "owner")
            throw new InvalidOperationException("Cannot modify owner permissions");

        var updates = new List<UpdateDefinition<ExpenseBookMember>>();

        if (request.Role != null && IsValidNonOwnerRole(request.Role))
            updates.Add(Builders<ExpenseBookMember>.Update.Set(m => m.Role, request.Role));

        if (request.Permissions != null)
        {
            var p = new PagePermissions
            {
                Dashboard = request.Permissions.Dashboard,
                Expenses  = request.Permissions.Expenses,
                Budgets   = request.Permissions.Budgets,
                Settings  = request.Permissions.Settings,
                Insights  = request.Permissions.Insights
            };
            updates.Add(Builders<ExpenseBookMember>.Update.Set(m => m.Permissions, p));
        }

        if (request.AllowedCategoryIds != null)
            updates.Add(Builders<ExpenseBookMember>.Update.Set(m => m.AllowedCategoryIds, request.AllowedCategoryIds));

        if (request.CanDeleteExpenses.HasValue)
            updates.Add(Builders<ExpenseBookMember>.Update.Set(m => m.CanDeleteExpenses, request.CanDeleteExpenses.Value));

        updates.Add(Builders<ExpenseBookMember>.Update.Set(m => m.UpdatedAt, DateTime.UtcNow));

        await _context.ExpenseBookMembers.UpdateOneAsync(
            m => m.Id == memberId, Builders<ExpenseBookMember>.Update.Combine(updates));

        if (member.UserId != null)
            await _cache.RemoveAsync(CacheKeys.MemberPermissions(bookId, member.UserId));

        var updated = await _context.ExpenseBookMembers.Find(m => m.Id == memberId).FirstOrDefaultAsync();
        User? user = member.UserId != null
            ? await _context.Users.Find(u => u.Id == member.UserId).FirstOrDefaultAsync()
            : null;
        return MapToDto(updated!, user);
    }

    public async Task RemoveMemberAsync(string bookId, string memberId, string requestingUserId)
    {
        var perms = await GetResolvedPermissionsAsync(bookId, requestingUserId);
        if (!perms.CanManageMembers)
            throw new UnauthorizedAccessException("You do not have permission to manage members");

        var member = await _context.ExpenseBookMembers
            .Find(m => m.Id == memberId && m.ExpenseBookId == bookId && !m.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Member not found");

        if (member.Role == "owner")
            throw new InvalidOperationException("Cannot remove the owner");

        // Soft-delete if user is a member of another book; hard-delete otherwise
        bool softDelete = false;
        if (member.UserId != null)
        {
            softDelete = await _context.ExpenseBookMembers
                .Find(m => m.UserId == member.UserId && m.ExpenseBookId != bookId
                        && m.InviteStatus == "accepted" && !m.IsDeleted)
                .AnyAsync();
        }

        if (softDelete)
        {
            var upd = Builders<ExpenseBookMember>.Update
                .Set(m => m.InviteStatus, "revoked")
                .Set(m => m.IsDeleted, true)
                .Set(m => m.DeletedAt, DateTime.UtcNow);
            await _context.ExpenseBookMembers.UpdateOneAsync(m => m.Id == memberId, upd);
        }
        else
        {
            await _context.ExpenseBookMembers.DeleteOneAsync(m => m.Id == memberId);
        }

        if (member.UserId != null)
            await _cache.RemoveAsync(CacheKeys.MemberPermissions(bookId, member.UserId));
    }

    public async Task<AcceptInviteResponse> AcceptInviteAsync(string token, string userId)
    {
        var member = await _context.ExpenseBookMembers
            .Find(m => m.InviteToken == token && m.InviteStatus == "pending" && !m.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Invite not found or already used");

        var book = await _context.ExpenseBooks
            .Find(b => b.Id == member.ExpenseBookId)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Expense book no longer exists");

        var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync()
            ?? throw new UnauthorizedAccessException("User not found");

        if (!string.IsNullOrEmpty(member.InvitedEmail) &&
            !string.Equals(user.Email, member.InvitedEmail, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("This invite was sent to a different email address");

        var upd = Builders<ExpenseBookMember>.Update
            .Set(m => m.UserId, userId)
            .Set(m => m.InviteStatus, "accepted")
            .Set(m => m.InviteToken, null)   // consume token
            .Set(m => m.UpdatedAt, DateTime.UtcNow);

        await _context.ExpenseBookMembers.UpdateOneAsync(m => m.Id == member.Id, upd);

        return new AcceptInviteResponse
        {
            ExpenseBookId = book.Id,
            BookName      = book.Name,
            Role          = member.Role
        };
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string GenerateToken() =>
        Convert
```


## License: unknown
https://github.com/vankhoektcn/vK2G8CC3YZhAySO/blob/061d38bf62c7ea30ec2695f3631533966d37bfaf/App_Code/KB_Class/HS_BANGGIATHUOC.cs

```
I'm in Ask mode so I can't create new files directly. Let me provide the complete implementation — all code ready to apply. Switch to Agent mode to have these created, or use the code blocks below directly.

---

## Full ACL Implementation

### Files to Create (Backend)

---

#### `expensesBackend/Domain/Entities/ExpenseBookMember.cs`

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class PagePermissions
{
    [BsonElement("dashboard")]
    public string? Dashboard { get; set; }  // "view" | "none" | null = role default

    [BsonElement("expenses")]
    public string? Expenses { get; set; }   // "view" | "write" | "none" | null

    [BsonElement("budgets")]
    public string? Budgets { get; set; }    // "view" | "write" | "none" | null

    [BsonElement("settings")]
    public string? Settings { get; set; }   // "view" | "write" | "none" | null

    [BsonElement("insights")]
    public string? Insights { get; set; }   // "view" | "none" | null
}

public class ExpenseBookMember
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("expenseBookId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string ExpenseBookId { get; set; } = string.Empty;

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? UserId { get; set; }

    [BsonElement("invitedEmail")]
    public string? InvitedEmail { get; set; }

    /// <summary>One-time token in invite link. Nulled after acceptance.</summary>
    [BsonElement("inviteToken")]
    public string? InviteToken { get; set; }

    /// <summary>pending | accepted | revoked</summary>
    [BsonElement("inviteStatus")]
    public string InviteStatus { get; set; } = "pending";

    /// <summary>owner | admin | member | viewer</summary>
    [BsonElement("role")]
    public string Role { get; set; } = "viewer";

    /// <summary>
    /// Null = use role defaults (full access for that role).
    /// Non-null = custom overrides; CanModifyBook + CanManageMembers become false.
    /// </summary>
    [BsonElement("permissions")]
    public PagePermissions? Permissions { get; set; }

    /// <summary>Empty = all categories visible.</summary>
    [BsonElement("allowedCategoryIds")]
    public List<string> AllowedCategoryIds { get; set; } = new();

    [BsonElement("canDeleteExpenses")]
    public bool CanDeleteExpenses { get; set; } = false;

    [BsonElement("addedBy")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string AddedBy { get; set; } = string.Empty;

    [BsonElement("addedAt")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;

    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; set; }
}
```

---

#### `expensesBackend/Domain/DTOs/MemberDTOs.cs`

```csharp
namespace ExpensesBackend.API.Domain.DTOs;

public class PagePermissionsDto
{
    public string? Dashboard { get; set; }
    public string? Expenses { get; set; }
    public string? Budgets { get; set; }
    public string? Settings { get; set; }
    public string? Insights { get; set; }
}

/// <summary>Fully resolved permissions for a user on a book — ready for UI/enforcement use.</summary>
public class ResolvedPermissions
{
    public string Role { get; set; } = "none";
    public string Dashboard { get; set; } = "none";
    public string Expenses { get; set; } = "none";
    public string Budgets { get; set; } = "none";
    public string Settings { get; set; } = "none";
    public string Insights { get; set; } = "none";
    public bool CanDeleteExpenses { get; set; } = false;
    public bool CanManageMembers { get; set; } = false;
    public bool CanModifyBook { get; set; } = false;
    public bool IsOwner { get; set; } = false;
    public List<string> AllowedCategoryIds { get; set; } = new();
}

public class ExpenseBookMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string ExpenseBookId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? InvitedEmail { get; set; }
    public string? UserName { get; set; }
    public string InviteStatus { get; set; } = "pending";
    public string Role { get; set; } = "viewer";
    public PagePermissionsDto? Permissions { get; set; }
    public List<string> AllowedCategoryIds { get; set; } = new();
    public bool CanDeleteExpenses { get; set; }
    public DateTime AddedAt { get; set; }
}

public class InviteMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "viewer";
    public PagePermissionsDto? Permissions { get; set; }
    public List<string> AllowedCategoryIds { get; set; } = new();
    public bool CanDeleteExpenses { get; set; } = false;
}

public class InviteMemberResponse
{
    public ExpenseBookMemberDto Member { get; set; } = null!;
    public string InviteLink { get; set; } = string.Empty;
}

public class UpdateMemberPermissionsRequest
{
    public string? Role { get; set; }
    public PagePermissionsDto? Permissions { get; set; }
    public List<string>? AllowedCategoryIds { get; set; }
    public bool? CanDeleteExpenses { get; set; }
}

public class AcceptInviteResponse
{
    public string ExpenseBookId { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```

---

#### `expensesBackend/Services/Interfaces/IMemberService.cs`

```csharp
using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IMemberService
{
    Task<List<ExpenseBookMemberDto>> GetMembersAsync(string bookId, string requestingUserId);
    Task<InviteMemberResponse> InviteMemberAsync(string bookId, string inviterUserId, InviteMemberRequest request, string baseUrl);
    Task<ExpenseBookMemberDto> UpdateMemberAsync(string bookId, string memberId, string requestingUserId, UpdateMemberPermissionsRequest request);
    Task RemoveMemberAsync(string bookId, string memberId, string requestingUserId);
    Task<AcceptInviteResponse> AcceptInviteAsync(string token, string userId);
    Task<ResolvedPermissions> GetResolvedPermissionsAsync(string bookId, string userId);
    Task EnsureMemberAccessAsync(string bookId, string userId);
    Task<bool> IsMemberAsync(string bookId, string userId);
    Task InvalidatePermissionsCacheAsync(string bookId, string userId);
}
```

---

#### `expensesBackend/Services/MemberService.cs`

```csharp
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
    private static readonly TimeSpan PermissionsTtl = TimeSpan.FromMinutes(5);

    public MemberService(MongoDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    // ─── Permission Resolution ────────────────────────────────────────────────

    public async Task<ResolvedPermissions> GetResolvedPermissionsAsync(string bookId, string userId)
    {
        var key = CacheKeys.MemberPermissions(bookId, userId);
        return await _cache.GetOrSetAsync(key, () => ResolveFromDbAsync(bookId, userId), PermissionsTtl)
               ?? new ResolvedPermissions { Role = "none" };
    }

    private async Task<ResolvedPermissions> ResolveFromDbAsync(string bookId, string userId)
    {
        // Owner check first (authoritative)
        var isOwner = await _context.ExpenseBooks
            .Find(b => b.Id == bookId && b.UserId == userId)
            .AnyAsync();

        if (isOwner)
            return RoleDefaults("owner");

        // Member check
        var member = await _context.ExpenseBookMembers
            .Find(m => m.ExpenseBookId == bookId && m.UserId == userId
                    && m.InviteStatus == "accepted" && !m.IsDeleted)
            .FirstOrDefaultAsync();

        return member == null ? new ResolvedPermissions { Role = "none" } : ResolveFromMember(member);
    }

    private static ResolvedPermissions ResolveFromMember(ExpenseBookMember m)
    {
        var r = RoleDefaults(m.Role);

        if (m.Permissions != null)
        {
            // Custom permissions = limited admin, loses modify/manage rights
            r.Dashboard = m.Permissions.Dashboard ?? r.Dashboard;
            r.Expenses  = m.Permissions.Expenses  ?? r.Expenses;
            r.Budgets   = m.Permissions.Budgets   ?? r.Budgets;
            r.Settings  = m.Permissions.Settings  ?? r.Settings;
            r.Insights  = m.Permissions.Insights  ?? r.Insights;
            r.CanModifyBook    = false;
            r.CanManageMembers = false;
        }

        r.CanDeleteExpenses  = m.CanDeleteExpenses;
        r.AllowedCategoryIds = m.AllowedCategoryIds;
        return r;
    }

    private static ResolvedPermissions RoleDefaults(string role) => role switch
    {
        "owner"  => new() { Role="owner",  Dashboard="view", Expenses="write", Budgets="write", Settings="write", Insights="view", CanDeleteExpenses=true,  CanManageMembers=true,  CanModifyBook=true,  IsOwner=true  },
        "admin"  => new() { Role="admin",  Dashboard="view", Expenses="write", Budgets="write", Settings="view",  Insights="view", CanDeleteExpenses=true,  CanManageMembers=true,  CanModifyBook=true,  IsOwner=false },
        "member" => new() { Role="member", Dashboard="view", Expenses="write", Budgets="none",  Settings="none",  Insights="view", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false },
        "viewer" => new() { Role="viewer", Dashboard="view", Expenses="view",  Budgets="none",  Settings="none",  Insights="view", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false },
        _        => new() { Role="none",   Dashboard="none", Expenses="none",  Budgets="none",  Settings="none",  Insights="none", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false }
    };

    public async Task EnsureMemberAccessAsync(string bookId, string userId)
    {
        var p = await GetResolvedPermissionsAsync(bookId, userId);
        if (p.Role == "none")
            throw new UnauthorizedAccessException("You do not have access to this expense book");
    }

    public async Task<bool> IsMemberAsync(string bookId, string userId)
    {
        var p = await GetResolvedPermissionsAsync(bookId, userId);
        return p.Role != "none";
    }

    public async Task InvalidatePermissionsCacheAsync(string bookId, string userId)
        => await _cache.RemoveAsync(CacheKeys.MemberPermissions(bookId, userId));

    // ─── Member CRUD ──────────────────────────────────────────────────────────

    public async Task<List<ExpenseBookMemberDto>> GetMembersAsync(string bookId, string requestingUserId)
    {
        await EnsureMemberAccessAsync(bookId, requestingUserId);

        var members = await _context.ExpenseBookMembers
            .Find(m => m.ExpenseBookId == bookId && !m.IsDeleted)
            .ToListAsync();

        // Enrich with user info
        var userIds = members.Where(m => m.UserId != null).Select(m => m.UserId!).Distinct().ToList();
        var userMap = userIds.Any()
            ? (await _context.Users.Find(u => userIds.Contains(u.Id)).ToListAsync()).ToDictionary(u => u.Id)
            : new Dictionary<string, User>();

        // Add synthetic owner record if not already in member collection
        var book = await _context.ExpenseBooks.Find(b => b.Id == bookId).FirstOrDefaultAsync();
        var dtos = new List<ExpenseBookMemberDto>();

        if (book != null && members.All(m => m.UserId != book.UserId || m.Role != "owner"))
        {
            userMap.TryGetValue(book.UserId, out var ownerUser);
            dtos.Add(new ExpenseBookMemberDto
            {
                Id            = $"owner-{book.UserId}",
                ExpenseBookId = bookId,
                UserId        = book.UserId,
                InvitedEmail  = ownerUser?.Email,
                UserName      = ownerUser?.Name,
                InviteStatus  = "accepted",
                Role          = "owner",
                CanDeleteExpenses = true,
                AddedAt       = book.CreatedAt
            });
        }

        foreach (var m in members)
        {
            userMap.TryGetValue(m.UserId ?? "", out var u);
            dtos.Add(MapToDto(m, u));
        }

        return dtos;
    }

    public async Task<InviteMemberResponse> InviteMemberAsync(
        string bookId, string inviterUserId, InviteMemberRequest request, string baseUrl)
    {
        var perms = await GetResolvedPermissionsAsync(bookId, inviterUserId);
        if (!perms.CanManageMembers)
            throw new UnauthorizedAccessException("You do not have permission to manage members");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required");

        if (!IsValidNonOwnerRole(request.Role))
            throw new ArgumentException("Invalid role. Valid: admin, member, viewer");

        var email = request.Email.Trim().ToLowerInvariant();

        // Check for duplicate active membership
        var existingUser = await _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
        if (existingUser != null)
        {
            var alreadyMember = await _context.ExpenseBookMembers
                .Find(m => m.ExpenseBookId == bookId && m.UserId == existingUser.Id
                        && m.InviteStatus == "accepted" && !m.IsDeleted)
                .AnyAsync();
            if (alreadyMember)
                throw new ArgumentException("This user is already a member");

            // Check if they are the owner
            var isOwner = await _context.ExpenseBooks
                .Find(b => b.Id == bookId && b.UserId == existingUser.Id)
                .AnyAsync();
            if (isOwner)
                throw new ArgumentException("This user is the owner of this expense book");
        }

        // Check for duplicate pending invite
        var hasPending = await _context.ExpenseBookMembers
            .Find(m => m.ExpenseBookId == bookId && m.InvitedEmail == email
                    && m.InviteStatus == "pending" && !m.IsDeleted)
            .AnyAsync();
        if (hasPending)
            throw new ArgumentException("A pending invite already exists for this email");

        var token = GenerateToken();
        var member = new ExpenseBookMember
        {
            ExpenseBookId = bookId,
            UserId        = existingUser?.Id,
            InvitedEmail  = email,
            InviteToken   = token,
            InviteStatus  = "pending",
            Role          = request.Role,
            Permissions   = request.Permissions == null ? null : new PagePermissions
            {
                Dashboard = request.Permissions.Dashboard,
                Expenses  = request.Permissions.Expenses,
                Budgets   = request.Permissions.Budgets,
                Settings  = request.Permissions.Settings,
                Insights  = request.Permissions.Insights
            },
            AllowedCategoryIds = request.AllowedCategoryIds ?? new(),
            CanDeleteExpenses  = request.CanDeleteExpenses,
            AddedBy            = inviterUserId,
            AddedAt            = DateTime.UtcNow,
            UpdatedAt          = DateTime.UtcNow
        };

        await _context.ExpenseBookMembers.InsertOneAsync(member);

        var inviteLink = $"{baseUrl.TrimEnd('/')}/accept-invite?token={token}";
        return new InviteMemberResponse { Member = MapToDto(member, existingUser), InviteLink = inviteLink };
    }

    public async Task<ExpenseBookMemberDto> UpdateMemberAsync(
        string bookId, string memberId, string requestingUserId, UpdateMemberPermissionsRequest request)
    {
        var perms = await GetResolvedPermissionsAsync(bookId, requestingUserId);
        if (!perms.CanManageMembers)
            throw new UnauthorizedAccessException("You do not have permission to manage members");

        var member = await _context.ExpenseBookMembers
            .Find(m => m.Id == memberId && m.ExpenseBookId == bookId && !m.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Member not found");

        if (member.Role == "owner")
            throw new InvalidOperationException("Cannot modify owner permissions");

        var updates = new List<UpdateDefinition<ExpenseBookMember>>();

        if (request.Role != null && IsValidNonOwnerRole(request.Role))
            updates.Add(Builders<ExpenseBookMember>.Update.Set(m => m.Role, request.Role));

        if (request.Permissions != null)
        {
            var p = new PagePermissions
            {
                Dashboard = request.Permissions.Dashboard,
                Expenses  = request.Permissions.Expenses,
                Budgets   = request.Permissions.Budgets,
                Settings  = request.Permissions.Settings,
                Insights  = request.Permissions.Insights
            };
            updates.Add(Builders<ExpenseBookMember>.Update.Set(m => m.Permissions, p));
        }

        if (request.AllowedCategoryIds != null)
            updates.Add(Builders<ExpenseBookMember>.Update.Set(m => m.AllowedCategoryIds, request.AllowedCategoryIds));

        if (request.CanDeleteExpenses.HasValue)
            updates.Add(Builders<ExpenseBookMember>.Update.Set(m => m.CanDeleteExpenses, request.CanDeleteExpenses.Value));

        updates.Add(Builders<ExpenseBookMember>.Update.Set(m => m.UpdatedAt, DateTime.UtcNow));

        await _context.ExpenseBookMembers.UpdateOneAsync(
            m => m.Id == memberId, Builders<ExpenseBookMember>.Update.Combine(updates));

        if (member.UserId != null)
            await _cache.RemoveAsync(CacheKeys.MemberPermissions(bookId, member.UserId));

        var updated = await _context.ExpenseBookMembers.Find(m => m.Id == memberId).FirstOrDefaultAsync();
        User? user = member.UserId != null
            ? await _context.Users.Find(u => u.Id == member.UserId).FirstOrDefaultAsync()
            : null;
        return MapToDto(updated!, user);
    }

    public async Task RemoveMemberAsync(string bookId, string memberId, string requestingUserId)
    {
        var perms = await GetResolvedPermissionsAsync(bookId, requestingUserId);
        if (!perms.CanManageMembers)
            throw new UnauthorizedAccessException("You do not have permission to manage members");

        var member = await _context.ExpenseBookMembers
            .Find(m => m.Id == memberId && m.ExpenseBookId == bookId && !m.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Member not found");

        if (member.Role == "owner")
            throw new InvalidOperationException("Cannot remove the owner");

        // Soft-delete if user is a member of another book; hard-delete otherwise
        bool softDelete = false;
        if (member.UserId != null)
        {
            softDelete = await _context.ExpenseBookMembers
                .Find(m => m.UserId == member.UserId && m.ExpenseBookId != bookId
                        && m.InviteStatus == "accepted" && !m.IsDeleted)
                .AnyAsync();
        }

        if (softDelete)
        {
            var upd = Builders<ExpenseBookMember>.Update
                .Set(m => m.InviteStatus, "revoked")
                .Set(m => m.IsDeleted, true)
                .Set(m => m.DeletedAt, DateTime.UtcNow);
            await _context.ExpenseBookMembers.UpdateOneAsync(m => m.Id == memberId, upd);
        }
        else
        {
            await _context.ExpenseBookMembers.DeleteOneAsync(m => m.Id == memberId);
        }

        if (member.UserId != null)
            await _cache.RemoveAsync(CacheKeys.MemberPermissions(bookId, member.UserId));
    }

    public async Task<AcceptInviteResponse> AcceptInviteAsync(string token, string userId)
    {
        var member = await _context.ExpenseBookMembers
            .Find(m => m.InviteToken == token && m.InviteStatus == "pending" && !m.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Invite not found or already used");

        var book = await _context.ExpenseBooks
            .Find(b => b.Id == member.ExpenseBookId)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Expense book no longer exists");

        var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync()
            ?? throw new UnauthorizedAccessException("User not found");

        if (!string.IsNullOrEmpty(member.InvitedEmail) &&
            !string.Equals(user.Email, member.InvitedEmail, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("This invite was sent to a different email address");

        var upd = Builders<ExpenseBookMember>.Update
            .Set(m => m.UserId, userId)
            .Set(m => m.InviteStatus, "accepted")
            .Set(m => m.InviteToken, null)   // consume token
            .Set(m => m.UpdatedAt, DateTime.UtcNow);

        await _context.ExpenseBookMembers.UpdateOneAsync(m => m.Id == member.Id, upd);

        return new AcceptInviteResponse
        {
            ExpenseBookId = book.Id,
            BookName      = book.Name,
            Role          = member.Role
        };
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string GenerateToken() =>
        Convert
```


## License: unknown
https://github.com/vankhoektcn/vK2G8CC3YZhAySO/blob/061d38bf62c7ea30ec2695f3631533966d37bfaf/App_Code/KB_Class/HS_BANGGIATHUOC.cs

```
I'm in Ask mode so I can't create new files directly. Let me provide the complete implementation — all code ready to apply. Switch to Agent mode to have these created, or use the code blocks below directly.

---

## Full ACL Implementation

### Files to Create (Backend)

---

#### `expensesBackend/Domain/Entities/ExpenseBookMember.cs`

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ExpensesBackend.API.Domain.Entities;

public class PagePermissions
{
    [BsonElement("dashboard")]
    public string? Dashboard { get; set; }  // "view" | "none" | null = role default

    [BsonElement("expenses")]
    public string? Expenses { get; set; }   // "view" | "write" | "none" | null

    [BsonElement("budgets")]
    public string? Budgets { get; set; }    // "view" | "write" | "none" | null

    [BsonElement("settings")]
    public string? Settings { get; set; }   // "view" | "write" | "none" | null

    [BsonElement("insights")]
    public string? Insights { get; set; }   // "view" | "none" | null
}

public class ExpenseBookMember
{
    [BsonId]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("expenseBookId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string ExpenseBookId { get; set; } = string.Empty;

    [BsonElement("userId")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string? UserId { get; set; }

    [BsonElement("invitedEmail")]
    public string? InvitedEmail { get; set; }

    /// <summary>One-time token in invite link. Nulled after acceptance.</summary>
    [BsonElement("inviteToken")]
    public string? InviteToken { get; set; }

    /// <summary>pending | accepted | revoked</summary>
    [BsonElement("inviteStatus")]
    public string InviteStatus { get; set; } = "pending";

    /// <summary>owner | admin | member | viewer</summary>
    [BsonElement("role")]
    public string Role { get; set; } = "viewer";

    /// <summary>
    /// Null = use role defaults (full access for that role).
    /// Non-null = custom overrides; CanModifyBook + CanManageMembers become false.
    /// </summary>
    [BsonElement("permissions")]
    public PagePermissions? Permissions { get; set; }

    /// <summary>Empty = all categories visible.</summary>
    [BsonElement("allowedCategoryIds")]
    public List<string> AllowedCategoryIds { get; set; } = new();

    [BsonElement("canDeleteExpenses")]
    public bool CanDeleteExpenses { get; set; } = false;

    [BsonElement("addedBy")]
    [BsonSerializer(typeof(FlexibleStringSerializer))]
    public string AddedBy { get; set; } = string.Empty;

    [BsonElement("addedAt")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;

    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; set; }
}
```

---

#### `expensesBackend/Domain/DTOs/MemberDTOs.cs`

```csharp
namespace ExpensesBackend.API.Domain.DTOs;

public class PagePermissionsDto
{
    public string? Dashboard { get; set; }
    public string? Expenses { get; set; }
    public string? Budgets { get; set; }
    public string? Settings { get; set; }
    public string? Insights { get; set; }
}

/// <summary>Fully resolved permissions for a user on a book — ready for UI/enforcement use.</summary>
public class ResolvedPermissions
{
    public string Role { get; set; } = "none";
    public string Dashboard { get; set; } = "none";
    public string Expenses { get; set; } = "none";
    public string Budgets { get; set; } = "none";
    public string Settings { get; set; } = "none";
    public string Insights { get; set; } = "none";
    public bool CanDeleteExpenses { get; set; } = false;
    public bool CanManageMembers { get; set; } = false;
    public bool CanModifyBook { get; set; } = false;
    public bool IsOwner { get; set; } = false;
    public List<string> AllowedCategoryIds { get; set; } = new();
}

public class ExpenseBookMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string ExpenseBookId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? InvitedEmail { get; set; }
    public string? UserName { get; set; }
    public string InviteStatus { get; set; } = "pending";
    public string Role { get; set; } = "viewer";
    public PagePermissionsDto? Permissions { get; set; }
    public List<string> AllowedCategoryIds { get; set; } = new();
    public bool CanDeleteExpenses { get; set; }
    public DateTime AddedAt { get; set; }
}

public class InviteMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "viewer";
    public PagePermissionsDto? Permissions { get; set; }
    public List<string> AllowedCategoryIds { get; set; } = new();
    public bool CanDeleteExpenses { get; set; } = false;
}

public class InviteMemberResponse
{
    public ExpenseBookMemberDto Member { get; set; } = null!;
    public string InviteLink { get; set; } = string.Empty;
}

public class UpdateMemberPermissionsRequest
{
    public string? Role { get; set; }
    public PagePermissionsDto? Permissions { get; set; }
    public List<string>? AllowedCategoryIds { get; set; }
    public bool? CanDeleteExpenses { get; set; }
}

public class AcceptInviteResponse
{
    public string ExpenseBookId { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```

---

#### `expensesBackend/Services/Interfaces/IMemberService.cs`

```csharp
using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IMemberService
{
    Task<List<ExpenseBookMemberDto>> GetMembersAsync(string bookId, string requestingUserId);
    Task<InviteMemberResponse> InviteMemberAsync(string bookId, string inviterUserId, InviteMemberRequest request, string baseUrl);
    Task<ExpenseBookMemberDto> UpdateMemberAsync(string bookId, string memberId, string requestingUserId, UpdateMemberPermissionsRequest request);
    Task RemoveMemberAsync(string bookId, string memberId, string requestingUserId);
    Task<AcceptInviteResponse> AcceptInviteAsync(string token, string userId);
    Task<ResolvedPermissions> GetResolvedPermissionsAsync(string bookId, string userId);
    Task EnsureMemberAccessAsync(string bookId, string userId);
    Task<bool> IsMemberAsync(string bookId, string userId);
    Task InvalidatePermissionsCacheAsync(string bookId, string userId);
}
```

---

#### `expensesBackend/Services/MemberService.cs`

```csharp
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
    private static readonly TimeSpan PermissionsTtl = TimeSpan.FromMinutes(5);

    public MemberService(MongoDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    // ─── Permission Resolution ────────────────────────────────────────────────

    public async Task<ResolvedPermissions> GetResolvedPermissionsAsync(string bookId, string userId)
    {
        var key = CacheKeys.MemberPermissions(bookId, userId);
        return await _cache.GetOrSetAsync(key, () => ResolveFromDbAsync(bookId, userId), PermissionsTtl)
               ?? new ResolvedPermissions { Role = "none" };
    }

    private async Task<ResolvedPermissions> ResolveFromDbAsync(string bookId, string userId)
    {
        // Owner check first (authoritative)
        var isOwner = await _context.ExpenseBooks
            .Find(b => b.Id == bookId && b.UserId == userId)
            .AnyAsync();

        if (isOwner)
            return RoleDefaults("owner");

        // Member check
        var member = await _context.ExpenseBookMembers
            .Find(m => m.ExpenseBookId == bookId && m.UserId == userId
                    && m.InviteStatus == "accepted" && !m.IsDeleted)
            .FirstOrDefaultAsync();

        return member == null ? new ResolvedPermissions { Role = "none" } : ResolveFromMember(member);
    }

    private static ResolvedPermissions ResolveFromMember(ExpenseBookMember m)
    {
        var r = RoleDefaults(m.Role);

        if (m.Permissions != null)
        {
            // Custom permissions = limited admin, loses modify/manage rights
            r.Dashboard = m.Permissions.Dashboard ?? r.Dashboard;
            r.Expenses  = m.Permissions.Expenses  ?? r.Expenses;
            r.Budgets   = m.Permissions.Budgets   ?? r.Budgets;
            r.Settings  = m.Permissions.Settings  ?? r.Settings;
            r.Insights  = m.Permissions.Insights  ?? r.Insights;
            r.CanModifyBook    = false;
            r.CanManageMembers = false;
        }

        r.CanDeleteExpenses  = m.CanDeleteExpenses;
        r.AllowedCategoryIds = m.AllowedCategoryIds;
        return r;
    }

    private static ResolvedPermissions RoleDefaults(string role) => role switch
    {
        "owner"  => new() { Role="owner",  Dashboard="view", Expenses="write", Budgets="write", Settings="write", Insights="view", CanDeleteExpenses=true,  CanManageMembers=true,  CanModifyBook=true,  IsOwner=true  },
        "admin"  => new() { Role="admin",  Dashboard="view", Expenses="write", Budgets="write", Settings="view",  Insights="view", CanDeleteExpenses=true,  CanManageMembers=true,  CanModifyBook=true,  IsOwner=false },
        "member" => new() { Role="member", Dashboard="view", Expenses="write", Budgets="none",  Settings="none",  Insights="view", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false },
        "viewer" => new() { Role="viewer", Dashboard="view", Expenses="view",  Budgets="none",  Settings="none",  Insights="view", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false },
        _        => new() { Role="none",   Dashboard="none", Expenses="none",  Budgets="none",  Settings="none",  Insights="none", CanDeleteExpenses=false, CanManageMembers=false, CanModifyBook=false, IsOwner=false }
    };

    public async Task EnsureMemberAccessAsync(string bookId, string userId)
    {
        var p = await GetResolvedPermissionsAsync(bookId, userId);
        if (p.Role == "none")
            throw new UnauthorizedAccessException("You do not have access to this expense book");
    }

    public async Task<bool> IsMemberAsync(string bookId, string userId)
    {
        var p = await GetResolvedPermissionsAsync(bookId, userId);
        return p.Role != "none";
    }

    public async Task InvalidatePermissionsCacheAsync(string bookId, string userId)
        => await _cache.RemoveAsync(CacheKeys.MemberPermissions(bookId, userId));

    // ─── Member CRUD ──────────────────────────────────────────────────────────

    public async Task<List<ExpenseBookMemberDto>> GetMembersAsync(string bookId, string requestingUserId)
    {
        await EnsureMemberAccessAsync(bookId, requestingUserId);

        var members = await _context.ExpenseBookMembers
            .Find(m => m.ExpenseBookId == bookId && !m.IsDeleted)
            .ToListAsync();

        // Enrich with user info
        var userIds = members.Where(m => m.UserId != null).Select(m => m.UserId!).Distinct().ToList();
        var userMap = userIds.Any()
            ? (await _context.Users.Find(u => userIds.Contains(u.Id)).ToListAsync()).ToDictionary(u => u.Id)
            : new Dictionary<string, User>();

        // Add synthetic owner record if not already in member collection
        var book = await _context.ExpenseBooks.Find(b => b.Id == bookId).FirstOrDefaultAsync();
        var dtos = new List<ExpenseBookMemberDto>();

        if (book != null && members.All(m => m.UserId != book.UserId || m.Role != "owner"))
        {
            userMap.TryGetValue(book.UserId, out var ownerUser);
            dtos.Add(new ExpenseBookMemberDto
            {
                Id            = $"owner-{book.UserId}",
                ExpenseBookId = bookId,
                UserId        = book.UserId,
                InvitedEmail  = ownerUser?.Email,
                UserName      = ownerUser?.Name,
                InviteStatus  = "accepted",
                Role          = "owner",
                CanDeleteExpenses = true,
                AddedAt       = book.CreatedAt
            });
        }

        foreach (var m in members)
        {
            userMap.TryGetValue(m.UserId ?? "", out var u);
            dtos.Add(MapToDto(m, u));
        }

        return dtos;
    }

    public async Task<InviteMemberResponse> InviteMemberAsync(
        string bookId, string inviterUserId, InviteMemberRequest request, string baseUrl)
    {
        var perms = await GetResolvedPermissionsAsync(bookId, inviterUserId);
        if (!perms.CanManageMembers)
            throw new UnauthorizedAccessException("You do not have permission to manage members");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required");

        if (!IsValidNonOwnerRole(request.Role))
            throw new ArgumentException("Invalid role. Valid: admin, member, viewer");

        var email = request.Email.Trim().ToLowerInvariant();

        // Check for duplicate active membership
        var existingUser = await _context.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
        if (existingUser != null)
        {
            var alreadyMember = await _context.ExpenseBookMembers
                .Find(m => m.ExpenseBookId == bookId && m.UserId == existingUser.Id
                        && m.InviteStatus == "accepted" && !m.IsDeleted)
                .AnyAsync();
            if (alreadyMember)
                throw new ArgumentException("This user is already a member");

            // Check if they are the owner
            var isOwner = await _context.ExpenseBooks
                .Find(b => b.Id == bookId && b.UserId == existingUser.Id)
                .AnyAsync();
            if (isOwner)
                throw new ArgumentException("This user is the owner of this expense book");
        }

        // Check for duplicate pending invite
        var hasPending = await _context.ExpenseBookMembers
            .Find(m => m.ExpenseBookId == bookId && m.InvitedEmail == email
                    && m.InviteStatus == "pending" && !m.IsDeleted)
            .AnyAsync();
        if (hasPending)
            throw new ArgumentException("A pending invite already exists for this email");

        var token = GenerateToken();
        var member = new ExpenseBookMember
        {
            ExpenseBookId = bookId,
            UserId        = existingUser?.Id,
            InvitedEmail  = email,
            InviteToken   = token,
            InviteStatus  = "pending",
            Role          = request.Role,
            Permissions   = request.Permissions == null ? null : new PagePermissions
            {
                Dashboard = request.Permissions.Dashboard,
                Expenses  = request.Permissions.Expenses,
                Budgets   = request.Permissions.Budgets,
                Settings  = request.Permissions.Settings,
                Insights  = request.Permissions.Insights
            },
            AllowedCategoryIds = request.AllowedCategoryIds ?? new(),
            CanDeleteExpenses  = request.CanDeleteExpenses,
            AddedBy            = inviterUserId,
            AddedAt            = DateTime.UtcNow,
            UpdatedAt          = DateTime.UtcNow
        };

        await _context.ExpenseBookMembers.InsertOneAsync(member);

        var inviteLink = $"{baseUrl.TrimEnd('/')}/accept-invite?token={token}";
        return new InviteMemberResponse { Member = MapToDto(member, existingUser), InviteLink = inviteLink };
    }

    public async Task<ExpenseBookMemberDto> UpdateMemberAsync(
        string bookId, string memberId, string requestingUserId, UpdateMemberPermissionsRequest request)
    {
        var perms = await GetResolvedPermissionsAsync(bookId, requestingUserId);
        if (!perms.CanManageMembers)
            throw new UnauthorizedAccessException("You do not have permission to manage members");

        var member = await _context.ExpenseBookMembers
            .Find(m => m.Id == memberId && m.ExpenseBookId == bookId && !m.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Member not found");

        if (member.Role == "owner")
            throw new InvalidOperationException("Cannot modify owner permissions");

        var updates = new List<UpdateDefinition<ExpenseBookMember>>();

        if (request.Role != null && IsValidNonOwnerRole(request.Role))
            updates.Add(Builders<ExpenseBookMember>.Update.Set(m => m.Role, request.Role));

        if (request.Permissions != null)
        {
            var p = new PagePermissions
            {
                Dashboard = request.Permissions.Dashboard,
                Expenses  = request.Permissions.Expenses,
                Budgets   = request.Permissions.Budgets,
                Settings  = request.Permissions.Settings,
                Insights  = request.Permissions.Insights
            };
            updates.Add(Builders<ExpenseBookMember>.Update.Set(m => m.Permissions, p));
        }

        if (request.AllowedCategoryIds != null)
            updates.Add(Builders<ExpenseBookMember>.Update.Set(m => m.AllowedCategoryIds, request.AllowedCategoryIds));

        if (request.CanDeleteExpenses.HasValue)
            updates.Add(Builders<ExpenseBookMember>.Update.Set(m => m.CanDeleteExpenses, request.CanDeleteExpenses.Value));

        updates.Add(Builders<ExpenseBookMember>.Update.Set(m => m.UpdatedAt, DateTime.UtcNow));

        await _context.ExpenseBookMembers.UpdateOneAsync(
            m => m.Id == memberId, Builders<ExpenseBookMember>.Update.Combine(updates));

        if (member.UserId != null)
            await _cache.RemoveAsync(CacheKeys.MemberPermissions(bookId, member.UserId));

        var updated = await _context.ExpenseBookMembers.Find(m => m.Id == memberId).FirstOrDefaultAsync();
        User? user = member.UserId != null
            ? await _context.Users.Find(u => u.Id == member.UserId).FirstOrDefaultAsync()
            : null;
        return MapToDto(updated!, user);
    }

    public async Task RemoveMemberAsync(string bookId, string memberId, string requestingUserId)
    {
        var perms = await GetResolvedPermissionsAsync(bookId, requestingUserId);
        if (!perms.CanManageMembers)
            throw new UnauthorizedAccessException("You do not have permission to manage members");

        var member = await _context.ExpenseBookMembers
            .Find(m => m.Id == memberId && m.ExpenseBookId == bookId && !m.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Member not found");

        if (member.Role == "owner")
            throw new InvalidOperationException("Cannot remove the owner");

        // Soft-delete if user is a member of another book; hard-delete otherwise
        bool softDelete = false;
        if (member.UserId != null)
        {
            softDelete = await _context.ExpenseBookMembers
                .Find(m => m.UserId == member.UserId && m.ExpenseBookId != bookId
                        && m.InviteStatus == "accepted" && !m.IsDeleted)
                .AnyAsync();
        }

        if (softDelete)
        {
            var upd = Builders<ExpenseBookMember>.Update
                .Set(m => m.InviteStatus, "revoked")
                .Set(m => m.IsDeleted, true)
                .Set(m => m.DeletedAt, DateTime.UtcNow);
            await _context.ExpenseBookMembers.UpdateOneAsync(m => m.Id == memberId, upd);
        }
        else
        {
            await _context.ExpenseBookMembers.DeleteOneAsync(m => m.Id == memberId);
        }

        if (member.UserId != null)
            await _cache.RemoveAsync(CacheKeys.MemberPermissions(bookId, member.UserId));
    }

    public async Task<AcceptInviteResponse> AcceptInviteAsync(string token, string userId)
    {
        var member = await _context.ExpenseBookMembers
            .Find(m => m.InviteToken == token && m.InviteStatus == "pending" && !m.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Invite not found or already used");

        var book = await _context.ExpenseBooks
            .Find(b => b.Id == member.ExpenseBookId)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Expense book no longer exists");

        var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync()
            ?? throw new UnauthorizedAccessException("User not found");

        if (!string.IsNullOrEmpty(member.InvitedEmail) &&
            !string.Equals(user.Email, member.InvitedEmail, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("This invite was sent to a different email address");

        var upd = Builders<ExpenseBookMember>.Update
            .Set(m => m.UserId, userId)
            .Set(m => m.InviteStatus, "accepted")
            .Set(m => m.InviteToken, null)   // consume token
            .Set(m => m.UpdatedAt, DateTime.UtcNow);

        await _context.ExpenseBookMembers.UpdateOneAsync(m => m.Id == member.Id, upd);

        return new AcceptInviteResponse
        {
            ExpenseBookId = book.Id,
            BookName      = book.Name,
            Role          = member.Role
        };
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string GenerateToken() =>
        Convert
```

