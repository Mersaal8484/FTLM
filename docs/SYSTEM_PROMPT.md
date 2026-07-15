# FuelTrack-Rebuild — System State Prompt

## Context
You are continuing work on **FuelTrack-Rebuild**, an ASP.NET Core 10.0 + React 18 rebuild of the FuelTrack-PEC system. This prompt captures the current verified state as of **2026-07-13**.

## Verified Build State
- **Backend**: `dotnet build` → **0 errors**, 29 warnings (pre-existing NuGet advisory: AutoMapper 12.0.1 only)
- **Backend Tests**: 107/107 passed, 5 skipped (InMemory unique constraints), **0 failed**
- **Frontend Build**: `npm run build` → **success** (1698 modules, 4.74s)
- **Frontend Tests**: 9/9 passed
- **Integration Tests**: FIXED — `Program.cs:357` guarded with `IsRelational()` check; no longer crashes on InMemory provider

## Architecture Summary

### Domain Layer (`src/FuelTrack.Domain/Models/`)
- **UserScope.cs** — Flat table replacing `UserSubStation`/`UserGenerator` junctions
  - `UserId`, `BranchId` (int?, null=All), `StationId` (int?, null=All), `SubStationId` (int?, null=All)
  - Navigation: `User`, `Branch`, `Station`, `SubStation`
- **ScopeType.cs** — Enum: None=0, Branch=1, Station=2, SubStation=3
- **User.cs** — Has `Scopes` collection (replaces `SubStationLinks`/`GeneratorLinks`)
- **No `UserSubStation.cs` or `UserGenerator.cs`** — tables dropped from migration

### Infrastructure Layer
- **FuelTrackDbContext.cs** — `UserScopes` DbSet, soft-delete filters, composite unique index `(UserId, BranchId, StationId, SubStationId)`
- **Migration**: `20260712152709_InitialSchema.cs` includes `UserScopes` table (no separate UserScope migration exists)
- **UserScopeFilterService.cs** — Implements `IUserScopeFilterService`:
  - `GetAccessibleSubStationIdsAsync(userId)` — resolves hierarchy (Branch→Station→SubStation)
  - `GetAccessibleStationIdsAsync(userId)` — resolves hierarchy
  - `GetAccessibleBranchIdsAsync(userId)` — resolves hierarchy
  - `HasAccessToSubStationAsync(userId, subStationId)` — individual check
  - Global scope: `BranchId==null && StationId==null && SubStationId==null` = Admin/All access
- **UserScopeService.cs** — Higher-level service with:
  - `GetAccessibleGeneratorIdsAsync`, `GetAccessibleFuelTankIdsAsync`, `GetAccessibleOilTankIdsAsync`, `GetAccessibleMeterIdsAsync`
  - `ApplyScope<T>(query, userId, scopeType, getId, ...)` — generic IQueryable filter
  - `IsAdminAsync(userId)` — checks for global scope
- **7 services wired to `IUserScopeFilterService`**:
  - `StationReadingService`, `FuelMovementService`, `ReportService`, `NotificationService`, `PeriodicInventoryService`, `OilTransactionService`, `ReadingService`

### Application Layer
- **DTOs**: `UserScopeDto` (ScopeType, ScopeId?), `AssignScopesDto` (List of scopes)
- **Validators**: `AssignScopesDtoValidator.cs`, `CreateUserDtoValidator.cs`
- **Interfaces**: `IUserScopeFilterService.cs`, `IUserService.cs` (has `AssignScopesAsync`)

### API Layer
- **UserScopeFilterAttribute.cs** — Global `IAsyncAuthorizationFilter` registered in `Program.cs:41`
  - Skips unauthenticated requests
  - Bypasses `SystemOwner` role
  - Reads `subStationId` from route/query params
  - Returns 403 if no access
- **UsersController.cs** — Has `POST /api/users/{id}/scopes` endpoint (Admin-only)
  - **Missing**: Old `/sub-stations`/`/generators` endpoints removed
- **Program.cs:348-468** — Startup seeding:
  - SQL warm-up with retry (5 attempts)
  - ModuleRegistrySeed + PermissionGenerator
  - Admin user creation with full scope (`UserScopes` with all nulls)
  - Recurring jobs: Hangfire dashboard, cleanup, backups, analytics

### Frontend
- **Users.jsx** (620 lines) — Full scope UI:
  - `scopes` in form state
  - `ScopeRow` component: 3-level cascade (Branch → Station → SubStation)
  - "All (Global)" at branch level
  - `addScope`/`removeScope`/`updateScope` helpers
  - Global scope warning UI
  - `POST /api/users/{id}/scopes` on edit
- **usePermissions.js** — Permission aliases:
  - `tanks` → `fuel_tanks`
  - `oil` → `oil_transactions`
  - `inventory` → `periodic_inventory`
  - `readings` → `station_readings` + fallback to `daily_readings`
  - `alerts` → `notifications`
  - `stations` → fallback to `branches` + `sub_stations`
  - `settings` → fallback to `roles`, `lookups`, `meters`, `controllers`

## Known Issues & Gaps

### Fixed
1. ~~**Integration tests fail** — `Program.cs:357` guarded with `IsRelational()` check ✅~~
2. ~~**`PermissionHelper.cs` / `PermissionMapper.cs` missing** — Not ported to rebuild; superseded by `ModuleDefinition`/`PagePermission`/`RolePermission` permission model. No action needed. Ref: `docs/AGENTS.md` session 2026-07-13~~

### Non-Critical
4. **NuGet vulnerabilities** — AutoMapper 12.0.1 (high); no newer version of `AutoMapper.Extensions.Microsoft.DependencyInjection` available yet
5. **Migration history mismatch** — AGENTS.md mentions `20260710105501_ReplaceJunctionTablesWithUserScope` but only `20260712152709_InitialSchema` exists (singled into InitialSchema during rebuild)
6. **Git working tree dirty** — Many untracked files from rebuild, some deleted docs

## Key Design Decisions
- **ScopeType enum** (not string) — type-safe
- **ScopeId nullable** — null = "All" at that level
- **Flat UserScopes table** — single table replacing 2 junction tables
- **IUserScopeFilterService as separate interface** — avoids circular deps
- **Global filter, not endpoint-level** — every endpoint protected by default
- **Admin seeding** — `UserScopes` with all null IDs = full access

## Important File Locations
| Component | Path |
|-----------|------|
| UserScope model | `src/FuelTrack.Domain/Models/UserScope.cs` |
| ScopeType enum | `src/FuelTrack.Domain/Models/ScopeType.cs` |
| DbContext | `src/FuelTrack.Infrastructure/FuelTrackDbContext.cs` |
| Scope filter service | `src/FuelTrack.Infrastructure/Services/UserScopeFilterService.cs` |
| Scope service | `src/FuelTrack.Infrastructure/Services/UserScopeService.cs` |
| Global filter attribute | `src/FuelTrack.API/Filters/UserScopeFilterAttribute.cs` |
| Users controller | `src/FuelTrack.API/Controllers/UsersController.cs` |
| Program startup | `src/FuelTrack.API/Program.cs` |
| Frontend Users page | `frontend/src/pages/Users.jsx` |
| Permissions hook | `frontend/src/permissions/usePermissions.js` |
| Integration test base | `tests/FuelTrack.Tests/Integration/IntegrationTestBase.cs` |

## Next Steps (if continuing work)
1. ~~Fix integration test DB provider issue~~ ✅ Done
2. ~~Remove stale `PermissionHelper.cs`/`PermissionMapper.cs` references from AGENTS.md~~ ✅ Done
3. Move remaining AGENTS.md migration/session history references to match current codebase state
4. Monitor AutoMapper for a 12.0.2+ release to fix GHSA-rvv3-g6hj-g44x
5. Consider cleaning up pre-existing CS86xx/CSSYSLIB warnings (CS0105, CS8604, SYSLIB0060, EF1002)

## Testing Commands
```bash
# Backend build
dotnet build FuelTrack.sln

# Unit tests only (exclude integration)
dotnet test FuelTrack.sln --filter "FullyQualifiedName!~Integration"

# Frontend build
cd frontend && npm run build

# Frontend tests
cd frontend && npm test -- --run
```
