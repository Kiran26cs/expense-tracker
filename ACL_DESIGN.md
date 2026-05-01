# Expense Tracker — User Access Control (ACL) Design Reference

> **Status**: Design approved, implementation pending  
> **Last updated**: April 2026  
> **Purpose**: Authoritative reference for the multi-user ACL system. Update this file whenever design decisions change.

---

## Table of Contents

1. [Overview & Goals](#1-overview--goals)
2. [Roles](#2-roles)
3. [Permission Model](#3-permission-model)
4. [Data Model](#4-data-model)
5. [Permission Resolution Algorithm](#5-permission-resolution-algorithm)
6. [Category Restrictions](#6-category-restrictions)
7. [Invite Flow](#7-invite-flow)
8. [User Lifecycle](#8-user-lifecycle)
9. [Expense Book Access Rules](#9-expense-book-access-rules)
10. [API Enforcement Points](#10-api-enforcement-points)
11. [Frontend Enforcement Points](#11-frontend-enforcement-points)
12. [Caching Strategy](#12-caching-strategy)
13. [MongoDB Collections & Indexes](#13-mongodb-collections--indexes)
14. [Design Decisions Log](#14-design-decisions-log)

---

## 1. Overview & Goals

The ACL system allows an expense book to be shared with multiple users, each with fine-grained control over what they can see and do.

### Key Principles

- **One owner per book** — the user who created the book. Cannot be changed.
- **Multiple admins allowed** — full admin = same rights as owner except cannot delete the book.
- **Page-level permissions** — each page (Dashboard, Expenses, Budgets, Settings, Insights) can be individually set to `view`, `write`, or `none`.
- **Category-level data restrictions** — a user can be restricted to only see data belonging to specific categories.
- **Category restrictions cascade** — if a user can only see category X, they also can't see budgets for any other category.
- **Inherited settings** — all users on a book share the book's currency and settings. No per-user settings overrides.
- **Multi-book membership** — a user can be a member of multiple expense books with different roles in each.
- **Invite-only join** — users are added by the owner/admin. No self-service join.
- **OTP/Google auth unchanged** — existing authentication flow is not modified. Invite tokens only link identity to book membership.

---

## 2. Roles

Four roles with preset defaults. Permissions can be further customized per member.

| Role | Description |
|---|---|
| `owner` | Created the book. Unrestricted. Cannot be changed or removed. |
| `admin` | Full admin — can do everything except: cannot delete the book, cannot promote others to owner. Can manage members, modify book name/icon, access all pages. |
| `member` | Standard contributor — can add/edit expenses, view dashboard and insights. No access to budgets or settings. Cannot delete expenses by default. |
| `viewer` | Read-only access to expenses, dashboard, insights. Cannot write anything. |

### Role Default Permissions

| Role | dashboard | expenses | budgets | settings | insights | canDelete | canManageMembers | canModifyBook |
|---|---|---|---|---|---|---|---|---|
| `owner` | view | write | write | write | view | ✅ | ✅ | ✅ |
| `admin` | view | write | write | view | view | ✅ | ✅ | ✅ |
| `member` | view | write | none | none | view | ❌ | ❌ | ❌ |
| `viewer` | view | view | none | none | view | ❌ | ❌ | ❌ |

> **Note**: `canModifyBook` = can rename, change icon/color of the expense book.  
> `canManageMembers` = can invite, edit, and remove other members (non-owners).  
> `canDeleteExpenses` = per-member flag independent of role; can be overridden separately.

---

## 3. Permission Model

### Three Layers

```
MemberAccess
├── Layer 1: Role  (coarse defaults — determines base permissions)
│   └── owner | admin | member | viewer
│
├── Layer 2: PagePermissions (optional fine-grained overrides)
│   ├── dashboard:  "view" | "none"
│   ├── expenses:   "view" | "write" | "none"
│   ├── budgets:    "view" | "write" | "none"
│   ├── settings:   "view" | "write" | "none"
│   └── insights:   "view" | "none"
│
└── Layer 3: CategoryRestrictions (data-level, scoped to all data access)
    └── allowedCategoryIds: string[]   // empty = no restriction (all categories)
```

### Page Permission Values

| Value | Effect |
|---|---|
| `"none"` | Page is hidden in sidebar. API returns 403 if accessed directly. |
| `"view"` | Page is visible. Data is readable. Write operations return 403. |
| `"write"` | Full read + write access on this page. |
| `null` | Not set — fall through to role default. |

### Custom Permissions Side Effects

When `PagePermissions` is explicitly set on a member (non-null), that member automatically **loses** `canModifyBook` and `canManageMembers` — even if their role is `admin`. This prevents a partially-restricted admin from managing other members.

Exception: an admin with `null` PagePermissions (unmodified role defaults) retains full admin rights including member management and book modification.

---

## 4. Data Model

### `expenseBookMembers` Collection

```json
{
  "_id": "ObjectId → string",
  "expenseBookId": "string",
  "userId": "string | null",
  "invitedEmail": "string | null",
  "inviteToken": "string | null",
  "inviteStatus": "pending | accepted | revoked",
  "role": "owner | admin | member | viewer",
  "permissions": {
    "dashboard": "view | none | null",
    "expenses": "view | write | none | null",
    "budgets": "view | write | none | null",
    "settings": "view | write | none | null",
    "insights": "view | none | null"
  },
  "allowedCategoryIds": ["categoryId1", "categoryId2"],
  "canDeleteExpenses": false,
  "addedBy": "string (userId of the inviter)",
  "addedAt": "DateTime",
  "updatedAt": "DateTime",
  "isDeleted": false,
  "deletedAt": "DateTime | null"
}
```

#### Field Notes

| Field | Notes |
|---|---|
| `userId` | `null` until invite is accepted. Matched to user by email on acceptance. |
| `invitedEmail` | The email the invite was sent to. Must match the logged-in user's email when accepting. |
| `inviteToken` | Base64url-encoded 32-byte random token. Set to `null` after acceptance (one-time use). |
| `inviteStatus` | `pending` = invite created, not yet accepted. `accepted` = active member. `revoked` = removed. |
| `permissions` | `null` means use role defaults entirely. Individual fields may also be null (use role default for that field). |
| `allowedCategoryIds` | Empty array = no restriction. Non-empty = user only sees data for those category IDs. |
| `canDeleteExpenses` | Explicit per-member flag. Not derived from role. Default `false` for new members (except owner). |
| `isDeleted` | Soft-delete flag. Used when removed user is a member of other books — record is kept for cross-book integrity. |

### `ResolvedPermissions` (computed — not stored)

This is the object computed at runtime and cached in Redis for each `(bookId, userId)` pair.

```json
{
  "role": "owner | admin | member | viewer | none",
  "dashboard": "view | none",
  "expenses": "view | write | none",
  "budgets": "view | write | none",
  "settings": "view | write | none",
  "insights": "view | none",
  "canDeleteExpenses": true,
  "canManageMembers": true,
  "canModifyBook": true,
  "isOwner": true,
  "allowedCategoryIds": []
}
```

`role: "none"` means the user has no access to this book at all.

---

## 5. Permission Resolution Algorithm

```
FUNCTION ResolvePermissions(bookId, userId):

  1. Check if userId == expenseBook.UserId (owner)
     → YES: return RoleDefaults("owner")

  2. Query expenseBookMembers WHERE:
        expenseBookId = bookId
        AND userId = userId
        AND inviteStatus = "accepted"
        AND isDeleted = false

  3. If no member record found:
     → return { role: "none", all: "none" }

  4. Start with RoleDefaults(member.role)

  5. If member.permissions IS NOT NULL:
       For each page field (dashboard, expenses, budgets, settings, insights):
         If permissions[page] is not null → override resolved value
       Set canModifyBook = false
       Set canManageMembers = false

  6. Set canDeleteExpenses = member.canDeleteExpenses  (explicit, never derived)

  7. Set allowedCategoryIds = member.allowedCategoryIds

  8. Cache result in Redis with 5-minute TTL

  9. Return resolved permissions
```

### Role Defaults Function

```
FUNCTION RoleDefaults(role):
  owner  → { dashboard:view, expenses:write, budgets:write, settings:write, insights:view, canDelete:true,  canManage:true,  canModify:true,  isOwner:true  }
  admin  → { dashboard:view, expenses:write, budgets:write, settings:view,  insights:view, canDelete:true,  canManage:true,  canModify:true,  isOwner:false }
  member → { dashboard:view, expenses:write, budgets:none,  settings:none,  insights:view, canDelete:false, canManage:false, canModify:false, isOwner:false }
  viewer → { dashboard:view, expenses:view,  budgets:none,  settings:none,  insights:view, canDelete:false, canManage:false, canModify:false, isOwner:false }
  none   → { all: none,      canDelete:false, canManage:false, canModify:false, isOwner:false }
```

---

## 6. Category Restrictions

### What Is Filtered

When `allowedCategoryIds` is non-empty, the restriction applies to **all data access** for that user:

| Data Type | Filter Applied |
|---|---|
| **Expenses** | Only expenses where `category IN allowedCategoryIds` are returned |
| **Budgets** | Only budgets where `category IN allowedCategoryIds` are returned |
| **Dashboard** | Summary totals are computed using only the allowed categories |
| **Insights** | Charts and analytics only include data from allowed categories |
| **Category picker** (UI) | Dropdown only shows allowed categories when adding/editing expenses |

### What Is NOT Filtered

- Settings — settings are inherited from the book and not category-specific
- The `allowedCategoryIds` list itself — members can always see their own restrictions

### Category IDs vs Names

Restrictions are stored as **category document IDs** (`_id` from the `categories` collection), not names. This ensures renames don't break restrictions.

---

## 7. Invite Flow

### Step-by-step

```
1. Owner/Admin opens Members page → fills in email + role + optional permissions
   ↓
2. Backend creates expenseBookMember record:
      inviteStatus = "pending"
      userId = (user's ID if email already registered, else null)
      inviteToken = <32-byte base64url random string>
   ↓
3. Backend returns: { member, inviteLink }
      inviteLink = "{baseUrl}/accept-invite?token={token}"
   ↓
4. Owner/Admin copies the link from the UI and shares it manually
   (email delivery is deferred — no email service integrated yet)
   ↓
5. Invited person opens the link
   ↓
   ├── NOT logged in → redirect to login page
   │     Login page checks sessionStorage for "pendingInviteToken"
   │     After successful login/signup → redirects back to /accept-invite?token=...
   │
   └── Already logged in → proceed to accept
   ↓
6. POST /api/members/accept?token={token}
      Server validates:
        - Token exists, status is "pending", not deleted
        - If invitedEmail is set: logged-in user's email must match
      On success:
        - Sets userId = currentUser.Id
        - Sets inviteStatus = "accepted"
        - Sets inviteToken = null  (consumed, cannot be reused)
   ↓
7. User sees success screen with book name + role
   → "Go to Expense Book" navigates to /{bookId}/dashboard
```

### Invite Token Security

- Tokens are 32-byte cryptographically random values, base64url-encoded (no `+`, `/`, `=`)
- One-time use: token is nulled immediately on acceptance
- No expiry currently (can be added: store `inviteExpiresAt` and check on accept)
- Email validation on accept: prevents token theft across accounts

---

## 8. User Lifecycle

### Membership Across Multiple Books

A user can be a member of multiple expense books with entirely different roles in each. Each book has its own `expenseBookMember` record.

```
User A:
  Book 1 (Personal) → owner
  Book 2 (Work)     → admin
  Book 3 (Family)   → viewer, allowedCategoryIds: [catId_food, catId_bills]
```

### User Deletion Rules

When a member is removed from a book:

```
IF user is a member of ANY OTHER book (accepted, not deleted):
  → Soft delete: set isDeleted=true, inviteStatus="revoked", deletedAt=now
  (record preserved for cross-book membership integrity)
ELSE:
  → Hard delete: remove the record entirely
```

The owner record in `expenseBooks.UserId` is never deleted via the member system. Deleting an expense book is a separate flow.

### Owner Cannot Be Removed

The owner is identified by `expenseBook.UserId`. There is no `expenseBookMember` record for the owner. The owner cannot be removed from their own book through the members API.

---

## 9. Expense Book Access Rules

### Who Can Do What

| Action | Owner | Admin (full) | Admin (custom perms) | Member | Viewer |
|---|---|---|---|---|---|
| View dashboard | ✅ | ✅ | depends | ✅ | ✅ |
| View expenses | ✅ | ✅ | depends | ✅ | ✅ |
| Add/edit expenses | ✅ | ✅ | depends | ✅ | ❌ |
| Delete expenses | ✅ | ✅ | ❌ | ❌ | ❌ |
| View budgets | ✅ | ✅ | depends | ❌ | ❌ |
| Manage budgets | ✅ | ✅ | depends | ❌ | ❌ |
| View settings | ✅ | ✅ (read-only) | depends | ❌ | ❌ |
| Edit settings | ✅ | ❌ | ❌ | ❌ | ❌ |
| View insights | ✅ | ✅ | depends | ✅ | ✅ |
| Rename/re-icon book | ✅ | ✅ | ❌ | ❌ | ❌ |
| Delete book | ✅ | ❌ | ❌ | ❌ | ❌ |
| Invite members | ✅ | ✅ | ❌ | ❌ | ❌ |
| Edit member permissions | ✅ | ✅ | ❌ | ❌ | ❌ |
| Remove members | ✅ | ✅ | ❌ | ❌ | ❌ |
| View members list | ✅ | ✅ | ✅ | ✅ | ✅ |

> **Admin (full)** = admin with no custom `PagePermissions` set (null).  
> **Admin (custom perms)** = admin with explicit `PagePermissions` set — loses manage/modify rights.

### Expense Book Listing

`GET /api/expensebooks` returns:
- All books where `expenseBook.UserId == requestingUserId` (owned)
- All books where an accepted, non-deleted `expenseBookMember` record exists for this user

---

## 10. API Enforcement Points

### `GET /api/Expenses`
1. Resolve permissions for the book
2. If `expenses == "none"` → 403
3. If `allowedCategoryIds` is non-empty → add `category IN [...]` filter to query

### `POST /api/Expenses`, `PUT /api/Expenses/{id}`
1. Resolve permissions
2. If `expenses != "write"` → 403
3. If `allowedCategoryIds` is non-empty → validate that the expense's category is in allowed list

### `DELETE /api/Expenses/{id}`
1. Resolve permissions
2. If `!canDeleteExpenses` → 403

### `GET /api/Budgets`
1. Resolve permissions
2. If `budgets == "none"` → 403
3. If `allowedCategoryIds` is non-empty → filter budgets by allowed categories

### `POST /api/Budgets/upsert-version`, `DELETE /api/Budgets/{id}`
1. Resolve permissions
2. If `budgets != "write"` → 403

### `GET /api/settings`
1. Resolve permissions
2. If `settings == "none"` → 403 (returns inherited currency from book, not user-settable)

### `PUT /api/settings`
1. Resolve permissions
2. If `settings != "write"` → 403

### `PUT /api/expensebooks/{id}` (rename/modify book)
1. Resolve permissions
2. If `!canModifyBook` → 403

### `DELETE /api/expensebooks/{id}` (delete book)
1. Check `expenseBook.UserId == requestingUserId` (owner only)
2. If not owner → 403

### `GET /api/expensebooks/{id}/members`
1. Any accepted member can list members (visibility: all roles)

### `POST /api/expensebooks/{id}/members/invite`
1. Resolve permissions
2. If `!canManageMembers` → 403

### `PUT /api/expensebooks/{id}/members/{memberId}`
1. Resolve permissions
2. If `!canManageMembers` → 403
3. Cannot modify owner member record

### `DELETE /api/expensebooks/{id}/members/{memberId}`
1. Resolve permissions
2. If `!canManageMembers` → 403
3. Cannot remove owner

### `POST /api/members/accept?token=...`
1. Must be authenticated
2. Token must exist, status = "pending", not deleted
3. If `invitedEmail` set on record → must match logged-in user's email

---

## 11. Frontend Enforcement Points

> Frontend enforcement is **convenience only** — it never replaces backend checks. All write operations are also enforced server-side.

### `BookAccessService`

- Fetches `GET /api/expensebooks/{bookId}/members/me` on book navigation
- Caches resolved permissions in an Angular Signal
- All components inject this service to check permissions reactively

### Sidebar (`SidebarComponent`)

- Nav items are computed from `permissions()` signal
- Pages with permission `"none"` are removed from the nav
- `members` nav item shown only when `canManageMembers = true`

| Sidebar Item | Show condition |
|---|---|
| Dashboard | `dashboard !== "none"` |
| Expenses | `expenses !== "none"` |
| Budget | `budgets !== "none"` |
| Insights | `insights !== "none"` |
| Settings | `settings !== "none"` |
| Members | `canManageMembers === true` |

### Expense List Page

| Element | Condition |
|---|---|
| `+ Add Transaction` button | `expenses === "write"` |
| `Edit` button per row | `expenses === "write"` |
| `Delete` button per row | `canDeleteExpenses === true` |
| Category filter dropdown | Filtered to `allowedCategoryIds` when non-empty |

### Add/Edit Expense Page

- If `expenses !== "write"` → redirect away (guard)
- Category field options filtered to `allowedCategoryIds` if restricted

### Budget Page

- If `budgets === "none"` → redirect away (guard)
- If `budgets === "view"` → all write controls hidden

### Settings Page

- If `settings === "none"` → redirect away (guard)
- If `settings === "view"` → save button hidden, form disabled

### Route Guard

A `bookAccessGuard` is applied to each child route inside `/:bookId/`:

```typescript
// Pseudocode
canActivate(route) {
  const page = route.data['page'];          // e.g. 'expenses'
  const level = route.data['minLevel'];     // e.g. 'view'
  const perms = bookAccessService.permissions();
  return perms[page] >= level;              // 'write' > 'view' > 'none'
}
```

---

## 12. Caching Strategy

### Redis Key: `member-perms:{bookId}:{userId}`

| Property | Value |
|---|---|
| TTL | 5 minutes |
| Invalidated when | Member permissions updated, member removed, invite accepted |
| On miss | Query DB and recompute |
| Failure behavior | On Redis error → query DB directly (fail-open, not fail-closed) |

### Cache Invalidation Events

| Event | Keys Invalidated |
|---|---|
| `InviteMember` (accepted immediately) | `member-perms:{bookId}:{userId}` |
| `UpdateMember` | `member-perms:{bookId}:{member.userId}` |
| `RemoveMember` | `member-perms:{bookId}:{member.userId}` |
| `AcceptInvite` | `member-perms:{bookId}:{userId}` |
| Owner book delete | All `member-perms:{bookId}:*` (pattern delete or TTL expiry) |

---

## 13. MongoDB Collections & Indexes

### `expenseBookMembers`

```
Index 1: { expenseBookId: 1, userId: 1 }
  Purpose: Get member record for a specific (book, user) pair — primary permission lookup
  Unique: No (user may have revoked + new record)

Index 2: { userId: 1 }
  Purpose: Get all books a user is a member of (for book listing enrichment)

Index 3: { inviteToken: 1 }
  Purpose: Fast invite acceptance lookup
  Sparse: true (most records have null token after acceptance)

Index 4: { expenseBookId: 1, invitedEmail: 1 } (partial — pending only)
  Purpose: Enforce email uniqueness per book for pending invites
  Unique: true (partial filter: inviteStatus == "pending")
  Prevents race-condition duplicate invites for the same email
```

### MongoDB Shell Commands

Run these commands once against your database (e.g. via MongoDB Compass Shell or `mongosh`).
Replace `ExpenseTrackerDB` with your actual database name if different.

```js
use ExpenseTrackerDB

// ── Index 1: (bookId, userId) — permission resolution lookup ──────────────
db.expenseBookMembers.createIndex(
  { expenseBookId: 1, userId: 1 },
  { name: "idx_member_book_user", background: true }
)

// ── Index 2: userId — get all books a user is a member of ─────────────────
db.expenseBookMembers.createIndex(
  { userId: 1 },
  { name: "idx_member_user", background: true }
)

// ── Index 3: inviteToken — fast invite acceptance lookup (sparse) ──────────
db.expenseBookMembers.createIndex(
  { inviteToken: 1 },
  { name: "idx_member_token", sparse: true, unique: true, background: true }
)

// ── Index 4: (bookId, invitedEmail) partial — prevent duplicate pending invites
// This makes invitedEmail unique per book only when inviteStatus = "pending"
db.expenseBookMembers.createIndex(
  { expenseBookId: 1, invitedEmail: 1 },
  {
    name: "idx_member_book_email_pending",
    unique: true,
    partialFilterExpression: { inviteStatus: "pending" },
    background: true
  }
)
```

> **Note**: Index 4 enforces email uniqueness at the DB level for pending invites, eliminating the race-condition gap between the two application-level duplicate checks.

### Modified `expenseBooks` (no schema changes — `UserId` remains the owner field)

No changes to the `expenseBooks` collection schema. The owner is always `expenseBook.UserId`. A future migration could add `ownerId` as an alias, but `UserId` remains authoritative.

---

## 14. Design Decisions Log

This section records key decisions made during design so future changes have context.

| Decision | Rationale | Alternative Considered |
|---|---|---|
| Owner not stored in `expenseBookMembers` | Avoids dual source of truth. Owner is always `expenseBook.UserId`. | Store owner as a member record → rejected: complicates ownership transfer |
| 5-minute permission cache TTL | Short enough that permission changes take effect quickly. User can refresh to force re-check. | 30 min (too long — confusing UX when admin revokes access) |
| Soft delete when user has other book memberships | Preserves cross-book membership data integrity | Hard delete always → rejected: would corrupt member list in other books |
| Category restrictions by ID not name | Renames don't break restrictions | By name → rejected: brittle |
| Custom permissions nullify manager/modifier rights | Prevents a partially-restricted "admin" from managing other members | Allow per-flag override → rejected: too complex, security risk |
| Invite via link only (no email sending) | Email integration deferred. Owner copies and shares link manually. | Email sending → deferred to Phase 4 |
| Settings are inherited (no per-user settings) | Simpler UX; all members on a book use the same currency | Per-user currency override → rejected: complicates all amount displays |
| `canDeleteExpenses` is an explicit per-member flag | Role alone doesn't determine delete — even a `member` could be granted delete rights | Derive from role only → rejected: not flexible enough per use case |
| Accept-invite validates email match | Security: prevents token theft — only the intended email can use the link | No email validation → rejected: security vulnerability |
| `inviteToken` set to null on acceptance | One-time use enforced at DB level (token field disappears) | Keep token, mark used → rejected: token could be replayed if mark update fails |
