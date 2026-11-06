# Bitbus discovery and implementation design

**Phase:** 0 — Discovery and design  
**Branch:** `feature/bitbus-discovery`  
**Scope:** design only; no runtime, schema, or dependency changes

## Executive summary

FuelTrack already has the hierarchy, generator master data (including `Generator.Model`), daily generator and site meter readings, tanks, a station-scoped warehouse ledger, work permits, installed-part history, notification delivery, module/permission registration, audit interception, and a React UI for each of those areas. Bitbus should extend those capabilities rather than replace them.

The major new domains are hourly operational readings, tank calibration/level history, feeders, independent service intervals/replacement events, station settings, and attachments. The existing warehouse and permit modules need targeted extension and hardening rather than wholesale replacement. No current entity represents feeder equipment; no general attachment/file-storage abstraction or offline write queue exists.

`SubStation` is the correct implementation of the customer term **Site** and `Station` remains **Station**. This is consistent with foreign keys from generators, tanks, meters, readings, and permits to `SubStation`.

## 1. Relevant existing map

### Architecture and cross-cutting conventions

| Area | Existing implementation | Bitbus impact |
|---|---|---|
| Domain / persistence | `FuelTrack.Domain` models; EF Core context in `src/FuelTrack.Infrastructure/FuelTrackDbContext.cs` | New persistent entities inherit `BaseEntity`; register a global soft-delete filter. |
| Audit | `AuditSaveChangesInterceptor` is registered in infrastructure DI | Reuse it; no second audit mechanism. |
| Scope | `IUserScopeFilterService` / `UserScopeFilterService` resolves Branch → Station → SubStation access | Every new read service must filter by reachable SubStation/Station IDs before projection. |
| Authorization | `PermissionEnforcementMiddleware` maps controller names to module/action keys; `ModuleRegistrySeed` seeds modules and `PermissionGenerator` creates permissions | Add module definitions, mapping entries, and page routes together in Phase 1. |
| DTOs / validation | Application DTOs are records; FluentValidation validators live under `Application/Validators` | Follow this pattern for all request DTOs. |
| Queries | Existing newer warehouse services use `AsNoTracking()` with projections, although several maintenance reads still use `Include` | New work uses projections only; do not copy the older `Include` pattern. |
| Migration | EF migrations in `Infrastructure/Migrations`; `MigrationRunner` applies the pending migration on startup | One reviewed, additive migration per schema phase; `dotnet ef migrations add` is the established generation path. |
| Tests | xUnit unit/service tests and API integration tests in `tests/FuelTrack.Tests`; Vitest component tests in `frontend/src` | Add calculation tests as pure units, authorization/scope tests as service/API integration tests, and focused UI tests. |

### Hierarchy, generator and meter data

| Requirement area | Existing code | Assessment |
|---|---|---|
| Branch → Station → Site | `Branch`, `Station`, `SubStation`; `UserScope` and `UserScopeFilterService` support this exact hierarchy | Reuse. **Site = `SubStation`** (D1). |
| Generator master | `Generator` has `SubStationId`, name, serial number, nullable `Model`, manufacturer, nominal/actual power, fuel rate, lookups, and controller navigation | Reuse. D9 needs no generator-model schema change. Normalize matching in one service (trim/case-insensitive model key), not distributed queries. |
| Generator hours / energy | `DailyReading` stores previous/current energy and hours; `ReadingService` creates and scopes it | Existing daily readings stay untouched. New end-of-day stream must become the authoritative input for the new due engine, without rewriting legacy calculations. |
| Controller | One optional `Controller` per generator, with `EnergyReading` and `OperatingHours`, plus Modbus configuration | Reference only; no `FuelTrack.ModbusGateway` changes. |
| Meters | `Meter` belongs to a `SubStation` and may be tied to a generator; stores meter type plus current hours/energy | Reuse master equipment where appropriate. It does not model a feeder, multiplier, or reading history. |
| Sent energy | `SubStationReading` is one exported-energy reading per `SubStation` / timestamp and links optionally to `Meter`; status/approval workflow exists | Extend by introducing a feeder-specific end-of-day meter-reading history; do not overload the single site-export aggregate. |

### Operations, tank and fuel data

| Requirement area | Existing code | Assessment |
|---|---|---|
| Generator readings | `DailyReading`, `ReadingService`, `ReadingsController`, `AddReading.jsx` | Reuse only as legacy daily flow. Its uniqueness currently is `(GeneratorId, ReadingDate)` and it requires a status workflow, so it cannot satisfy unapproved hourly Bitbus readings. |
| Fuel tanks | `FuelTank` belongs to `SubStation`, with name/code/capacity/min/dead/actual stock. `FuelTankService`, `FuelTanksController`, `TanksManagement.jsx` exist. `FuelTankType` enum exists but is not used by `FuelTank`. | Extend `FuelTank` additively for geometry/orientation/factor/type. Add a separate immutable tank-level reading entity; do not mutate `ActualStockLiters` as the reading history. |
| Fuel movement | Fuel and oil transaction services, stock summaries and reports exist | Reuse only for existing fuel/oil movement reporting. Tank-level measurements and reconciliation are new. |
| Feeders | No `Feeder` entity, service, controller, page, or persistence model found | New master entity and reading stream. |
| Attachments | No general attachment entity, endpoint, `IFormFile` upload flow, or application file-storage abstraction found (tenant logo upload is separate administration behavior) | New shared attachment abstraction is required in Phase 1; storage must be tenant-ready and outside web root. |
| Offline/sync | PWA uses `vite-plugin-pwa`/`generateSW` for static asset caching; no IndexedDB queue, mutation retry protocol, or server idempotency record found | New in scope for idempotent requests; offline synchronization itself is not fully specified and remains a reported gap. |

### Warehouse and maintenance data

| Requirement area | Existing code | Assessment |
|---|---|---|
| Parts catalog | `SparePart` contains bilingual name, part number, category/unit lookup, price, global `MinStockQuantity`, and legacy replacement interval/priority fields | Extend catalog for serialization/category metadata as needed. Do not use the legacy part interval fields for the new service engine; `ServiceIntervalDefinition` remains independent (D9). |
| Station stock | `WarehouseStock` has `(SparePartId, StationId)`, current/reserved quantities and filtered unique index | Reuse; D5 is already structurally aligned. Move/retain minimum stock per station × part using a new/additive `MinimumQuantity` on `WarehouseStock`; preserve legacy global min for compatibility until migrated deliberately. |
| Receiving | `WarehouseSupply` / items, `WarehouseSupplyService`, `/api/warehouse/supply`, `WarehouseSupply.jsx` | Reuse/extend. Confirming a voucher creates stock and ledger movement. |
| Issue requests | `DispatchRequest` / items include station, optional site and work permit, requested/approved quantities, status; `WarehouseTransactionService` manages ledger | Reuse/extend for explicit reduced-quantity reason, concurrency-safe execution, and serial-number allocation. |
| Ledger and reports | `WarehouseTransaction`, stock/reports services and warehouse pages already exist | Reuse report/export pipeline; add Bitbus report fields/aggregations. |
| Work permits | `WorkPermit` already supports site, optional generator, type/department/priority/status lookups, assignee user, tasks, issue requests, installed parts and logs. `WorkPermitService` generates a number and pages/controllers exist. | Extend rather than recreate. Existing number format/sequence requires replacement to meet D7 and must be made concurrency-safe. |
| Replacement history | `InstalledPart` records an installation and estimated replacement hour and is used by `AlertGeneratorService` / `SparePartReplacementAlertJob` | Keep as physical-part lifecycle where useful, but add independent `ServiceReplacementRecord` as the sole service-clock history. Do not make `InstalledPart` the service engine. |
| Alerts | `Notification`, recipients, scoped publisher, `AlertGeneratorService`, Hangfire jobs including `SparePartReplacementAlertJob` exist | Reuse notification transport and scheduling. Implement service due state transition de-duplication rather than the current 24-hour repeat-window behavior. |

### Frontend, localization, tests and exports

| Area | Finding | Design consequence |
|---|---|---|
| Routes/pages | `App.jsx` is the route hub; generator, tank, station-reading, warehouse, permit, maintenance dashboard and report pages already exist | Add small focused Bitbus pages/routes and navigation entries; reuse current query/client patterns. |
| i18n | `frontend/src/i18n.js` contains a single in-code `appTranslations` object with English/Arabic keys. Some legacy components still hard-code strings. | Phase 1 adds a `bitbus` namespace/key grouping in both languages and a parity test/audit. All new user-visible text must use keys. |
| RTL | Current app supports Arabic/English language switching and is already used in Arabic pages | New operator flow must be explicitly mobile/RTL tested. |
| Exports | Spare-parts CSV export and warehouse report export endpoints exist; report utility supports client-side export | Reuse this approach for new history and report exports. |
| Background jobs | Hangfire jobs run through API jobs, including low stock and installed-part alerts | Add due-alert processing using the same scheduler/notification boundaries. |

## 2. Requirement overlap and gap analysis

### 5.1 Operational data

| Requirement | Disposition | Rationale / implementation boundary |
|---|---|---|
| Hourly generator readings | **New** | `OperationalGeneratorReading` is separate from approval-based `DailyReading`; reference generator/site and enforce unique `(GeneratorId, SlotStartUtc)`. |
| Plausibility warnings / editable window | **New** | A validator/result warning contract and station policy are absent. |
| Tank setup and geometry | **Extend + new** | Add calibration metadata to `FuelTank`; add `TankLevelReading` snapshot records; create pure `TankGeometry`. |
| Tank photos | **New** | Depends on Phase 1 attachment abstraction. |
| Feeder master and slot load | **New** | New `Feeder` and `FeederLoadReading`. |
| End-of-day meter readings | **New / reference legacy** | Add `EndOfDayGeneratorReading` and `EndOfDayFeederReading`; retain legacy `DailyReading` and `SubStationReading` unchanged. |
| Deltas, reset behavior and produced-vs-sent | **New** | One pure calculation service. Derived output is computed on read; meter raw values and reset note are stored. |
| Idempotent one-screen save | **New** | New `OperationalSubmission`/idempotency key record plus an atomic command. |
| Generator history and export | **New / reuse export pattern** | New filtered operational history API plus CSV export. |

### 5.2 Service intervals and alerts

| Requirement | Disposition | Rationale / implementation boundary |
|---|---|---|
| Bilingual service items | **New** | Independent `ServiceItem` lookup entity/table, seeded idempotently; not part catalog rows. |
| Model × item intervals | **New** | `ServiceIntervalDefinition` references a model key and service item; `Generator.Model` already exists. |
| Replacement records | **New** | `ServiceReplacementRecord` is distinct from `InstalledPart`; one writer service owns it. |
| Baseline / early replacement | **New** | Implement in a pure due calculator and replacement command service. |
| Due dashboard / alert | **Extend + new** | Reuse `AlertGeneratorService` delivery and jobs; new due engine/state record or dedupe key is required. |

### 5.3 Inventory

| Requirement | Disposition | Rationale / implementation boundary |
|---|---|---|
| Parts/catalog and station stocks | **Extend** | Existing parts, stocks, receipts, dispatches and ledger already cover the core. |
| Min stock per station × part | **Extend** | Add `MinimumQuantity` at `WarehouseStock`; existing part-level field is insufficient for D5. |
| Receiving / issue approval | **Extend** | Existing workflow supports these, but must enforce approved ≤ requested, reduction comment, and idempotent/concurrent safe execution. |
| Serialized components | **New** | Add serial-instance/assignment records; supply-item text serial cannot support lifecycle/history by itself. |
| Service interval catalog link | **Extend** | Optional FK from `ServiceIntervalDefinition` to `SparePart`, added only in Phase 4. |

### 5.4 Maintenance

| Requirement | Disposition | Rationale / implementation boundary |
|---|---|---|
| Work permits and tasks | **Extend** | Existing permits/assignees/tasks/logs/issue links are the base. |
| Site sequence numbering | **Extend** | Replace generator implementation with site-code sequence allocation under a transaction/unique index; no yearly reset. |
| Assignees | **Reuse** | `WorkPermit.AssignedToId` already references an existing user. Scope validation is required. |
| Completion → replacement handoff | **Extend** | Permit completion calls `IServiceReplacementService`, not `InstalledPart` directly. |
| General-permit restrictions / readiness | **Extend + new** | Enforce generator-null rules for general permits; add informational readiness record/fields without touching existing generator status. |

### 5.5 Reports and stocktake

| Requirement | Disposition | Rationale / implementation boundary |
|---|---|---|
| Warehouse reports / export | **Extend** | Existing warehouse report service/page/export are reusable. |
| Stocktake cycle/variance adjustments | **Extend** | `PeriodicInventory` is currently fuel/oil-oriented; new warehouse stocktake session/items should be separate to avoid changing its behavior. |
| Operational/service reports | **New** | New read projections backed by the new operational and service tables. |

## 3. Proposed model and migration sequence

All new entities extend `BaseEntity`, have an EF soft-delete filter, and use `activeFilter` for every new filtered unique index.

### Phase 1 — foundations

1. `StationOperationalSettings` (one-to-one `Station`): reading interval/configuration, IANA timezone, produced-vs-sent warning threshold, editable window, oil-pressure unit. Add filtered unique index on `StationId`.
2. Attachment foundation: `Attachment` (tenant-addressable storage key, validated MIME, byte size, SHA-256, metadata); `AttachmentLink` (attachment → entity type/id, sort order). Do not store client filenames as trusted paths.
3. Seed module definitions/lookups/service-item basics and i18n navigation keys. No Phase 1 operational entities.

### Phase 2 — operational data

1. Add nullable calibration fields to `FuelTank`: `TankOrientation`, diameter/length/factor, computed capacity, and type lookup/enum reference. Preserve existing `CapacityLiters` and backfill only through an explicitly approved migration plan.
2. `OperationalSubmission` with globally unique client request ID for idempotent composite writes.
3. `OperationalGeneratorReading` (`GeneratorId`, `SlotStartUtc`, `IsRunning`, nullable electrical readings, raw fields, warnings metadata) with filtered unique `(GeneratorId, SlotStartUtc)`.
4. `TankLevelReading` (`FuelTankId`, timestamp, liquid level, computed litres, calibration snapshot, submission) with a filtered unique `(FuelTankId, ReadingAtUtc)`.
5. `Feeder` (`SubStationId`, code/name, optional linked meter) with filtered unique `(SubStationId, Code)`; `FeederLoadReading` with filtered unique `(FeederId, SlotStartUtc)`.
6. `EndOfDayGeneratorReading` and `EndOfDayFeederReading`, each retaining raw meter, business date, reset flag/note, and immutable delta result state; filtered unique `(entity, BusinessDate)`. The last generator reading feeds due calculations.
7. `ProducedVsSentSnapshot` is **not** persisted; calculate in a query/service.

### Phase 3 — service interval engine

1. `ServiceItem` (bilingual name/code; filtered unique code).
2. `ServiceIntervalDefinition` (`GeneratorModelKey`, `ServiceItemId`, interval hours, importance, warning lead, optional catalog part link initially null) with filtered unique `(GeneratorModelKey, ServiceItemId)`.
3. `ServiceReplacementRecord` (`GeneratorId`, `ServiceItemId`, hours, occurred date, optional work permit, early flag/reason) plus index `(GeneratorId, ServiceItemId, ReplacementHours)`.
4. `ServiceDueNotificationState` only if notification uniqueness cannot be represented safely by existing records; unique `(GeneratorId, ServiceItemId)` to record state transitions rather than a time-window repeat.

### Phase 4 — inventory

1. Add nullable/additive `MinimumQuantity` to `WarehouseStock` for D5.
2. `SerializedPartInstance` linked to spare part, station, optional receipt/issue; unique filtered serial per part/manufacturer scope after owner decides normalization rules.
3. Add optional `ServiceIntervalDefinition.SparePartId` FK after catalog extension is complete.
4. Add filtered uniqueness / optimistic concurrency mechanism for dispatch execution after verifying provider-compatible approach. Use a database transaction and conditional quantity update to prevent negative stock.

### Phase 5 — maintenance

1. Add `WorkPermit.SequenceNumber` (per SubStation) and filtered unique `(SubStationId, SequenceNumber)`; calculate display `SiteCode-WP-00001` from data. Backfill existing permits only through an approved migration.
2. Add explicit permit completion/readiness data and optional `WorkPermitAssignee` junction only if multiple assignees are approved; otherwise preserve single existing assignee.
3. Add service replacement record link/FK from permit completion path; no changes to generator status.

### Phase 6 — reports and stocktake

1. `WarehouseStocktake` and `WarehouseStocktakeItem` separate from fuel/oil `PeriodicInventory`, with variance/adjustment ledger references and period type.
2. Add only query-supporting indexes identified against production-like volumes.

## 4. Decision register and owner decisions

| ID | Finding / recommendation | Default for implementation if unanswered |
|---|---|---|
| D1 | Verified: `SubStation` owns generators/tanks/meters/permits, so it is Site; `Station` is station. | Site = `SubStation`; Station = `Station`. |
| D2 | Tanks currently have only capacity; geometry is new. Geometry inputs should be decimals in cm; factor belongs on the tank. | Horizontal/vertical, capacity = geometric volume × factor ÷ 1000. |
| D3 | Existing readings do not model reset semantics. | Reject a lower value unless reset is flagged with note; return null delta. |
| D4 | Existing `DailyReading` does not distinguish stopped running. | `IsRunning=false` permits null electrical fields. |
| D5 | Stock is already station × part; min stock is currently global on `SparePart`. | Add per-stock minimum; leave old part field compatible. |
| D6 | Existing dispatch supports partial approval UI/workflow; mandatory reduced-quantity comment must be enforced. | Approve ≤ request with comment if reduced; never exceed availability. |
| D7 | Existing permit number generation needs verification/replacement for exact business format and parallel safety. | Per Site continuous `SiteCode-WP-{seq:D5}`. |
| D8 | `WorkPermit.AssignedToId` already targets users; no employee/technician model was found. | Scoped existing users. |
| D9 | `Generator.Model` exists and is nullable. | Model × service item; normalize through one model-key function. |
| D10 | `InstalledPart` has a reason field but no interval-based service event semantics. | Require a reason if prior replacement delta is below the interval. |
| D11 | Existing generator status must remain untouched. | `Ready`, `ReadyWithLimitations`, `NotReady`, informational only. |
| D12 | Legacy `DailyReading` has no active/apparent load fields. | Active kW required when running; apparent kVA optional. |
| D13 | No station-level oil pressure unit setting exists. | `bar`, configured per station. |
| D14 | Notification transport and jobs exist. | Compute on read/dashboard and send only once per due-state transition. |
| D15 | No station timezone/boundary setting exists. | Store UTC; use station IANA timezone to derive business day. |

### Decisions required before implementation

1. Confirm D2 tank shapes/calibration source: are all tanks cylindrical, or are rectangular/custom lookup tables required?
2. Confirm D7 for historical permits: should existing permits be left with their current number, or backfilled into the new site sequence when Phase 5 migrates?
3. Confirm the allowed image size and retention policy for operational photos. The brief requires type validation but does not set a size/retention value.
4. Confirm whether only one user is assigned per permit. The current model supports one; adding multiple assignees changes the data model.
5. Confirm whether feeder meters can have CT/PT multiplication factors and whether they are individually linked to imported meter records.
6. Confirm the editable/backdate window values and the permission name/roles allowed to override it.
7. Confirm station business timezone(s). The supplied default is per-station, but no values were provided.

## 5. Source-brief gaps and conflicts

The supplied English brief explicitly excludes solar integration, fuller sync/reconciliation work, and Modbus changes; none will be invented.

The pre-existing, untracked `docs/CODEX_PROMPT_BITBUS.md` contains a broader Arabic execution plan that conflicts with this supplied Phase-0 brief in several material ways (different phase plan, unrestricted reading times rather than hourly-by-default slots, a broader solar/offline scope, and different approval-flow expectations). It is treated as background only, not as an implementation instruction. Owner confirmation is needed before any requirement from it is adopted.

Additional gaps that affect data-model decisions:

- The exact meter rollover range and the permission/action required to flag a reset are not specified.
- No tank/feeder photo count, retention, object-storage provider, or upload size is specified.
- “Client should compress” does not set browser support or quality/target-byte policy.
- “At most once per state change” needs final due-state definitions (for example, `NotDue`, `DueSoon`, `Overdue`) and whether moving backward after a new replacement should notify again.
- The brief does not say whether generator models are controlled vocabulary values or free text; this affects duplicate interval definitions.
- Existing warehouse serial text is single-value per receipt item; serialized quantity greater than one needs a per-instance scan/import workflow.

## 6. Risks and guardrails

- Existing `DailyReading` and `SubStationReading` use approval status flows and uniqueness rules incompatible with the new direct-save hourly flow. Treat them as legacy, not tables to reshape.
- Existing maintenance read services contain `Include` chains; new Bitbus query services must use projections as required by the brief.
- Existing warehouse stock/transaction services must be reviewed and strengthened for concurrent approval before Phase 4; a check-then-write sequence alone cannot guarantee non-negative stock.
- The current notification service has user-facing strings in backend code. New Bitbus notifications should use localized templates/keys or a structured notification payload design decided in Phase 1.
- Existing module names/middleware mappings use broad `warehouse` and `maintenance` modules. New granular permissions need a deliberate module registration decision, not an accidental bypass.
- `activeFilter` is provider-aware in `FuelTrackDbContext`; every new unique soft-delete index must use it. Current legacy unique indexes such as fuel tank/daily reading do not all follow this pattern; they will not be changed in this phase.

## 7. Phase 0 report

```text
Phase: Discovery and design   Branch: feature/bitbus-discovery   Commit: pending
1. Done: documented verified code map, reuse/extend/new analysis, proposed model and migration sequence, decision register, source gaps, and risks.
2. Not done (and why): no feature code by phase contract; no schema changes, migrations, dependencies, or UI changes were made.
3. Assumptions made / Decision Register defaults applied: D1 Site=SubStation; D5 station×part inventory; D8 scoped users; D9 existing Generator.Model; all other decision defaults retained exactly as supplied pending owner response.
4. Decisions needed from the owner: the seven decisions in section 4 plus confirmation that the English brief supersedes conflicting legacy documentation.
5. Tests: added 0. `dotnet build FuelTrack.sln` and `dotnet test FuelTrack.sln` could not restore because NuGet package access to `api.nuget.org` is blocked (`NU1301` / socket-permission and TLS EOF failures); no project test ran. `npm run build` is blocked by the local PowerShell execution policy; its `npm.cmd run build` equivalent then failed because `frontend/node_modules` is absent and `vite` cannot be resolved. The required `npm.cmd ci` restore was not authorized, so frontend build/test could not run. These are environment/dependency-restore blockers, not test failures caused by this documentation-only phase.
6. Migrations: none; destructive operations: none.
7. Risks and follow-ups: listed in section 6; Phase 1 must establish configuration, attachments, module/i18n structure, and permission strategy before operational data.
8. Suggested manual check in the UI: inspect current generator, tank, station-reading, warehouse, work-permit, and notification pages to validate the documented reuse targets before approving Phase 1.
```
