# Phase 1 — Foundations

**Branch:** `feature/bitbus-foundations-remediation`
**Status:** delivered; owner verification required

## Delivered

- Added generic customer feature configuration with a safe default: `operational_data` is disabled and remains in `legacy` mode until explicitly enabled. Legacy daily readings, warehouse behaviour, and maintenance alerts are unchanged.
- Added the `operational_data`, `inventory`, `maintenance`, and hidden `solar` module definitions. Existing warehouse controllers remain governed by the existing `warehouse` module; the new `inventory` module is intentionally not yet mapped to those legacy endpoints.
- Added idempotent bilingual reference-data seeding for part categories, maintenance departments, and service items. The seeder is constrained to those categories and preserves unrelated lookup data.
- Added a one-to-one `StationOperationalSettings` foundation, with `Asia/Aden`, oil-pressure unit, balance threshold, 65 KB reading-photo limit, and reviewer separation defaults.
- Added shared private attachment storage and attachment links. Files are tenant-addressable, outside web root, and restricted to verified JPEG/WebP signatures within the hard 65 KB limit.
- Added an idempotency store for future composite submissions. The key is uniquely scoped to **(tenant, authenticated user, client request ID)**; `Guid.Empty` is the explicit single-tenant scope.
- Added the operational navigation shell, bilingual Arabic/English keys and parity test, and responsive dark/amber/teal styling based on the approved Bitbus visual references. No Phase 2 entry flow is included.

## Deliberately separate regression fixes

The following fixes were discovered while verifying Phase 1 but are not Phase 1 functionality. They are kept in a separate sibling commit so the foundations commit remains reviewable:

- Station-local business-day default for periodic-inventory reports.
- Spare-part category filtering by either display name or stable code.
- Ephemeral Data Protection keys in the integration-test host only.
- Existing warehouse test expectation correction and periodic-inventory number formatting alignment.

## Compatibility boundaries

- No legacy endpoint, table, job, or documentation file was removed.
- `DailyReading`, its legacy UI, and `SparePartReplacementAlertJob` are unchanged. The feature setting prevents access to the new operational path until deliberate customer activation.
- No Modbus Gateway code was changed.
- No Phase 2 operational readings, tank geometry, feeder entities, service-interval engine, warehouse changes, or work-permit changes are included.
- The old `inventory` module is not a replacement for `warehouse`; permission migration is deferred to the inventory phase. Existing maintenance endpoints retain their established `maintenance` mapping.

## Migrations

1. `20261106213503_AddOperationalFoundations` creates only Phase 1 foundation tables: attachments and links, customer feature settings, station operational settings, and idempotency requests.
2. `20261107101500_ScopeIdempotencyRequests` additively corrects the idempotency uniqueness boundary to `(TenantId, UserId, ClientRequestId)`.

The EF snapshot was updated to represent the current model. Its update also absorbed historical model metadata that had drifted from the prior snapshot; no unrelated DDL was placed in either migration `Up()` method, but the snapshot correction remains historical migration-accounting debt. The legacy migration chain is SQL Server-specific while tenant schemas use PostgreSQL, so automatic tenant rollout is **not implemented**. This must be designed as a provider-compatible migration strategy before multi-tenant deployment.

## Tests added

- Attachment validation: verified signature/type and hard-size acceptance/rejection.
- Idempotency replay and changed-command conflict.
- Idempotency isolation across tenant and user scopes.
- Reference-data idempotency and bilingual stable codes.
- Reference-data lookup-category scope: unrelated legacy lookup data remains untouched.
- Station operational-settings authorization scope.
- Customer feature activation leaves legacy readings untouched.

Provider-compatible tenant migration needs PostgreSQL/platform integration coverage before it is implemented; this is not simulated by the InMemory provider.

## Verification required from owner

No restore, build, migration, or test command was run during remediation, by request. Run the following from the remediation branch:

```powershell
dotnet build FuelTrack.sln --no-restore
dotnet ef migrations has-pending-model-changes --project src/FuelTrack.Infrastructure --startup-project src/FuelTrack.API
dotnet test FuelTrack.sln --no-build --no-restore
cd frontend
npm.cmd run test -- --run
npm.cmd run build
```

Do not enable multi-tenant migration rollout until a provider-compatible strategy has been implemented and verified against a disposable Platform database with at least two tenant schemas.

## Next gate

Phase 2 remains blocked until the owner replies exactly: `approved phase 2`.
