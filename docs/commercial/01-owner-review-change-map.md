# Commercial Station Mode — C1 Owner-Review Change Map

**Phase:** C1 — Read-only change map  
**Working branch:** `feature/commercial-change-map`  
**Status:** Complete — implementation and verification are **UNVERIFIED** (pending owner approval)  
**Authority:** `FuelTrack Commercial Station Mode Brief v5.1`, the owner's Arabic design rules/review, `docs/bitbus/CODEX_BRIEF_V4.md`, and the completed [Phase 0 feasibility report](00-feasibility.md).

---

## Executive Conclusion

Commercial mode adapts FuelTrack for single-station commercial power generation companies without re-keying the established `Branch → Station → SubStation` ownership hierarchy. It is a tenant-level capability with a server-side policy, not a fragile collection of page-specific `if` statements.

The technical hierarchy stays active and invisible: exactly **one Branch, one Station, and one SubStation** exist for a commercial tenant. All existing assets continue to reference the hidden `SubStationId` foreign key, while commercial APIs and UI resolve that ID internally.

### Binding Product & Design Rules (Confirmed by Owner)

1. **Tank Geometry:** Main and daily fuel tanks are **vertical cylinders with a conical bottom** (*أسطوانية عمودية بقاع قمعي*) where fuel exits; level is measured from the cone apex. *(Note: C3 initially implemented horizontal cylinders with conical ends based on an early misunderstanding; corrected in C3.1/C3.2 to VerticalCylinderWithConicalBottom = 0).*
2. **Main Tank Dead Stock:** Main tank has an unusable dead-stock level (*مخزون ميت غير قابل للاستخدام*); it is used exclusively to compute available/usable stock (`Usable = Max(0, Actual − DeadStock)`), never as dispensable stock.
3. **No Theoretical Consumption:** Generator fuel consumption does **not** depend on the legacy theoretical consumption rate (`FuelConsumptionRatePerHour` / `StandardFuelRate`) under any circumstances.
4. **Daily Consumption Formula:** Daily generator fuel consumption is calculated strictly from daily generator tank readings:
   $$\text{Daily Consumption} = \text{Opening Balance} + \text{Refills / Inbound Transfers} - \text{End-of-Day Balance}$$
   *(رصيد البداية + التعبئة/التحويلات الداخلة − رصيد نهاية اليوم)*.
5. **Main Tank Accounting:** Main tank balance depends strictly on physical level readings and actual movements (receipts, transfers out).
6. **Hidden Technical Fields:** Nominal power (`NominalPower`), power factor (`PowerFactor`), and consumption rate (`FuelConsumptionRatePerHour`) are hidden in commercial mode; they must not appear as zero or default values in the UI, and must not enter any consumption calculations.
7. **Feeder Definition:** A feeder is an export/sale metering point. Energy from diesel generators and solar joins the station bus and exports through feeder meters. Total energy sent to the grid is the sum of all active feeder meters.
8. **Solar Plant Metering & Solar Feeder (Confirmed by Owner in C4.1):**
   - The solar plant has its own independent cumulative production meter (`SolarEnergyProduced`), captured at daily closing under the Tier-2 approval workflow.
   - The solar plant also has its own feeder with its own energy meter. It is an ordinary feeder meter, summed alongside generator feeders into "energy sent to the grid" (`FeederEnergySent`).
9. **Maintenance Oil Stock Dynamic Configuration (Confirmed by Owner in C4.1, for C6):**
   - The oil item and quantity for oil-change replacements are NOT hardcoded. The owner configures them in oil stock settings.
   - If a generator's service definition has no oil item or quantity configured, the service replacement record must display a prominent warning indicating stock was not decremented; it must never fail or skip silently.
10. **Permit Numbering Prefix (Confirmed by Owner in C4.1, for C6):**
    - Work permit numbering format is confirmed and approved: `{StationCode}-WP-{seq:D5}`.

---

## C1 Boundaries and Evidence

- This phase changes documentation only. No runtime, schema, migration, dependency, tenant, test, database, or UI code was changed.
- Source inspection was read-only. No build, restore, test, EF, package, Docker, or database command was run.
- File and line references below document the inspected source at the C1 base and serve as implementation targets for phases C2 through C7.

---

## Current Architecture That Constrains Commercial Mode

| Constraint | Evidence in Source | Consequence & Architectural Guardrail |
|---|---|---|
| Station requires a Branch; Site requires a Station | `src/FuelTrack.Domain/Models/Station.cs:5-11`; `SubStation.cs:5-17` | A commercial tenant cannot safely omit the technical Branch or Site rows without breaking foreign key cascades. |
| Operational assets remain Site-owned | `Generator.cs:7`; `FuelTank.cs:7`; `Feeder.cs:5`; `WorkPermit.cs:6`; `OilTransaction.cs:7` | Commercial presentation may hide the Site, but backend services must resolve it server-side. |
| Existing scope expands hierarchy | `src/FuelTrack.Infrastructure/Services/UserScopeFilterService.cs:15-178` | Station scope already resolves Site access; commercial users receive neither editable hierarchy scopes nor cross-station choices. |
| Operational capture and review are Site-based | `OperationalCaptureService.cs:24-61`; `OperationalReviewService.cs:24-61` | C2 must provide an internal commercial-site resolver rather than altering operational capture schema. |
| Tenant feature infrastructure already exists | `CustomerFeatureService.cs:37`; `CustomerFeatureSettingsSeed.cs:15`; `CustomerFeaturesController.cs` | Commercial mode must be a tenant capability (`HierarchyMode.Commercial`) alongside existing features, not a frontend-only preference. |
| Navigation is registry-driven | `frontend/src/navigation/registry.js:35-91`; `components/Sidebar.jsx:109-272` | C2 can enforce one mode-aware navigation filter for all commercial hiding. |

---

## Owner Review Map — Items 1 to 10

### 1. Regions, Stations and Sites — Cancel in Commercial Mode

| Current Surface | Current Data / Required Values | Proposed Commercial Change | Risk & Verification Tests |
|---|---|---|---|
| Route `/locations` in `frontend/src/App.jsx:309`; page `frontend/src/pages/LocationsManagement.jsx:160`; navigation item `frontend/src/navigation/registry.js:73` | `Station.BranchId` is required; `SubStation.StationId` is required. Existing `UserScope` can store Branch, Station and Site IDs (`UserScope.cs:18-46`). | **C2 — Additive schema + server policy + UI.** Add tenant `HierarchyMode.Commercial` and an idempotent hidden-hierarchy provisioning invariant. Hide the navigation item and route in commercial mode; guard hierarchy create/update/delete APIs on the server. Resolve the single technical Site internally for all requests. | High security risk if only hidden in UI. Test commercial route/API denial (403/404); exactly-one-active hierarchy invariant; Enterprise CRUD unchanged; commercial user cannot select or infer hidden IDs. |

**Activation Design:** Before activation, produce the Phase-0 read-only preflight: active hierarchy counts, assets per Site, existing user scopes, permit-number risks, pending operational work, and tenant schema versions. Reject activation when a candidate tenant has more than one active Station or Site. On approval, create one technical Branch, Station, and Site only when missing; do not silently alter existing user scopes. Deactivation restores Enterprise presentation only; it deletes nothing.

---

### 2. Generator Settings — Remove Commercial-Only Fields; Add Cumulative Hours

| Current Surface | Current Fields & Dependencies | Proposed Commercial Change | Risk & Verification Tests |
|---|---|---|---|
| Route `/generators` in `App.jsx:311`; UI `pages/Generators.jsx:197-236, 610-632`; API `GeneratorsController`; service `GeneratorService.cs:23-218`; DTOs `CreateGeneratorDto.cs:3-29`, `UpdateGeneratorDto.cs:3-27` | `Generator.SubStationId` and `ActualPowerKW` are required. `NominalPower` and `FuelConsumptionRatePerHour` are currently non-null in DB schema (`Generator.cs:7-27`). `Generators.jsx:202-206` currently injects `0` when empty. Service-due engine uses approved running-hours readings and `ServiceReplacementRecord` baselines. | **C3 — Server defaults / nullable schema + UI.**<br>1. Hide Site, owner, ownership type, generator type, nominal capacity, power factor, and consumption rate in commercial mode.<br>2. **Owner Rule Applied:** Do not display zero or default values in the UI for hidden fields.<br>3. Make DTOs and API validation allow omitting these fields in commercial mode without forcing client zeroes.<br>4. Add cumulative running hours field / baseline source.<br>5. Actual power remains in kW. | In commercial mode, calculations must never use nominal power or theoretical rate. Test: commercial DTO omission receives approved server handling; Enterprise fields unchanged; cumulative running hours cannot regress or double-count approved daily hours. |

---

### 3. Tank Settings — Hide Hierarchy, Preserve Main-Tank Behaviour

| Current Surface | Current Data / Required Values | Proposed Commercial Change | Risk & Verification Tests |
|---|---|---|---|
| Route `/tanks` in `App.jsx:331`; UI `pages/TanksManagement.jsx:41-175, 232, 481`; API `FuelTanksController`; service `FuelTankService.cs:22-247` | `FuelTank.SubStationId`, `FuelLookupId`, `Name`, `Code`, and `CapacityLiters` are required. `ActualStockLiters`, `ParentTankId`, and `GeneratorId` are nullable. Geometry fields: `DiameterCm`, `CylinderHeightCm`, `ConeHeightCm`, `MultiplicationFactor` (`FuelTank.cs:5-38`). | **C3 — Server default + UI.** Hide hierarchy selection; resolve the technical Site server-side. Preserve the main-tank model, receipts, transfers out, and dead-stock calculations. Main tank does not link to a generator. | Main tank receipts/transfers are existing core business logic. Test: hidden input tampering rejected, default Site resolution, main-tank fuel movement regression, and Enterprise forms unchanged. |

---

### 4. Meters — Present Sent-Energy Meters as Feeder Meters

| Current Surface | Current Data / Required Values | Proposed Commercial Change | Risk & Verification Tests |
|---|---|---|---|
| Route `/meters` in `App.jsx:310`; UI `pages/MetersManagement.jsx:58-203, 215`; API `MetersController`; service `MeterService.cs:21-207` | `Meter.SubStationId`, `MeterTypeLookupId`, and `GeneratorId` are nullable (`Meter.cs:5-19`). DTO accepts optional Site and Generator (`CreateMeterDto.cs:3-12`). Operational capture references `Feeder` IDs and attached meter IDs; `MeterReadingSubjectType` has `GeneratorEnergyProduced`, `GeneratorRunningHours`, and `FeederEnergySent`. | **C3 — UI + server default, no enum rename.** Show `Feeder meter` / `عداد الفيدر` for the existing sent-energy type; hide fuel- and generator-meter types from commercial lists, filters, and forms; resolve/omit Site internally. Keep internal lookup codes and enums unchanged. | A commercial meter must maintain a hidden internal 1:1 `Feeder` row; otherwise capture/review/balance queries fail. Test: only feeder meters selectable in commercial mode, direct creation of hidden types rejected, and Enterprise meter types unchanged. |

---

### 5. Generator Screens — Free-Text Manufacturer/Model; Hide Connection Settings

| Current Surface | Current Data / Required Values | Proposed Commercial Change | Risk & Verification Tests |
|---|---|---|---|
| Route `/controllers` in `App.jsx:336`; UI `pages/Controllers.jsx:74-229`; generator UI also exposes controller fields in `Generators.jsx:610-632` | `Generator.Manufacturer` and `Generator.Model` are already nullable strings (`Generator.cs:11-12`), supporting free text. The controller entity stores network/Modbus fields (`Controller.cs:6-20`), and `Controllers.jsx` manages those settings. | **C3 — UI-only plus server guard.** Keep manufacturer/model as free text with optional suggestions; hide Modbus/IP/port/unit/register/auto-connect controls in commercial mode and reject commercial writes to them. Do not change `FuelTrack.ModbusGateway`. | Terminology reconciled: manufacturer/model live on `Generator`, not `Controller`. C3 treats `Controllers` as the screen/connection page and retains generator manufacturer/model on the generator card. Test UI/API hiding and Enterprise controller configuration unchanged. |

---

### 6. Oil Stock Settings — Remove Site Selection; Monthly Daily View

| Current Surface | Current Data / Required Values | Proposed Commercial Change | Risk & Verification Tests |
|---|---|---|---|
| Routes `/setup-oils`, `/stock/oil`, `/oils` in `App.jsx:315,330,333`; pages `SetupOils.jsx`, `OilStockPage.jsx`, `OilInventory.jsx` | `Oil` has required `OilLookupId`, non-null `CurrentStock`/`MinStock`, and optional `StationId`/`SubStationId` (`Oil.cs:5-17`). `OilService.cs:228-273` has a direct add-to-generator transaction that decrements stock. | **C3/C5 — Server default + UI.** Hide Site selection and bind commercial oil stock to the tenant’s technical Station. In C5, provide the requested daily one-month ledger/table using existing oil transactions, not a duplicate balance table. | Test: no Site data leaks, monthly range defaults and paging, existing transaction math preserved, and Enterprise station/site filtering unaffected. |

---

### 7. Daily Fuel Tanks — New Commercial Page, Linked to Generator

| Field Requested | Existing `FuelTank` Mapping | Gap / Confirmed Design Change |
|---|---|---|
| Generator link | `GeneratorId` exists and is nullable | Required for daily-tank records in commercial mode; exactly one active daily tank per generator. |
| Tank shape | `TankShape.cs:6` defines `VerticalCylinderWithConicalBottom = 0` | **Corrected Owner Rule (C3.1/C3.2):** Both main and daily tanks are **vertical cylinders with conical bottom** (*أسطوانية عمودية بقاع قمعي*) with fuel outlet at the cone apex. Uses `VerticalCylinderWithConicalBottom` (shape 0). The obsolete horizontal shape (shape 1) is rejected by API validators. |
| Tank dimensions | Geometry stores `DiameterCm`, `CylinderHeightCm`, `ConeHeightCm` | For vertical cylinder with conical bottom: `DiameterCm` is tank cylinder diameter, `ConeHeightCm` is cone height from apex, and `CylinderHeightCm` is cylinder height above the cone. Total height = `ConeHeightCm + CylinderHeightCm`. |
| Multiplication factor | `MultiplicationFactor` exists and is nullable | Required for commercial daily tanks; preserves calibration semantics. |
| Fuel type | Required `FuelLookupId` | Reused directly. |
| Actual level | `ActualStockLiters` holds current stock, while immutable level history is `TankLevelReading` | UI and API distinguish current displayed stock from an approved/recorded level observation. |
| Main vs. Daily separation | `ParentTankId` represents hierarchy; no explicit purpose column | Add `TankRole` (`Main = 1, Daily = 2`) to make the role explicit and prevent configuration errors. |

**Validation Architecture:** Current transfer validation already recognizes parent relationships in `OperationalCaptureService.cs:378-389`. C3 must preserve that architecture. Tests: daily-to-generator validation, main tank remains unlinked to generator, transfer pairing, fuel-type match, scope denial, and Enterprise compatibility.

---

### 8. Users — Remove Operating Shift and Data Scope in Commercial Mode

| Current Surface | Current Data / Required Values | Proposed Commercial Change | Risk & Verification Tests |
|---|---|---|---|
| Route `/users` in `App.jsx:334`; UI `pages/Users.jsx:57-196, 429-589`; API `UsersController.cs:118-122`; service `UserService.cs:198-244` | Shift controls are UI role logic. Scope assignment is persisted in `UserScope` and enforced by `UserScopeFilterService`. `AssignScopesAsync` requires Station scopes to include Branch IDs and Site scopes to include Station IDs (`UserService.cs:226-243`). | **C2 — UI + server policy.** Hide shift and scope editor in commercial mode. Reject commercial scope-assignment requests; grant every commercial user internal access to the single technical Station/Site through the mode-aware scope resolver. Preserve all existing `UserScope` rows and Enterprise UI. | High authorization risk. Test: commercial scope-assignment request rejection/tampering, all commercial users resolve to the one station only, cross-tenant denial, and Enterprise scope assignment regressions. |

---

### 9. Roles and Permissions — Map to Five Product Tabs

| Current Surface | Current State | Proposed Commercial Architecture |
|---|---|---|
| Route `/roles` in `App.jsx:335`; UI `pages/RolesManagement.jsx`; permission matrix component; module/route seeds in `ModuleRegistrySeed.cs` | Permissions are module/action based; navigation evaluates them from `navigation/registry.js`. Current technical keys are broader than the five commercial tabs. | **C7 — Configuration/UI/server policy.** Keep existing technical permission keys; ship commercial role templates as pre-configured grants over them. Do not rename existing modules or weaken server middleware. |

#### Proposed Commercial Role Matrix (Mapped to Five Tabs)

| Commercial Role | Settings | Generation | Fuel & Oils | Maintenance | Reports |
|---|---|---|---|---|---|
| **Administrator** | Manage users, roles, equipment, rollback | View | Manage | Manage | View & Export |
| **Operator** | Profile only | Submit hourly monitoring & daily readings; view generator cards | Create permitted movements & daily stocktake lines | Create/update assigned permit tasks | View allowed |
| **Operations Manager** | View settings | Approve daily closing & readings; review & rollback | Approve movements & daily stocktakes | Approve permits | View & Export |
| **Maintenance** | View equipment & service definitions | View generator state | Request/consume approved oil & parts | Manage permit work & service replacements | View maintenance reports |

---

### 10. Feeders Page — Feeder Meter is the Commercial Feeder

| Current Surface | Current Data / Required Values | Proposed Commercial Change | Risk & Verification Tests |
|---|---|---|---|
| Route `/feeders` in `App.jsx:332`; UI `pages/FeedersManagement.jsx:14-160`; API `OperationalFeedersController`; service `FeederService.cs:9-101` | `Feeder.SubStationId`, code, and name are required; multiplier defaults to 1; attached `MeterId` is optional (`Feeder.cs:3-14`). Operational capture, review, previous readings, and `EnergyBalanceService` identify exported readings by **Feeder**, not directly by Meter. | **C3 — Hide route/UI; server-maintained 1:1 legacy mapping.** A commercial feeder meter automatically creates/maintains one hidden `Feeder` with the technical Site and `MeterId`. Capture submits the hidden feeder ID internally. Do not delete the `Feeders` table or route for Enterprise. | A direct “delete Feeder” implementation would break capture, review, and energy balance. Test: one meter → one active feeder invariant, no duplicate mapping, meter deactivation handling, official daily closing delta/balance, and Enterprise feeder CRUD unchanged. |

---

## Solar and Feeder Rule — Accounting Boundary

The owner confirmed that solar energy joins the same feeder network used to sell generator energy:

```text
Generator production meters ─┐
                             ├─ Station Bus ── Feeder Meter ── Exported / Sold Energy
Solar production meter ──────┘
```

1. **Single Point of Export:** The feeder meter reports exported/sold energy once. It is not duplicated by source.
2. **Production vs. Export:** Generator and solar production are import/production signals; they must never be created as separate selling feeders.
3. **Current Code Alignment:** `EnergyBalanceService.cs:25-111` already computes balance as $\text{Produced} - \text{Sent}$.
4. **Scope Containment:** C3 implements the meter-to-hidden-feeder compatibility mapping. It does not introduce complex solar billing or tariff models.

---

## Operator and Approval Workflow Map

| Workflow Step | Current Implementation | Confirmed Commercial Direction | Test & Verification Focus |
|---|---|---|---|
| **Hourly Generator Screen Readings (Tier 1)** | `OperationalGeneratorReading` records air/oil pressure, water/oil temp, active/apparent/peak load (`OperationalGeneratorReading.cs:5-42`). Stored as `Recorded` immediately. | **Monitoring only, no approval.** Displayed on generator cards; never used as official settlement. | Recorded status immediately, out-of-scope denial, latest-card display, no approval queue item created. |
| **End-of-Day Readings (Tier 2)** | `MeterReading` has `DailyClosing` kind (`MeterReading.cs:5-35`); required approval policy in `ReadingApprovalPolicy.cs:10-41`. | **Official, requires Manager Approval.** Captures running hours, total produced energy, feeder meters, and daily tank closing levels. | Pending → Approved/Rejected; official delta computed only after approval; monotonic/reset validation enforced; reviewer separation. |
| **Daily Generator Fuel Consumption** | `FuelLoadSafetyService.GetConsumptionAsync` queries level reading segments dynamically. | **Owner Rule Applied:** Computed as $\text{Opening} + \text{Refills} - \text{Closing}$. Tied directly to the generator's daily fuel tank. | Verified against tank level readings; alerts on unregistered refills or negative deltas. |
| **Operator Entry Page** | `/operations/capture` at `App.jsx:296`; `OperationalCaptureService`. | Single-station presentation; internal Site default. Hides technical hierarchy. | Idempotency, single-station default, tamper-proof hidden IDs, Enterprise capture unchanged. |
| **Daily Approval Page** | `/operations/review` at `App.jsx:298`; legacy `/review-changes` at `App.jsx:354`. | Single commercial daily approval entry point; hide duplicate/legacy route from commercial navigation. | Manager allowed; operator denied; no hourly lines in daily approval queue. |
| **Rollback After Approval** | `/rollback` at `App.jsx:338`. | Strictly role-gated (Operations Manager / Admin); preserves audit trail and recalculates cleanly. | Permitted roles only; approval history retained; deterministic state rollback. |

---

## Maintenance Note — C6 Gap Map

| Owner Requirement | Current State in Source | Required C6 Work |
|---|---|---|
| Fixed generator maintenance card: oil change, filters, nozzles by running hours | `ServiceItem`, interval definitions, due calculation, and `ServiceReplacementRecord` exist in `ServiceDueService.cs:119-144` and `ServiceReplacementService.cs:40-160`. | Present commercial maintenance card per generator; map standard service items; show hours since last service, remaining hours, and due status. |
| Restart maintenance calculation without destroying history | Replacement validation prevents hours from regressing except on explicit meter reset (`ServiceReplacementService.cs:76-96`). | **Confirmed:** Retain baseline-based calculation; do **not** zero cumulative running hours. |
| Oil set replacement decrements oil stock | `OilService.AddToGeneratorAsync` decrements stock (`OilService.cs:228-273`), but `ServiceReplacementService` currently does not link to oil stock. | **Gap to bridge:** Implement an atomic link: approved oil replacement automatically generates an oil transaction decrementing stock. |
| Permit workflow: operator → maintenance management → operations manager approval | `/maintenance/permits` (`App.jsx:328`); `WorkPermitService.cs:224-423` handles basic CRUD and status changes. | Define explicit state/role transition matrix and per-permit report; use Station terminology. |

---

## Single-Station Invariant & Hidden-Field Policy

1. **Exact-One Invariant:** A commercial tenant has exactly one active technical Branch, Station, and SubStation. All are preserved to maintain foreign key integrity across the 189+ referencing files.
2. **Central Capability:** C2 exposes a backend capability `HierarchyMode.Commercial`. The frontend mode provider, navigation, forms, filters, tables, exports, and notifications consume this single source of truth.
3. **API Guardrails:** Any commercial API request that attempts to create, update, or delete hierarchy nodes is rejected (400/403). Requests resolve the technical Site server-side.
4. **Reversibility:** Switching back to Enterprise mode restores full hierarchy presentation and management without any data loss.

---

## Refined Delivery Plan (C2 through C7)

```text
C1: Change Map (Read-Only) [THIS PHASE]
    └── Map all 10 items, workflows, invariants, confirmed owner design rules, and C2-C7 plan.

C2: Commercial Foundation
    ├── HierarchyMode enum & tenant capability
    ├── Hidden provisioning of Branch, Station, SubStation
    ├── Single-station invariant & scope resolution without assignment
    ├── Remove Regions/Stations/Sites page from commercial mode
    └── Users page without shift and data scope; mode hook & navigation shell

C3: Equipment Settings
    ├── Generators: hide nominal capacity, power factor, consumption rate; add cumulative hours
    ├── Generator screens: free-text manufacturer/model, hide connection settings
    ├── Tanks: main tank unlinked from generator; new Daily Fuel Tanks page (linked to generator)
    ├── Tank Geometry: vertical cylinder with a conical bottom (shape 0)
    ├── Meters: rename sent energy to feeder meter; hide fuel/generator meters; hidden 1:1 Feeder row
    └── Oil stock: unlinked from site

C4: Operator and Approval Workflows
    ├── Hourly monitoring readings (air/oil pressure, temp, min/max load) sent without approval
    ├── Daily closing readings (running hours, produced energy, feeder meters, daily fuel closing, stocktake)
    ├── Daily fuel consumption calculation (Opening + Refills - Closing)
    └── Daily approval page & rollback audit

C5: Fuel and Oils Tab
    ├── Align commercial fuel/oil pages
    ├── Monthly daily oil ledger
    └── Persisted periodic physical stocktake (counted, book, variance, photo, approval)

C6: Maintenance
    ├── Generator maintenance card (oil change, filters, nozzles)
    ├── Next-due indicator (without zeroing cumulative hours)
    ├── Atomic oil set replacement decrementing oil stock
    └── Permit workflow (operator → maintenance management → operations manager)

C7: Roles, Navigation & Hardening
    ├── Commercial role templates (Admin, Operator, Operations Manager, Maintenance)
    ├── Five-tab navigation layout
    ├── Branding, alerts, and warnings audit
    ├── i18n & hidden-word audit
    └── PostgreSQL recovery notes & test verification
```

---

## Decisions & Confirmations Status

### Confirmed by Owner (Resolved)
- [x] **Tank Shape:** Vertical cylinder with a conical bottom (*أسطوانية عمودية بقاع قمعي*) for main and daily tanks.
- [x] **Main Tank Dead Stock:** Unusable; used solely to compute usable stock (`Usable = Max(0, Actual − DeadStock)`), not dispensable stock.
- [x] **Theoretical Consumption:** Completely removed from operational calculations.
- [x] **Daily Consumption Formula:** $\text{Opening} + \text{Refills/Inbound} - \text{Closing}$.
- [x] **Main Tank Dynamics:** Governed by level readings and physical movements.
- [x] **Hidden Generator Fields:** Nominal power, power factor, and consumption rate are hidden and never forced to zero.
- [x] **Feeder Role:** Feeder meter is the commercial feeder; solar joins station bus and exports through feeder meter.
- [x] **Maintenance Counter:** Maintain last-service baseline; do not zero cumulative running hours counter.

### Open Questions for Owner (Before Respective Phases)
1. **Solar Production Metering (for C4/C5):** Does the solar installation have a dedicated production meter read at daily closing, or is solar contribution currently unmetered at the bus?
2. **Oil Set Stock Link (for C6):** When an "oil change set" is replaced, what exact oil item and default quantity in litres should be decremented from stock?
3. **Permit Numbering Prefix (for C6):** Confirm permit format `{StationCode}-WP-{seq:D5}` for commercial mode.

---

## Phase C1 Report

```text
Phase: C1 — Change map
Branch: feature/commercial-change-map
Commit: d928ea7
1. Done:
   - Mapped all 10 owner-review items with file, line, and entity evidence.
   - Incorporated all 6 confirmed owner design rules: vertical conical bottom tanks (corrected in C3.1/C3.2), dead stock usable calculation, elimination of theoretical consumption, opening+refill-closing daily formula, physical main tank movements, and hidden generator fields.
   - Mapped operator/approval two-tier workflow, maintenance C6 gaps, single-station invariant, feeder strategy, daily fuel tank geometry mapping, role matrix proposal, and C2–C7 delivery plan.
2. Not done (and why):
   - No runtime, schema, migration, test, tenant, or UI code was changed, per strict C1 read-only contract.
3. Assumptions made / decisions applied:
   - Feeder meter is the sole export sale meter; hidden 1:1 Feeder row preserves capture and energy balance.
   - Tank geometry strategy: vertical cylinder with conical bottom (VerticalCylinderWithConicalBottom = 0); horizontal conical strategy obsoleted in C3.1/C3.2.
4. Decisions needed from the owner:
   - Solar production metering details: **CLOSED / CONFIRMED (C4.1)** — Dedicated independent cumulative production meter (`SolarEnergyProduced`) read at daily closing (Tier 2), plus ordinary feeder meter for solar export to grid.
   - Maintenance oil change set default item & quantity: **CLOSED / CONFIRMED (C4.1 / C6)** — Dynamic configuration in oil stock settings; never hard-coded; warning displayed when unconfigured.
   - Permit numbering prefix confirmation: **CLOSED / CONFIRMED (C4.1 / C6)** — `{StationCode}-WP-{seq:D5}`.
5. Tests:
   - Added 0 (read-only phase).
   - Owner verification is UNVERIFIED (no executable code modified).
6. Migrations:
   - None. Destructive operations: none. Rollback: not applicable.
7. Conflicts found:
   - Reconciled: Owner confirmed tanks are vertical cylinder with conical bottom where fuel exits at apex (corrected from initial horizontal misconception).
   - Reconciled: "Generator screens" terminology corresponds to Modbus controllers in backend; manufacturer/model live on Generator entity.
8. Risks and follow-ups:
   - Additive schema for TankRole in C3 and PostgreSQL upgrade 010.
   - Atomic link between service replacement and oil stock decrement in C6.
9. Suggested manual check in the UI:
   - None for C1 (documentation only).
```

---

## Standard Owner Verification Commands

*Note: No code changed in C1; these commands are the standard verification block for subsequent executable phases.*

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

PostgreSQL proof (for schema phases, on disposable database only):
```powershell
$env:ConnectionStrings__Platform = '<disposable PostgreSQL connection string>'
dotnet run --project tests/PostgresModel.Proof
```
