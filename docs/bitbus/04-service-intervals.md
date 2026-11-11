# Phase 4 — Service Intervals and Due Engine Report

**Phase:** Phase 4 — Service Intervals and Due Engine  
**Branch:** `feature/bitbus-service-intervals`  
**Base commit:** `32275c0`  
**Status:** Corrective pass in progress. Code corrections for audit items (W1–W13, C1–C5, B2–B14) applied and awaiting owner review; C6 notification payload structure stopped and reported awaiting owner decision (Choice A vs Choice B). Tests and verification remain unverified until executed by the owner in PowerShell.

---

## 1. Summary of Corrective Pass

### 1.1 Security, Scope & Authorization (W1, W9, C1)
- **User Scope on Every Read/Write (W1):** Injected `IUserScopeFilterService` into `ServiceDueService` and `ServiceReplacementService`. Out-of-scope access returns 403 Forbidden on writes (`RecordReplacementAsync`, `RecordMultiItemReplacementAsync`, `RecordBulkBaselineAsync`, `VoidAsync`), 403 on single generator tables, and empty/filtered results on queries (`GetStationMaintenanceTablesAsync`, `GetDueDashboardSummaryAsync`, `GetHistoryAsync`).
- **Scoped Evaluation (B6):** `POST /api/service-due/evaluate` accepts optional `generatorId` and scopes evaluation to generators in the caller's accessible substations.
- **Granular Permissions & Scoped Middleware (W9, C1):** In `PermissionEnforcementMiddleware.cs`, mapped:
  - `ServiceItems` & `ServiceIntervalDefinitions`: GET -> `maintenance.view`, Writes -> `maintenance.configure_service`
  - `ServiceReplacements` & `ServiceDue`: GET -> `maintenance.view`, Writes -> `maintenance.record_service`
  - Enforced single decision point via `ICustomerFeatureService.IsOperationalDataNewModeAsync()`. Restricted the new mode requirement and system owner bypass restriction to the four new service controllers only. Existing Phase 1–3b controllers remain completely unaffected with system owner immediate bypass preserved.
  - Updated `ModuleRegistrySeed.cs` custom actions for `maintenance` module to `"close_permit,configure_service,record_service"`.

### 1.2 Feature Gating & Shared Engine Core (W2, W3, B4)
- **Single Source of Truth for Feature Gate (B4):** Replaced all inline `Value != "legacy"` checks with `ICustomerFeatureService.IsOperationalDataNewModeAsync()`.
  - Defined `CustomerFeatureService.LegacyModeValue = "legacy"` as the single string definition in `src`.
  - `PermissionEnforcementMiddleware`, `AlertGeneratorService`, and `ServiceDueEvaluationService` (both evaluation entry points) now resolve and invoke `IsOperationalDataNewModeAsync()`.
  - Suppressed legacy parts replacement alerts in `AlertGeneratorService` when in new mode.
  - Gated background tenant/Hangfire evaluations in `ServiceDueEvaluationService` to return 0 when not in new mode.
- **Service Record Helper (W3):** Created `ServiceRecordHelper.cs`:
  - Fixed ordering bug: `GetLatestActiveRecordAsync` orders by `OccurredAtUtc DESC, Id DESC` (never by `HoursAtService`).
  - Unified meter reset checks: `HasMeterResetAfterAsync` checks for approved daily closing running hours readings with `IsMeterReset == true` after the last service record.

### 1.3 Service Replacements & Bulk Baselines (C3, C4, W5, B3)
- **Unconfigured Item Recording (C4):** Recording a baseline or replacement for an active item without a model definition is explicitly allowed (`IntervalHoursSnapshot = null`, `IsEarly = false`).
- **Idempotency Protection (C3):** In `ServiceReplacementService`, pre-save validations (negative hours, missing generator/item) execute before creating or acquiring idempotency keys, preventing invalid requests from polluting idempotency state.
- **Multi-Item Replacement (B3, W5):** Added `RecordMultiItemReplacementAsync` (`POST /api/service-replacements/multi`) to record replacements for multiple items at the same operating hours atomically in one transaction.
- **Detailed Bulk Baseline Reporting (W5):** Added `RecordBulkBaselineDetailedAsync` returning per-row success/failure outcomes (`BulkBaselineResultDto`).

### 1.4 Service Due Engine & Notifications (C5, C6, B2, B5, W7, W10)
- **NotConfigured Items in Tables (C5, W10):** `ServiceDueService` returns a row for every active service item. Unconfigured items return `ServiceDueStatus.NotConfigured` with a link to the definitions page (`/maintenance/service/definitions`). The UI distinguishes between "generator has no model" and "model has no definition for this item".
- **Optimized Set-Based Dashboard Query (W10):** `GetDueDashboardSummaryAsync` aggregates due/overdue counts directly from `ServiceDueStates` and builds detailed lists only for affected generators.
- **Evaluation Safety & Notification Severity (B2, B5, W7):** `ServiceDueEvaluationService` persists the evaluated state transition to the database before publishing notifications (preventing concurrent duplicates). Notification severity is derived directly from `ServiceIntervalImportance` and status.

### 1.5 Frontend Hardening & Idempotency (C2, B12, Shared Modal)
- **Client Request ID Lifecycle (C2):** Implemented `clientRequestIdRef` (`useRef(crypto.randomUUID())`) in `GeneratorMaintenanceTable.jsx`, `RecordReplacement.jsx`, and `BulkBaseline.jsx`. The UUID is generated once per form attempt, reused on retries/double-clicks, and renewed only upon successful submission or when the user edits form inputs after a validation error.
- **Unified Modal Visibility Props:** Fixed all 5 occurrences of `isOpen` to `open` in `GeneratorMaintenanceTable.jsx` (record, history, void modals) and `ServiceDefinitionsAdmin.jsx` (definition, item modals).
- **Re-enabled Record Button for NotConfigured (C4):** Removed `disabled={item.status === 5}` so users can baseline or record service on unconfigured items.
- **Maintenance Dashboard Legacy Gating (B12):** In `MaintenanceDashboard.jsx`, the Service Due Engine widget is conditionally rendered only when the endpoint succeeds (`serviceDueSummary !== null`), remaining completely hidden in legacy mode.

### 1.6 Database Model Proof
- Added Phase 4 service intervals and due state scenario to `tests/PostgresModel.Proof/Program.cs`, asserting end-to-end table creation, seeding from migration `008`, entity persistence, and status tracking in PostgreSQL.

---

## 2. Test Coverage & Matrix

### Unit & Integration Test Suites
- `tests/FuelTrack.Tests/Services/ServiceDueCalculatorTests.cs`: 16 domain calculation tests.
- `tests/FuelTrack.Tests/Services/ServiceDueServiceTests.cs`: 10 lifecycle, scoping (with real substation and station returning 403), unconfigured recording (asserting NotConfigured row), multi-item replacement, and idempotency tests.
- `tests/FuelTrack.Tests/Services/CustomerFeatureServiceTests.cs`: 5 feature gate mode tests.
- `tests/FuelTrack.Tests/Middleware/PermissionEnforcementMiddlewareTests.cs`: 30-case matrix covering:
  - Controllers: `OperationalCaptures`, `ServiceItems`, `ServiceIntervalDefinitions`, `ServiceReplacements`, `ServiceDue`
  - Modes: `disabled`, `enabled_legacy`, `enabled_new`
  - Callers: `normal user with permission`, `system owner`
- `frontend/src/pages/GeneratorMaintenanceTable.test.jsx`: Vitest tests for table rendering, NotConfigured item rendering with active button and definition link, clientRequestId reuse on retry, and void dialog opening from history list.
- `frontend/src/pages/ServiceDefinitionsAdmin.test.jsx`: Vitest tests proving add-definition and add-item dialogs open when buttons are clicked.

---

## 3. Decisions Needed from the Owner

1. **P4-5 (Single-Step Immediate Persistence vs Two-Step Review Lifecycle):**
   - Service replacements and initial baselines are committed immediately by authorized users (`maintenance.record_service`) without a Tier-2 approval queue.
   - Any operational error is resolved through a void operation (`VoidAsync`) requiring a mandatory justification, restoring the previous active record.
   - *Decision:* Confirm whether immediate persistence with void-audit fulfills operational requirements, or if a draft/approval review queue is desired for a future phase.

2. **Arabic Naming for Seeded Service Items (FuelFilter & ValveAdjustment):**
   - Migration `008_phase4_service_intervals.sql` seeded the following defaults:
     - `FuelFilter`: `فلتر الوقود` / `Fuel filter`
     - `ValveAdjustment`: `ضبط الصمامات` / `Valve adjustment`
   - *Decision:* Confirm if these terms match local site terminology or if alternative terms (`فلتر الديزل`, `وزن البلوف`) are preferred.

3. **Notification Severity Mapping Matrix:**
   - Severity is mapped from `ServiceIntervalImportance` and `ServiceDueStatus`:
     - `Overdue` + `Critical`/`High` -> `danger`
     - `Overdue` + `Normal`/`Low` -> `warning`
     - `Due` -> `warning`
     - `DueSoon` + `Critical` -> `warning`
     - `DueSoon` + `Normal`/`Low` -> `info`
   - *Decision:* Confirm if this severity mapping aligns with notification dispatch policies.

4. **At-Most-Once Upward Status Alert Policy:**
   - The engine notifies only upon upward status escalation (`Ok -> DueSoon -> Due -> Overdue`) within an active maintenance cycle.
   - Repeated hourly evaluations or subsequent daily closings while remaining in the same due tier do not generate duplicate notifications.
   - Returning to `Ok` upon service recording does not dispatch an alert.
   - *Decision:* Confirm whether at-most-once upward escalation is acceptable, or if a recurring daily reminder is required for unresolved Overdue states.

5. **System Owner Feature Gate Enforcement (404 vs Bypass):**
   - System owners intentionally do NOT bypass customer feature gates on new service controllers (`ServiceItems`, `ServiceIntervalDefinitions`, `ServiceReplacements`, `ServiceDue`).
   - If `operational_data` is disabled or in `legacy` mode, all callers (including SystemOwner) receive `404 Not Found`.
   - *Decision:* Confirm that strict feature gate enforcement (404) for System Owners on new controllers is the intended security and product behavior.

6. **C6 Notification Format Choice (Choice A vs Choice B):**
   - **Context:** The `Notification` table and `CreateNotificationDto` have no structured data/params column (only plain `Title` and `Message`). `Notifications.jsx` only executes static key lookup (`t(message)`) without argument interpolation.
   - **Choice A (Recommended):** Store formatted human-readable text directly in `Message` (e.g. `Generator G-01: Oil Change is overdue by 15.0 h`). It displays directly in `Notifications.jsx` without requiring schema changes or custom interpolation.
   - **Choice B:** Add a structured JSON/payload column to the `Notification` schema, update API DTOs, and extend `Notifications.jsx` to parse parameters and interpolate dynamic i18n keys.

---

## 4. Owner Verification Commands (PowerShell)

Please execute the following commands in PowerShell from `D:\FTM\.worktrees\bitbus-service-intervals`:

```powershell
Set-Location 'D:\FTM\.worktrees\bitbus-service-intervals'

# 1. Backend Build
dotnet build FuelTrack.sln

# 2. Backend Tests
dotnet test FuelTrack.sln --no-build

# 3. Frontend Tests & Build
Set-Location frontend
npm.cmd run test -- --run
npm.cmd run build
Set-Location ..
```

---

## 5. Next Gate
Awaiting owner choice on **C6 (Choice A vs Choice B)** and verification results: `approved phase 4`.
