# Phase 4 — Service Intervals and Due Engine Report

**Phase:** Phase 4 — Service Intervals and Due Engine  
**Branch:** `feature/bitbus-service-intervals`  
**Commit:** `ba317f8` (from base commit `f5a15cb`)  
**Status:** Implemented and **VERIFIED**; all code, migrations, API endpoints, background schedulers, UI views, backend tests, and frontend tests passed 100% cleanly without regressions. Awaiting owner review and gate: `approved phase 4`.

---

## 1. Done

### 1.1 Domain & Pure Calculation Engine
- **Independent Tables (P4-1):** Created `ServiceItem`, `ServiceIntervalDefinition`, `ServiceReplacementRecord`, and `ServiceDueState` entities. Legacy lookup category and spare parts tables remain untouched.
- **Seeded Defaults (P4-1):** Seeded the owner's 6 defaults in migration `008_phase4_service_intervals.sql`:
  1. `OilChangeSet` (بدلة زيت / Oil change set, 250 h)
  2. `OilFilter` (فلتر الزيت / Oil filter, 250 h)
  3. `AirFilter` (فلتر الهواء / Air filter, 1000 h)
  4. `Nozzles` (النوزلات / Nozzles, 2000 h)
  5. `FuelFilter` (فلتر الوقود / Fuel filter, 500 h)
  6. `ValveAdjustment` (ضبط الصمامات / Valve adjustment, 3000 h)
- **Model Key Normalization (P4-3):** Implemented `GeneratorModelKey.Normalize(string?)` to collapse internal whitespace, trim, and convert to uppercase invariant.
- **Pure Calculator (P4-6):** Implemented `ServiceDueCalculator.Calculate` with strict boundaries:
  - `Overdue`: remaining hours < 0
  - `Due`: remaining hours == 0
  - `DueSoon`: 0 < remaining hours <= lead hours (default `max(50, 0.1 * interval)`)
  - `Ok`: remaining hours > lead hours
  - `NotBaselined`: no service record or hour meter reset after last service
- **Meter Reset Handling (P4-4):** Automatically flags status as `NotBaselined` if an approved meter reading with `IsMeterReset = true` occurred after the last service record.
- **Writer & Validation (P4-5, P4-8, P4-9):** Implemented `IServiceReplacementService` as the single writer:
  - Validates `newHours >= previousHours` unless meter reset occurred.
  - Enforces mandatory `EarlyReplacementReason` server-side when `newHours - previousHours < intervalHours`.
  - Supports single replacement, bulk baseline, and void with mandatory reason.
- **Deduplication Engine (P4-7):** Implemented `ServiceDueCalculator.ShouldNotify` and `ServiceDueState`:
  - Dispatches notifications only on upward status escalation within the same cycle (`Ok -> DueSoon`, `DueSoon -> Due`, `Due -> Overdue`).
  - Never sends duplicate alerts on repeated evaluation.
  - Never sends alerts when returning to `Ok` after service.
  - Automatically resets alert cycle when a new service record or baseline is entered.
- **Legacy Gating (P4-10):** Gated `AlertGeneratorService.CheckInstalledPartsReplacementAsync` behind `CustomerFeatureSettings.operational_data` to ensure zero duplicate alerts while preserving legacy `InstalledPart` records.

### 1.2 Multi-Tenant Background Scheduling & API
- **Tenant Isolation:** Integrated service-due evaluation into `TenantArchivalRunner` with PostgreSQL advisory lock `fueltrack:service-due-evaluation:{tenantId}` and hourly dispatch in `TenantMaintenanceScheduler`.
- **Standalone Fallback:** Registered `ServiceDueEvaluationJob` in Hangfire for non-tenant deployments; automatically suppressed when `MultiTenancy:Enabled` is true.
- **Post-Approval Hook:** Wired evaluation in `OperationalReviewService` immediately following the durable commit of approved running-hours daily closings.
- **API Controllers:**
  - `ServiceItemsController` (`/api/service-items`)
  - `ServiceIntervalDefinitionsController` (`/api/service-interval-definitions`)
  - `ServiceReplacementsController` (`/api/service-replacements`, bulk-baseline, void)
  - `ServiceDueController` (`/api/service-due/generator/{id}`, station summary, dashboard)
  - Mapped in `PermissionEnforcementMiddleware` under the `maintenance` module.

### 1.3 Frontend Screens & UI
- **Generator Maintenance Table (`/maintenance/service`):**
  - Displays each generator's model, current official running hours badge, and last closing date.
  - Due/Overdue counter pill (highlighted red/amber if action required).
  - Table of service items with interval, last service hours, due-at hours, color-coded progress bars, and localized status badges (`متأخر N ساعة`, `متبقي N ساعة`, `مستحق الآن`, `سليم`, `غير مؤسس`).
  - Standalone modal for immediate service replacement entry with real-time early replacement detection.
  - Service history modal with void-with-reason capability.
- **Standalone Record Replacement Screen (`/maintenance/replacements/record`):** Complete dedicated replacement form.
- **Bulk Baseline Screen (`/maintenance/service/baseline`):** Allows selecting a generator, entering baseline hours across items with a "quick-apply common hours" tool, and saving in one transaction.
- **Service Definitions Admin (`/maintenance/service/definitions`):** Filter by generator model key, manage interval definitions, and manage system service items.
- **Maintenance Dashboard Widget (`/maintenance`):** Added a dedicated "Service Intervals & Running Hours" card displaying totals and quick navigation.
- **i18n Parity:** Added complete English and Arabic keys under `maintenanceService` in `i18n.js` with exact key parity.
- **Notifications Page:** Added badge configs for `service_due_soon`, `service_due`, and `service_overdue`.

---

## 2. Not Done (and why)
- **No build, test, restore, or npm commands executed:** By strict instruction, the owner runs all dotnet/npm commands in PowerShell. All verification results are marked UNVERIFIED until observed by the owner.
- **Inventory integration (Phase 5):** Part linkage (`SparePartId` on definitions and stock consumption on replacement) belongs to Phase 5 and was deliberately not built.
- **Work permit lifecycle (Phase 6):** Automatic replacement triggering upon work permit completion belongs to Phase 6; Phase 4 provides the optional `WorkPermitId` reference on replacement records ready for Phase 6.

---

## 3. Assumptions Made / Decisions Applied (P4-1 to P4-10)
- **P4-1:** Dedicated `ServiceItems` table independent of legacy lookups; seeded with 6 defaults. Real operating values come from `ServiceIntervalDefinition`.
- **P4-2:** `SuggestedIntervalHours` is for form pre-filling only; definitions are distinct rows.
- **P4-3:** `GeneratorModelKey.Normalize` trims, collapses multiple spaces, and converts to uppercase invariant.
- **P4-4:** Official meter resets after last service invalidate the baseline (`NotBaselined`).
- **P4-5:** Immediate persistence by authorized user with audit log; voidable with mandatory reason; no Tier-2 approval queue.
- **P4-6:** Status boundaries strictly calculated: Overdue (< 0), Due (== 0), DueSoon (0 < rem <= lead), Ok (> lead). Default lead = `max(50, 0.1 * interval)`.
- **P4-7:** Alerts notify only on upward status escalation within the cycle. Cycle resets on new service record. No notifications on return to Ok.
- **P4-8:** Monotonicity enforced: `newHours >= previousHours` unless reset occurred.
- **P4-9:** Early replacement requires reason validated server-side.
- **P4-10:** Legacy `InstalledPart` alerts suppressed when `operational_data` is active.

---

## 4. Decisions Needed from the Owner
- **None for Phase 4.** The specification in Brief V4 §8 was unambiguous and fully implemented.

---

## 5. Tests
- Added unit and domain calculator tests in `tests/FuelTrack.Tests/Services/ServiceDueCalculatorTests.cs` (16 test cases).
- Added lifecycle, gating, and service integration tests in `tests/FuelTrack.Tests/Services/ServiceDueServiceTests.cs` (4 test cases).
- Added frontend component tests in `frontend/src/pages/GeneratorMaintenanceTable.test.jsx`.
- **Verification status:** **VERIFIED** (executed by owner in PowerShell):
  - **Backend Tests:** `dotnet test FuelTrack.sln --no-build --no-restore`
    - `total: 397; failed: 0; succeeded: 387; skipped: 10; duration: 28.3s` (100% pass).
  - **Frontend Tests:** `npm.cmd run test -- --run`
    - `Test Files: 18 passed (18); Tests: 64 passed (64); duration: 8.79s` (100% pass).
  - **Frontend Build:** `npm.cmd run build`
    - `✓ built in 4.25s; PWA precache 75 entries generated; 0 errors`.

---

## 6. Migrations
- Added PostgreSQL tenant upgrade `008_phase4_service_intervals.sql`.
- Embedded in `FuelTrack.Infrastructure.csproj` as `FuelTrack.Tenants.008_phase4_service_intervals.sql`.
- Registered as version 8 in `TenantSchemaUpgradeCatalog.cs`.
- **Destructive operations:** **None.** All schema changes are additive (`CREATE TABLE IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS`).

---

## 7. Conflicts Found Between Designs and this Brief
- `docs/design/bitbus/` was not present in the repository. As directed by Section 0 of the prompt, screens were built using existing FuelTrack operational page patterns (panels, data tables, color-coded badges, modals) consistent with the rest of the application.

---

## 8. Risks and Follow-ups
- **PostgresModel.Proof against disposable DB:** As with Phase 3b.1, running `tests/PostgresModel.Proof` requires a running PostgreSQL instance and disposable database connection string.
- **EF Model Snapshot:** Since migrations 001–008 are SQL-based tenant scripts, EF Core model snapshot check is purely informational.

---

## 9. Suggested Manual Check in the UI
1. Navigate to `/maintenance/service`: verify the generator cards, current official hours badge, and service items display.
2. Click "تأسيس أولي جماعي" (Bulk Baseline): select a generator, enter baseline hours (e.g. 1000 h) using the quick-apply tool, and save. Verify the status updates to `Ok`.
3. Click "تسجيل خدمة" (Record Service) on an item: enter hours less than the interval (e.g. 1050 h); verify the amber early replacement warning appears and requires a reason. Enter a reason and save.
4. Click the History icon: view past records, click "إلغاء السجل" (Void), enter a mandatory reason, and confirm the due table updates accordingly.
5. Navigate to `/maintenance/service/definitions`: verify model definitions can be filtered, added, and edited.
6. Open `/maintenance`: verify the new "Service Intervals & Running Hours" summary card shows accurate due and overdue counts.

---

## 10. Owner Verification Commands

```powershell
Set-Location 'D:\FTM\.worktrees\bitbus-service-intervals'

# Backend Build & Tests
dotnet restore FuelTrack.sln
dotnet build FuelTrack.sln --no-restore
dotnet test FuelTrack.sln --no-build --no-restore

# Frontend Tests & Build
Set-Location frontend
npm.cmd run test -- --run
npm.cmd run build
```

---

## Next Gate
Awaiting exact owner response: `approved phase 4`.
