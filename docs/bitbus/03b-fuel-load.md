# Phase 3b.1 — Fuel & Load Safety Patch

Phase: Phase 3b.1 (Fuel & Load Safety Remediation Patch)   Branch: feature/bitbus-fuel-load-fixes   Commit: a2d5886

## 1. Done

- **Operating limits scheduling under multi-tenancy:**
  - Implemented hourly evaluation in `TenantMaintenanceScheduler` via `TenantArchivalRunner.EvaluateOperatingLimitsAsync` matching token cleanup and archival patterns.
  - Guarded with PostgreSQL advisory lock `fueltrack:operating-limit-evaluation:{tenantId}`.
  - Filtered to active tenants with active subscriptions only; skips suspended or unknown tenants.
  - Disabled legacy Hangfire `OperatingLimitEvaluationJob` recurring job registration when `MultiTenancy:Enabled` is true in `Program.cs`.
- **Dead stock ("النقطة الميتة"):**
  - Updated `FuelStockAlertJob` to calculate usable fuel as `usable = max(0, ActualStockLiters - DeadStockLiters)` against `MinAlertLiters`.
  - Added `TankDeadStockEpisode` entity and database mapping with open-episode index (`IX_TankDeadStockEpisodes_Tank_Open`).
  - Implemented `EvaluateTankDeadStockAsync` in `FuelLoadSafetyService` and triggered from `OperationalCaptureService` on tank level reading receipt. Emits one translated notification (`operational.warnings.dailyTankAtDeadStock`) per episode; subsequent readings at dead stock do not re-alert; level recovery resolves the episode.
  - Updated `TankSetup.jsx` to support entering dead stock either by volume (litres) or by height (cm), storing only the entered value and displaying the derived geometry counterpart in preview.
- **Consistency warnings:**
  - Replaced hardcoded warning messages in `FuelLoadSafetyService.AddConsistencyWarningsAsync` with i18n keys: `operational.warnings.dailyTankAtDeadStock`, `operational.warnings.activeLoadKvaPowerFactorMismatch`, `operational.warnings.threePhaseKvaMismatch`, and `operational.warnings.repeatedActiveLoad`.
  - Added translations in `frontend/src/i18n.js` with full Arabic and English key parity.
  - Fixed formula denominator: `|kW - kVA × PF| / kVA > 10%`.
  - Added configurable station setting `RepeatedValueWarningCount` (default 6) on `StationOperationalSettings`.
- **PostgreSQL tenant upgrade 006:**
  - Created `src/FuelTrack.Platform/MultiTenancy/006_phase3b_patch.sql` creating `TankDeadStockEpisodes` and adding `RepeatedValueWarningCount` to `StationOperationalSettings`.
  - Embedded as resource in `FuelTrack.Infrastructure.csproj` and registered version 6 in `TenantSchemaUpgradeCatalog.cs`.
- **Acceptance and regression tests written (without running):**
  - Added 10 backend tests across `FuelLoadSafetyServiceTests`, `FuelStockAlertJobTests`, and `TankGeometryTests`.
  - Added 1 frontend test in `TankSetup.test.jsx`.

## 2. Not done (and why)

- No build or test execution was run by the agent, adhering to strict user constraint. Verification commands are left for owner execution in PowerShell.
- No npm/package dependencies modified; lockfiles untouched.

## 3. Assumptions made / decisions applied

- Dead stock volume is derived from apex liquid level using conical cylinder geometry (`TankGeometry.CalculateLitres` and `CalculateLiquidLevelCm`).
- Dead stock episode alert publishes scoped notification under `"operational_data"` module using the translated message key.
- Operating limit evaluation runs hourly in tenant background maintenance and leaves the legacy Hangfire path for non-tenant deployments.

## 4. Decisions needed from the owner

- Confirm if alert notification email delivery for `TankDeadStockEpisode` should be added in addition to the scoped in-app notification.

## 5. Tests

11 tests were written without running them:
1. `FuelStockAlertJobTests.ExecuteAsync_LowTanks_ComparesUsableStockAfterDeadStock`: verifies low fuel alert triggers based on usable stock (`Actual - DeadStock <= MinAlert`).
2. `TankGeometryTests.CalculateLiquidLevelCm_DerivedFromLitres_MatchesOriginalLevel`: verifies height derivation round-trip from litres.
3. `TankGeometryTests.DeadStockInput_InCmOrLitres_ProducesEquivalentCalculatedVolume`: verifies cm vs litres input equivalence.
4. `FuelLoadSafetyServiceTests.FuelServices_UserOutsideScope_ReturnsNotFoundOrForbidden`: verifies fuel consumption and reconciliation scope filters reject unauthorized user.
5. `FuelLoadSafetyServiceTests.FuelConsumption_LateArrivingReading_RecalculatesSegmentsCorrectly`: verifies dynamic segment recalculation upon late reading arrival.
6. `FuelLoadSafetyServiceTests.EvaluateTankDeadStock_CreatesEpisodeAndAlertOnce_ResolvesWhenRecovered`: verifies one alert per episode, deduplication, and resolution upon level recovery.
7. `FuelLoadSafetyServiceTests.ConsistencyWarnings_ActiveLoadVsKvaPf_EvaluatesAgainstKvaDenominator`: boundary test for 10% tolerance against kVA denominator.
8. `FuelLoadSafetyServiceTests.ConsistencyWarnings_ThreePhaseKva_Evaluates15PercentBoundary`: boundary test for 15% tolerance on three-phase calculation.
9. `FuelLoadSafetyServiceTests.ConsistencyWarnings_RepeatedActiveLoad_5DoesNotWarn_6Warns_AndUsesStationSetting`: verifies 5 identical readings do not warn while 6th warns, honoring station setting.
10. `FuelLoadSafetyServiceTests.EvaluateOperatingLimits_MultiTenantIsolation_BreachInOneTenantDoesNotAffectOther`: verifies tenant isolation in limit evaluation where tenant A breach does not affect tenant B.
11. `TankSetup.test.jsx`: verifies dead stock mode toggle between litres and cm and payload submission.

## 6. Migrations / destructive operations

- PostgreSQL tenant upgrade `006_phase3b_patch.sql` added:
  - Table `"TankDeadStockEpisodes"` with indexes `"IX_TankDeadStockEpisodes_Tank_Open"` and `"IX_TankDeadStockEpisodes_TriggerReading"`.
  - Column `"StationOperationalSettings"."RepeatedValueWarningCount"` integer DEFAULT 6.
- Destructive operations: None.

## 7. Conflicts found between designs and this brief

- None. UI matches operational dark/teal shell and translation parity is preserved.

## 8. Risks and follow-ups

- Ensure migration 006 is applied via tenant upgrade runner before executing operational capture for daily tanks.

## 9. Suggested manual check in the UI

1. Open Tank Setup, switch dead stock mode to cm, enter 25 cm, click Preview, and verify derived litres and usable volume match.
2. Ingest daily tank reading at or below dead stock; verify single notification appears and reading is recorded.
3. Ingest 6 consecutive identical active load readings and confirm warning appears on 6th reading only.
