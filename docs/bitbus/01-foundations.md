# Phase 1 — Foundations

**Branch:** `feature/bitbus-foundations`  
**Status:** complete — verified on 2026-11-07

## Delivered

- Added generic feature configuration with a safe default: `operational_data` is disabled and remains in the `legacy` mode until an authorized customer enables it. The operational-data endpoints are feature-gated; legacy warehouse and maintenance behaviour is unchanged.
- Added the `operational_data`, `inventory`, `maintenance`, and hidden `solar` module definitions and the required action permissions. Operational settings, attachment capture, review, export, configuration, and meter-reset actions have deliberate middleware mappings.
- Added idempotent, bilingual reference-data seeding for part categories, maintenance departments, and service items. Existing reference data is reused rather than replaced.
- Added station operational settings with a one-to-one model and defaults for `Asia/Aden`, oil-pressure unit, balance threshold, 65 KB reading-photo limit, and reviewer separation.
- Added a shared attachment foundation: storage keys are tenant-addressable, physical files are stored outside web root, and uploads accept only verified JPEG/WebP signatures within the hard 65 KB server limit. Attachments can be linked to future operational entities.
- Added an idempotency-request store and service for future composite operational submissions.
- Added the operational navigation shell, Arabic/English translations, translation-key parity test, and responsive dark/amber/teal styling based on the approved design references. The shell intentionally contains no Phase 2 data-entry flow.
- Added one additive migration: `20261106213503_AddOperationalFoundations`.

## Compatibility boundaries

- No legacy module, endpoint, table, job, or documentation file was removed. The source brief requires separate explicit approval for legacy removals.
- `DailyReading`, the legacy readings UI, and `SparePartReplacementAlertJob` were not altered. The feature switch prevents a customer from reaching the new operational path until it is deliberately enabled.
- No Modbus Gateway code was changed.
- No Phase 2 operational readings, tank geometry, feeder entities, service-interval engine, warehouse changes, or work-permit changes were included.

## Additional regressions corrected

- Periodic inventory reports now include the current station business-day snapshot when no end date is supplied.
- Spare-part category filtering accepts either the display name or the stable category code.
- Integration tests now use an in-memory Data Protection provider, so password-reset tests do not require access to a developer's external DPAPI key directory.

## Verification

Executed by the owner:

```text
dotnet restore FuelTrack.sln                         succeeded (warnings only)
dotnet build FuelTrack.sln --no-restore              succeeded (warnings only)
dotnet ef migrations has-pending-model-changes        no pending model changes
dotnet test FuelTrack.sln --no-build --no-restore    281 passed, 0 failed, 10 skipped
frontend: npm.cmd run test -- --run                   49 passed, 0 failed
frontend: npm.cmd run build                           succeeded
```

The warnings are existing dependency/security advisories (`NU1902`/`NU1903`), npm audit findings, and existing compiler/analyzer warnings. They are not Phase 1 test failures.

## Migration note

The EF model snapshot was brought current with the reviewed additive migration. A historical manual migration is not discoverable by the EF migration assembly for an ad-hoc script range; no historical migration was edited or recreated in this phase.

## Next gate

Phase 2 requires owner approval. It will add operational readings, tank calibration/level history, feeders, end-of-day readings, and the atomic submission flow on top of these foundations.
