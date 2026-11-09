# Phase 2.1 Follow-up, Phase 3, and Phase 3b Report

This report follows the Phase report template in `CODEX_BRIEF_V4.md`. Status is based on the checked-in phase reports and the remediated `feature/bitbus-fuel-load-fixes` worktree.

## Completion and branch/commit status

| Phase | Status | Branch | Commit |
|---|---|---|---|
| Phase 2.1 follow-up | Complete; phase report and verification recorded | `feature/bitbus-capture-fixes` | `71d8aebb3cd9bc7d88eb416d06a60dc391495bee` |
| Phase 3 | Complete; phase report records backend/frontend verification | `feature/bitbus-review` | `ea5565e2219cdf2f97c16a7c323b5c465dee0cb7` |
| Phase 3b & 3b.1 | Complete; remediation patch and acceptance tests applied | `feature/bitbus-fuel-load-fixes` | `a2d5886` |

Phase 4 has **not** been started.

## Phase 2.1 follow-up

**Branch:** `feature/bitbus-capture-fixes`
**Commit:** `71d8aebb3cd9bc7d88eb416d06a60dc391495bee`

1. **Done:** Central approval policy and official-data predicate; Tier-1 capture/void behavior; tank event structure; photo stamping and reuse references; minute-normalized capture/device time; per-line idempotency results and offline-ready `AwaitingPhoto`; tank geometry strategy and preview API; visual shell cleanup; PostgreSQL tenant schema upgrade integration.
2. **Not done:** Existing tenant schema upgrade orchestration, an approved self-hosted font, removal of legacy identifiers/assets, and a production-ready non-tenant PostgreSQL baseline/cutover.
3. **Assumptions / decisions:** PostgreSQL is the Bitbus tenant runtime; historical EF migrations remain untouched; Tier-1 approval override is station-configurable; the tank is a vertical cylinder with a conical bottom.
4. **Owner decisions:** Approve how and when existing tenants are upgraded; provide/license a self-hosted font or approve system fonts; decide whether to remove remaining legacy identifiers/assets; decide the non-tenant PostgreSQL cutover.
5. **Tests:** `ReadingApprovalPolicyTests`, `TankGeometryTests`, `OperationalCaptureServiceTests`, and `photoStamp.test.js`.
6. **Migrations:** `002_bitbus_operational_capture.sql` (tenant schema upgrade v2).
7. **Design conflicts:** No material capture-flow conflict recorded.
8. **Risks:** Existing tenants need controlled migration orchestration.
9. **Suggested manual check:** Submit Tier-1 and Tier-2 captures, retry the same device line, and verify only Tier-2 is reviewable; preview tank calibration and stamped photo.

## Phase 3

**Branch:** `feature/bitbus-review`
**Commit:** branch tip `ea5565e2219cdf2f97c16a7c323b5c465dee0cb7`

1. **Done:** Tier-2 review queue and approve/reject paths; approver/submission checks; meter deltas; energy-balance API and board; scoped photo access; measured internal-consumption balance; bilingual UI.
2. **Not done:** Solar readings remain a graceful placeholder until the Solar phase.
3. **Assumptions / decisions:** Internal consumption is measured only when a station designates its meter; otherwise the balance remains provisional.
4. **Owner decisions:** No new Phase 3 decision is recorded beyond those in Phase 2.1.
5. **Tests:** `OperationalReviewServiceTests`, `MeterDeltaServiceTests`, `EnergyBalanceServiceTests`, `OperationalReview.test.jsx`, and `EnergyBalanceBoard.test.jsx`.
6. **Migrations:** `003_phase3_review_columns.sql` (tenant schema upgrade v3).
7. **Design conflicts:** No material conflict recorded. Solar is not implemented in this phase.
8. **Risks:** Ensure v3 is applied via the approved Platform-managed tenant process.
9. **Suggested manual check:** Submit a Tier-2 line, approve/reject it as a different user, then compare the energy balance with and without a configured internal-consumption meter.

## Phase 3b & Phase 3b.1 Remediation Patch

**Branch:** `feature/bitbus-fuel-load-fixes`
**Report details:** See [`docs/bitbus/03b-fuel-load.md`](./03b-fuel-load.md).

1. **Done:**
   - Multi-tenant hourly operating-limit evaluation under `TenantMaintenanceScheduler` with advisory locking and active subscription gating.
   - Usable fuel calculation `max(0, Actual - DeadStock)` in `FuelStockAlertJob`.
   - `TankDeadStockEpisode` tracking and single notification per episode via `FuelLoadSafetyService.EvaluateTankDeadStockAsync`.
   - `TankSetup.jsx` dual-mode input (litres or cm) with derived preview display.
   - i18n keys for consistency warnings and dead stock in `frontend/src/i18n.js` with full parity.
   - Denominator fix to kVA in kW vs kVA×PF consistency check.
   - Configurable `RepeatedValueWarningCount` on station settings.
   - PostgreSQL tenant upgrade `006_phase3b_patch.sql` embedded and registered in catalog.
   - Full test coverage added for all acceptance items (11 tests added without running).
2. **Not done:** Test and build execution by agent (reserved for owner execution).
3. **Migrations:** `004_phase3b_fuel_load.sql`, `005_phase3b_operational_indicators.sql`, `006_phase3b_patch.sql`.

## Verification commands (run in order)

Run from PowerShell:

```powershell
Set-Location 'D:\FTM\.worktrees\bitbus-fuel-load-fixes'
dotnet build FuelTrack.sln
dotnet test FuelTrack.sln --no-build
Set-Location 'D:\FTM\.worktrees\bitbus-fuel-load-fixes\frontend'
npm.cmd run test -- --run
npm.cmd run build
```
