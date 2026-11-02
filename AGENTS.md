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

---

## Summary — Session 2026-08-21 (Phase 2 & Phase 3 Implementations)

### Phase 2: Frontend Architecture & UI Component Library
- **UI Component Library**: Created `frontend/src/components/ui/` (`Button`, `PageHeader`, `Modal`, `DataTable`, `FormField`, `index.js`).
- **Custom Query Hooks**: Created `useDashboardData`, `useGeneratorsData`, and `useFuelMovementsData` with TanStack Query.
- **i18n Localization**: Fully localized `FuelMovements.jsx` (tabs, form, actions, status) in Arabic and English via `i18n.js`.
- **Type Safety**: Integrated `prop-types` across all shared UI components.
- **Pages Integrated**: `FuelMovements.jsx` and `Generators.jsx` now use `PageHeader`.

### Phase 3: Database Performance & Query Optimization
- **ReportService Projections**: Refactored `GetDashboardStatsAsync`, `GetFuelOilDashboardAsync`, `GetStationReadingsReportAsync`, `GetOilTransactionsReportAsync`, `GetPeriodicInventoryReportAsync`, `GetInventorySummaryAsync`, and `GetFuelTransactionsReportInternalAsync` to use lightweight `.Select()` projections.
- **Double-Query Elimination**: Replaced in-memory full entity scans with in-database aggregation for all report summaries.
- **Performance Indexes Migration**: Added `20260821200000_AddPerformanceIndexes.cs` covering composite & filtered indexes for high-frequency ledger queries (`FuelTransactions`, `OilTransactions`, `DailyReadings`, `SubStationReadings`, `PeriodicInventories`).

### Phase 4: Enterprise Hardening, Security & System Resilience
- **Hangfire Dashboard Security**: Enforced role authorization (`SystemOwner` / `Admin`) via `HangfireAllowAdminFilter`.
- **Database Maintenance & Statistics**: Optimized `DatabaseMaintenanceJob` to run `EXEC sp_updatestats` safely with relational database guards and clean expired refresh tokens.
- **Auth Flow Standardization**: Refactored `ForgotPassword.jsx` and `ForceChangePassword.jsx` to strictly use design system tokens (`.login-page`, `.login-card`, `.form-control`, `.login-btn`).

### Phase 5: Enterprise Compliance, Data Retention & Disaster Recovery
- **Data Archival Service & Jobs**: Implemented `IDataArchivalService` and `DataArchivalService` for automated archiving of `AuditLogs` (>90 days) and `DailyReadings` (>365 days). Registered monthly Hangfire recurring job `DataArchivalJob`.
- **Disaster Recovery & Backup Strategy**: Created comprehensive DR runbook `docs/DISASTER_RECOVERY_AND_BACKUP_PLAN.md` with SLA definitions (RPO: 15m, RTO: 1h, 99.9% availability) and `scripts/backup-database.sql`.
- **Unit Tests**: Added `DataArchivalServiceTests.cs` (3 tests).

### Verification
- **Backend Tests**: 257 passed, 10 skipped, **0 failed** (Duration: 24s).
- **Backend Build**: 0 errors, 14 warnings.
- **Frontend Build**: 1982 modules transformed in 7.14s, 0 errors.

---

## Summary — Session 2026-08-21 (Phase 6: Frontend Unification, Compliance & SLA Metrics)

### Goal
- [x] Unify core frontend pages with shared UI components (`PageHeader`, `Button`, `DataTable`, `Modal`)
- [x] Integrate TanStack Query hook `useDashboardData` and build `usePeriodicInventoryData`
- [x] Implement enterprise data compliance & privacy service (`ComplianceService`, `ComplianceController`)
- [x] Enhance Prometheus SLA gauges (Uptime, Database Latency, Archival Counter) in `FuelTrackMetrics`
- [x] Add comprehensive unit tests and verify end-to-end build integrity

### What Was Built
1. **Frontend Modernization & Integration**:
   - `frontend/src/pages/Dashboard.jsx`: Refactored to use `useDashboardData` (TanStack Query), `PageHeader`, `Button`, and standardized HSL theme variables.
   - `frontend/src/pages/PeriodicInventoryPage.jsx`: Integrated `PageHeader` and `Button` from the UI component library.
   - `frontend/src/pages/LocationsManagement.jsx`: Integrated `PageHeader` and `Button` and aligned color tokens.
   - `frontend/src/hooks/usePeriodicInventoryData.js`: Created TanStack Query hook with mutation invalidation.
2. **Enterprise Compliance & GDPR Rights**:
   - `IComplianceService.cs` & `ComplianceService.cs`: Implemented `ExportUserDataAsync`, `AnonymizeUserPersonalDataAsync`, and `GetComplianceAuditSummaryAsync`.
   - `ComplianceController.cs`: Added endpoints `GET /api/compliance/export-user/{id}`, `POST /api/compliance/anonymize-user/{id}`, and `GET /api/compliance/audit-summary` (restricted to Admin/SystemOwner).
   - Registered `IComplianceService` in `DependencyInjection.cs`.
3. **Observability & SLA Metrics**:
   - `FuelTrackMetrics.cs`: Added `fueltrack.system.uptime_seconds` gauge, `fueltrack.db.query_duration_ms` histogram, and `fueltrack.archival.records_archived` counter.
4. **Testing & Verification**:
   - `ComplianceServiceTests.cs`: Added 4 unit tests covering user data exports, personal data anonymization, and compliance audit summaries.
   - Total backend tests: **261 passed, 0 failed**, 10 skipped (in-memory view constraints).
   - Frontend build: **1983 modules transformed in 7.95s**, 0 errors.

---

## Summary — Session 2026-08-21 (Phase 7: Frontend Page Decomposition & Bundle Optimization)

### Goal
- [x] Optimize Vite bundle splitting via `manualChunks` in `vite.config.js`
- [x] Decompose `Reports.jsx` by extracting sub-tabs (`FuelIncomingReportTab.jsx`, `FuelTransferReportTab.jsx`, `FuelLossReportTab.jsx`)
- [x] Standardize `AuditLogs.jsx` with `PageHeader`, `Button`, and design system tokens
- [x] Verify frontend builds and backend test suite integrity

### What Was Built
1. **Vite Bundle Optimization (`vite.config.js`)**:
   - Configured `manualChunks` to split heavy libraries:
     - `vendor-core` (React, React-Router, Zustand, i18next)
     - `vendor-query` (@tanstack/react-query)
     - `vendor-charts` (chart.js, react-chartjs-2)
     - `vendor-icons` (lucide-react)
     - `vendor-canvas` (html2canvas)
     - `vendor-xlsx` (xlsx-js-style)
   - Results: `Dashboard` reduced from 186kB to **20.6kB**, `Reports` reduced from 1.39MB to **512kB**.
2. **Modular Report Sub-Components (`frontend/src/components/Reports/`)**:
   - Created `FuelIncomingReportTab.jsx`, `FuelTransferReportTab.jsx`, and `FuelLossReportTab.jsx` with full PropTypes and design tokens.
3. **AuditLogs Modernization (`frontend/src/pages/AuditLogs.jsx`)**:
   - Standardized with `PageHeader`, `Button`, and method badges.
4. **Verification**:
   - Frontend Build: 1983 modules transformed in **6.81s**, 0 errors.
   - Backend Tests: **261 passed, 0 failed**, 10 skipped (Duration: 23s).

---

## Summary — Session 2026-08-21 (Phase 8: Data Protection & Security Hardening)

### Goal
- [x] Harden production secret validation for 256-bit AES data encryption key (`DataEncryption:Key`)
- [x] Protect administrative & metric endpoints (Hangfire authorization filter, Prometheus endpoint & production Swagger protection)
- [x] Add comprehensive security unit tests (`StartupSecretsValidatorTests.cs`)
- [x] Verify complete system test suite and frontend build

### What Was Built
1. **Production Secret Hardening (`StartupSecretsValidator.cs`)**:
   - Added validation for `DataEncryption:Key` checking for 32-byte production key and placeholder rejection.
2. **Security & Validation Tests (`StartupSecretsValidatorTests.cs`)**:
   - Added unit tests for production secrets validation, placeholder detection, and CORS wildcard protection.
3. **Verification**:
   - Total Backend Tests: **264 passed, 0 failed**, 10 skipped (Duration: 23s).
   - Frontend Build: 1983 modules transformed in **6.81s**, 0 errors.

---

## Summary — Session 2026-08-21 (Phase 9: Frontend Unit Test Coverage for UI Component Library)

### Goal
- [x] Add Vitest unit tests for all 4 shared UI components (`Button`, `Modal`, `PageHeader`, `FormField`)
- [x] Fix all test assertions to match actual component implementations
- [x] Achieve 100% pass rate on new component tests

### What Was Built
1. **UI Component Tests (`frontend/src/components/ui/`)**:
   - `Button.test.jsx`: 5 tests — renders text, variant CSS class, size inline-style, click handling, loading spinner disable, icon rendering.
   - `Modal.test.jsx`: 3 tests — closed state renders nothing, open state renders title/body/footer, close button callback.
   - `PageHeader.test.jsx`: 3 tests — title/subtitle rendering, badge slot, actions slot.
   - `FormField.test.jsx`: 3 tests — label with required star, error message display, hint text display.
2. **Key Discoveries Fixed in Tests**:
   - `Button` size uses inline styles (not CSS classes) — assertions updated to check `btn.style.height`.
   - `FormField` helper text uses `hint` prop (not `helper`) — prop corrected.
   - `Modal` close button uses `aria-label="Close modal"` — query updated to `getByRole('button', { name: /Close modal/i })`.
3. **Verification**:
   - Frontend UI Tests: **14 passed, 0 failed** (Duration: 2.39s).
   - Backend Tests: **264 passed, 0 failed**, 10 skipped (pre-existing).

---

## Summary — Session 2026-08-21 (Phase 10: Unified Design System Implementation)

### Goal
- [x] Audit `docs/system_audit_report.md` Section 10 and implement all design system unification tasks
- [x] Add `--purple` and `--purple-subtle` CSS design tokens to `styles.css`
- [x] Replace all hardcoded hex colors with CSS Variables across frontend pages
- [x] Verify frontend build integrity

### What Was Audited & Found Already Complete
- `Login.jsx` — already uses `.login-page`, `.login-card`, `.login-btn` CSS classes ✅
- `Dashboard.jsx` — no hardcoded hex colors found ✅
- `FuelMovements.jsx` — no hardcoded hex colors found ✅
- `components/ui/` — Component Library (Button, Modal, DataTable, FormField, PageHeader) already exists ✅
- `ErrorBoundary` — wraps every Route in `App.jsx` ✅
- Dashboard cache key — already includes `userScopeKey` (`u{userId}` or `admin`) ✅
- `react-bootstrap` — not present in `package.json` ✅
- `--card-bg` CSS variable — defined at line 52 as `var(--surface)` ✅

### What Was Built

#### CSS Design Tokens (`styles.css`)
- Added `--purple: hsl(262, 83%, 63%)` and `--purple-subtle: hsla(262, 83%, 63%, 0.12)` under "Extended Semantic" section

#### Hardcoded Color Fixes

**`TanksManagement.jsx`:**
- `getFillColor()` — `#ef4444` → `var(--danger)`, `#f59e0b` → `var(--warning)`, `#10b981` → `var(--success)`
- Summary KPI cards — `#10b981` → `var(--success)`, `#8b5cf6` → `var(--purple)`, `#f59e0b` → `var(--warning)`
- "Inactive" badge — `rgba(239,68,68,0.12)`/`#ef4444` → `var(--danger-subtle)`/`var(--danger)`
- Form error block — `rgba(239,68,68,0.08)`/`#ef4444` → `var(--danger-subtle)`/`var(--danger)`
- Dead stock label — `#64748b` → `var(--text-muted)`
- Station badge count — `color: '#fff'` → `color: 'var(--text)'`
- Delete button — `color: '#fff'` → `color: 'var(--text)'`

**`StockInventory.jsx`:**
- `alertCfg` object — all hardcoded `#ef4444`/`#10b981`/`#ef444415`/`#10b98115` → `var(--danger)`/`var(--success)`/`var(--danger-subtle)`/`var(--success-subtle)`
- `transferIn` cell color — `#10b981` → `var(--success)`
- Fill progress bar — `#ef4444`/`#10b981` → `var(--danger)`/`var(--success)`
- Actual Stock KPI card — `#10b981`/`rgba(16,185,129,0.12)` → `var(--success)`/`var(--success-subtle)`
- Branch button color — `color: '#fff'` → `color: 'var(--text)'`
- Table header "Received" — `#10b981` → `var(--success)`

**`WorkPermits.jsx`:**
- "Completed" KPI card — `#10b981`/`rgba(16,185,129,0.12)` → `var(--success)`/`var(--success-subtle)`

**`SetupOils.jsx`:**
- `stockColor` logic — `#ef4444` → `var(--danger)`, `#10b981` → `var(--success)`
- Station badge count — `color: '#fff'` → `color: 'var(--text)'`
- Delete button — `color: '#fff'` → `color: 'var(--text)'`

### Verification
- **Frontend Build**: ✓ built in **6.57s**, 0 errors
- **Tests**: Frontend UI component tests all pass (14/14).

### Files Changed This Session
- `frontend/src/styles.css` — Added `--purple` + `--purple-subtle` design tokens
- `frontend/src/pages/TanksManagement.jsx` — 7 color fixes
- `frontend/src/pages/StockInventory.jsx` — 6 color fixes
- `frontend/src/pages/WorkPermits.jsx` — 1 color fix
- `frontend/src/pages/SetupOils.jsx` — 3 color fixes
- `AGENTS.md` — This summary


