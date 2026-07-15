# Fix Arabic Language Support in User Management

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ensure Arabic names, usernames, and all text fields work end-to-end across Frontend, Backend, and Database layers.

**Architecture:** The system already uses `nvarchar` columns and has no character-set regex blocking Arabic. Changes are minimal: enable ASP.NET Identity to accept Arabic usernames, add RTL input support in the frontend, and verify the full flow works.

**Tech Stack:** ASP.NET Core 8, Entity Framework Core, ASP.NET Identity, React, SQL Server

---

## File Structure

| File | Action | Purpose |
|------|--------|---------|
| `src/FuelTrack.Infrastructure/DependencyInjection.cs` | Modify | Add `AllowedUserNameCharacters` with Arabic support |
| `frontend/src/pages/Users.jsx` | Modify | Add `dir="auto"` to name input, improve RTL rendering |
| `src/FuelTrack.Application/Validators/RBAC/CreateUserDtoValidator.cs` | Review | Verify no character regex on FullName (already OK) |
| `src/FuelTrack.Infrastructure/Migrations/` | Check | Verify all User columns are `nvarchar` (already OK) |

---

### Task 1: Enable Arabic Characters in ASP.NET Identity

**Files:**
- Modify: `src/FuelTrack.Infrastructure/DependencyInjection.cs:32-42`

**Why:** ASP.NET Identity default `AllowedUserNameCharacters` is `abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+`. This rejects Arabic characters in `UserName`. Even though `UserName` is validated as email (which is ASCII by RFC), setting `AllowedUserNameCharacters` explicitly prevents future issues if the system ever allows non-email usernames.

- [ ] **Step 1: Read the current Identity configuration**

Read `src/FuelTrack.Infrastructure/DependencyInjection.cs` lines 28-45 to see the current `AddIdentity` block.

- [ ] **Step 2: Add AllowedUserNameCharacters with Arabic support**

Add `options.User.AllowedUserNameCharacters` inside the `AddIdentity` lambda. Include Arabic Unicode range `\u0600-\u06FF` plus the default ASCII characters:

```csharp
services.AddIdentity<User, Role>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyz" +
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
        "0123456789" +
        "-._@+ " +
        "\u0600-\u06FF"; // Arabic Unicode block
})
```

- [ ] **Step 3: Build to verify no compilation errors**

Run: `dotnet build src/FuelTrack.API/FuelTrack.API.csproj`
Expected: Build succeeded with 0 errors (warnings OK).

---

### Task 2: Add RTL Input Support in Users.jsx

**Files:**
- Modify: `frontend/src/pages/Users.jsx:385` (FullName input)

**Why:** Arabic text in a plain `<input>` defaults to LTR rendering, which looks wrong for right-to-left languages. Adding `dir="auto"` lets the browser detect text direction automatically.

- [ ] **Step 1: Read the current name input**

Read `frontend/src/pages/Users.jsx` around line 385 to see the current FullName input element.

- [ ] **Step 2: Add dir="auto" to the FullName input**

Change:
```jsx
<input className="form-control" value={form.name} onChange={e => fld('name', e.target.value)} />
```

To:
```jsx
<input className="form-control" value={form.name} onChange={e => fld('name', e.target.value)} dir="auto" />
```

- [ ] **Step 3: Build frontend to verify**

Run: `cd frontend && npm run build`
Expected: Build succeeds with 0 errors.

---

### Task 3: Verify Database Column Types (No Changes Needed)

**Why:** Confirm all User table columns use `nvarchar` for Unicode support.

- [ ] **Step 1: Check migration for User table column types**

Read `src/FuelTrack.Infrastructure/Migrations/20260627161613_InitialMigration.cs` around lines 185-215.

Verify: `FullName` → `nvarchar(max)`, `UserName` → `nvarchar(256)`, `Email` → `nvarchar(256)`.

- [ ] **Step 2: Check DbContext configuration**

Read `src/FuelTrack.Infrastructure/FuelTrackDbContext.cs` around lines 58-68.

Verify: No `.HasColumnType("varchar")` overrides on User properties.

Expected: All User columns are `nvarchar`. No changes needed.

---

### Task 4: Verify Backend Validators (No Changes Needed)

**Why:** Confirm no character-set regex blocks Arabic in FullName or other text fields.

- [ ] **Step 1: Check CreateUserDtoValidator**

Read `src/FuelTrack.Application/Validators/RBAC/CreateUserDtoValidator.cs`.

Verify: `FullName` uses only `NotEmpty()` + `MaximumLength(100)` — no `Matches()` with ASCII regex.

- [ ] **Step 2: Check UpdateUserDtoValidator**

Read `src/FuelTrack.Application/Validators/RBAC/UpdateUserDtoValidator.cs`.

Verify: `FullName` uses only `MaximumLength(100)` — no character restriction.

Expected: No changes needed. Arabic is already accepted.

---

### Task 5: Verify Frontend Form Validation (No Changes Needed)

**Why:** Confirm no JavaScript regex blocks Arabic characters in name fields.

- [ ] **Step 1: Check Users.jsx form validation**

Read `frontend/src/pages/Users.jsx` around lines 195-210.

Verify: Name validation only checks `!form.name` (empty check) — no `/[a-zA-Z]/` pattern on name.

Expected: No changes needed. Arabic is already accepted.

---

### Task 6: Build and Test

**Why:** Verify everything works end-to-end.

- [ ] **Step 1: Full backend build**

Run: `dotnet build`
Expected: 0 errors.

- [ ] **Step 2: Full frontend build**

Run: `cd frontend && npm run build`
Expected: 0 errors.

- [ ] **Step 3: Start API and test Arabic name creation via curl**

Run API: `dotnet run --project src/FuelTrack.API`

Test with Arabic name:
```bash
curl -X POST http://localhost:5000/api/auth/login -H "Content-Type: application/json" -d '{"email":"admin@ft.com","password":"Admin@123"}'
```

Then use the returned token to create a user with Arabic name via the Users endpoint.

- [ ] **Step 4: Verify Arabic displays correctly in browser**

Open `http://localhost:5173`, navigate to Users page, create a user with Arabic name (e.g., "محمد أحمد علي"). Verify:
- Name saves without error
- Name displays correctly in the users list
- No `????` or garbled text

---

### Task 7: Commit

- [ ] **Step 1: Stage and commit all changes**

```bash
git add -A
git commit -m "feat: enable Arabic language support in user management

- Add Arabic Unicode range to AllowedUserNameCharacters in ASP.NET Identity
- Add dir=\"auto\" to FullName input for proper RTL rendering
- Verified: all User columns use nvarchar, no character-blocked validators"
```

---

## Summary of Changes

| File | Change | Impact |
|------|--------|--------|
| `DependencyInjection.cs` | Add `AllowedUserNameCharacters` with Arabic `\u0600-\u06FF` | Arabic usernames accepted by Identity |
| `Users.jsx` | Add `dir="auto"` to FullName input | Arabic text renders RTL in input field |

## Verification Criteria

- [ ] Arabic full name (e.g., "محمد أحمد علي") saves to database
- [ ] Arabic full name displays correctly in users list
- [ ] No `????` in database (nvarchar confirmed)
- [ ] `dotnet build` succeeds with 0 errors
- [ ] `npm run build` succeeds with 0 errors
- [ ] Existing English names still work
- [ ] Login, CRUD, search, sort all functional
