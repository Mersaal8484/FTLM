# Commercial power-station mode — Phase 0 feasibility

**Phase:** 0  
**Branch:** `feature/commercial-feasibility`  
**Commit:** the resulting local `docs(commercial)` commit (hash reported in the phase handoff)  
**Scope:** read-only feasibility and design. No runtime, schema, dependency, test, migration, tenant, or operational-code change was made.

## Executive decision

The existing `Branch → Station → SubStation` structure should **not** be re-keyed or removed for commercial mode. The evidence is substantial: `Station.BranchId` and the operational/maintenance entities' `SubStationId` are required relationships, `UserScopeFilterService` resolves access through all three levels, and the hierarchy reaches warehouse, permits, notifications, reports, dashboards, cache keys, APIs, and the frontend.

**Recommendation: Option A — additive commercial mode with one hidden technical Branch and one hidden technical SubStation per commercial Station.** Commercial users see, select, and are assigned only to Station. Existing foreign keys continue to point to the hidden SubStation; the technical Branch/Site are never shown in commercial navigation, filters, forms, reports, exports, permission pages, notifications, or translations. Enterprise is the default and unchanged. Switching modes changes presentation and assignment rules only, so it is reversible without data loss.

This is better than a schema-wide re-key because it contains the change to the mode policy, provisioning/activation, scope adapters, and UI selection. It avoids dual ownership and avoids rewriting the 189 source/test files that currently reference `SubStationId`.

## 1. Blast-radius map: Branch and Site (`SubStation`)

### Measured source surface

Read-only source searches on the current branch found the following file counts. Counts are an impact indicator, not a count of database columns: a file can contain multiple uses.

| Search term | Files with a match | Meaning |
|---|---:|---|
| `BranchId` | 83 | hierarchy, scope, warehouse/report filters, DTOs, services, APIs and tests |
| `SubStationId` | 189 | the principal ownership key for operational data, maintenance and many legacy flows |
| `SubStation` | 256 | entity, navigation, services, controllers, reports, jobs, frontend and tests |
| `Branch` | 117 | hierarchy, administration, filters, permissions and tests |

### Core model, constraints and indexes

| Surface | Evidence | Commercial impact |
|---|---|---|
| Required hierarchy | `src/FuelTrack.Domain/Models/Station.cs:5-11` requires `BranchId`; `SubStation.cs:5-17` requires `StationId`. | A Station cannot exist without a Branch in the current model; a site cannot exist without a Station. |
| Asset ownership | `Generator.cs:7,26`; `FuelTank.cs:7,34`; `Meter.cs:7,17`; `Feeder.cs:5,11`; `WorkPermit.cs:6,24`; `DispatchRequest.cs:5,17`; `Notification.cs:5,15`. | Generators, tanks, meters, feeders, permits and notifications directly or indirectly depend on Site. |
| EF foreign keys | `FuelTrackDbContext.cs:195-213` defines Branch→Station and Station→SubStation; `:221-253` binds operational submissions and feeders to Site; `:953` binds permits to Site. | Re-keying would affect all foreign keys and delete rules. |
| Indexes and uniqueness | `FuelTrackDbContext.cs:697-705` indexes Generator.Site, Site.Station and Station.Branch; `:2600-2668` uses Site in operational submission/feeder indexes. | Commercial mode must preserve uniqueness and query performance through the technical Site. |
| Global filters | `FuelTrackDbContext.cs:119-121,164-165` add soft-delete filters for Branch, Station, Site and Feeder. | Hidden records must remain active, not be soft-deleted. |

### Scope, authorization and notifications

| Surface | Evidence | Risk if bypassed |
|---|---|---|
| User scope storage | `UserScope.cs:19-46` stores nullable Branch, Station and Site IDs. | Commercial scope assignment needs a station-only UI and validation; legacy rows must remain valid. |
| Scope resolver | `UserScopeFilterService.cs:13-178` expands Branch/Station/Site scopes to reachable Site and Station IDs. | All existing asset access relies on the technical Site list. A commercial adapter must make a Station scope yield its hidden Site only. |
| Current operational feeder scope | `FeederService.cs:9-101` checks Site access for create/update/deactivate/read. | The commercial service boundary must resolve Station input to its hidden Site before this code is reached. |
| Phase 4 re-check | `ServiceDueService.cs:12-89,230-249` injects and applies scope; `ServiceReplacementService.cs:15-44,301-341` scopes generator reads/writes; controllers delegate to those services. | This is evidence that P4-A01 was addressed after the old audit. Full controller/service matrix and API proof remain **UNVERIFIED** until owner runs tests. |
| Notifications | `Notification.cs:5-16` has optional `SubStationId`; indexes in `FuelTrackDbContext.cs:682-686`. | Commercial notification projection must show Station terminology and never reveal the hidden Site. |
| Permission middleware/seeds | `PermissionEnforcementMiddleware.cs` and `ModuleRegistrySeed.cs` are existing cross-cutting entry points; no commercial-mode gate exists. | A new mode policy must be enforced before exposing hierarchy routes and scope forms. |

### Operational, warehouse, reports and jobs

| Area | Evidence | Commercial consequence |
|---|---|---|
| Operational capture | `OperationalSubmission` uses `SubStationId` and is indexed by Site (`FuelTrackDbContext.cs:2600-2603`); `OperationalCaptureService.cs:42-61,418-419` resolves generators/feeders by that ownership. | Capture may keep the physical Site key internally; UI must accept/select Station only. |
| Feeder topology | `Feeder.cs:5-14`; `FuelTrackDbContext.cs:244-253,2654-2665`; `FeederService.cs:15-100`. | Existing Feeder has Site, code, name, multiplier and optional Meter only. It cannot declare Generator/Solar ownership. |
| Official energy/due engine | `MeterReading.cs:8-35`; `MeterDeltaService.cs:44-120`; `ServiceDueService.cs:98-105`. | Daily Closing data is the current official source; new backup rules must be integrated here. |
| Balance dashboard/cache | `EnergyBalanceService.cs:25-111` queries every Site under a Station; `TenantAwareCacheService.cs` and dashboard query code include hierarchy dimensions. | The Station is already the natural commercial aggregate; cache keys must omit hidden dimensions from user-visible state but remain tenant-safe. |
| Fuel reconciliation | `FuelLoadSafetyService.cs:18-188` reconciles main-tank transfers, daily-tank transfers and generator consumption by Site. | It does **not** persist physical end-of-day stocktake records. |
| Warehouse | `WarehouseStock` is Station × Part (`FuelTrackDbContext.cs:846-858`); `DispatchRequest.cs:5-24` retains both Station and Site. | Stock itself aligns with commercial Station; issue/permit flows need a hidden Site mapping. |
| Work permits/numbering | `WorkPermit.cs:5-38`; `WorkPermitService.cs:18-160`; `FuelTrackDbContext.cs:1119`. | Permits are Site-owned now; commercial display/sequence needs a deliberate Station-facing projection. |
| Legacy site readings/reports | `SubStationReadingService.cs`, `ReportService.cs:459-481`, `SubStationReadingsController.cs`, `StationReadingsReport.jsx`. | Commercial mode must hide these Site-labelled legacy pages or project them as Station reports. |
| Jobs and scheduler | `TenantMaintenanceScheduler.cs:11-91` dispatches isolated, eligible tenants; existing job/services reference Site-owned assets. | Reuse this scheduler; do not add a parallel commercial scheduler. |

### Frontend and user-visible blast radius

The reusable `frontend/src/components/common/HierarchicalFilter.jsx` renders Branch → Station → SubStation. Its hook consumers include report pages (`frontend/src/pages/reports/hooks/useReportFilters.js`, `useReportData.js`) and legacy report pages. Explicit hierarchy/selectors also exist in `TanksManagement.jsx`, `Users.jsx` scope assignment (`ScopeRow`), `WarehouseDispatch.jsx`, `WorkPermits.jsx`, `SubStationReading.jsx`, `WarehouseReports.jsx`, and report tabs. Arabic/English strings and legacy `Site` labels occur in `frontend/src/i18n.js` and several pages.

Commercial mode therefore needs a single hierarchy-mode-aware frontend source of truth rather than page-by-page hiding. The first implementation phase must audit all of these routes, exports, placeholders, navigation links, permission screens and notification renderers.

## 2. R1 option analysis — commercial hierarchy

| Option | Feasibility / permissions | EF & tenant-upgrade impact | Risk / test effort | Reversibility and existing tenants |
|---|---|---|---|---|
| **A. Hidden auto-provisioned Branch + hidden Site per commercial Station — recommended** | High. Existing Station-level `UserScope` already expands to Sites. Commercial policy assigns only Station scope and resolves the station's hidden Site internally. | Add a tenant `HierarchyMode`, an invariant/mapping for the hidden records, and an idempotent next tenant upgrade. Existing asset FKs remain unchanged. | Lowest. Requires comprehensive mode-aware UI/API scope regression tests, provisioning/activation tests and hidden-record guardrails. | Strong. Enterprise defaults unchanged; switching Commercial→Enterprise exposes the same data again. No FK/data rewrite. |
| **B. Re-key entities directly to Station; optional/remove Site** | Technically possible, but poor fit. Every Site-owned entity/service/report and scope query must change; tables such as permits and operational readings lose their required key. | Broad EF migration and a new tenant upgrade touching many foreign keys, indexes, uniqueness filters, DTOs and SQL. Existing tenant data needs backfill/decision for stations with several Sites. | Highest. The measured 189 `SubStationId` files make regression risk unacceptable. Scope/security and report bugs are likely. | Weak. Rolling back requires restoring/backfilling many keys. Existing enterprise tenants are exposed to migration regression. |
| **C. Hybrid: add `StationId`, retain nullable Site** | Looks incremental but creates two possible owners, ambiguous queries, and dual-write requirements. | Adds columns, constraints and backfill to almost every Site-owned table; requires invariants ensuring Station matches Site. | High. Every projection must decide precedence; scope leaks become easier. | Partial, but does not reduce complexity. Not justified for a one-Site-per-commercial-Station model. |

**Conclusion:** changing the database structure is allowed but not beneficial here. Option A safely reuses the current structure, limits the schema work to commercial policy/mapping records, and protects enterprise tenants. Option B is only defensible if the owner later requires several visible commercial logical sites per Station with independent ownership; that is not in this brief. Option C should not be selected.

## 3. R2–R8 gap verification

| Requirement | Current code state | Evidence and correction |
|---|---|---|
| **R2: feeder belongs to exactly one generator or solar source; balance per source** | **Partial / Missing** | `Feeder` has `SubStationId`, code/name, multiplier and optional Meter only (`Feeder.cs:5-14`). No `GeneratorId`, `SolarSourceId`, source type or check constraint exists. Station balance aggregates all generator/feeder readings (`EnergyBalanceService.cs:25-111`), not generator-specific feeder loss. |
| **R3: primary controller and backup meter** | **Missing** | `Generator` has optional `Controller` only (`Generator.cs:26-38`); `Meter` may link to Generator (`Meter.cs:7-19`), but no role/primary/fallback fields, no `PrimaryUnavailable`/reason, no dual-reading storage or tolerance rule. |
| **R4: hourly meter readings official policy** | **Implemented only as the current daily-closing policy; owner decision required** | `MeterReading` has Periodic/DailyClosing (`MeterReading.cs:13-18`); `MeterDeltaService.cs:49-72` and due services only use official `DailyClosing`; `ReadingApprovalPolicy.cs:10-41` makes DailyClosing the reviewable meter type. `Tier1RequiresApproval` exists (`StationOperationalSettings.cs:25-26`) but is not an hourly-official policy. |
| **R5: SolarSource/SolarImport and independent solar feeders** | **Missing** | `MeterReadingSubjectType.cs:3-8` has only three values—no SolarImport. `EnergyBalanceService.cs:75-89` defensively checks for a missing enum value, so current solar contribution is always zero. There is no `SolarSource` model, migration, controller or UI. |
| **R6: end-of-day physical fuel stocktake** | **Missing** | `FuelLoadSafetyService.cs:120-188` performs a read-time reconciliation of transfers/receipts/consumption. It has no persisted counted level/litres/photo/book balance/variance/approval record. |
| **R7: scope audit and matrix** | **Partial** | The old audit marks P4-A01 missing (`docs/bitbus/04-completion-audit.md:54,80`), but current Service Due/Replacement code now applies scope as noted in §1. No mechanical all-service/controller scope matrix or owner-run API proof was found. |
| **R8: feeder subscriber count / zone** | **Missing** | `Feeder.cs:5-14` contains no subscriber count or zone; these are safe low-priority nullable additions after topology is settled. |

### R4 alternatives and recommendation

| Policy | Benefits | Cost / risk |
|---|---|---|
| A. Hourly meter readings are official after monotonic/reset checks | Supports official hourly losses and source-level charts. | High review/validation burden, late/out-of-order complexity, approval policy expansion, and a larger correction workflow. |
| **B. Hourly readings remain monitoring-only; approved DailyClosing remains official — recommended initially** | Matches current `MeterDeltaService`, due engine and Tier-2 review boundary. Daily loss, service hours and stock use audited closing data. Hourly charts remain explicitly labelled **estimated/monitoring**, not official. | Hourly loss is indicative only; operator must reliably submit one primary/fallback daily closing. |
| C. Per-station policy switch | Gives tenants flexibility. | Requires an explicit policy enum, audit/display indicator, separate calculation paths, extensive test matrix and reviewer UX. `Tier1RequiresApproval` is not sufficient. |

Recommendation: choose **B** for the first commercial release. It preserves the proven official-data boundary and reduces security/review risk. Reconsider C only after R2/R3 are complete and the owner demonstrates an operational need for official hourly settlement.

## 4. Existing-data, activation and rollback plan

### Read-only pre-activation report

Before a tenant may enable Commercial mode, a read-only activation check must report:

1. Station count and Sites per Station; reject/require owner resolution when any commercial candidate Station has more than one active Site.
2. Generator, tank, meter, feeder and permit counts per Site; identify records whose Station relationship is indirect.
3. User scopes containing Branch or Site values and the users who would lose visible assignments; propose equivalent Station scope only, never apply it silently.
4. Existing permit numbers and their Site codes; show any collision risk for Station-facing commercial numbering.
5. Pending Tier-2 submissions, unresolved notifications, and warehouse dispatches linked to Sites.
6. Whether tenant schema versions 1–9 are complete/checksum-valid; whether an administrator connection is available for the next idempotent upgrade.

### Safe activation and rollback

- Take a verified tenant PostgreSQL backup before the approved schema upgrade. Do not alter shared/live databases from development tools.
- The next upgrade is a new EF migration plus matching version `010` (or the then-next verified number) SQL resource, `IF NOT EXISTS`, transactional and checksum-registered in `TenantSchemaUpgradeCatalog`.
- `TenantSchemaMigrator.cs:12-55` already wraps an upgrade in a PostgreSQL advisory-lock transaction and verifies checksums. A failed upgrade rolls back its transaction; an operational rollback is therefore mode deactivation, not schema deletion.
- Enable Commercial mode only after pre-activation validation and explicit owner confirmation. Preserve the technical Branch/Site and legacy columns on disable; switch the tenant back to Enterprise and restore legacy UI/scope assignment. Do not delete data.
- A fleet/coordinator that invokes `TenantSchemaMigrator.UpgradeAsync` for all existing tenants was **not found** by this source review. This is a release blocker/owner decision for any rollout beyond a new tenant or an individually administered upgrade.

## 5. Proposed follow-on phases

| Phase | Scope and schema | Tests / main risk |
|---|---|---|
| 1 — scope audit and fixes | Finish P4-A01–A11 re-verification; add service/controller scope matrix. No commercial schema unless a prerequisite is found. | API deny tests for every read/write; protect enterprise regression. |
| 2 — hierarchy mode | `HierarchyMode` and hidden hierarchy/mapping invariant; commercial projections and station-only assignment; next EF + tenant upgrade. | Enterprise unchanged; commercial hides Branch/Site everywhere; station-scope-only; mode reversibility; disposable PostgreSQL migration. |
| 3 — topology and backup meter | Feeder exactly-one-source FK/check, generator/solar ownership, backup-meter/primary selection, tolerance and photo/audit fields. | Check constraint, source balance, fallback selection, discrepancy and due engine using fallback hours. |
| 4 — hourly policy | Apply owner-selected R4 policy; add explicit policy enum if C is approved. | Monotonic/reset/order/approval and estimated-chart labels. |
| 5 — solar | `SolarSource`, SolarImport enum/read pipeline, independent solar feeders and diesel-saved estimate. | Approved-only solar balance and estimate labelling. |
| 6 — fuel stocktake | Persistent tank stocktake/approval/alert and daily efficiency report. | Count/book/variance math, photo, approval, scope. |
| 7 — hardening/handover | i18n/hard-coded string audit, permission matrix, index/performance review, PostgreSQL proof and recovery/runbooks. | Full commercial/enterprise isolation proof and tenant-upgrade coordinator decision. |

## 6. Owner decisions required

1. Approve **Option A** (hidden auto-provisioned Branch and one hidden Site per commercial Station), or choose B/C with the risks above.
2. Choose R4: recommended **B** (hourly monitoring; DailyClosing official), A (official hourly), or C (station policy switch).
3. Confirm whether commercial mode is limited to one hidden Site per Station. If not, define the required business meaning of several commercial logical sites.
4. Confirm station-facing permit-number format and whether legacy Site codes remain stored but hidden.
5. Confirm the primary/backup meter model: may the backup be a new `Meter` record, or must it link to an imported asset; provide default discrepancy tolerance.
6. Confirm whether the Phase 8 solar scope is pulled forward immediately after topology (recommended for this station) or remains in its original sequence.
7. Authorize the design and rollout of the missing existing-tenant upgrade coordinator; the current migration primitive exists but fleet orchestration was not found.
8. Provide a disposable PostgreSQL connection string when the approved schema phase reaches its proof gate.

## 7. Phase 0 report

```text
Phase: 0 — Commercial feasibility
Branch: feature/commercial-feasibility
Commit: resulting local docs(commercial) commit (hash reported in the phase handoff)
1. Done: read-only hierarchy, scope, tenant-upgrade, feeder, solar, fuel and Phase-4 audit analysis; recommended Option A and R4 option B.
2. Not done (and why): no runtime/schema/dependency/test/migration change by Phase 0 contract; no owner option decision yet.
3. Assumptions made / decisions applied: none. Enterprise remains the default; all recommendations require owner approval.
4. Decisions needed from the owner: the eight items in section 6, especially R1 Option A and R4 policy B.
5. Tests: added 0; UNVERIFIED. No build/test/restore/EF/database/package command was run by Codex. Pre-existing failures: none observed; not independently verified.
6. Migrations: none. Destructive operations: none. Future rollback: transaction rollback for a failed new tenant upgrade; mode deactivation retains additive data and restores Enterprise presentation.
7. Conflicts found: the brief says provisioning creates a Branch and Station; current TenantProvisioningService creates schema/upgrades/admin scope only. The brief's feeder/backup/solar/physical-stocktake gap claims are confirmed. P4-A01 is no longer accurate for the inspected Service Due/Replacement services, but complete matrix verification remains outstanding.
8. Risks and follow-ups: hierarchy re-keying would have broad cross-cutting risk; fleet tenant-upgrade coordination was not found; commercial projection must prevent all Branch/Site disclosure.
9. Suggested manual check in the UI: after a later approved hierarchy phase, verify a commercial user sees station-only navigation, filters, forms, exports, scope assignment, notifications and reports, while an enterprise tenant remains unchanged.
```

### Owner verification commands — do not run until a subsequent approved implementation phase

```powershell
Set-Location 'D:\FTM'
dotnet restore FuelTrack.sln
dotnet build FuelTrack.sln --no-restore
dotnet ef migrations has-pending-model-changes --project src/FuelTrack.Infrastructure --startup-project src/FuelTrack.API
dotnet test FuelTrack.sln --no-build --no-restore
Set-Location frontend
npm.cmd run test -- --run
npm.cmd run build
```

PostgreSQL proof for a later approved schema change, only on an owner-provided disposable database:

```powershell
$env:ConnectionStrings__Platform = '<disposable PostgreSQL connection string>'
dotnet run --project tests/PostgresModel.Proof
```

**Stop condition:** wait for `approved phase 0` and the owner's R1/R4 decisions. Do not start Phase 1.
