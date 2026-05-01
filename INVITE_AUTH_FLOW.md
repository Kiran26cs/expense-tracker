# Invite & Auth Flow — Complete Reference

## Overview

The invite flow lets an expense book owner add members who can view or edit the same book. It handles both **new users** (sign up) and **existing users** (sign in) arriving via an invite link.

---

## 1. Generating an Invite Link (Owner)

**Frontend:** `members.component.ts` → calls `MemberService.inviteMember(bookId, { email, role })`

**Backend:** `POST /api/expensebooks/{bookId}/members/invite`

What happens:
1. ACL check — requesting user must have `canManageMembers = true`
2. Duplicate check — rejects if email already has an accepted membership or a pending pending invite
3. Generates a 32-byte cryptographically random URL-safe token: `RandomNumberGenerator.GetBytes(32)` → base64url
4. Inserts `ExpenseBookMember` document with:
   - `inviteStatus: "pending"`
   - `inviteToken: "<token>"`
   - `userId: null` (not known yet)
   - `invitedEmail: "<email>"`
5. Returns invite link: `{App:FrontendUrl}/accept-invite?token=<token>`

**Config:** `appsettings.json → "App": { "FrontendUrl": "http://localhost:4200" }`

---

## 2. Recipient Opens the Invite Link

URL: `http://localhost:4200/accept-invite?token=<token>`

**Route:** `accept-invite` — **no auth guard** (removed so unauthenticated users can land here)

**Component:** `accept-invite.component.ts → ngOnInit()`

```
Wait for auth state to settle (polls auth.isLoading() every 50ms)
│
├─ If authenticated → call AcceptInvite API immediately
│
└─ If NOT authenticated:
      Store token in sessionStorage('pendingInviteToken')
      Navigate to /signup?redirect=/accept-invite&token=<token>
```

---

## 3. New User — Signup Flow

**Component:** `signup.component.ts`

```
User enters name + email/phone
  ↓
POST /api/Auth/send-otp   → OTP sent (logged to console in dev)
  ↓
User enters 6-digit OTP
  ↓
POST /api/Auth/verify-otp  → marks OTP record as verified=true
  ↓
POST /api/Auth/signup?otp=<otp>
  ↓ (backend)
  IsOtpVerifiedAsync() — checks OTP record: exists, verified=true, not expired
  If user already exists → return login token (treat as login, not error)
  If new user → create User document → return JWT token
  ↓
Frontend stores token in localStorage('authToken')
Sets userSignal from response
  ↓
Checks sessionStorage('pendingInviteToken')
  ├─ Found → navigate to /accept-invite?token=<pendingToken>
  └─ Not found → navigate to /
```

---

## 4. Existing User — Login Flow

**Component:** `login.component.ts`

```
User enters email/phone
  ↓
POST /api/Auth/send-otp
  ↓
User enters 6-digit OTP
  ↓
POST /api/Auth/login?otp=<otp>
  ↓
Frontend stores JWT, sets user signal
  ↓
Checks sessionStorage('pendingInviteToken')
  ├─ Found → navigate to /accept-invite?token=<pendingToken>
  └─ Not found → navigate to /
```

---

## 5. Accepting the Invite (Authenticated)

**Component:** `accept-invite.component.ts → acceptInvite()`

**Backend:** `POST /api/members/accept?token=<token>`  (top-level route, no `bookId` prefix)

What happens:
1. Looks up `ExpenseBookMember` where `inviteToken == token AND inviteStatus == "pending" AND !isDeleted`
2. If not found → `KeyNotFoundException` → 404
3. Prevent double-join — checks if user already has an accepted membership for same book
4. Updates the member document:
   - `userId = <requestingUserId>`
   - `inviteStatus = "accepted"`
   - `$unset inviteToken` ← **removes the field entirely** (not set to null) so the sparse unique index doesn't conflict
   - `updatedAt = now`
5. Invalidates Redis permission cache for `(bookId, userId)`
6. Returns `{ expenseBookId, expenseBookName, member }`

**Frontend on success:**
- Sets result signal (shows success card with book name)
- Removes `pendingInviteToken` from sessionStorage
- User clicks "Open Expense Book" → navigates to `/:bookId/dashboard`

---

## 6. Post-Invite: Accessing the Book

After accept, the member appears in `ExpenseBookMembers` with `inviteStatus: "accepted"`.

**Book listing** (`GET /api/expensebooks`):
- Returns owned books UNION books where `userId` has accepted membership

**Data access** (Expenses, Budget, Dashboard):
- All queries filter by `expenseBookId` (not `userId`) when a book is specified
- ACL is checked at controller level using `MemberService.GetResolvedPermissionsAsync(bookId, userId)`
- Permissions are Redis-cached for 10 minutes per `(bookId, userId)` pair

**Profile** (`GET /api/Auth/me`):
- Returns the **requesting user's own** record from MongoDB — never the book owner's data

---

## 7. MongoDB Index Issue & Fix

The `ExpenseBookMembers` collection has a sparse unique index on `inviteToken`:

```js
db.expenseBookMembers.createIndex(
  { inviteToken: 1 },
  { unique: true, sparse: true }
)
```

**Problem that was occurring:**  
When accepting an invite, the code was doing `.Set(m => m.InviteToken, (string?)null)`. MongoDB stores this as an explicit `null` field. The sparse index *does* index explicit `null` values, causing a duplicate key error when a second invite was accepted.

**Fix applied:**  
Changed to `.Unset(m => m.InviteToken)` — this removes the field from the document entirely. The sparse index ignores documents where the field is absent, so no conflict.

---

## 8. Key Files

| File | Purpose |
|---|---|
| `expensesBackend/Controllers/MembersController.cs` | Invite + accept endpoints |
| `expensesBackend/Services/MemberService.cs` | Token generation, accept logic, permission resolution |
| `expensesBackend/Services/AuthService.cs` | OTP, signup, login, `/me` |
| `expensesBackend/Controllers/AuthController.cs` | Auth endpoints |
| `expensesBackend/appsettings.json` | `App:FrontendUrl` for invite link base URL |
| `expensesNgApp/src/app/pages/accept-invite/` | Accept invite page |
| `expensesNgApp/src/app/pages/signup/` | Signup + pending token redirect |
| `expensesNgApp/src/app/pages/login/` | Login + pending token redirect |
| `expensesNgApp/src/app/app.routes.ts` | `accept-invite` route has **no auth guard** |

---

## 9. Environment Configuration

```json
// appsettings.json
{
  "App": {
    "FrontendUrl": "http://localhost:4200"
  }
}
```

For production, override via environment variable: `App__FrontendUrl=https://yourdomain.com`
