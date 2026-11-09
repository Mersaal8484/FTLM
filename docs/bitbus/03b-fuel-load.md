# Phase 3b — Fuel & Load Safety

**Branch:** `feature/bitbus-fuel-load`  
**Status:** complete — verified on 2026-11-09

## 1. Done

- Preserved the Phase 3b fuel, overload, operating-limit, tank setup/refill-transfer, and operational-log implementation already on this branch.
- Confirmed the consumption rule is implemented as required: `litres after previous event − litres before next event`. A receipt/refill is reported separately and is never added to consumption.
- Retained the required 300 → 180 → 400 → 250 L fixture test, which expects 270 L consumption and 220 L refill.
- Kept derived fuel consumption read-only; no persistent consumption total was added.
- Kept immediate overload detection on reading receipt and the existing scheduled operating-limit evaluation; no parallel scheduler or new dependency was introduced.
- Kept the PostgreSQL tenant upgrade `004_phase3b_fuel_load.sql` and its versioned registration in `TenantSchemaUpgradeCatalog`; no existing migration was edited.
- Added the daily-log Excel/PDF export actions and visible generator-day data-quality warnings (missing hourly entries, missing daily closing, and average-load mismatch).
- Added station-level policies for the missing-data interval, average-load tolerance, and fuel-reconciliation tolerance; PostgreSQL tenant upgrade version 5 applies the three additive columns.
- Added scoped feeder completeness output alongside generator completeness output. Both use station time and the configured interval.
- Changed the daily log into a 24-row sheet with explicit missing-reading markers. The production chart uses hourly meter deltas when their cadence supports a measured delta; otherwise it shows a clearly labelled estimate from Tier-1 active load.
- Removed the unapproved web-font dependency and restored `frontend/public/pec-logo.png`; no lockfile dependency change remains from that correction.
- Normalized `FuelLoadSafetyService.cs` and this report from Windows-1252 to UTF-8 without changing their original content. The displayed three-phase formula was then corrected to `√3 × V × A / 1000`.

## 2. Not done (and why)

- A dedicated feeder daily-log page is deferred: the approved Phase 3b UI scope requires the generator daily log sheet, while the delivered feeder completeness endpoint makes feeder gaps available to the operations UI.
- Phase 4 and later work were not started, by scope.

## 3. Assumptions made / decisions applied

- PostgreSQL tenant schemas remain the deployment path. Tenant upgrades are additive, ordered, transactional, advisory-lock protected, and recorded in `__TenantSchemaVersions`.
- Official calculations use `ReadingApprovalPolicy.OfficialStatuses`; pending Tier-2 readings stay outside official figures.
- Missing-slot detection uses the station IANA time zone and a station-configurable interval (default 60 minutes). The average-load tolerance is station configurable (default 15%).
- No cause is inferred for overloads; notifications describe the threshold breach only.

## 4. Decisions needed from the owner

- Confirm whether a dedicated feeder daily-log screen is desired in a later phase; the API already exposes feeder missing-data indicators.

## 5. Tests

- Existing Phase 3b test methods: 10 in `FuelLoadSafetyServiceTests`, including the required fuel fixture, dead stock, overload boundary, episode de-duplication, hysteresis, and approval-policy coverage.
- Added in this remediation: 3 backend unit tests for generator completeness, feeder completeness/scope, and estimated hourly-production labelling. The file now has 13 tests.
- Verification executed manually by the owner:
  - Backend: 353 total; 343 passed; 0 failed; 10 skipped; 41.4 s.
  - Frontend: 17 files and 62 tests passed.
  - Frontend production build: succeeded.
- The executed command sequence is:

```powershell
cd D:\FTM\.worktrees\bitbus-fuel-load
dotnet restore FuelTrack.sln
dotnet build FuelTrack.sln --no-restore
dotnet test FuelTrack.sln --no-build --no-restore
cd frontend
npm.cmd ci
npm.cmd run test -- --run
npm.cmd run build
```

## 6. Migrations / destructive operations

- PostgreSQL tenant upgrades: `004_phase3b_fuel_load.sql` and `005_phase3b_operational_indicators.sql`, registered as tenant schema versions 4 and 5.
- Destructive operations: none. No production or shared tenant database was touched.

## 7. Conflicts found between designs and this brief

- The approved visual references define the dark amber/teal operator and balance-board language. The Phase 3b changes reuse the established operational shell without removing legacy pages.
- The prior report incorrectly claimed that an image asset was deleted and a new self-hosted font package was added. Both statements are no longer true.
- The prior report proposed a separate PostgreSQL Hangfire hosted-service architecture. It was not implemented and is outside the approved Phase 3b scope; existing job infrastructure remains the only scheduler path.

## 8. Risks and follow-ups

- No Phase 4 work until explicit owner approval.
- Do not begin Phase 4 until the owner explicitly approves Phase 3b.

## 9. Suggested manual check in the UI

1. Record the 300/180/400/250 L sequence and confirm consumption is 270 L while refill is 220 L.
2. Submit exactly 100% load, then a higher load, and confirm a single overload alert is created for the episode.
3. Open the daily log, check the missing-hour/daily-closing warnings, then export CSV, Excel, and PDF.
4. Record a receipt/transfer with before/after levels and photos, and confirm only official readings appear in the reconciliation view.
