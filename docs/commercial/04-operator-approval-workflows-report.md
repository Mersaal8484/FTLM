# Phase C4 — Operator and Approval Workflows Report

**Branch:** `feature/commercial-foundation`  
**Date:** 2026-11-17  
**Status:** ✅ Completed & Verified

---

## 1. Overview & Owner Design Rules Implemented

This phase implements Phase C4 (Operator and Approval Workflows) of the FuelTrack Commercial Station Mode, strictly adhering to the owner's design rules:

1. **Two-Tier Operational Workflows:**
   - **Tier 1 (Hourly Monitoring):** Routine generator readings (air/oil pressure, water/oil temperatures, active/apparent load, line voltage, frequency) are stored immediately as `OperationalReadingStatus.Recorded`. No manager approval queue items are generated.
   - **Tier 2 (Daily Closing & Transfers):** End-of-day meter readings (cumulative hours, energy counter, feeder meters) and tank movements (receipts, transfers, daily closing levels) require Operations Manager / Station Manager approval before computing official settlement deltas.

2. **Daily Fuel Consumption Formula ($\text{Opening} + \text{Inbound Refills} - \text{Closing}$):**
   - Eliminates theoretical fuel consumption calculations completely.
   - Tied directly to each generator's daily fuel tank (`TankRole.Daily` or `ParentTankId != null`).
   - Implemented in `FuelLoadSafetyService.GetDailyConsumptionAsync`.
   - Computes daily fuel consumption, produced energy (kWh), and efficiency ($\text{kWh/L}$).
   - Emits alerts when closing balance exceeds opening + refills, or when daily tank level drops below dead stock.

3. **Live Generator Telemetry Cards (`GeneratorsOverview.jsx`):**
   - Commercial mode displays responsive operational cards for generators instead of the multi-level tree view.
   - Cards display live metrics: active load, running hours, oil pressure, water temp, air pressure, oil temp, and linked daily tank volume and level.
   - Generator detail modal hides nominal power, power factor, and theoretical consumption in commercial mode.

4. **Paired Main→Daily Tank Transfers (`TankRefillTransfer.jsx`):**
   - In commercial mode, standalone `TransferOut` is hidden; `TransferIn` is presented as "تحويل من الرئيسي لليومي (Transfer Main→Daily)".
   - Selects source Main tank and destination Daily tank, capturing before/after levels for both.
   - Creates atomically linked lines (`linkedClientLineId`) with `TransferOut` (Main) and `TransferIn` (Daily).
   - Fuel receipts restricted to Main storage tanks.

5. **Daily Review & Consumption Integration (`OperationalReview.jsx` & `OperationalDailyLog.jsx`):**
   - `OperationalReview.jsx` displays a dedicated "استهلاك الوقود اليومي للمولدات" breakdown table on daily closing submissions.
   - `OperationalDailyLog.jsx` fetches and displays the daily fuel consumption summary and efficiency alongside hourly production.

6. **Strict Role-Gated Rollback:**
   - `RollbackController.Execute` restricted strictly to Operations Manager and Admin roles.

---

## 2. Changes Summary

### New Files (1)

| File | Purpose |
|------|---------|
| `src/FuelTrack.Application/DTOs/Generators/GeneratorOperationalCardDto.cs` | DTO representing real-time telemetry, meter readings, and linked daily tank stock for commercial generator cards |

### Modified Files (20)

| Component | Files | Changes |
|-----------|-------|---------|
| **Application** | `FuelLoadSafetyDto.cs` | Added `DailyGeneratorFuelConsumptionDto` record |
| | `ReviewDto.cs` | Extended `SubmissionDetailDto` to include `DailyFuelConsumptions` |
| | `IFuelLoadSafetyService.cs` | Added `GetDailyConsumptionAsync` interface method |
| | `IGeneratorService.cs` | Added `GetOperationalCardsAsync` interface method |
| **Domain & Services** | `FuelLoadSafetyService.cs` | Implemented `GetDailyConsumptionAsync` formula ($\text{Opening} + \text{Inbound} - \text{Closing}$) and warnings |
| | `FuelTankService.cs` | Cleaned up duplicate mapping fragment from C3 merge |
| | `GeneratorService.cs` | Implemented `GetOperationalCardsAsync` with live telemetry & daily tank levels |
| | `OperationalCaptureService.cs` | Auto-resolves commercial substation; bypasses Tier-1 approval in commercial mode; enforces paired tank transfers |
| | `OperationalReviewService.cs` | Populates `DailyFuelConsumptions` on review submission detail for daily closing |
| **API Controllers** | `GeneratorsController.cs` | Added `GET /api/generators/operational-cards` endpoint |
| | `FuelLoadSafetyController.cs` | Added `GET /api/fuel-load-safety/generators/{id}/daily-consumption/{date}` endpoint |
| | `RollbackController.cs` | Enforces Admin / OperationsManager role check before executing rollback |
| **Frontend** | `useCommercialMode.js` | Returns both `isCommercial` and `isCommercialMode` for complete hook parity |
| | `navigation/registry.js` | Hides legacy `/review-changes` and `/feeders` routes in commercial mode |
| | `pages/GeneratorsOverview.jsx` | Renders commercial generator operational cards and telemetry; hides tree view |
| | `pages/OperationalCapture.jsx` | Scopes to commercial station; displays hourly/closing tabs; unifies tank capture |
| | `pages/OperationalDailyLog.jsx` | Added daily fuel consumption KPI panel with formula explanation & efficiency |
| | `pages/OperationalReview.jsx` | Renders daily fuel consumption breakdown table on daily closing reviews |
| | `pages/TankRefillTransfer.jsx` | Implemented paired Main→Daily tank transfers with dual level inputs |
| **Tests** | `FuelLoadSafetyServiceTests.cs` | Added 2 tests for `GetDailyConsumptionAsync` (calculation & dead stock warning) |

---

## 3. Automated Verification

- **Backend Build (`dotnet build FuelTrack.sln`):** 0 Errors, Clean compilation.
- **Unit Tests (`dotnet test`):**
  - `FuelLoadSafetyServiceTests`: All 37 tests passed.
  - `HorizontalConicalTankGeometryTests`: All 8 tests passed.
  - `CommercialStationModeServiceTests`: All 5 tests passed.
  - `CommercialHierarchyGuardTests`: All passed.
