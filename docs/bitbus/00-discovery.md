# Bitbus discovery revision and migration design

**Phase:** 0R — revised discovery

**Branch:** `feature/bitbus-discovery`

**Authoritative brief:** `docs/FuelTrack for Bitbus Operational Data, Inventory and Maintenance.md` (v3)
**Scope:** discovery, removal proposal, design reconciliation, and migration planning only. No runtime, schema, dependency, or legacy-code changes are made in this phase.

## Executive summary

The v3 brief replaces all earlier Bitbus instructions. Bitbus operational data will be a free-time, photo-required, review-based pipeline: a submitted line is `Pending`; only an `Approved` line is official and may feed deltas, energy balance, tank state, or the service-due engine.

`SubStation` is the customer **Site** and `Station` is the customer **Station**. Existing `DailyReading` and `SubStationReading` reuse the approval pattern, but their daily uniqueness/data shape cannot implement arbitrary capture time, per-line photos, or mixed idempotent submissions. Warehouse and work permits remain extension targets; the installed-part clock cannot remain the service-due engine.

The approved visual references are present in `docs/design/bitbus/`. They control layout and visual language; v3 controls behaviour. Tenant schema migration is a separate PostgreSQL SQL-upgrade path, not ordinary EF startup migration, and needs a coordinator before the first Bitbus schema change.

## 1. Verified implementation map

| Area | Current implementation | Phase impact |
|---|---|---|
| Architecture | Domain → Application → Infrastructure → API; React/Vite frontend. `FuelTrack.ModbusGateway` is separate. | Follow the established layers; never modify ModbusGateway. |
| Conventions | `BaseEntity`, soft-delete filters, `activeFilter` filtered unique indexes, audit interceptor. | Required for all new persistent entities. |
| Scope/authorization | `IUserScopeFilterService`, `PermissionEnforcementMiddleware`, `ModuleRegistrySeed`, permission generation. | Apply scope to every new read and write service; register generic module/action permissions. |
| Hierarchy | Generators, tanks, meters and permits use `SubStation`; `WarehouseStock` uses `Station`. | Site = SubStation; warehouse is Station-scoped. |
| Generator/meters | Nullable `Generator.Model`, controllers and `Meter` master data exist. | Reuse masters; add feeder/solar masters and one normalized model-key function. |
| Legacy readings | `DailyReading`/reading service/controller and `SubStationReading` service/controller have approval statuses, but date-based unique rules. | Reuse approval semantics only; build separate free-time records. |
| Tanks/fuel | `FuelTank` has capacity/current stock; fuel/oil transactions and reports exist. | Add calibration and immutable level snapshots. Pending observations cannot change state. |
| Warehouse | Spare parts, Station × part stock, supply, dispatch, transaction ledger and reports exist. | Extend and harden transaction/concurrency behaviour. |
| Maintenance | `WorkPermit`, tasks, logs, one assignee and `InstalledPart` exist. | Extend permit workflow; replace numbering algorithm and due-service source after approval. |
| Jobs/notifications | Notification transport, `AlertGeneratorService`, Hangfire and `SparePartReplacementAlertJob` exist. | Reuse transport; retire installed-part alert source at service-engine cutover. |
| Attachments/idempotency | No generic storage abstraction or idempotency store found. | Phase 1 foundations. |
| Frontend/i18n | Routes/sidebar, i18n, RTL, existing query/style patterns exist. | Map approved design tokens into the existing system, not a parallel UI system. |

## 2. v3 operational architecture

```text
Operator submission (Site + client request id)
  ├─ generator/controller readings + photo(s)
  ├─ tank-level readings + photo(s)
  ├─ feeder load/meter readings + photo(s)
  └─ generic meter readings + photo(s)
        ↓
Pending only — no official calculation or state change
        ↓ scoped reviewer (different from submitter)
approve atomically / reject a line with mandatory reason
        ↓
Approved immutable raw values → deltas, balance, tank state, due engine, history/export
```

Required invariants:

- Free capture time: `CapturedAtUtc` is the actual reading time, `ReceivedAtUtc` is server-set, and `BusinessDate` derives from the Station timezone. There are no slots or reading interval.
- Each line requires at least one JPEG/WebP photo. The server rejects a line without one and hard-rejects a file above 65 × 1024 bytes.
- Pending/rejected values never influence official projections. Submission and review decisions are idempotent.
- An approved edit becomes a new pending version; the previously approved version remains official until its replacement is approved.
- Approval rechecks meter order against neighbouring approved captures, not insertion/approval order.

### Planned model sequence

| Phase | Work | Boundary |
|---|---|---|
| 1 | Generic customer modules, permissions, lookups, settings, attachments, idempotency, i18n, shells/tokens. | No reading interval; timezone defaults to `Asia/Aden`. |
| 2 | Submission/lines/versions, tank calibration, feeders, generic meters, capture UI/history. | Pending only; photo required. |
| 3 | Review, approval, notifications, deltas, balance board, history/export. | Approved-only calculations. |
| 4 | Independent service interval/replacement/due engine. | Approved running-hours meters only. |
| 5–7 | Inventory, maintenance, reports/stocktake. | Extend existing domains. |
| 8 | Solar sources/import/export. | Same capture/review pipeline; no fuel/service role. |

## 3. Final decisions applied

| Decision | Applied design |
|---|---|
| D1 | Site = `SubStation`; Station = `Station`. |
| D2 | Main/daily tanks are vertical cylinders with a conical bottom; level is measured from cone apex, using `D`, `Hcyl`, `Hc`, factor in cm. |
| D3 | A lower meter value requires reset flag, note and permission; delta is null. |
| D4/D12/D13 | `IsRunning=false` permits null electrical fields; active kW required when running; oil pressure defaults to bar per Station. |
| D5/D6 | Minimum stock is Station × part; partial issue approval needs a reason and cannot go negative. |
| D7/D8 | New permits are per-Site continuous `SiteCode-WP-00001`; old numbers are unchanged; multiple scoped assignees use a junction. |
| D9/D10/D14 | Model × service item, one normalized model key, strict early-replacement reason, notifications on state transition only. |
| D15 | UTC storage and `Asia/Aden` default station timezone. |
| D16 | JPEG/WebP, maximum 65KB, browser-canvas compression, no retention limit. |
| D17 | No edit/backdate window or override; only skew warning. |
| D18/D19 | Reviewer differs from submitter by default; review is a submission with per-line rejection and reason. |
| D20/D21 | Feeder multiplier defaults to 1 and is snapshotted; solar is Phase 8 scope. |

## 4. Approved design inventory and reconciliation

| File | Covers | Findings |
|---|---|---|
| `Main.dc.html` | Mobile RTL generator entry prototype. | Generator, time, meter/hours/pressure/temperature/load fields, photo panel, dark theme, submit action. |
| `شاشة المشغل — إدخال قراءة مولد@3x.png` | Rendered operator reference. | Same mobile hierarchy and bottom navigation treatment. |
| `Dashboard.dc.html` | Desktop RTL balance/reviewer dashboard prototype. | Sidebar, KPI cards, source/sink bars, alerts, generator/feeder tables. |
| `لوحة موازنة الطاقة@3x.png` | Rendered balance-board reference. | Desktop proportions and visual states. |

Phase 1 token mapping: dark layered surfaces, amber primary accent (`#F5A524`), green/amber/coral status colours, sky-blue solar, 12px control radius, 16px card radius, tabular numerals, Arabic-first RTL, and 44px minimum touch targets.

| Design observation | v3 rule | Proposed handling / owner decision |
|---|---|---|
| Prototype says entry time is recorded automatically on submit. | v3 distinguishes editable capture time and server received time. | Show editable capture time; show received time to reviewer/audit. v3 prevails. |
| One visible photo panel. | One or more photos are required. | Preserve visual treatment and add counter/add-more control. |
| Offline/sync badge and navigation. | Full offline queue/sync is out of scope. | Show connectivity only; no claimed queue or sync workflow. |
| Dashboard hourly chart. | Capture has no hourly slots. | Ask whether display-only time buckets are allowed; they must not impose reading cadence. |
| Solar cards/series appear in board. | Solar is Phase 8. | Hide solar-specific content until Solar capability is enabled. |
| “Last synchronization” label. | Official figures are approval-driven. | Rename to “last approved-data update” unless owner defines another meaning. |
| Tank discrepancy alert. | No reconciliation rule exists in v3. | Do not invent an alert. |

## 5. Legacy removal/retirement list — approval required

Development mode permits destructive changes, but **nothing below is removed in Phase 0R or Phase 1 without explicit owner approval**.

| Candidate | Reason | Verified impact | Recommendation |
|---|---|---|---|
| `DailyReading`, `ReadingService`, `ReadingsController`, daily routes/pages/indexes | Cannot model free time, photos, submission review, generic subjects or idempotency. | `Generator.DailyReadings`, analytics/efficiency, archive/query/archival services, dashboard/report queries and navigation. | Retire together only after Phase 3 replacement capture/review/history is proven. |
| `SubStationReading`, service/controller, module/page/indexes | One aggregate site record cannot model feeder subjects, meter photos or multiplier snapshots. | Its approval endpoints, notifications, summaries/reports/dashboard routes. | Retire after Phase 3 feeder/meter history and balance exist. |
| `DailyReadingArchive` and daily-reading archival/query paths | Archive targets the obsolete table. | Compliance/export counts and scheduled archival. | Rewrite with an approved raw-reading retention policy; v3 leaves raw retention open. |
| `InstalledPart`, installed-part service/controller/DTOs and permit links | Part-instance estimated hours are not model × service-item replacement clocks. | Work permit projections, maintenance dashboard, dispatch item links. | Retire as the due source in Phase 4; owner decides whether physical install history survives separately. |
| `SparePartReplacementAlertJob` and `CheckInstalledPartsReplacementAsync` | Would double-alert after new due engine. | Notification counts and maintenance dashboard due/upcoming widgets. | Disable/remove at Phase 4 cutover in the same deployment as new state transitions. |
| `SparePart.ReplacementIntervalHours` usage | New intervals belong to independent model × service item definitions. | Spare-part service, warehouse projections, installed-part service. | Do not use for new rules; remove only after approved catalog/service link. |
| Existing `GeneratePermitNumberAsync` year/prefix scan | Wrong format and race-prone. | Permit creation, notification/display/search expectations. | Replace in Phase 6 with transactional Site counter and filtered unique sequence; keep existing permit numbers. |

Not proposed for removal: `WorkPermit`, warehouse supply/dispatch/ledger, notifications, audit, users/roles, generator/meter/tank masters, fuel movements and maintenance logs.

## 6. Tenant migration process

### Verified current behaviour

1. Single-tenant `Program.cs` runs EF `Database.MigrateAsync()` (or SQLite `EnsureCreatedAsync`) at startup; `MigrationRunner` likewise handles one resolved DbContext.
2. In multi-tenant mode startup deliberately skips this migration because it has no tenant request context.
3. Tenant connections are built by `TenantResolutionMiddleware` from Platform metadata and credentials.
4. `TenantDatabaseBootstrapper` provides the baseline. `TenantSchemaMigrator.UpgradeAsync` applies trusted, versioned PostgreSQL SQL upgrades inside one transaction advisory lock, validates checksums/version history and rolls the affected tenant back on failure.

### Required Phase 1 prerequisite

EF migrations do not automatically reach tenant schemas, and no fleet migration coordinator exists. Before the first Bitbus schema change:

1. Produce a reviewed EF migration for single-tenant development **and** a matching reviewed tenant PostgreSQL upgrade from one logical change.
2. Add a privileged coordinator that enumerates eligible Platform tenants, resolves trusted credentials, invokes the tenant upgrade under its lock, and records per-tenant outcome.
3. Treat a fleet rollout as resumable: each tenant is atomic; a failed tenant stays on its old version, is reported, and is not marked healthy/eligible until retry succeeds.
4. Run under controlled deployment/maintenance, never request parameters. Add proof tests for success, checksum mismatch, statement-failure rollback, retry and activation block.

## 7. Module and permission strategy

`ModuleRegistrySeed` currently holds global definitions including legacy `daily_readings`, `station_readings`, broad `warehouse`, and broad `maintenance`; it does not itself provide a generic per-customer capability toggle.

Phase 1 should add generic definitions plus tenant-scoped capability/configuration records:

| Module | Scope | Actions |
|---|---|---|
| `operational_data` | Site/Station | view, capture, edit/withdraw pending, review, approve, reject, export, configure, flag_meter_reset |
| `inventory` | Station | view, catalog/manage, receive, request issue, approve issue, stocktake, report, configure |
| `maintenance` | Site/Station | view, create/edit, transition/close, assign, record replacement, configure intervals, report |
| `solar` (Phase 8) | Site | view, capture, review, configure, export |

Keep legacy warehouse/maintenance permissions on legacy endpoints until their Phase 5/6 replacements are ready. New controllers use only the new keys and capability checks. New operational capture must not share routes/actions with legacy daily/station entries.

## 8. Remaining owner questions

1. Confirm every tank has the cone **only at the bottom** and level is measured from its apex.
2. May the dashboard’s visual hourly chart use display-only buckets from free-time approved data, leaving gaps empty?
3. Does “last synchronization” mean connectivity only, or should it become “last approved-data update”?
4. Should physical `InstalledPart` history remain after the new service engine cutover?
5. Confirm the solar default of one output meter per `SolarSource` and no instantaneous solar kW.
6. Approve/amend/reject the removal list in section 5.
7. Approve the tenant migration-coordinator direction before Phase 1.

## 9. Risks and guardrails

- Rebuild prototypes with existing React patterns; never copy the HTML or add a styling framework.
- Every official projection must filter Approved values only.
- 65KB with a 960px readability floor may fail for some photos: offer crop/retake, never silently reduce below readable quality.
- Reset is an explicit permission/note exception; approval order never replaces timestamp order.
- Solar import is a source; its dedicated feeder is still one export feeder—do not double count it.
- The superseded Phase 1 draft is preserved in a reversible Git stash: `wip: superseded Bitbus phase 1 foundations`; it is not part of this phase.

## 10. Phase 0R report

```text
Phase: Revised discovery (0R)   Branch: feature/bitbus-discovery   Commit: pending
1. Done: v3-based architecture, final decisions, approved-design inventory/conflicts, legacy-removal proposal, tenant migration analysis, and module/permission strategy.
2. Not done: no runtime/schema/dependency/legacy-code change by Phase 0R contract; removals await owner approval.
3. Decisions applied: v3 D1–D21.
4. Owner decisions: section 8; especially removal list, tank assumption, dashboard aggregation, tenant coordinator.
5. Tests: NuGet restore was repaired with `dotnet restore FuelTrack.sln --ignore-failed-sources --force-evaluate`; `dotnet build FuelTrack.sln --no-restore` then passed. Frontend build passed and Vitest passed (10 files, 48 tests). Existing backend tests still fail outside this documentation change: two `ReportServiceTests` periodic-inventory assertions return no rows, and `AuthApiTests.ForgotPassword_ValidEmail_ReturnsOk` returns 500 instead of 200; ten database/view tests are skipped. Existing package-vulnerability/compiler warnings were also reported; no Bitbus code was compiled or tested in this documentation-only phase.
6. Migrations: none; destructive operations: none.
7. Design conflicts: section 4.
8. Risks/follow-up: sections 6 and 9; Phase 1 starts only after approval and a working build/test environment.
9. Manual check: review this report against the four files in docs/design/bitbus/.
```
