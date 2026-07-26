# Category Limit Tracking — Design Document

## Overview

Category limits are enforced per user, with the behaviour varying by plan. The limit is not stored
as a fixed parameter on the book or member document — instead the **allowed maximum** is derived
from `User.Plan` at runtime via `PlanLimits.MaxCategories()`, while the **consumed count** is
maintained as an incrementing counter on the `ExpenseBookMember` document.

---

## Plan Rules

| Plan    | Limit        | Scope         | Tracked in UI          |
|---------|-------------|---------------|------------------------|
| Free    | 20 total    | Across ALL books the user owns/joined | User Account Settings  |
| Starter | 50 per book | Per expense book                      | Book Settings page     |
| Pro     | Unlimited   | —             | Not shown (no limit)   |

---

## Data Storage

### `ExpenseBookMember` collection

Each user–book relationship has one document. A new field was added:

```
categoriesUsed: int  (default 0)
```

- One document exists per user per expense book (owner included).
- `categoriesUsed` tracks how many **non-default** categories **this user** has created in **this book**.
- Default/seeded categories (`IsDefault = true`) are never counted.

### Why `ExpenseBookMember` and not `ExpenseBook`?

Categories can be added by any member with write access (owner, admin). The limit is a **per-user
allowance**, not a per-book quota. Storing the counter on the member document means each user's
contribution is independently tracked regardless of who else shares the book.

---

## Enforcement — Backend (`CategoryService.CreateCategoryAsync`)

The check logic differs by plan:

### Free plan — global sum check
```csharp
// Sum categoriesUsed across ALL member docs for this user
var memberDocs = await _context.ExpenseBookMembers
    .Find(m => m.UserId == requestingUserId && !m.IsDeleted)
    .ToListAsync();
var totalUsed = memberDocs.Sum(m => m.CategoriesUsed);

if (totalUsed >= PlanLimits.MaxCategories(PlanType.Free))   // 20
    throw new InvalidOperationException("Free plan is limited to 20 categories in total.");
```

### Starter plan — per-book check
```csharp
// Check only this book's member doc
var memberDoc = await _context.ExpenseBookMembers
    .Find(m => m.UserId == requestingUserId && m.ExpenseBookId == expenseBookId && !m.IsDeleted)
    .FirstOrDefaultAsync();
var bookUsed = memberDoc?.CategoriesUsed ?? 0;

if (bookUsed >= PlanLimits.MaxCategories(PlanType.Starter))   // 50
    throw new InvalidOperationException("Starter plan is limited to 50 categories per book.");
```

### Pro plan
```csharp
// No check. No counter update.
```

---

## Counter Increment / Decrement

After a successful category **create** (non-Pro only):
- Increment `categoriesUsed` by +1 on the user's member doc for that book.
- If no member doc exists yet (book owner who predates this feature), one is upserted automatically.

After a successful category **delete** (non-Pro only):
- Decrement `categoriesUsed` by -1 on the user's member doc for that book.
- Guards against going below 0 (existing count can never be negative).

Import (`ImportCategoriesAsync`) calls `CreateCategoryAsync` per item, so the same
check-and-increment path applies naturally — the import will stop adding categories once the
limit is hit and report failures for the remaining items.

---

## `GET /api/usage` Endpoint

Returns account-level usage metrics. Category fields are only meaningful for Free plan:

```json
{
  "categoriesUsed": 11,
  "categoriesLimit": 20
}
```

For Starter and Pro, `categoriesLimit` is returned as `-1` (unlimited at account level).
The per-book meter for Starter is derived on the frontend from the loaded category list.

---

## Frontend — Where the Meter Appears

### Free plan → User Account Settings (`/account`)
- Source: `GET /api/usage` → `categoriesUsed / categoriesLimit`
- Shows a progress bar (yellow at 70 %, red at 90 %).
- Hidden for Starter and Pro.

### Starter plan → Book Settings page (`/:bookId/settings`)
- Source: loaded `categories` signal filtered to `isDefault === false`.
- Shows `X / 50 categories used in this book` with progress bar.
- Hidden for Free and Pro.

### Pro plan → not shown anywhere (unlimited).

---

## Applicability to Other Entity Types

This same pattern can be reused for any per-user or per-book quota:

1. **Choose the right scope** — is the limit global (like Free categories) or per-book (like
   Starter categories)?
2. **Add a counter field** to `ExpenseBookMember` (per-book scope) or to a user-level document
   (global scope).
3. **Derive the maximum** from `PlanLimits` at runtime — never store the cap, only the consumed
   count.
4. **Enforce at write time** in the relevant service, split by plan as shown above.
5. **Expose via `/api/usage`** for account-level display, or via a book-scoped endpoint for
   per-book display.
6. **Pro plan always skips** both the check and the counter update.

---

## Key Files

| File | Role |
|------|------|
| `expensesBackend/Domain/Entities/ExpenseBookMember.cs` | `CategoriesUsed` counter field |
| `expensesBackend/Domain/PlanType.cs` | `PlanLimits.MaxCategories()` static lookup |
| `expensesBackend/Services/CategoryService.cs` | Enforcement + increment/decrement logic |
| `expensesBackend/Controllers/UsageController.cs` | `/api/usage` — sums member docs for Free |
| `expensesBackend/Domain/DTOs/UsageDto.cs` | `CategoriesUsed`, `CategoriesLimit` fields |
| `expensesNgApp/.../settings.component.ts` | Per-book meter (Starter) |
| `expensesNgApp/.../user-settings.component.ts` | Global meter (Free) |
