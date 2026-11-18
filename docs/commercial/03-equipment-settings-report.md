# Phase C3 — Equipment Settings Report

**Branch:** `feature/commercial-foundation`  
**Date:** 2026-11-17  
**Status:** ✅ Completed & Ready for Review

---

## 1. Overview & Owner Design Rules Implemented

This phase implements Phase C3 (Equipment Settings) of the FuelTrack Commercial Station Mode, strictly adhering to the owner's design rules:

1. **Tank Geometry Strategy (`HorizontalCylinderWithConicalEnds`):**
   - Both main storage tanks and generator daily tanks follow the horizontal cylinder with conical ends specification (`TankShape.HorizontalCylinderWithConicalEnds = 1`).
   - Implemented via `HorizontalConicalTankGeometryStrategy` using exact circular segment integration for the horizontal cylindrical barrel and composite Simpson integration for the two conical end-caps.
   - `TankGeometry` facade extended with overloads for full capacity and bisection liquid-level inversion.
   - `TankVolumeService` dead stock calculation uses the configured tank shape.

2. **TankRole Schema & Constraints:**
   - Added `TankRole` enum: `Main = 1`, `Daily = 2` (nullable on entity, enterprise-safe with nulls).
   - Additive SQL migration `20261116120000_AddTankRoleAndHorizontalShape` adding `[TankRole] int NULL` to `FuelTanks`.
   - `FuelTankService` validates that daily tanks are associated with a generator (`GeneratorId` required when `TankRole == Daily`).

3. **Generator Settings & Field Hiding:**
   - In commercial mode, nominal power (`ratedCapacityKw`), power factor (`powerFactor`), and theoretical fuel consumption rate (`fuelConsumptionRatePerHour`) are completely hidden in the UI.
   - No zeros or defaults displayed.
   - Not required in commercial mode forms.
   - Validators (`CreateGeneratorDtoValidator`, `UpdateGeneratorDtoValidator`) allow non-negative values for backward compatibility.

4. **Feeder Meters:**
   - In commercial mode, meters page focuses exclusively on "عداد الفيدر / Feeder Meter".
   - Fuel and generator meter tabs and options hidden in commercial mode.
   - `MeterService` automatically maintains a hidden 1:1 `Feeder` entity in commercial mode on create, update, and delete.

5. **Oil Stock Unlinking:**
   - Site/station dropdowns hidden in `SetupOils.jsx` in commercial mode.
   - `OilService.CreateAsync` automatically assigns the technical station and substation from the commercial scope.
   - `OilStockPage.jsx` renders a clean, flat table for the commercial station instead of the 3-level hierarchical accordion (Branch → Station → Site), and hides the redundant "Locations" KPI card.

6. **Daily Fuel Tanks Page:**
   - Created dedicated commercial page: `frontend/src/pages/DailyTanks.jsx`.
   - Route `/commercial/daily-tanks` guarded by `<CommercialOnly>` and `<Protected module="tanks">`.
   - Navigation item registered in `frontend/src/navigation/registry.js` with `modeVisibility: 'commercial'`.
   - i18n parity for `sidebar.dailyTanks` in both Arabic (`الخزانات اليومية`) and English (`Daily Tanks`).

---

## 2. Changes Summary

### New Files (5)

| File | Purpose |
|------|---------|
| `src/FuelTrack.Domain/Enums/TankRole.cs` | Enum: `Main = 1`, `Daily = 2` |
| `src/FuelTrack.Domain/Services/HorizontalConicalTankGeometryStrategy.cs` | Mathematical volume calculator for horizontal cylinder with conical ends |
| `src/FuelTrack.Infrastructure/Migrations/20261116120000_AddTankRoleAndHorizontalShape.cs` | Additive SQL migration adding `TankRole` column to `FuelTanks` |
| `tests/FuelTrack.Tests/Services/HorizontalConicalTankGeometryTests.cs` | 8 unit tests verifying calibration, symmetry, bisection, and dead stock |
| `frontend/src/pages/DailyTanks.jsx` | Dedicated daily fuel tanks management page for commercial stations |

### Modified Files (24)

| Component | Files | Changes |
|-----------|-------|---------|
| **Domain** | `TankShape.cs` | Added `HorizontalCylinderWithConicalEnds = 1` |
| | `FuelTank.cs` | Added `TankRole? TankRole { get; set; }` |
| | `TankGeometry.cs` | Registered strategy, horizontal capacity & level derivation |
| | `TankVolumeService.cs` | Uses `tank.Shape` when deriving dead stock volume |
| **Application** | `CreateFuelTankDto.cs` | Added `TankRole?`, `GeneratorId?`, `ParentTankId?`, `Shape?` |
| | `UpdateFuelTankDto.cs` | Added `TankRole?`, `GeneratorId?`, `ParentTankId?`, `Shape?` |
| | `FuelTankDetailDto.cs` | Added `TankRole?`, `GeneratorId?`, `GeneratorName?`, `ParentTankId?`, `ParentTankName?`, `Shape?` |
| | `FuelTankListDto.cs` | Added `TankRole?`, `GeneratorId?`, `GeneratorName?`, `ParentTankId?`, `Shape?` |
| | `TankCalibrationDto.cs` | Added `TankShape? Shape = null` |
| | `CreateGeneratorDtoValidator.cs` | Allows non-negative nominal power & fuel rate |
| | `UpdateGeneratorDtoValidator.cs` | Allows non-negative nominal power & fuel rate |
| **Infrastructure** | `FuelTrackDbContextModelSnapshot.cs` | Added `TankRole` property mapping |
| | `FuelTankService.cs` | Added daily tank generator validation, mapped role/generator/parent/shape |
| | `TankCalibrationService.cs` | Injected commercial mode, supports horizontal conical geometry in preview & upsert |
| | `MeterService.cs` | Auto-maintains hidden 1:1 `Feeder` records in commercial mode |
| | `OilService.cs` | Auto-populates station/substation from commercial scope |
| **Frontend** | `App.jsx` | Added `CommercialOnly` guard, `DailyTanks` lazy import and route |
| | `navigation/registry.js` | Added `daily-tanks` item with `modeVisibility: 'commercial'` |
| | `i18n.js` | Added `dailyTanks` key in Arabic and English |
| | `pages/Generators.jsx` | Hid nominal capacity, power factor, fuel consumption rate in commercial mode |
| | `pages/MetersManagement.jsx` | Renamed to Feeder Meter, hid non-feeder tabs/types, auto-scoped site |
| | `pages/SetupOils.jsx` | Hid location inputs and table column in commercial mode |
| | `pages/OilStockPage.jsx` | Renders direct table instead of accordion, hid Locations KPI in commercial mode |
| | `pages/TankSetup.jsx` | Sends `shape: 1` in commercial mode |

---

## 3. Tests Added

### `HorizontalConicalTankGeometryTests` (8 tests)
1. `CalculateLitres_ReturnsZeroWhenEmpty` — Verifies 0 L at level 0 cm.
2. `CalculateLitres_FullTankEqualsCalculatedCapacity` — Verifies volume at `h = D` matches capacity ($V_{cyl} + 2 V_{cone}$).
3. `CalculateLitres_HalfFullEqualsHalfCapacityBySymmetry` — Verifies geometric symmetry (volume at $h = R$ is exactly 50% capacity).
4. `CalculateLitres_IsMonotonicallyIncreasing` — Verifies volume increases monotonically from empty to full.
5. `CalculateLitres_AppliesMultiplicationFactor` — Verifies density/calibration scaling factor.
6. `CalculateLitres_RejectsLevelOutsideDiameter` — Verifies exceptions for $h < 0$ or $h > D$.
7. `CalculateLiquidLevelCm_InvertsVolumeAccurately` — Verifies numerical bisection recovers original cm from volume.
8. `TankVolumeService_ResolvesDeadStockForHorizontalConicalTank` — Verifies dead stock resolution for horizontal conical tanks.

---

## 4. Owner Verification Commands

```powershell
# 1. Run geometry tests
cd D:\FTM
dotnet test tests/FuelTrack.Tests/FuelTrack.Tests.csproj --filter "HorizontalConicalTankGeometryTests|TankGeometryTests" --logger "console;verbosity=normal"

# 2. Run commercial foundation and controller tests
cd D:\FTM
dotnet test tests/FuelTrack.Tests/FuelTrack.Tests.csproj --filter "CommercialStationModeServiceTests|CommercialHierarchyGuardTests" --logger "console;verbosity=normal"

# 3. Run frontend unit tests
cd D:\FTM\frontend
npm test -- --watchAll=false --testPathPattern="Sidebar"

# 4. Full test suite regression
cd D:\FTM
dotnet test tests/FuelTrack.Tests/FuelTrack.Tests.csproj --logger "console;verbosity=minimal"
cd D:\FTM\frontend
npm test -- --watchAll=false
```

---

## 5. Correction: Vertical Cylinder with Conical Bottom Geometry

### Owner Rule
The station owner clarified and confirmed that all commercial station fuel tanks (both the main storage tanks and the generator daily service tanks) are **VERTICAL cylinders with a CONICAL BOTTOM** where fuel exits at the apex (the ruler/level reading is measured from the cone apex $h = 0$).

Phase C3 had erroneously implemented `TankShape.HorizontalCylinderWithConicalEnds` (Shape = 1). The system has been corrected so commercial station mode routes to the standard vertical conical geometry (`TankShape.VerticalCylinderWithConicalBottom = 0`), reusing `VerticalConicalTankGeometryStrategy` unchanged.

### Summary of Changes
1. **`FuelTankService.cs` (~line 40):** Default shape in commercial mode is `TankShape.VerticalCylinderWithConicalBottom` (enum value `0`). Creation and update validate that `Shape = 1` is strictly rejected with an argument error.
2. **`TankCalibrationService.cs` (~lines 21, 43, 79):**
   - Commercial default shape set to `VerticalCylinderWithConicalBottom`.
   - Removed horizontal geometry branches for height and level bounds:
     $$\text{fullHeight} = \text{ConeHeightCm} + \text{CylinderHeightCm}$$
     $$\text{maxLevel} = \text{CylinderHeightCm} + \text{ConeHeightCm}$$
   - Validation rejects any calibration payload specifying `Shape = 1`.
3. **`TankGeometry.cs` & `TankShape.cs`:**
   - Retained `HorizontalConicalTankGeometryStrategy` and enum value `TankShape.HorizontalCylinderWithConicalEnds = 1` for backward compatibility without mutating already-applied migration `20261116120000`.
   - Marked both enum member and strategy class as `[Obsolete("Not used by any station mode")]`.
4. **API Validation:**
   - `CreateFuelTankDtoValidator` and `UpdateFuelTankDtoValidator` reject `Shape = 1` across all station modes with HTTP 400 Bad Request.
   - `ExceptionHandlingMiddleware` handles `ArgumentException` returning 400 Bad Request.
5. **`DailyTanks.jsx` & `TankSetup.jsx`:**
   - `DailyTanks.jsx`: sends `shape: 0` on create and edit.
   - Form exposes vertical geometry inputs (`diameterCm`, `cylinderHeightCm`, `coneHeightCm`, `multiplicationFactor`), fuel type, dead stock, and actual level reusing standard calibration fields.
   - Subtitle and descriptive text updated to reflect vertical cylindrical tanks with conical bottoms.
   - `TankSetup.jsx`: payload sends `shape: 0`.
6. **Audit Query (`docs/commercial/sql/list-horizontal-tanks.sql`):**
   - Read-only script listing all `FuelTanks` where `Shape = 1` for administrative review without automated destructive conversion.
7. **Verification Tests:**
   - Added `CommercialTankVerticalGeometryTests.cs` verifying default shape 0 in commercial mode, API rejection of shape 1, vertical conical calibration fixtures ($D=200, H_c=50, H_{cyl}=150$), density factor scaling, and dead stock deduction.

---

## 6. Phase C3.2 Corrections (Equipment Settings Refinements)

### 1. Generators & Cumulative Running Hours
- **Field Hiding in Commercial Mode:** `Owner Name`, `Ownership Type`, and `Generator Type` are completely hidden from the commercial generator form.
- **Null Values Sent:** In commercial mode, `handleSubmit` sends `null` (not `0`) for `NominalPower`, `FuelConsumptionRatePerHour`, `OwnerName`, `GeneratorTypeLookupId`, and `OwnershipTypeLookupId`.
- **Consumer Impact Audit:**
  - `Generator.NominalPower` and `Generator.FuelConsumptionRatePerHour` remain non-nullable columns in the database with server-side default `0`.
  - `CreateGeneratorDto` and `UpdateGeneratorDto` accept nullable `double?` and `decimal?`.
  - Service layer (`GeneratorService`) assigns `dto.NominalPower ?? 0` and `dto.FuelConsumptionRatePerHour ?? 0m`.
  - `ReportService`, `LossAnalysisService`, `FuelConsumptionAnalysisService`, and `ServiceDueEngine` all check `if (FuelConsumptionRatePerHour > 0)` or sum safely, ensuring zero produces no calculation errors or divide-by-zero exceptions.
- **Cumulative Running Hours Field:**
  - Mapped directly to `Controller.OperatingHours` on the linked controller.
  - Validated $\ge 0$ and $\ge$ latest approved daily-closing meter reading for running hours.
  - Serves as the baseline for the service-due engine without requiring any schema changes.

### 2. Controllers (Generator Screens)
- **Manufacturer & Model:** In commercial mode, Manufacturer and Model are editable free-text fields. Model provides a datalist with `DSE8610` and `DSE8920` as suggestions.
- **Hidden Network Controls:** IP address, port, Modbus unit ID, register type, auto-connect toggle, and connection-status test controls/calls are hidden in commercial mode.
- **Server Defaults:** Backend defaults (`port: 502`, `modbusUnitId: 1`, `registerType: FC03`, `autoConnect: false`, `ipAddress: null`) are maintained.

### 3. Tanks Management (Location Hiding)
- **Hierarchy Hidden:** Branch, Station, and Area (SubStation) selector fields are hidden in the commercial modal.
- **Card Display:** Region/Area row is hidden from tank cards in commercial mode; grouping displays unified station tanks.
- **Auto-Scoping:** Form validation skips SubStation selection and automatically attaches the tenant's single commercial technical substation.

### 4. PostgreSQL Tenant Upgrade 010 (TankRole Parity)
- **Migration Script:** `010_commercial_tankrole.sql` introduces `"TankRole" integer` on `"FuelTanks"` with an idempotent index for filtering daily/main tanks.
- **Catalog Registration:** Version 10 registered in `TenantSchemaUpgradeCatalog.cs`.
- **Embedded Resource:** Configured in `FuelTrack.Infrastructure.csproj`.
- **Rollback Plan:**
  ```sql
  DROP INDEX IF EXISTS "IX_FuelTanks_TankRole";
  ALTER TABLE "FuelTanks" DROP COLUMN IF EXISTS "TankRole";
  ```

---

## 7. Job 2: Horizontal Geometry Removed Entirely (Owner Decision: No Horizontal Tanks)

### Owner Decision
The owner confirmed **there are no horizontal tanks** in the system. The horizontal geometry is therefore removed entirely from the codebase (not just marked obsolete).

### Changes Made
1. **Deleted:** `HorizontalConicalTankGeometryStrategy.cs` and `HorizontalConicalTankGeometryTests.cs`.
2. **`TankShape.cs`:** `HorizontalCylinderWithConicalEnds = 1` removed. Only `VerticalCylinderWithConicalBottom = 0` remains. DB column and migration `20261116120000` are **not touched**.
3. **`TankGeometry.cs`:** Removed horizontal strategy field, switch arm, and all `HorizontalCylinderWithConicalEnds` ternary branches. `fullLevel` is always `coneHeightCm + cylinderHeightCm`.
4. **`FuelTankService.cs`:** Guards replaced with `Enum.IsDefined` check. Message: `"Unsupported tank shape. Only vertical cylinder with conical bottom (Shape = 0) is supported."`
5. **`TankCalibrationService.cs`:** `IsValid()` returns false for undefined enum values via `Enum.IsDefined`.
6. **`CreateFuelTankDtoValidator.cs` / `UpdateFuelTankDtoValidator.cs`:** Obsolete-member guards replaced with `Enum.IsDefined`-based `Must(...)` rule.
7. **`CommercialTankVerticalGeometryTests.cs`:** 5 `TankShape.HorizontalCylinderWithConicalEnds` → `(TankShape)1`; message assertions updated; stale `#pragma` removed.
8. **`FuelLoadSafetyServiceTests.cs`:** Seed tanks at lines ~892 and ~1022 switched to `TankShape.VerticalCylinderWithConicalBottom`. No expected values change — tests assert on stored `ComputedLitres`, not geometry math.
9. **`docs/commercial/sql/list-horizontal-tanks.sql`:** Retained as a **pre-deployment check**. Run before deploying; if any `Shape = 1` rows exist, the owner must handle them manually.

### Verification Command
```bash
git grep -i "HorizontalCylinder\|HorizontalConical"
# Returns ONLY: migration 20261116120000 and docs/
```

