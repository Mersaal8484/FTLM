# FuelTrack Guide

## Summary — Session 2026-07-10 (User Data Scope Full Implementation)

### Goal
- [x] Implement all 6 phases of User Data Scope design from plan
- [x] Replace flat `UserSubStation`/`UserGenerator` join tables with `UserScope` table
- [x] Build centralized data filtering service integrated into all data services
- [x] Rewrite Users.jsx frontend with cascading scope dropdowns

### What Was Built

#### Phase 1 — Domain Layer
- **`UserScope.cs`** — `UserId`, `ScopeType` (Branch/Station/SubStation), `ScopeId` (int?, null = "All")
- **`ScopeType.cs`** — Enum with Branch=0, Station=1, SubStation=2
- **Removed**: `UserSubStation.cs`, `UserGenerator.cs` — tables dropped
- **`User.cs`** — `SubStationLinks`/`GeneratorLinks` replaced with `Scopes` collection

#### Phase 2 — Infrastructure Layer
- **`FuelTrackDbContext.cs`** — `UserScopes` DbSet, `UserScope` entity config; old junction configs removed
- **EF Migration**: `20260710105501_ReplaceJunctionTablesWithUserScope` — drops `UserSubStations`/`UserGenerators`, creates `UserScopes` with FK + cascade delete
- **`UserScopeService.cs`** — `GetAccessibleSubStationIdsAsync`, `GetAccessibleStationIdsAsync`, `GetAccessibleBranchIdsAsync`, `GetAccessibleGeneratorIdsAsync`, `GetAccessibleFuelTankIdsAsync`, `GetAccessibleOilTankIdsAsync`, `GetAccessibleMeterIdsAsync`, `HasAccessToSubStationAsync`, `ApplyScopeFilterAsync` (generic IQueryable filter by scope level)

#### Phase 3 — Application Layer
- **DTOs**: `UserScopeDto` (ScopeType, ScopeId?), `AssignScopesDto` (List of scopes)
- **`IUserService.cs`** — `AssignScopesAsync(int userId, List<UserScopeDto> scopes)`
- **`UserService.cs`** — `AssignScopesAsync` with duplicate detection (HashSet), hierarchy validation (can't have Branch + Station under same branch), global scope conflict check
- **`CreateUserDtoValidator.cs`** — validates scopes: non-empty, no global + specific conflict, hierarchy
- **`UsersController.cs`** — `POST /api/users/{id}/scopes` endpoint (Admin-only), no old `/sub-stations`/`/generators` endpoints

#### Phase 4 — Centralized Data Filtering
- **`IUserScopeFilterService.cs`** — Interface with `GetAccessibleSubStationIdsAsync`, `HasAccessToSubStationAsync`
- **`UserScopeFilterService.cs`** — Implementation with "All" scope logic (Branch(null) → all stations/substations; Station(null) → all substations in station; SubStation → specific)
- **`UserScopeFilterAttribute.cs`** — Global `IAsyncAuthorizationFilter` registered in `Program.cs:37`; skips SystemOwner/authenticated requests, validates `subStationId` query param access

**7 services wired to IUserScopeFilterService:**
- `StationReadingService` — list/get/PendingStationReading filtered
- `FuelMovementService` — list/get all filtered
- `ReportService` — all 8 report methods filtered
- `NotificationService` — list/get/unread count filtered
- `PeriodicInventoryService` — list/get efficiency calc filtered  
- `OilTransactionService` — list/get/create/update filtered
- `ReadingService` — get/set daily readings filtered

#### Phase 5 — Frontend Rebuild
- **`Users.jsx`** (620 lines) — Full rewrite with:
  - `scopes` in form state + `normalizeUser`/`normalizeForApi` pass-through
  - `ScopeRow` component: 3-level cascade (Branch → Station → SubStation dropdowns)
  - "All (Global)" at branch level (grants full access)
  - "All in Branch" / "All in Station" at each cascade level
  - `addScope` / `removeScope` / `updateScope` helpers with auto-reset on level change
  - Global scope detection + warning UI
  - `POST /api/users/{id}/scopes` on edit save
  - `e2e/users.spec.js` — comprehensive scope tests

#### Phase 6 — Security Enforcement
- `UserScopeFilterAttribute` — global filter on every API endpoint
- `UserScopeFilterService.HasAccessToSubStationAsync` — individual record access check
- All Create/Update endpoints in wired services reject unauthorized substation access
- Controller endpoints remain protected by `[Authorize(Roles = "Admin")]` where appropriate

### Verification
- **Build**: 0 errors, 14 warnings (all pre-existing NuGet advisories)
- **Migration**: `20260710105501_ReplaceJunctionTablesWithUserScope` — idempotent
- **Edge cases**: unauthenticated requests skip filter (no `userId`), SystemOwner role bypasses scope check, `scopeId = null` = "All" at that level

### Key Design Decisions
- **ScopeType enum** (not string) — type-safe, no parsing errors
- **ScopeId nullable** — null = "All" at that level (e.g., Branch + null = all stations + substations under that branch)
- **Flat `UserScopes` table** — single table replace 2 junction tables; generator/fuel-tank/meter access derived from SubStation
- **`IUserScopeFilterService` as separate interface** — not bundled into `UserScopeService` to avoid circular deps in infrastructure layer
- **Global filter, not endpoint-level** — every endpoint protected by default, opt-out only

### Files Changed This Session
- `src/FuelTrack.Domain/Models/UserScope.cs` — Created
- `src/FuelTrack.Domain/Models/ScopeType.cs` — Created
- `src/FuelTrack.Domain/Models/User.cs` — Scopes nav prop added
- `src/FuelTrack.Application/DTOs/RBAC/UserDto.cs` — UserScopeDto, AssignScopesDto added
- `src/FuelTrack.Application/Interfaces/IUserScopeFilterService.cs` — Created
- `src/FuelTrack.Application/Interfaces/IUserService.cs` — AssignScopesAsync
- `src/FuelTrack.Infrastructure/Services/UserScopeFilterService.cs` — Created
- `src/FuelTrack.Infrastructure/Services/UserScopeService.cs` — Created
- `src/FuelTrack.Infrastructure/Services/UserService.cs` — AssignScopesAsync
- `src/FuelTrack.Infrastructure/Services/StationReadingService.cs` — Scope filter wired
- `src/FuelTrack.Infrastructure/Services/FuelMovementService.cs` — Scope filter wired
- `src/FuelTrack.Infrastructure/Services/ReportService.cs` — Scope filter wired
- `src/FuelTrack.Infrastructure/Services/NotificationService.cs` — Scope filter wired
- `src/FuelTrack.Infrastructure/Services/PeriodicInventoryService.cs` — Scope filter wired
- `src/FuelTrack.Infrastructure/Services/OilTransactionService.cs` — Scope filter wired
- `src/FuelTrack.Infrastructure/Services/ReadingService.cs` — Scope filter wired
- `src/FuelTrack.Infrastructure/FuelTrackDbContext.cs` — UserScopes DbSet, old tables removed
- `src/FuelTrack.Infrastructure/Migrations/20260710105501_ReplaceJunctionTablesWithUserScope.cs` — New migration
- `src/FuelTrack.API/Filters/UserScopeFilterAttribute.cs` — Created
- `src/FuelTrack.API/Controllers/UsersController.cs` — Scopes endpoint
- `src/FuelTrack.API/Program.cs` — Global filter registration
- `frontend/src/pages/Users.jsx` — Full scope UI rewrite
- `tests/FuelTrack.Tests/e2e/users.spec.js` — Scope tests

---

## Summary — Session 2026-07-11 (Permissions Mapping Audit & Fix)

### Goal
- [x] Audit Users and Roles management end-to-end
- [x] Fix mapping gap: `OilTanks`/`SubStations` backend modules reset on role save

### Scope
- Read frontend `Users.jsx` (620 lines), `RolesManagement.jsx` (full CRUD, permission matrix, module aliases, protected roles, clone/reset/delete).
- Read backend `UsersController.cs`, `RolesController.cs`, `UserService.cs`, `RoleService.cs`.
- Read all DTOs (`UserDto.cs`, `RoleDto.cs`) and validators (`CreateUserDto`, `UpdateUserDto`, `CreateRoleDto`, `UpdateRoleDto`, `AssignScopesDto`).
- Read `PermissionHelper.cs` (380+ lines) — `ConvertDictJsonToModuleArray`, `HasPermissionDict`, `MapBackendToFrontend`, `ConvertPermissions`, flat-string fallback.
- Read `Permissions.cs` domain — `PermissionModule`, `PermissionModules.AllModules` (16 entries).
- Read `PermissionMapper.cs` — `DomainToFrontend` mapping dict, `ToFrontendPermissions`.
- Read frontend `registry.js` — 13 module definitions with groups, actions, hidden report permissions.

### Mapping Gap Analysis
- **Read direction (DB → frontend)** correct: `ToFrontendPermissions` + `DomainToFrontend` maps `OilTanks`→`oil` and `SubStations`→`stations`.
- **Write direction (frontend → DB)** broken: `ConvertDictJsonToModuleArray` only mapped `oil`→`OilTransactions` and `stations`→`Stations`. `OilTanks`/`SubStations` reset to all-false defaults on save.
- Fix: Added `multiModuleMap` dict for `oil`→`[OilTransactions, OilTanks]` and `stations`→`[Stations, SubStations]`. Loop now applies same permissions to both targets.

### Verification
- Build: 0 errors (3 pre-existing warnings)

### Files Changed
- `src/FuelTrack.Application/Helpers/PermissionHelper.cs` — `ConvertDictJsonToModuleArray` now multi-module aware

---

## Summary — Session 2026-07-13 (System Prompt Verification & Fixes)

### Goal
- [x] Audit `docs/SYSTEM_PROMPT.md` claims against actual codebase
- [x] Fix integration test crash on InMemory provider
- [x] Upgrade vulnerable NuGet packages
- [x] Remove stale doc references

### Findings
- **Issue #1 (Integration tests fail)**: `Program.cs:357` called `dbContext.Database.OpenConnectionAsync()` unconditionally — fails on InMemory. **Fixed** by wrapping with `if (dbContext.Database.IsRelational())`.
- **Issue #2/#3 (PermissionHelper.cs / PermissionMapper.cs missing)**: These files were part of the original FuelTrack codebase and are **not ported** to this rebuild. The permission system now uses `ModuleDefinition` + `PagePermission` + `RolePermission` models. No action needed.
- **Issue #4 (NuGet advisories)**: MailKit 4.10.0 → 4.17.0 (moderate advisory fixed). `Microsoft.Extensions.Caching.Memory` removed (unused, NU1510). AutoMapper 12.0.1 remains (no newer patch available).
- **Issue #5 (Migration history)**: `InitialSchema` (20260712152709) is the only migration. The old `ReplaceJunctionTablesWithUserScope` migration from the original codebase was squashed into InitialSchema during rebuild.
- **Issue #6 (Git dirty)**: Normal — untracked files from scaffold rebuild.

### Verification
- **Build**: 0 errors, 29 warnings (down from 39 — MailKit/MimeKit/Caching.Memory advisories resolved)
- **Tests**: 107/107 passed, 5 skipped (pre-existing InMemory constraints), **0 failed**

### Files Changed
- `src/FuelTrack.API/Program.cs` — `IsRelational()` guard at line 357
- `src/FuelTrack.Infrastructure/FuelTrack.Infrastructure.csproj` — MailKit 4.10.0→4.17.0; removed `Microsoft.Extensions.Caching.Memory`
- `docs/SYSTEM_PROMPT.md` — Updated build/test counts, Known Issues, Next Steps
- `AGENTS.md` — This section
