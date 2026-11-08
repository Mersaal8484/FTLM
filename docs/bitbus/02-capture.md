# Phase 2 — Operational Capture

**Branch:** `feature/bitbus-capture-phase2`  
**Status:** implementation complete; owner verification pending

## Delivered

- Added raw, immutable Phase 2 capture entities: `OperationalSubmission`, generator readings, tank-level readings, feeders, feeder-load readings, and extensible generic meter readings.
- Added additive tank calibration fields for the approved vertical-cylinder-with-conical-bottom geometry. Each tank observation stores a calibration snapshot so later setup changes never rewrite history.
- Added scoped feeder master creation/listing and scoped tank-calibration update endpoints. They reuse the existing Site hierarchy and do not alter legacy tank-stock semantics.
- Added the pure `TankGeometry` calculation using centimetres and a tank factor. It rejects out-of-range levels rather than silently clamping them.
- Added atomic `POST /api/operational-captures` with a tenant/user-scoped idempotency key, scope checks, duplicate guards, photo ownership checks, required-photo validation, and a five-minute future-time limit.
- Every Phase 2 record is created as `Pending`. No endpoint in this phase approves, rejects, calculates deltas, changes stocks, updates service clocks, or contributes to an official dashboard.
- Added a reference endpoint filtered through the existing reachable-Site scope and an operator-only submission-history endpoint filtered to the authenticated submitter.
- Added the responsive RTL operator flow at `/operations/capture` and history at `/operations/history`, using the approved dark/amber/teal visual language. Photos are resized/compressed in the browser before the existing server-side JPEG/WebP signature and 65-KB validation.
- Added the reviewed additive migration `20261107120000_AddOperationalCapture` and advanced the EF model snapshot to the same Phase 2 model.

## Compatibility boundaries

- `DailyReading`, `SubStationReading`, legacy tank stock, warehouse, permits, alerts, and Modbus Gateway are unchanged.
- The existing shared attachment service remains the only file-storage route; operational lines link previously validated attachments atomically after their IDs are created.
- Free `CapturedAtUtc` values are allowed. There is no hourly slot, edit window, backdate window, or override permission in this phase.
- The migration does not create solar entities, official deltas, balance snapshots, service intervals, review workflows, or inventory/maintenance changes.

## Tests added

- `TankGeometryTests`: apex, cone proportionality, cone/cylinder junction, full capacity, factor, and invalid level boundaries.
- `OperationalCaptureServiceTests`: Pending-only creation, exact idempotent replay, future-time rejection, required-photo rejection, and same-subject/same-time duplicate rejection.
- Scope coverage: capture rejects Sites outside the caller’s existing scope; the reference endpoint applies the same reachable-Site boundary, while history is intentionally limited to the submitting operator. Existing Phase 1 lookup-seed scope tests remain unchanged.

## Owner verification required

No restore, build, test, migration, or frontend command was run by the agent, as requested. Run these from `D:\FTM\.worktrees\bitbus-capture-phase2` after installing/restoring the project tools:

```powershell
dotnet restore FuelTrack.sln
dotnet build FuelTrack.sln --no-restore
dotnet ef migrations has-pending-model-changes --project src/FuelTrack.Infrastructure --startup-project src/FuelTrack.API
dotnet test FuelTrack.sln --no-build --no-restore
cd frontend
npm.cmd ci
npm.cmd run test -- --run
npm.cmd run build
```

## Next gate

Phase 3 requires explicit approval. It owns the reviewer workflow, per-line approval/rejection, meter reset/delta rules, derived official values, and the official dashboard. Nothing in this phase may be treated as official operational data.
