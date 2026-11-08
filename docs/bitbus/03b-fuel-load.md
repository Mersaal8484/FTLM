# Phase 3b — Fuel & Load Safety

**Branch:** `feature/bitbus-fuel-load`  
**Status:** implementation complete; ready for owner verification

## Delivered

### 1. Fuel Accounting (Computed on Read, Never Stored)
- `FuelConsumptionService` / `FuelLoadSafetyService`:
  - Computes segment consumption from daily-tank level drops between events: litres after previous event - litres before next event.
  - Refills are excluded from consumption (`RefillLitres` tracked separately).
  - Unregistered refills (level rise between two routine `Level` events without refill event) detected and flagged with a warning.
  - Pending refills (Tier-2 refills awaiting approval) are explicitly recognized as "Refill awaiting approval; consumption excluded until approved" rather than misclassified as unregistered refills. Official consumption and added figures exclude them until approved.
  - Dead stock (`TankVolumeService.UsableLitres`): usable litres = measured litres - dead stock (clamped at 0). Constant cancels in segment deltas, so consumption is identical with or without dead stock. Warning emitted when level is at or below dead stock.
- `FuelReconciliationService`:
  - Reconciles main-tank withdrawals (`TransferOut`) vs sum of daily-tank top-ups (`TransferIn`) vs generator consumption.
  - Computes variance and raises warning flags when variance exceeds station threshold (default 10%).

### 2. Central Approval Policy & Official Status Set
- `ReadingApprovalPolicy.OfficialStatuses` (`[Recorded, Approved]`):
  - Shared collection used across all calculation services in EF Core LINQ queries (`ReadingApprovalPolicy.OfficialStatuses.Contains(...)`) and in-memory evaluations.
  - Removed private status helpers and inline status checks.
  - Added architectural test (`Architecture_CalculationServices_DoNotContainInlineStatusChecks_UseReadingApprovalPolicy`) ensuring no inline status checks exist in calculation services.

### 3. Load, Overload, Limits, and Consistency
- Overload Episode Tracking:
  - Overload threshold configurable per station (`StationOperationalSettings.OverloadThresholdPercent`, default 100%).
  - Exactly 100% of rated kW is strictly NOT an overload; strictly above is an overload.
  - One notification per overload episode via `INotificationService`. Subsequent readings in the same episode update peak percentage without duplicate notifications.
  - Hysteresis: an episode resolves only after N consecutive normal readings (`StationOperationalSettings.OverloadHysteresisReadings`, default 3).
  - `ResolvedAtUtc` is stamped with the reading's `CapturedAtUtc` time (not `UtcNow`).
  - Out-of-order / older readings are ignored.
  - Notification failures are logged via `ILogger` rather than silently swallowed.
- Operating Limits Evaluation:
  - Batch queries across all limits and generators (no per-limit N+1).
  - Stored watermark (`LastEvaluatedCapturedAtUtc`, `LastEvaluatedReadingId`) on `GeneratorOperatingLimit` ensures every new Tier-1 reading is evaluated chronologically.
- Consistency Diagnostics:
  - Consecutive repeated values: warns only when 6 consecutive recent readings share the exact same active load.
  - Three-phase check: flags when `|kVA - (v3 · V · A / 1000)| / kVA > 15%`.
  - Active load vs apparent load check: flags when `|kW - (kVA · PF)| / kVA > 10%`.

### 4. Operational Daily Log & Reconciliation Reports
- `GET /api/fuel-load-safety/generators/{id}/daily-log/{date}`: returns all hourly operational parameters (Active Load, Apparent Load, PF, Frequency, Voltage, Current, Oil Pressure, Water Temperature, Oil Temperature, Peak Load, Warnings/Markers).
- `GET /api/fuel-load-safety/generators/{id}/daily-log/{date}.csv`: direct CSV export of all hourly operational parameters.
- `GET /api/fuel-load-safety/substations/{id}/reconciliation`: complete site reconciliation summary.

### 5. Approval Concurrency & Tier-1 Isolation
- In `OperationalReviewService`:
  - Approval and rejection are conditional on `Status == Pending`.
  - Per-subject lock (`ConcurrentDictionary<string, SemaphoreSlim>` keyed by subject) ensures serialized monotonicity validation on meter readings.
  - Verified Tier-1 readings are completely excluded from review queue (`Tier1Readings_AreExcludedFromReviewQueue`).

### 6. User Interface (Bitbus Dark/Amber/Teal Palette)
- `TankRefillTransfer.jsx` (Screen C): record receipts, top-ups, and tank-to-tank transfers with before/after levels and photo verification.
- `TankSetup.jsx`: conical-bottom cylindrical tank setup with live capacity and usable litres preview, plus dead stock configuration.
- `OperationalDailyLog.jsx`: 24-hour operational log sheet displaying all electrical and mechanical parameters per hour with CSV export.
- Integrated into `App.jsx` routes and `OperationsHub.jsx` navigation cards.

### 7. Asset & Font Cleanup
- Deleted unused `frontend/public/pec-logo.png`.
- Updated PWA Web App Manifest theme and background colors to the Bitbus palette (`#1a1a2e` / `#0f172a`).
- Added `@fontsource/ibm-plex-sans-arabic` with SIL OFL-1.1 licence notice in `styles.css`.

### 8. Opt-in PostgreSQL Integration Tests
- `PostgreSqlIndexIntegrationTests.cs`: opt-in integration tests against a real PostgreSQL instance via `TEST_POSTGRES_CONNECTION` (skipped cleanly when absent).

## Proposed PostgreSQL Background Job Storage for Owner Approval
- **Package:** `Hangfire.PostgreSql` version `1.20.10`
- **Multi-Tenant Scheduling Architecture:**
  A background hosted service (`TenantHangfireJobRunner`) iterates across active tenant connection strings provided by `ITenantProvider`, applies tenant schema context, and executes `EvaluateOperatingLimitsAsync` per tenant schema.