# Phase 2.1 Follow-up, Phase 3, and Phase 3b Report

This report follows the Phase report template in `CODEX_BRIEF_V4.md`. Status is based on the checked-in phase reports and the current `feature/bitbus-fuel-load` worktree. Phase 3b source changes are present locally but are not committed or verified.

## Completion and branch/commit status

| Phase | Status | Branch | Commit |
|---|---|---|---|
| Phase 2.1 follow-up | Complete; phase report and verification recorded | `feature/bitbus-capture-fixes` | `71d8aebb3cd9bc7d88eb416d06a60dc391495bee` (report); implementation `43fc062946743f29311142efc130d34376cdc81a` |
| Phase 3 | Complete; phase report records backend/frontend verification | `feature/bitbus-review` | `ea5565e2219cdf2f97c16a7c323b5c465dee0cb7` (branch tip); implementation `18cc1f68fec80d720368457d3e27e8662ecf619e`; report `449636abb77146165f60ca296b06f32099b6b048` |
| Phase 3b | **Not complete**; changes remain uncommitted and requested acceptance tests are absent | `feature/bitbus-fuel-load` | No Phase 3b commit. Current base is `ea5565e2219cdf2f97c16a7c323b5c465dee0cb7`; worktree has modified and untracked files. |

Phase 4 has **not** been started. This report contains only a Phase 4 plan.

## Phase 2.1 follow-up

**Branch:** `feature/bitbus-capture-fixes`
**Commit:** `71d8aebb3cd9bc7d88eb416d06a60dc391495bee` (report; implementation commit is listed above)

1. **Done:** Central approval policy and official-data predicate; Tier-1 capture/void behavior; tank event structure; photo stamping and reuse references; minute-normalized capture/device time; per-line idempotency results and offline-ready `AwaitingPhoto`; tank geometry strategy and preview API; visual shell cleanup; PostgreSQL tenant schema upgrade integration.
2. **Not done:** Existing tenant schema upgrade orchestration, an approved self-hosted font, removal of legacy identifiers/assets, and a production-ready non-tenant PostgreSQL baseline/cutover.
3. **Assumptions / decisions:** PostgreSQL is the Bitbus tenant runtime; historical EF migrations remain untouched; Tier-1 approval override is station-configurable; the tank is a vertical cylinder with a conical bottom.
4. **Owner decisions:** Approve how and when existing tenants are upgraded; provide/license a self-hosted font or approve system fonts; decide whether to remove remaining legacy identifiers/assets; decide the non-tenant PostgreSQL cutover.
5. **Tests:** `ReadingApprovalPolicyTests`, `TankGeometryTests`, `OperationalCaptureServiceTests`, and `photoStamp.test.js`. The existing report records `dotnet build` successful, `dotnet test` 310 passed/0 failed/10 skipped, frontend 52 passed, and frontend build successful.
6. **Migrations:** `002_bitbus_operational_capture.sql` (tenant schema upgrade v2). No database migration was executed and no destructive operation was performed.
7. **Design conflicts:** No material capture-flow conflict recorded; supplied designs do not include a distributable licensed font.
8. **Risks:** Existing tenants need controlled migration orchestration. The legacy Hangfire SQL Server storage is not a suitable default for a PostgreSQL-only deployment.
9. **Suggested manual check:** Submit Tier-1 and Tier-2 captures, retry the same device line, and verify only Tier-2 is reviewable; preview tank calibration and stamped photo.

## Phase 3

**Branch:** `feature/bitbus-review`
**Commit:** branch tip `ea5565e2219cdf2f97c16a7c323b5c465dee0cb7`; implementation and report commits are listed above.

1. **Done:** Tier-2 review queue and approve/reject paths; approver/submission checks; meter deltas; energy-balance API and board; scoped photo access; measured internal-consumption balance; bilingual UI.
2. **Not done:** Solar readings remain a graceful placeholder until the Solar phase; Phase 3b fuel and load safety is tracked separately.
3. **Assumptions / decisions:** Internal consumption is measured only when a station designates its meter; otherwise the balance remains provisional and the threshold alert is suppressed. Audit history uses the existing audit interceptor.
4. **Owner decisions:** No new Phase 3 decision is recorded beyond those in the Phase 2.1 report; the existing font and tenant-migration decisions remain open.
5. **Tests:** `OperationalReviewServiceTests`, `MeterDeltaServiceTests`, `EnergyBalanceServiceTests`, `OperationalReview.test.jsx`, and `EnergyBalanceBoard.test.jsx`. Representative named tests include `ApproveSubmission_RejectsWhenApproverIsSubmitter`, `GetPendingSubmissions_FiltersToScope`, `GetDailyDelta_MeterReset_NullDelta`, and `GetStationBalance_WithInternal_ShowsMeasuredDiff`. The Phase 3 report records build success, 281 of 291 backend tests passing (the report does not categorize the remaining 10), 59 frontend tests passing, and frontend build success.
6. **Migrations:** `003_phase3_review_columns.sql` (tenant schema upgrade v3), adding review fields/indexes and submission/internal-meter fields. No production tenant migration was executed.
7. **Design conflicts:** No material conflict recorded. Solar is not implemented in this phase.
8. **Risks:** Ensure v3 is applied via the approved Platform-managed tenant process; solar remains absent; broader production compatibility concerns remain as documented in Phase 2.1.
9. **Suggested manual check:** Submit a Tier-2 line, approve/reject it as a different user, then compare the energy balance with and without a configured internal-consumption meter.

## Phase 3b

**Branch:** `feature/bitbus-fuel-load`
**Commit:** No Phase 3b commit. Current branch base: `ea5565e2219cdf2f97c16a7c323b5c465dee0cb7`.

1. **Done locally, not yet verified/committed:** Partial fuel-consumption/reconciliation APIs; dead-stock helper; overload event handling on capture; operating-limit evaluation; daily-log and CSV endpoints; tenant schema-upgrade catalog entry.
2. **Not done:** Required acceptance tests and verification; frontend refill/transfer and tank-configuration/setup-preview screens; full operational log UI/export and missing-data/estimated-chart UX. The current API is not evidence that these brief requirements are complete.
3. **Assumptions / decisions:** The implemented overload threshold uses generator rated kW; equality with rated capacity is explicitly not an overload. Operating limits are evaluated using the latest recorded/approved generator reading.
4. **Owner decisions:** Resolve the outstanding Phase 2.1 deployment/font decisions. Confirm the production Hangfire storage/queue approach before enabling scheduled work in the PostgreSQL tenant deployment.
5. **Tests:** **No Phase 3b tests have been added.** In particular, none of the required fuel fixture, refill, dead-stock, overload-boundary, or notification-episode cases is currently covered. Phase 3's existing internal-consumption balance test remains present.
6. **Migrations:** `004_phase3b_fuel_load.sql` is currently untracked; it adds `GeneratorOperatingLimits`, `OverloadEvents`, and `OperatingLimitEpisodes`. The modified `TenantSchemaUpgradeCatalog` registers it as version 4. No database migration was executed; no destructive operation is indicated.
7. **Design conflicts:** No Phase 3b UI changes are present; the requested screen flows remain outstanding.
8. **Risks:** The 3b worktree is dirty and has no phase commit. Its build/tests have not been run. `FuelLoadSafetyService` duplicates literal `Recorded`/`Approved` checks (and declares a local `Official` helper) instead of consistently using `ReadingApprovalPolicy.IsOfficial`; therefore the requirement that every calculation use the shared predicate is not met. The required SQL Server Hangfire storage is conditional and is not registered when multi-tenancy is enabled.
9. **Suggested manual check after implementation and tests:** Exercise fuel readings/refills in a disposable tenant, confirm consumption versus refill reconciliation, then cross the overload/limit thresholds and inspect event/notification de-duplication.

### Requested requirement check

| Requirement | Current evidence | Test coverage |
|---|---|---|
| 300/180/400/250 fixture gives 270 L consumption and 220 L added | No fixture or verified result found | **Absent** |
| Two refills in one day | No explicit implementation test found | **Absent** |
| Unregistered refill detection | A routine level increase emits an unregistered-refill warning in `FuelLoadSafetyService` | **No test** |
| Dead stock: 650 L measured - 100 L dead = 550 L usable; consumption unchanged | `TankVolumeService.UsableLitres` computes the difference; fuel consumption uses level deltas and does not subtract dead stock | **No fixture/test** |
| Exactly 100% is not overload | Comparison is `observed > ActualPowerKW` | **No boundary test** |
| One overload notification per episode | Existing open event is updated; notification is published only when a new episode is inserted | **No episode/notification test** |
| Internal-consumption balance | Implemented in Phase 3 | `GetStationBalance_WithInternal_ShowsMeasuredDiff` |
| `IsOfficial` used by every calculation | Meter delta and energy balance use it; Phase 3b fuel queries use inline status checks | **Not satisfied** |
| Tier-1 absent from review queue | Queue filters submissions to `Pending`; Tier-1 is ingested as `Recorded` | Scope test exists, but **no explicit Tier-1 exclusion test** |

### Background jobs and storage

- **Overload:** evaluated synchronously on receipt through `OperationalCaptureService`; it is not a Hangfire recurring job.
- **Operating limits:** `OperatingLimitEvaluationJob` is registered hourly in Hangfire.
- **Storage:** when Hangfire is enabled, it uses SQL Server storage from `DefaultConnection`, schema `Hangfire`. Hangfire registration is gated off when `MultiTenancy:Enabled` is true (and also in Development/Testing); therefore these schedules are not wired for the PostgreSQL multi-tenant production path.

## Verification commands (run in order)

These are verification commands only; they do not apply migrations. Run them from PowerShell:

```powershell
Set-Location 'D:\FTM\.worktrees\bitbus-fuel-load'
dotnet build FuelTrack.sln
dotnet test FuelTrack.sln --no-build
Set-Location 'D:\FTM\.worktrees\bitbus-fuel-load\frontend'
npm.cmd run test -- --run
npm.cmd run build
```

The commands do not make Phase 3b complete by themselves; add and pass the missing acceptance tests first.

## Phase 4 plan only (no implementation started)

After explicit owner approval of Phase 3b, create `feature/bitbus-service-intervals` for the six seeded service items and per-generator maintenance table view from brief section 8. Add calculator-boundary tests (exact interval, one hour before/after, missing baseline, hours regression), early-replacement validation, and alert de-duplication. Verify that an oil-set replacement at X hours produces the correct next-due state after the next approved hours reading. Do not start this phase until Phase 3b is complete and approved.
