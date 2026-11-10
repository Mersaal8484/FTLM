# Phase 3b.1 — Fuel & Load Safety Correction Report

**Branch:** `feature/bitbus-fuel-load-fixes`

**Base commit:** `ef20941` (`fix(operations): complete phase 3b fuel and load safety patch`)

**Status:** implemented; backend and frontend verification passed when run by the owner; PostgreSQL proof remains pending because Docker Desktop was not running; awaiting `approved phase 3b.1`.

## 1. Done

- Moved multi-tenant operating-limit evaluation onto the existing `TenantMaintenanceScheduler` / `TenantArchivalRunner` path. It selects only active tenants with active subscriptions, resolves a tenant connection, uses a PostgreSQL advisory lock per tenant/task, and skips unknown or suspended tenants.
- When multi-tenancy is enabled, the legacy Hangfire recurring operating-limit job is now explicitly removed from Hangfire storage. The legacy recurring job remains registered only for non-tenant deployments.
- Added a PostgreSQL proof scenario that drives `TenantMaintenanceScheduler.RunOnceAsync`: a breach in one active tenant creates exactly one operating-limit episode, while another tenant remains unchanged.
- Corrected fuel low-stock evaluation everywhere it is generated to use `max(0, actual stock − derived dead stock)`. This includes the Hangfire fuel alert job and the notification generator.
- Preserved the selected tank dead-stock input source: litre input clears the centimetre source; centimetre input clears the litre source. The alternate value is always derived through the approved tank geometry, and legacy tank DTOs expose the derived litre value for compatibility.
- Added tenant upgrade **007** for `FuelTanks.DeadStockLevelCm` and `FuelTanks.DeadStockInputUnit`. Upgrade 006 was already committed, so it was not edited.
- Daily-tank dead-stock episodes now use the captured reading snapshot, evaluate only official readings, and are reevaluated immediately after a Tier-2 approval commits. The notification is translated at display time and remains once per open episode.
- Replaced the consistency-warning notification presentation with Arabic/English keys. The kW/kVA/PF denominator remains `kVA`, and repeated-value count remains a station setting (default 6).
- Updated fuel dashboard, stock-summary, tank list, and tank detail projections so centimetre-source dead stock remains derived correctly instead of appearing as zero or null.

## 2. Not done (and why)

- No backend/frontend build, test, restore, package installation, or migration command was executed by the agent, by explicit owner instruction.
- No Phase 4 work was started. No dependency, `package.json`, or lockfile change was made.

## 3. Tests added or strengthened

Phase 3b.1 adds five verification cases:

1. `PostgresModel.Proof`: active-tenant scheduler evaluation creates one operating-limit episode and does not affect the other tenant.
2. `AlertGeneratorServiceTests.CheckFuelStockLevelsAsync_UsesUsableStockAfterDeadStock`: low-stock alert uses available fuel after dead stock.
3. `FuelLoadSafetyServiceTests.EvaluateTankDeadStock_IgnoresPendingReading_AndUsesCapturedSnapshot`: pending readings do not alert; an official reading uses its recorded dead-stock snapshot.
4. `TankGeometryTests.DeadStockSource_InCmOrLitres_ProducesTheSameUsableThreshold`: the stored centimetre and litre input forms yield the same usable threshold.
5. `OperationalReviewServiceTests.ApproveSubmission_EvaluatesDailyTankDeadStockAfterCommit`: a Tier-2 tank reading opens its safety episode only after approval commits.

The pre-existing Phase 3b tests for authorization scope, late-arriving segment recalculation, warning boundaries, and repeated readings remain intact.

Owner verification on 2026-11-10:

- `dotnet build FuelTrack.sln --no-restore`: passed.
- `dotnet test FuelTrack.sln --no-build --no-restore`: 370 total, 360 passed, 0 failed, 10 skipped.
- `npm.cmd run test -- --run`: 17 files and 63 tests passed, 0 failed.
- `npm.cmd run build`: passed; PWA service-worker assets generated.
- PostgreSQL model proof: not run because the local Docker Desktop daemon was unavailable. This remains an approval follow-up.

## 4. Migration and tenant delivery

- `006_phase3b_patch.sql` remains immutable and continues to contain the Phase 3b episode/settings changes.
- New `007_phase3b_1_corrections.sql` is embedded in `FuelTrack.Infrastructure.csproj` and registered after 006 in `TenantSchemaUpgradeCatalog`.
- Platform-managed tenant upgrades apply catalogued upgrades to each tenant schema; this patch does not reset, alter manually, or perform destructive operations on shared or production databases.
- Destructive operations: none.

## 5. Assumptions and decisions applied

- PostgreSQL remains the approved Bitbus multi-tenant database runtime.
- A stored tank level is authoritative only when its status is official (`Recorded` or `Approved`); a rejected or pending reading never opens a safety episode.
- Existing reports and legacy tank clients receive a derived litre value even when centimetres are the persisted source, preserving their contract without persisting both source values.

## 6. Risks and follow-ups

- Run the mandatory owner verification before approval, particularly the PostgreSQL proof, because it validates the connection/resolution/advisory-lock path unavailable to the in-memory tests.
- Notification text is translated by the frontend when a notification title/message is an `operational.*` key; non-key legacy notifications retain their stored text.

## 7. Owner verification commands

```powershell
Set-Location 'D:\FTM\.worktrees\bitbus-fuel-load-fixes'
dotnet restore FuelTrack.sln
dotnet build FuelTrack.sln --no-restore
dotnet test FuelTrack.sln --no-build --no-restore

# PostgreSQL proof needs a disposable PostgreSQL database connection string.
$env:PROOF_DATABASE = '<owner-provided disposable PostgreSQL connection string>'
dotnet run --project tests/PostgresModel.Proof --no-build --no-restore

Set-Location frontend
npm.cmd run test -- --run
npm.cmd run build
```

## 8. Suggested manual UI check

1. In Tank Setup, save a centimetre dead-stock level, reload the page, and confirm centimetres remain the selected input while litres are derived.
2. Submit a Tier-2 daily-tank reading at the dead-stock threshold; approve it; confirm one localized alert appears. Approve a recovery reading and then a new threshold reading to confirm a new episode begins.
3. Enable multi-tenancy, restart the API, and confirm the legacy Hangfire `operating-limit-evaluation` recurring entry is absent while tenant maintenance creates only the affected tenant's episode.

## Next gate

Do not start Phase 4. Await the exact owner response: `approved phase 3b.1`.
