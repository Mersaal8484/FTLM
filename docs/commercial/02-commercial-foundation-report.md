# Phase C2 — Commercial Foundation Report

**Branch:** `feature/commercial-foundation`  
**Date:** 2026-11-16  
**Status:** ✅ Committed

---

## What Was Done

### New Files (7)

| File | Purpose |
|------|---------|
| `frontend/src/hooks/useCommercialMode.js` | React hook — fetches `/api/commercial-station-mode`, caches result, exports `resetCommercialModeCache()` |
| `src/FuelTrack.API/Controllers/CommercialStationModeController.cs` | GET status, GET preflight (Admin), POST activate (Admin), POST deactivate (Admin) |
| `src/FuelTrack.Application/DTOs/Operations/CommercialStationModeDto.cs` | `CommercialStationModeDto`, `CommercialStationActivationPreflightDto`, `ActivateCommercialStationModeDto`, `CommercialStationScope` |
| `src/FuelTrack.Application/Interfaces/ICommercialStationModeService.cs` | `IsCommercialModeAsync`, `GetStatusAsync`, `GetActivationPreflightAsync`, `ActivateAsync`, `DeactivateAsync`, `GetScopeAsync` |
| `src/FuelTrack.Infrastructure/Services/CommercialStationModeService.cs` | Full implementation with idempotent 1-1-1 hierarchy provisioning and feature-flag management |
| `tests/FuelTrack.Tests/Services/CommercialStationModeServiceTests.cs` | 5 service-layer tests (see below) |
| `tests/FuelTrack.Tests/Controllers/CommercialHierarchyGuardTests.cs` | 1 controller-layer test (see below) |

### Modified Files (16)

| File | Change |
|------|--------|
| `frontend/src/App.jsx` | Commercial mode guard wraps protected routes |
| `frontend/src/components/Sidebar.jsx` | Hides "Locations" nav entry in commercial mode |
| `frontend/src/components/Sidebar.test.jsx` | Updated expected nav order for commercial mode |
| `frontend/src/components/common/HierarchicalFilter.jsx` | Hides Branch→Station→Site filter in commercial mode |
| `frontend/src/navigation/registry.js` | Locations entry conditional on `!isCommercialMode` |
| `frontend/src/pages/Users.jsx` | Hides shift editor and scope assignment panel in commercial mode |
| `src/FuelTrack.API/Controllers/BranchesController.cs` | Returns `404 NotFound` when commercial mode is active |
| `src/FuelTrack.API/Controllers/StationsController.cs` | Returns `404 NotFound` when commercial mode is active |
| `src/FuelTrack.API/Controllers/SubStationsController.cs` | Returns `404 NotFound` when commercial mode is active |
| `src/FuelTrack.API/Controllers/UsersController.cs` | Rejects scope-assignment calls in commercial mode |
| `src/FuelTrack.API/Middleware/PermissionEnforcementMiddleware.cs` | DI-wired `ICommercialStationModeService` |
| `src/FuelTrack.Infrastructure/Data/CustomerFeatureSettingsSeed.cs` | Seeds `commercial_station_mode` feature key on startup |
| `src/FuelTrack.Infrastructure/DependencyInjection.cs` | Registers `ICommercialStationModeService → CommercialStationModeService` |
| `src/FuelTrack.Infrastructure/Services/CustomerFeatureService.cs` | Extended to expose commercial mode flag |
| `src/FuelTrack.Infrastructure/Services/UserScopeFilterService.cs` | In commercial mode, all scope queries resolve to the single technical SubStation ID; ignores UserScope rows |
| `src/FuelTrack.Infrastructure/Services/UserService.cs` | Rejects scope-assignment in commercial mode |

---

## Key Design Decisions

### 1 — Feature Flag Key
`CustomerFeatureSettings` table, key = `"commercial_station_mode"`, value = `"commercial"` (active) or `"enterprise"` (inactive). No schema change required.

### 2 — 1-1-1 Hierarchy Invariant
On first activation, `ActivateAsync` idempotently provisions exactly one Branch, one Station, one SubStation with:
- Code pattern: `"TECH-{stationCode}"` / `"CS-{stationCode}"`
- These are the **technical hidden entities** — never shown in the UI; used only as scope anchor.

### 3 — Activation Preflight
`GetActivationPreflightAsync` checks for existing active Branches/Stations/SubStations and returns a `CanActivate` flag with a reason. If more than one hierarchy exists, activation is blocked.

### 4 — Scope Bypass Pattern
`UserScopeFilterService.GetSubStationIdsAsync` checks `IsCommercialModeAsync` first. When true, it calls `GetScopeAsync` and returns `[scope.SubStationId]` regardless of any `UserScope` rows — no per-user scope assignment has any effect in commercial mode.

### 5 — Controller Guards
`BranchesController`, `StationsController`, `SubStationsController` accept `ICommercialStationModeService` as a primary constructor parameter. All `GetAll` / `GetById` / `Create` / `Update` / `Delete` actions return `404 NotFound` immediately when commercial mode is active, preventing any hierarchy management from leaking through.

---

## Tests Added

### CommercialStationModeServiceTests (5 tests)
1. **Empty-tenant provision** — `ActivateAsync` on a clean DB creates exactly 1 Branch, 1 Station, 1 SubStation and sets the feature flag.
2. **Multi-hierarchy blocked** — Preflight returns `CanActivate = false` when more than one active hierarchy exists.
3. **Commercial scope ignores UserScope assignments** — `GetScopeAsync` returns the technical SubStation even when no UserScope rows exist for the user.
4. **Enterprise preserves scope resolution** — When feature is inactive, `GetScopeAsync` returns null and normal UserScope logic is unaffected.
5. **Separate tenant databases don't share scope** — Two in-memory DBs stay isolated (regression guard for the per-tenant DI pattern).

### CommercialHierarchyGuardTests (1 test)
- **CommercialMode_HidesTechnicalHierarchyEndpoints** — After activation, `BranchesController.GetAll`, `StationsController.GetAll`, and `SubStationsController.GetAll` each return `NotFoundResult`.

---

## Not Done (Deferred to C3+)

- Tank geometry: horizontal cylinder with conical ends (`TankShape.HorizontalCylinderWithConicalEnds`)
- `TankRole` enum and migration (Main / Daily)
- Generator field hiding in commercial mode (NominalPower, PowerFactor, FuelConsumptionRatePerHour)
- Daily fuel tanks page (`/commercial/daily-tanks`)
- Meters rename to "Feeder Meter / عداد الفيدر" + meter type guard
- Oil stock site-selection hiding

---

## Owner Verification Commands

```powershell
# 1. Run all tests
cd D:\FTM
dotnet test tests/FuelTrack.Tests/FuelTrack.Tests.csproj --filter "CommercialStationModeServiceTests|CommercialHierarchyGuardTests" --logger "console;verbosity=normal"

# 2. Run frontend tests
cd D:\FTM\frontend
npm test -- --watchAll=false --testPathPattern="Sidebar"

# 3. Full test suite (regression)
cd D:\FTM
dotnet test tests/FuelTrack.Tests/FuelTrack.Tests.csproj --logger "console;verbosity=minimal"
cd D:\FTM\frontend
npm test -- --watchAll=false
```

## Manual UI Checks

- Login → Sidebar should NOT show "Locations", "Branches", "Stations", "Sites" in commercial mode.
- `GET /api/branches` → should return 404 when commercial mode is active.
- `GET /api/commercial-station-mode` → should return `{ "isCommercial": true }` after activation.
- Users page → scope assignment panel should be hidden in commercial mode.
- HierarchicalFilter → Branch→Station→Site dropdowns should not appear in commercial mode.
