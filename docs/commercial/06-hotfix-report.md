# Commercial Mode Hotfix Report (H1/H2/H3)

## Status: UNVERIFIED (Awaiting owner verification)

### 1. Changes Delivered
- **H1 (Meters Tolerance):** Verified already implemented in commit `1f796e5` / `3316fc1` (`isCommercialMode ? empty : client.get(...)` with test coverage).
- **H2 (Work Permits):**
  - `WorkPermitService.cs`: Injected `ICommercialStationModeService`. In `CreateAsync`, resolves technical site server-side when in commercial mode (ignoring client input). Validates that generators belong to the resolved site. Scopes `GetAllAsync`, `GetByIdAsync`, `UpdateAsync`, `UpdateStatusAsync`, and `DeleteAsync` to the commercial station.
  - `CreateWorkPermitDtoValidator.cs`: Injected `ICommercialStationModeService` to allow `SubStationId = 0` in commercial mode while requiring it in enterprise mode.
  - `CreateWorkPermitDto.cs`: Made parameters default-safe for clean serialization.
  - `WorkPermits.jsx`: Uses `useCommercialMode()`. Bypasses `/api/sub-stations` fetch, hides Site dropdown in the create modal, and omits `subStationId` from create DTO in commercial mode.
  - Tests: Added `WorkPermitServiceCommercialTests.cs`, `CreateWorkPermitDtoValidatorTests.cs`, and `WorkPermits.test.jsx`.
- **H3 (UI Hierarchy Sweep):** Created `docs/commercial/06-commercial-ui-sweep.md` inventorying 15 frontend files with hierarchy controls mapped to future phases.

### 2. Owner Verification Commands (PowerShell from `D:\FTM\.worktrees\commercial-foundation`)

```powershell
Set-Location "D:\FTM\.worktrees\commercial-foundation"

# 1. Backend Build & Tests
dotnet build src/FuelTrack.API/FuelTrack.API.csproj
dotnet test tests/FuelTrack.Tests/FuelTrack.Tests.csproj --filter "FullyQualifiedName~WorkPermit|FullyQualifiedName~Commercial"

# 2. Frontend Tests & Build
npm --prefix frontend test -- frontend/src/pages/WorkPermits.test.jsx frontend/src/pages/MetersManagement.test.jsx --run
npm --prefix frontend run build
```
