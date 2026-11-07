# Phase 3 — Review, Approval, and Derived Data

**Branch:** `feature/bitbus-review`  
**Status:** implementation complete; owner verification pending

## Delivered

### Backend

- Added `SubmittedByUserId int?` to `OperationalSubmission` so the submitting operator is recorded independently of the EF audit fields.
- Added `InternalConsumptionMeterId int?` to `StationOperationalSettings` so each station can optionally nominate a meter whose readings represent internal consumption.
- Added `003_phase3_review_columns.sql` (Platform tenant-schema migration) which creates the approval/rejection columns on all four operational reading tables, adds `SubmittedByUserId` and `InternalConsumptionMeterId`, and creates the supporting indexes.
- Added `OperationalReviewService` — Tier-2 review queue (`GetPendingSubmissionsAsync`), full submission detail with per-line photo IDs and skew warnings (`GetSubmissionDetailAsync`), submission-level approval (`ApproveSubmissionAsync`), and individual line rejection (`RejectLineAsync`). The skew threshold (12 h) is evaluated client-side to avoid SQL Server-specific EF functions.
- Added `MeterDeltaService` — computes `(closing_today − prev_approved) × multiplier` for each `MeterReadingSubjectType` using only `Recorded` or `Approved` closing readings.
- Added `EnergyBalanceService` — aggregates generator import, solar import (gracefully absent until `SolarImport` enum value is defined), feeder export, and optionally measured internal consumption; derives `UnexplainedDiffKwh` / `UnexplainedDiffPct`; suppresses the threshold alert when `InternalConsumptionMeterId` is null.
- Added `OperationalReviewController` — `GET /api/operational-review` (queue), `GET /api/operational-review/{id}` (detail), `POST /api/operational-review/{id}/approve`, `POST /api/operational-review/lines/{lineType}/{lineId}/reject`.
- Added `EnergyBalanceController` — `GET /api/energy-balance/{stationId}?date=…`.
- Added `OperationalPhotosController` — `GET /api/operational-photos/{id}` with tenant scope check and station-level access control; never exposes photos via public URLs.
- Registered `IMeterDeltaService`, `IOperationalReviewService`, and `IEnergyBalanceService` in `Program.cs`.
- Updated `OperationalCaptureService` to record `SubmittedByUserId` from the authenticated caller at submission time.

### Frontend

- Added `/operations/review` route — `OperationalReview.jsx`: pending queue list, submission detail with per-line approve/reject controls, skew warnings, and photo reference links.
- Added `/energy-balance` route — `EnergyBalanceBoard.jsx`: station/date selector, balance rows (import, generator sub-total, solar, export, internal if measured, unexplained), provisional-balance label when internal consumption meter is not configured, threshold warning banner.
- Updated `i18n.js` with EN + AR keys for review queue, approval/rejection actions, energy balance rows, and provisional-balance messaging.
- Updated `App.jsx` with lazy imports and authenticated routes for both new pages.

## Owner decisions applied

| Decision | Implementation |
|---|---|
| Internal consumption meter absent | Show `غير مفسَّر (يشمل الاستهلاك الداخلي)` as the balance row label; threshold does NOT fire |
| Photo endpoint | New `GET /api/operational-photos/{id}` with scope check; no public URL exposure |
| Version history | Existing `AuditSaveChangesInterceptor` covers the trail; resubmit-on-rejection is the correction flow |
| Fonts | System-font fallback; no blocking on self-hosted assets |
| Legacy identifiers | Not removed (no explicit approval) |

## Schema migration

`src/FuelTrack.Platform/MultiTenancy/003_phase3_review_columns.sql`

- Approval/rejection columns on `OperationalGeneratorReadings`, `OperationalTankLevelReadings`, `OperationalFeederLoadReadings`, `OperationalMeterReadings`
- `SubmittedByUserId` on `OperationalSubmissions`
- `InternalConsumptionMeterId` on `StationOperationalSettings`
- Indexes on status and timestamp columns for queue performance

> [!IMPORTANT]
> This migration must be applied to each PostgreSQL tenant through the controlled Platform-managed mechanism approved in Phase 2.1. Do **not** execute automatically across production tenants.

## Tests added

- `OperationalReviewServiceTests` — 6 tests: queue empty-scope, queue sub-station filter, detail not-found, approve happy-path, approve re-approve idempotency, reject line.
- `MeterDeltaServiceTests` — 5 tests: no prior reading returns zero, single delta, multiplier applied, only Recorded/Approved readings used, multiple subjects independent.
- `EnergyBalanceServiceTests` — 3 tests: no data returns zeros, solar absent graceful, threshold suppressed when internal not configured.
- `OperationalReview.test.jsx` — 4 tests: empty queue renders message, queue row renders site/submitter, GET endpoint called on mount, approve POST called after confirm.
- `EnergyBalanceBoard.test.jsx` — 3 tests: station selector rendered, balance fetched on selection, unexplained row present when internal not configured.

## Compatibility boundaries

- All Phase 1 (`DailyReading`, legacy stock, generators, alerts) and Phase 2 entities are unmodified.
- The review workflow only targets `Status = Pending` Phase 2 submissions; `Recorded` Tier-1 lines are never touched.
- No new NuGet or npm dependencies introduced.
- `MeterReadingSubjectType.SolarImport` does not exist yet; `EnergyBalanceService` handles this gracefully with `Enum.IsDefined`.

## Deferred to Phase 3b (`feature/bitbus-fuel-load`)

- Fuel consumption derived values
- Overload detection events
- Operating limits enforcement
- Operational log sheet export

---

> Do **not** begin Phase 3b until the owner replies `approved phase 3`.
