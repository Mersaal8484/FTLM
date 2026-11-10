# Phase 4 — Service Intervals and Due Engine Report

**Phase:** Phase 4 — Service Intervals and Due Engine  
**Branch:** `feature/bitbus-service-intervals`  
**Base commit:** `32275c0`  
**Status:** Corrective pass complete. All audit items (W1–W13, C1–C6, B2–B14) resolved across backend, frontend, database proof, and test suites. Awaiting owner verification in PowerShell and gate: `approved phase 4`.

---

## 1. Summary of Corrective Pass

### 1.1 Security, Scope & Authorization (W1, W9, C1)
- **User Scope on Every Read/Write (W1):** Injected `IUserScopeFilterService` into `ServiceDueService` and `ServiceReplacementService`. Out-of-scope access returns 403 Forbidden on writes (`RecordReplacementAsync`, `RecordMultiItemReplacementAsync`, `RecordBulkBaselineAsync`, `VoidAsync`), 403 on single generator tables, and empty/filtered results on queries (`GetStationMaintenanceTablesAsync`, `GetDueDashboardSummaryAsync`, `GetHistoryAsync`).
- **Scoped Evaluation (B6):** `POST /api/service-due/evaluate` accepts optional `generatorId` and scopes evaluation to generators in the caller's accessible substations.
- **Granular Permissions & Scoped Middleware (W9, C1):** In `PermissionEnforcementMiddleware.cs`, mapped:
  - `ServiceItems` & `ServiceIntervalDefinitions`: GET -> `maintenance.view`, Writes -> `maintenance.configure_service`
  - `ServiceReplacements` & `ServiceDue`: GET -> `maintenance.view`, Writes -> `maintenance.record_service`
  - Restricted the `Value != "legacy"` check and system owner bypass restriction to the four new service controllers only. Existing Phase 1–3b controllers remain completely unaffected with system owner immediate bypass preserved.
  - Updated `ModuleRegistrySeed.cs` custom actions for `maintenance` module to `"close_permit,configure_service,record_service"`.

### 1.2 Feature Gating & Shared Engine Core (W2, W3, B4)
- **Centralized Feature Gating (W2, B4):** Added `IsOperationalDataNewModeAsync` to `ICustomerFeatureService` and implemented in `CustomerFeatureService`. Returns `true` only when `operational_data` is enabled and its value is not `"legacy"`.
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
- **Evaluation Safety & Notification Severity (B2, B5, W7):** `ServiceDueEvaluationService` persists the evaluated state transition to the database before publishing notifications (preventing concurrent duplicates). Notification severity is derived directly from `ServiceIntervalImportance` and status, and notification messages format generator name, item name, and hours directly.

### 1.5 Frontend Hardening & Idempotency (C2, B12)
- **Client Request ID Lifecycle (C2):** Implemented `clientRequestIdRef` (`useRef(crypto.randomUUID())`) in `GeneratorMaintenanceTable.jsx`, `RecordReplacement.jsx`, and `BulkBaseline.jsx`. The UUID is generated once per form attempt, reused on retries/double-clicks, and renewed only upon successful submission or when the user edits form inputs after a validation error.
- **Re-enabled Record Button for NotConfigured (C4):** Removed `disabled={item.status === 5}` so users can baseline or record service on unconfigured items.
- **Maintenance Dashboard Legacy Gating (B12):** In `MaintenanceDashboard.jsx`, the Service Due Engine widget is conditionally rendered only when the endpoint succeeds (`serviceDueSummary !== null`), remaining completely hidden in legacy mode.

### 1.6 Database Model Proof
- Added Phase 4 service intervals and due state scenario to `tests/PostgresModel.Proof/Program.cs`, asserting end-to-end table creation, seeding from migration `008`, entity persistence, and status tracking in PostgreSQL.

---

## 2. Test Coverage & Verification

### Unit & Integration Test Suites
- `tests/FuelTrack.Tests/Services/ServiceDueCalculatorTests.cs`: 16 domain calculation tests.
- `tests/FuelTrack.Tests/Services/ServiceDueServiceTests.cs`: 10 lifecycle, scoping, unconfigured recording, multi-item replacement, and idempotency tests.
- `tests/FuelTrack.Tests/Services/CustomerFeatureServiceTests.cs`: 5 feature gate mode tests.
- `tests/FuelTrack.Tests/Middleware/PermissionEnforcementMiddlewareTests.cs`: 5 permission mapping, legacy gate, and system owner bypass tests.
- `frontend/src/pages/GeneratorMaintenanceTable.test.jsx`: 3 Vitest tests (table rendering, NotConfigured item rendering with active button and definition link, and double-click/retry clientRequestId reuse).

---

## 3. Owner Verification Commands (PowerShell)

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

## 4. Next Step
Upon successful execution of the verification commands above, reply with:
`approved phase 4`
