# NAV-2 — navigation consolidation and capture improvements

**Branch:** `feature/bitbus-nav-consolidation`  
**Status:** implemented locally; owner verification pending

## Delivered

- Centralized sidebar navigation and operational-mode visibility in the navigation registry.
- Consolidated tank setup and calibration under `TanksManagement` without deleting legacy routes.
- Added scoped feeder management with create, edit, deactivate, optional meter link, multiplier, and idempotent default-feeder creation.
- Added the operational capture previous-reading API. It returns only counters within the caller's reachable site.
- Updated capture to add all remaining site generators/feeders, record their counters with the operational line, and remove the manual meter-subject selector from this workflow.
- Added periodic-round and daily-closing modes. Daily closing shows the official previous value, a live display-only delta, and an unapproved station preview of produced energy, sent energy, and the unexplained difference.
- Added reset-note enforcement for a lower counter value and a display-only interval-delta service.

## Explicit compatibility decision

`/fuel-movements` and all of its fuel/oil sub-tabs are unchanged. This was explicitly requested by the owner after the initial NAV-2 brief; no change to that page is included in this work.

## Tests added (not run by the agent)

- `MeterDeltaServiceTests`: previous usable interval delta with the current multiplier; reset reading has no interval delta.
- `OperationalCaptureServiceTests`: site-scoped previous counter history and out-of-scope concealment.

## Owner verification commands

The owner requested to run all build and test commands manually. No `dotnet`, `npm`, or test command was run by the agent for this change.

```powershell
dotnet build FuelTrack.sln --no-restore
dotnet test FuelTrack.sln --no-build --no-restore
Set-Location frontend
npm.cmd run test -- --run
npm.cmd run build
```

## Boundaries

- No migrations or dependencies were added.
- No existing route, legacy screen, or fuel/oil movement screen was removed.
- The live daily-closing preview is informational only. Approval continues to own official delta calculation and reporting.
