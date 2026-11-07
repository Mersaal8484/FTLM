# FuelTrack for Bitbus: Operational Data, Inventory and Maintenance

**Execution brief v4 for a coding agent (Codex) working in VS Code on the FuelTrack repository (`D:\FTM`).** v4 is v3 **merged with the owner's operating model (the site engineer's workflow) and a review of Phases 1 and 2**. It supersedes v3, the earlier addenda and `docs/CODEX_PROMPT_BITBUS.md` (background only). **Phases 0R, 1 and 2 are already delivered.** Read this document completely, then do **Phase 2.1 (section 14)** before anything else. Changes relative to v3 are marked **[v4]**.

**Workflow.** Work in phases. After each phase: build, test, commit, write the phase report, and **stop**. Do not start phase N+1 until the owner replies `approved phase N`. **[v4]** Phase 2.1 is a corrective patch on top of the delivered Phase 2; it must be reported and approved before Phase 3.

---

## 1. Mission and context

Bitbus is a commercial power-generation company with small diesel generators (Perkins, Caterpillar) and a planned solar plant. FuelTrack is customized for it with three subsystems that integrate with the existing users, permissions and Branch → Station → SubStation data:

1. **Operational Data**: operators capture readings from generator screens, meters, tank gauges and feeders, and upload them **with a photo of what was read**.
2. **Inventory (Warehouse)**: station-level spare-parts stock.
3. **Maintenance**: site/generator work permits, service-hour tracking, due alerts.

The product must stay **generic**: no "Bitbus" in business logic; modules are enabled per customer.

**References.**

- `https://github.com/Mersaal8484/FuelTrack-PEC` is the base system whose workflow the owner knows (Go engine + Node/Prisma backend + CRA frontend + Flutter app). Use it for **behavior only** (reading lifecycle `PENDING → APPROVED/REJECTED`, rejection reason, approver, single-transaction approval, `Idempotency-Key`). Never port its code or stack; this repository is ASP.NET Core + React/Vite.
- Approved UI designs: `docs/design/bitbus/` (section 4).

---

## 2. Operating rules

1. **Branching.** One branch per phase: `feature/bitbus-<slug>`. Never work on `development`. One Conventional Commit per phase. Do not push, merge or rebase unless asked.
2. **Development mode (greenfield).** No existing data must be preserved. The additive-only restriction is lifted: non-additive and destructive migrations are allowed and the dev database may be reset. **But** do not delete or replace any legacy code until the owner approves the removal list (Phase 0R). Never touch `.env*`, secrets, or real data; seed data is fictional and dev-only. **[v4]** Never edit a migration that already exists in the repository history; add new ones only. A database reset is a local-developer action: never run it against a shared or real database, and confirm which connection strings and tenant databases `MigrationRunner` can reach before any destructive step.
3. **Read before writing.** Read the relevant existing service, DTO, controller, page and tests before coding any topic. The code is the source of truth for facts; this brief is the source of truth for requirements. Report any disagreement.
4. **Verification gate (every phase):** `dotnet build FuelTrack.sln`, `dotnet test FuelTrack.sln`, `cd frontend && npm run build && npm test -- --run`. Never declare a phase complete with failures; list pre-existing unrelated failures. Phase 0 could not run these (NuGet blocked, no `node_modules`). If the environment still cannot build and test at the start of Phase 1, **stop and tell the owner**; do not proceed untested.
5. **i18n:** every user-facing string through i18next, Arabic primary and English secondary, with `ar`/`en` key parity enforced by a test. Use the glossary (section 15).
6. **Tests are mandatory** for: tank geometry, meter deltas, due calculator, stock balance, permit numbering, approval state machine and approval policy, photo validation and stamping, fuel consumption, overload detection, business-date handling, and scope/permission enforcement.
7. **No new dependencies** (NuGet/npm) without asking. Image compression uses the browser canvas API.
8. **Phase report** at the end of every phase (section 17).

**Stop-and-ask triggers:** a new dependency; anything touching `FuelTrack.ModbusGateway`; secrets; ambiguity about business rules not covered by section 6; any conflict between a design file and this brief.

---

## 3. Project facts carried over from Phase 0 (re-verify in code)

- Clean Architecture on .NET 10: Domain → Application → Infrastructure → API; also `ModbusGateway`, `Admin`, `Platform`. Frontend: React 18, Vite, zustand, TanStack Query, i18next, lucide-react.
- **Site = `SubStation`; Station = `Station`.** Generators, tanks, meters and permits belong to `SubStation`.
- `Generator.Model` exists (nullable). `WarehouseStock` is already station × part. `WorkPermit`, `DispatchRequest`, `WarehouseTransaction`, `InstalledPart`, notifications, Hangfire jobs, `ModuleRegistrySeed`, `PermissionEnforcementMiddleware` and `UserScopeFilterService` exist.
- No feeder entity, no generic attachment/storage abstraction, no idempotency store, no tank calibration exist.
- Legacy `DailyReading` / `SubStationReading` carry an approval status flow but use `(Generator, Date)`-style uniqueness and daily granularity. Their **approval pattern is reusable**; their schema and uniqueness are not suitable for free-time readings.
- Conventions for every new entity/service: inherit `BaseEntity`; soft delete via `HasQueryFilter`; **filtered unique indexes** using the existing `activeFilter`; record DTOs; FluentValidation; `AsNoTracking()` + `Select` projections (no heavy `Include` chains); `AuditSaveChangesInterceptor` (never duplicate it); `IUserScopeFilterService` in **every** new read service.

---

## 4. Design references

Approved designs are in `docs/design/bitbus/`. List the folder yourself and report what you found (it was previously mentioned as an operator generator-reading screen, an energy-balance board, and `Main.dc.html` / `Dashboard.dc.html` prototypes).

- The design is the source of truth for **layout, hierarchy, spacing, colours and visual states**. This brief is the source of truth for **behavior, data, validation and permissions**.
- Do not copy the HTML prototypes. Rebuild with existing React components, routing, TanStack Query and the existing theme; map design tokens onto the theme; add no parallel styling system.
- Where a design contradicts this brief (for example hourly slots, a missing photo control, or solar elements), **list the conflict and ask**. Do not silently choose.
- Only operational-data screens are designed. Inventory, maintenance, service-interval and report screens follow existing FuelTrack patterns, visually consistent with Bitbus.
- Verify RTL and phone width for every operator screen.

---

## 5. Core principles for operational readings

These rules override anything else in this document about readings.

**P1. Free-time capture.** There are **no slots, no fixed interval, no reading-interval setting**. The company decides how often to read (hourly, half-hourly, ad hoc). Every reading simply carries its own date and time.

**P2. Every reading has a timestamp and a photo.** Each reading records `CapturedAtUtc` (the real time of reading, **normalized to the minute**), `ReceivedAtUtc` (server clock, never client-controlled), the device clock at submit `ClientSentAtUtc` **[v4]**, and a derived `BusinessDate` (station-local time). Each reading line requires **at least one photo of the thing that was read**: a generator controller screen, a meter, a tank gauge/level indicator, or a feeder meter/screen. **[v4]** The photo carries a **stamp strip with the capture date/time** (7.6). The server rejects a line without a photo **at ingest for online submissions**; for submissions flagged by the client as coming from the offline queue, a line may arrive first in state `AwaitingPhoto` (photo uploaded afterwards) and can never be approved or counted as official until its photo is attached. (A single constant per reading type controls the photo requirement.)

**P3. [v4] Two data tiers with different approval rules.** This follows the site engineer's workflow and replaces v3's "everything is Pending".

- **Tier 1, operational monitoring data** (generator readings, routine tank `Level` readings, feeder load readings): entered every hour or half-hour **according to the site's policy** (free time, P1), **no approval step**. Stored immediately with status `Recorded`. Purpose: monitoring, charts, diagnostics, overload and limit alerts, and the fuel-level series. A user with the `void readings` permission can **void** a Tier-1 line with a mandatory reason (soft delete + audit); nobody edits values.
- **Tier 2, official data:** end-of-day `DailyClosing` meter readings (generator energy and running hours, feeder energy sent, solar import) and tank **Receipt / TransferOut / TransferIn** events. Stored as **Pending**; become official only through **Approve** or **Reject** by a user with the review permission. Pending and Rejected rows never feed any official calculation, dashboard figure, stock figure, or the due-service engine.

Implement **one central `ReadingApprovalPolicy`** (`RequiresApproval(subjectType, kind, eventType) → bool`, defaults as above, overridable per station setting) and **one `IsOfficial` predicate** (Tier 2: `Approved`; Tier 1: `Recorded`) used by every calculation. Test both. Everywhere this document says "Approved data" for a calculation it means `IsOfficial`. **Safety alerts (overload, operating limits) fire on receipt, not on approval.**

**P4. Idempotent submissions.** Every submission carries a client-generated request id. Retries never create duplicates. Approve and reject are idempotent too.

**P5. Raw values are immutable; derived values are computed.** Deltas, daily totals and produced-vs-sent are computed from **Approved** raw values and never typed. Tank litres are computed server-side and stored with the reading together with the calibration snapshot.

---

## 6. Decision register (final)

| ID | Decision |
| --- | --- |
| D1 | Site = `SubStation`; Station = `Station`. |
| D2 | **Tanks: default shape is a vertical cylinder with a conical bottom**, see 7.3, inputs in cm. **[v4]** Geometry sits behind a `TankShape` enum and a strategy interface (horizontal cylinder, rectangular and calibration table can be added without migrating existing data). The shape is confirmed for the current tanks; the interface only keeps future shapes cheap. |
| D3 | A meter value lower than the previous approved one is rejected unless the line is flagged `IsMeterReset` with a note; permission "flag meter reset"; that delta is null, never negative. |
| D4 | A generator reading has `IsRunning`; when false the electrical fields are nullable. |
| D5 | Minimum stock is per **station × part** (`WarehouseStock.MinimumQuantity`). |
| D6 | Issue approval may approve ≤ requested quantity, reduced approvals require a comment, never more than available. |
| D7 | Permit numbering per **site**, continuous, no yearly reset, display `{SiteCode}-WP-{seq:D5}`. Existing permits are not backfilled. |
| D8 | **Multiple assignees** per permit (junction table), existing users within the actor's scope. |
| D9 | Service interval key = generator model × service item. `Generator.Model` exists; normalize via one model-key function (trim, case-insensitive). |
| D10 | Early replacement is strict: fewer hours since the previous replacement of that item than the interval. |
| D11 | Readiness values `Ready`, `ReadyWithLimitations`, `NotReady`; informational only; never changes generator status. |
| D12 | `ActiveLoadKw` required when running; `ApparentLoadKva` optional; **[v4]** `PeakLoadKw` optional ("أعلى حمل": the peak since the previous reading as shown on the DC screen). There is **no minimum-load field anywhere.** |
| D13 | Oil pressure unit bar (station-configurable). |
| D14 | Due alerts reuse the existing notification mechanism; notify only on state transitions. |
| D15 | Timestamps stored in UTC. Default station timezone **`Asia/Aden`** (UTC+3, no DST), stored per station. |
| D16 | **Photos: at most 65 KB (65 × 1024 bytes)**, compressed in the app, JPEG or WebP only, no retention limit. |
| D17 | **No backdate/edit window and no override permission.** Auditing stays. A non-blocking skew warning exists (see 7.1). |
| D18 | The **approver must differ from the submitter** (configurable, default on). |
| D19 | **Review unit = one submission** (an operator's upload at a site), with **per-line reject** and a mandatory reason. Reviewers cannot edit values: they reject and the operator resubmits. |
| D20 | Feeders have an optional **multiplier** (CT/PT factor), default 1, snapshotted on each reading. |
| D21 | Solar is **in scope (Import and Export model defined)**: section 13. |
| D22 | **[v4]** Approval tiers and policy: P3. |
| D23 | **[v4]** **Overload** = `ActiveLoadKw` or `PeakLoadKw` above **100% of the generator's `ActualPowerKW`** (station setting, default 100%). One event and one notification per episode, raised on receipt. Show surrounding data only; never infer the cause. |
| D24 | **[v4]** **Average load ("متوسط الحمل")** is computed, never stored: energy delta ÷ hours. It is displayed separately from instantaneous load ("الحمل اللحظي") and peak load ("أعلى حمل"). i18n keys `load.average`, `load.instant`, `load.peak`. |
| D25 | **[v4]** Fuel consumption is computed from daily-tank level readings (before/after) and refill events (7.9), never from a configured standard rate. |
| D26 | **[v4]** Reading time is normalized to the minute; duplicate guard on `(subject, CapturedAtUtc)` after normalization. |
| D27 | **[v4]** A `DailyClosing` has an operator-selectable `BusinessDate` (default = station-local date of `CapturedAt`). |
| D28 | **[v4]** The operator app must be **offline-capable** (queue and sync, Phase 10). Phase 2.1 removes blockers; full implementation is Phase 10. |
| D29 | **[v4]** Station energy balance includes **internal consumption** (13 and 7.5). |
| D30 | **[v4]** Photo stamp strip with capture date/time and subject code on every photo. |

---

## 7. Functional specification: Operational Data

### 7.1 Capture model

One **submission** = one operator upload for one site, containing any mix of: generator readings, tank level readings, feeder load readings, meter readings (section 7.5). Fields on every line: `CapturedAtUtc`, photo(s), values.

- The UI fills `CapturedAt` with the current time when the operator starts the entry; the operator may adjust it to the true time of reading. **[v4]** The operator sets **one submission-level time** which every line inherits; each line may override it. Do not force a time field per line. Edits to the time are audited and visible to the reviewer.
- Reject `CapturedAt` later than server time + 5 minutes, except when the client's `ClientSentAtUtc` shows the device clock is skewed (then warn, do not reject). Past times are allowed with **no window** (D17). **[v4]** `CapturedAtUtc` is truncated to the minute (seconds = 0) before the duplicate check and storage.
- **Skew warning (non-blocking):** if `ReceivedAt − CapturedAt` exceeds a single configurable constant (suggested 12 hours), the review screen shows a warning. It never blocks.
- Duplicate guard: unique `(subject, CapturedAtUtc)` among non-deleted, **non-rejected, non-voided** rows, enforced by a filtered unique index **and** an application-level check returning HTTP 409 with a readable message. A rejected reading can be resubmitted.
- Plausibility checks (frequency 45–65 Hz, power factor 0–1, non-negative values, tank level within range) produce **non-blocking warnings** stored on the line; only impossible values (negative, wrong type, out of tank range) are hard errors.
- The operator may edit or withdraw a **Pending** submission. Editing an **Approved** Tier-2 reading returns it to Pending: **only the original submitter may start it**; the previously approved value stays official until re-approval; the reviewer sees old vs new; derived values and stock change only on re-approval; keep the version history. Tier-1 lines are not edited (void and re-enter).

### 7.2 Generator readings

Line-to-line voltage (V), current (A), water temperature, oil pressure (bar), oil temperature, active load kW (and optional kVA), reactive load kVAR, frequency (Hz), oil added (litres), power factor, `IsRunning`, **[v4]** optional `PeakLoadKw` and optional `AirPressureBar`. Photo: the generator controller screen. Tier 1 (`Recorded`, 7.1/P3).

### 7.3 Fuel tanks (main and daily)

**Tank setup (admin, once per tank):** shape (default vertical cylinder with conical bottom), diameter `D`, cylinder height `Hcyl`, cone height `Hc`, multiplication factor `f`. Capacity is derived and stored (documented exception to "no stored results"; existing alerts read capacity).

Shape (confirmed): vertical cylinder with a conical bottom. The operator enters the liquid **level `h` in cm measured from the cone apex**. With `r = D/2`:

- if `h ≤ Hc`: `V = π·r²·h³ / (3·Hc²)`
- if `h > Hc`: `V = π·r²·Hc/3 + π·r²·(h − Hc)`
- `litres = V · f / 1000`; capacity = litres at `h = Hc + Hcyl`.
- Reject `h < 0` or `h > Hc + Hcyl` with a clear message.

**Shape confirmed by the owner:** the tank is a **vertical cylinder whose bottom (the fuel outlet side) is a cone**. Still to confirm in your report: that the ruler/level reading is measured from the cone apex (the lowest point), and whether cone height `Hc` and cylinder height `Hcyl` are measured as separate dimensions (as modeled here). Keep the `TankShape` strategy interface, but do not build other shapes now. Add a **setup preview**: after entering dimensions show the derived capacity and a table of litres at 0/25/50/75/100% of height, so a wrong dimension is caught at setup.

Implement a pure, side-effect-free `TankGeometry` with exhaustive tests. **Required fixtures** (`D=200`, `Hc=50`, `Hcyl=150`, `f=1`): `h=0 → 0 L`; `h=25 → 65.45 L`; `h=50 → 523.60 L`; `h=100 → 2094.40 L`; `h=200 → 5235.99 L` (±0.01); `f=1.1` at `h=100 → 2303.83 L`; `h=-1` and `h=201` rejected. Store computed litres **and a snapshot of the calibration** with each reading; later calibration edits never change history. Photo(s): the gauge or level indicator, **1 to 5 per level**.

**[v4] Dead stock ("النقطة الميتة").** Each tank has a configurable dead stock: the fuel at the bottom that cannot be drawn and must not count as available. The existing `FuelTank.DeadStockLiters` column is the source; the tank settings screen lets the admin enter it **either in litres or as a level in cm** (converted with the tank geometry; store only the value entered, show the other as derived). Rules:
- `usableLiters = max(0, litres − deadStockLiters)`; usable capacity = capacity − dead stock. Show total litres, dead stock and usable litres on tank screens and in the setup preview.
- Minimum-stock and low-fuel alerts (`MinAlertLiters`, `FuelStockAlertJob`) compare against **usable** litres.
- **Fuel consumption is not affected** by dead stock (it is a difference between two litre values, so a constant cancels). Do not subtract it inside `FuelConsumptionService`.
- A level reading **at or below the dead-stock level** raises a non-blocking warning and a "daily tank at dead stock" alert (the generator may starve); it is still recorded.
- Dead stock changes apply from the date of change only: snapshot the dead-stock value with each reading, like the calibration, so history is not rewritten.

**[v4] Tank structure and events.**
- Tank relations: a daily tank has `ParentTankId` (its main tank) and `GeneratorId` (filtered unique index: one active daily tank per generator).
- `TankEventType`: `Level` (routine hourly level, Tier 1), `Receipt` (delivery into a main tank), `TransferOut` (main tank to daily tank), `TransferIn` (the matching daily-tank side; `LinkedEventId` links Out and In). Receipt and Transfer events are **Tier 2** and require **both a before-level and an after-level, each with at least one photo**. `Receipt` also records the delivery-note litres.
- Without distinct event types a level rise from a refill cannot be told apart from noise, so consumption cannot be computed (7.9).
- Main tanks may be refilled more than once per day; every refill is its own event.

### 7.4 Feeders

New master entity `Feeder` (site, code, name, multiplier, optional link to an existing `Meter`). A feeder **load reading** (kW or kVA) and **meter readings** (energy sent, kWh) both require a photo of the meter or its screen.

### 7.5 Meter readings and daily closing

A generic `MeterReading`: subject (generator energy-produced kWh, generator running hours, feeder energy-sent kWh; **subject type must be extensible** and includes solar import kWh, section 13), raw value, multiplier snapshot, kind (`Periodic` or `DailyClosing`), `IsMeterReset` flag and note, photo(s).

- **Daily closing:** at most one `DailyClosing` per subject per **`BusinessDate`** among non-rejected rows. **[v4]** The operator may select the intended `BusinessDate` (end-of-day readings are often taken at 23:55 or 00:05); default is the station-local date of `CapturedAt`; the choice is audited and shown to the reviewer. Add tests for a closing at 00:05 belonging to the previous day.
- **Daily delta** = (approved closing of the day − approved closing of the previous approved day) × multiplier. If days are missing, show the delta as a multi-day span (with `SpanDays`) and never average it.
- **Order independence:** because capture order and approval order can differ, **re-validate monotonicity at approval time** against the neighbouring approved readings by `CapturedAt`. Approval that would break monotonic order is refused with a clear message unless a reset is flagged.
- **Station produced-vs-sent:** Import (generator energy produced plus solar import) versus Export (feeder energy sent, including the solar plant's dedicated feeder), absolute and %, with a configurable warning threshold, computed from official data only. **[v4]** The balance includes **internal consumption**: `unexplained difference = produced − exported − internal`. Support an optional internal-consumption source (a meter subject or a configured line). Without one, label the difference "غير مفسَّر (يشمل الاستهلاك الداخلي)" so the threshold does not fire on normal auxiliary use.
- The **latest approved** running-hours meter by `CapturedAt` is the **current hours** source for the due engine, shown with its "as of" time.

### 7.6 Photos and attachments

- **Client** compresses every photo before upload using canvas: JPEG or WebP, reduce quality first, then dimensions, until ≤ 65 KB. Keep a **minimum readable size** (suggested long edge ≥ 960 px). If the limit cannot be met above that floor, the app asks the operator to retake closer or **crop to the display area** (in-app crop, no new dependency). Never silently upload an unreadable image. **[v4] Stamp:** before the size-reduction loop, draw a **bottom strip** with the reading's `CapturedAt` (station local time, `dd/MM/yyyy HH:mm`) and the subject code (e.g. `G2`, `T-D2`, `F3`); it must still fit within 65 KB. Test the stamping function (pure text/geometry).
- **Server** hard-rejects anything over 65 × 1024 bytes, sniffs magic bytes (JPEG/WebP only), ignores client filenames, stores through a storage abstraction **outside the web root**, tenant-addressable, and records SHA-256, size and MIME. **[v4]** Photos are served only through an **authorized API endpoint that applies the user-scope check**, never as a static URL. Re-encoded canvas images carry no EXIF/GPS; do not store client metadata.
- **Reuse detection:** if the same hash is attached to a reading in **another submission**, show a non-blocking warning to the reviewer. **[v4]** Within one submission the same stored photo may legitimately be attached to several lines (for example a feeder panel photo covering both its load line and its meter line): store once, reference many; never warn for that.
- Multiple photos per line are allowed (tank levels: 1 to 5).

### 7.7 Review and approval

Reuse the existing FuelTrack approval pattern (Pending / Approved / Rejected, approver and time, rejection reason, `409` when already decided), and the PEC behavior for the lifecycle.

- **[v4]** The queue contains **Tier-2** submissions and lines only (Tier 1 is never reviewed). A user with the **review readings** permission sees the Pending ones for the sites in scope. The reviewer sees, per line: values, **photo(s) with zoom**, `CapturedAt`, `ReceivedAt`, skew warning, previous approved value, delta preview, plausibility warnings, reuse warning.
- **Approve submission:** atomically approves all non-rejected lines. **Reject line or submission:** reason mandatory. Concurrent decisions on the same line are safe.
- For Tier-2 data, tank stock and any derived state change **only on approval**; Tier-1 `Level` readings update the current level when recorded.
- Notify reviewers when a submission arrives and notify the submitter on rejection (existing notification transport).
- Approver ≠ submitter (D18). All transitions audited.

### 7.8 UX

Mobile-first, RTL-correct, matching the designs. **[v4]** Operator screens:

- **A. Hourly round (Tier 1):** choose site, set one submission-level time, then one card per generator (running toggle; if stopped, electrical fields hidden but tank level kept; electrical fields, optional peak load; daily-tank level with live litres preview; photo control), a feeders card (load kW/kVA, photo), submit. Items are independent lines, so partial rounds are fine.
- **B. End of day (Tier 2):** per generator energy counter and running hours with photo, per feeder energy meter with photo, selectable business date; submit for approval.
- **C. Tank refill/transfer (Tier 2):** before and after levels with photos, delivery-note litres for receipts.
- **D. Record replacement** (section 8). **E. Solar** section (section 13). **F. Sync status** (Phase 10).
- Show per-line state after submission (Recorded / Pending / Approved / Rejected with reason / AwaitingPhoto) and let the operator resubmit rejected lines. Immediate, non-blocking warnings for out-of-range values, overload and limit breaches before submit.
- **History views** for generator, tank, feeder (date range, status, CSV export), the **energy-balance board** per the design, and read-only **operational log** charts. Loading, empty and error states, accessible labels. Keep camera flow fast: one photo may serve several lines.

### 7.9 [v4] Fuel consumption (computed on read, never stored)

`FuelConsumptionService`, per generator and range, from the generator's daily-tank events ordered by `CapturedAt`:

- Segment consumption = **litres after the previous event − litres before the next event**. A refill is not consumption (added = after − before). Total = generator consumption; `kWh per litre = official energy produced ÷ consumption`.
- A level rise between two `Level` events with **no refill event** = **unregistered refill**: a warning, and that segment is not treated as official consumption until reviewed.
- With hourly levels, segments are hourly and sum to the same total.
- `FuelReconciliationService`, per period: (a) withdrawn from main tanks (TransferOut), (b) sum of daily-tank refills (TransferIn), (c) generator consumption; plus delivery-note vs measured receipt; flag differences above a configurable threshold.
- **Dead stock:** usable litres = litres − dead stock (never below 0); consumption results are identical with or without dead stock; alerts use usable litres.
- **Required tests:** start 300 L, before refill 180 L, after refill 400 L, end 250 L ⇒ consumption **270 L**, added **220 L**; two main-tank refills in one day; unregistered refill; late-arriving reading recomputes neighbouring segments.

### 7.10 [v4] Load, overload, limits and consistency

- **Average load** per segment = official energy delta ÷ hours (kW), computed; cross-check against the mean of hourly `ActiveLoadKw` (warn beyond a configurable tolerance, default 15%).
- **Overload** (D23): detection and one notification per episode, on receipt, to in-scope users via the existing notification mechanism. The event detail lists surrounding readings: oil pressure, water and oil temperature, air pressure, voltage, frequency, power factor, running hours, the generator's service status (section 8), feeder loads at the same time. **No cause is inferred.** Dashboard "needs action" panel shows open overload events with a link to the reading.
- **Operating limits** (optional, default none): `GeneratorOperatingLimit` (generator, parameter: oil pressure, water temp, oil temp, frequency, voltage, power factor, current; min/max). Evaluated by a background job, one notification per episode, not on the ingest path.
- **Consistency warnings** (never rejections): `|kW − kVA·PF| / kVA` above 10%; three-phase `kVA ≈ √3·V·A/1000` outside 15%; the same value repeated N consecutive times (default 6) as a possible copy-paste warning.
- **Missing-data indicators** (not blockers): hours with no Tier-1 entry for a generator/feeder (site policy interval configurable per site, used only for this indicator); days with no `DailyClosing` by end of day in station time.
- **Hourly production chart** uses measured cumulative readings where they exist at that granularity; otherwise it is **estimated from Tier-1 active load** and labelled "تقدير من الحمل اللحظي". Never present an estimate as measured.

### 7.11 [v4] Operational log views

Read-only per-generator trend charts (oil pressure, temperatures, load, frequency, voltage), the **daily log sheet** (rows = hours, columns = parameters, per generator, mirroring the paper log) with Excel/PDF export, hour-gap markers, limit/overload markers, and the supervisor's void-with-reason action.

---

## 8. Service intervals and due alerts (independent tables)

- `ServiceItem` lookup, bilingual, seeded **[v4]** with the owner's six defaults: engine oil set (250 h), oil filter (250 h), fuel filter (500 h), air filter (1000 h), nozzles (2000 h), valve adjustment (3000 h) as default interval suggestions only (real values come from each model's manufacturer data via `ServiceIntervalDefinition`); extensible.
- `ServiceIntervalDefinition`: generator model × service item → `IntervalHours`, importance (Critical/High/Normal), warning lead hours; nullable `SparePartId` added in Phase 5. Independent of the parts table. Example: Perkins 4012, oil filter 500 h, nozzles 2000 h.
- `ServiceReplacementRecord`: generator, service item, **running hours at replacement (mandatory)**, date, optional work-permit link, early flag and reason. Entering it starts that item's clock.
- `IServiceReplacementService` is the **only writer**; called from a standalone "record replacement" screen and from permit completion.
- Early replacement (D10) requires a reason, validated server-side. New hours must be ≥ previous hours for that generator and item.
- Pure **due calculator**: last-replacement hours, interval, current hours, lead hours → `NotBaselined | Ok | DueSoon | Due | Overdue` plus remaining hours. Current hours come only from official (`IsOfficial`, i.e. approved `DailyClosing`) meter readings (7.5). Provide a bulk baseline-entry screen.
- Alerts per D14: **notify only when the state changes** (including returning to Ok after replacement without re-notifying). The new engine becomes the single source of due alerts. **[v4]** Retire `SparePartReplacementAlertJob` only after the new engine is live (Phase 4) so users never get two alerts for one event. **Do not delete `InstalledPart`** unless permit part lines plus the serialized-part lifecycle fully replace its history (serial, installed hours, removal, early-replacement reason); prefer migrating its data model into permit part lines and list exactly what would be lost.
- **[v4] Per-generator maintenance table view:** item, interval, last service hours, due-at, progress bar, label (red "متأخر N ساعة", amber/green "متبقي N ساعة"), and a counter of items due or overdue; a "record replacement" button opens the standalone entry (generator, item(s), running hours, date/time).

---

## 9. Inventory (Warehouse)

Inventory works at **Station** level. Extend the existing warehouse (supply, stock, dispatch, ledger); do not recreate it.

- Catalog: parts with category lookup, unit, `IsSerialized`, optional service-item link. Minimum stock per station × part (D5).
- Receiving: header (station, supply-voucher no., invoice no., invoice date) and lines (part, quantity, unit price, serial numbers when serialized). Quick-create part allowed.
- Serialized parts: one row per unit with unique serial per part (filtered, soft-delete aware); lifecycle InStock / Issued / ….
- Append-only ledger; **invariant: balance = sum of ledger**. Stock can never go negative: service check plus a database check constraint.
- Issue requests: details table with requested, **remaining stock**, approved quantities. Approval is **one transaction** that locks balances, verifies all lines, writes ledger entries, and is **idempotent**. Existing check-then-write logic must be replaced with a conditional, concurrency-safe update. Serialized parts require choosing specific serials.
- Catalog screen lets the user link an hour-bound part to its service interval (calls the Phase 4 API).

---

## 10. Maintenance (work permits)

Extend the existing `WorkPermit`.

- Numbering per site (D7), generated atomically (counter row + transaction, filtered unique `(SubStation, Sequence)`); concurrency test required.
- General (no generator) or generator-specific (generator required iff specific).
- Fields: type (routine, preventive, scheduled, half-life, life, inspection), generator or general, department (lookup), problem, cause, level (critical, emergency, normal), **multiple assignees** (D8).
- Parts: lines (part, quantity, `IsReplacement`); "Request from store" creates a linked issue request.
- Results: work performed, results, readiness (D11). **[v4] Digital defect log fields:** `InspectionResult`, defect category lookup (mechanical/electrical/fuel/cooling/control), severity lookup, actual start/end with computed downtime, before/after photos (same attachment pipeline and 65 KB rule).
- **Replacement rule:** any replacement requires the generator's **running hours** at that time; on completion each service-item line calls `IServiceReplacementService`; early replacement needs the requesting department's reason. Service-item lines are allowed only on generator-specific permits.
- Status: Draft → Open → InProgress → Completed → Closed; Cancelled from Draft/Open. Completion requires results, readiness, and hours when replacements exist. Closed is read-only. Transitions are permission-gated and scope-filtered by site.
- Extras: generator maintenance-history view; "Create permit" shortcut from a due alert, prefilled. **[v4]** A permit detail links to the generator's Tier-1 readings around the defect time.

---

## 11. Reports

1. Issued parts: to a site, a station, a generator, per department; period filter; quantities and value.
2. Supplied parts: per station and period, grouped by invoice, voucher, part, category.
3. Minimum-stock report with shortfall.
4. Periodic stock: monthly, quarterly, semi-annual, annual (opening, receipts, issues, adjustments, closing, value).
5. Stocktake sessions: snapshot, counted quantities, variance = counted − snapshot, adjustment movements on approval.
6. **[v4] Defect and maintenance log report:** filters (generator, site, period, permit type, category, severity, engineer, status, part used); **flexible text search** across problem, inspection result, cause and actions behind a search abstraction (`LIKE`/Full-Text on SQL Server; consider `pg_trgm` on PostgreSQL); Excel/PDF export through the existing mechanism; per-generator maintenance timeline.
7. **[v4] Daily operational log sheet** (7.11) with Excel/PDF export.
8. **[v4] Fuel reports:** per generator consumption and kWh per litre, reconciliation report (7.9), overload events list.

All reports: scope-filtered, DB-side projections, paginated, period boundaries in station time (D15), exported through the existing export mechanism.

---

## 12. Cross-cutting requirements

- **Security:** scope filter in every new read **and** write service; separate permissions per module and action: capture, review, configure, view, export, approve issue, close permit, report, flag meter reset. Operators cannot review; admin configuration is separate.
- **Modules:** register `OperationalData`, `Inventory`, `Maintenance` (and later `Solar`) as modules enabled per customer, using the existing `ModuleDefinition` / permission seeding; decide in Phase 1 how they relate to the existing broad `warehouse` and `maintenance` modules and document it.
- **Multi-tenancy:** this system runs tenant databases through Platform. Before the first migration, document exactly how migrations reach tenant databases and how `MigrationRunner` behaves; a failed tenant migration must not leave a tenant half-migrated.
- **Data integrity:** DB constraints for non-negative stock and valid enums; concurrency tokens on stock balances and sequence counters; transactions around multi-row operations.
- **Performance:** free-time readings grow fast; composite indexes on `(subject, CapturedAtUtc)`, filtered by status where useful; keyset paging; no N+1.
- **Notifications:** new notifications use localized keys or structured payloads, not hard-coded backend strings.
- **Observability:** structured logs on approvals, stock movements and alert evaluation; no PII or secrets.

---

## 13. Solar integration: Import and Export

The owner defined solar as follows. **Import** is the integrated energy that leaves the solar plant and enters the site network, read from the plant's output energy meter. **Export** is the energy sent out of the site: through the feeders, which carry solar and generator energy combined. For now the solar plant exports through its own dedicated feeder, which is an ordinary feeder linked to the solar source. Build it as specified below.

- New master `SolarSource` (site, code, name, optional multiplier, optional link to an existing `Meter`). A site may have several sources; start with one output (Import) meter per source; its dedicated export feeder is an ordinary Feeder (section 7.4) with a nullable link to the source.
- Solar uses the **same capture and approval pipeline** as every other reading (sections 5 and 7): subject type `SolarImport` (kWh) stored in `MeterReading`, `CapturedAt`, a mandatory photo of the meter or inverter display, Pending until approved, `Periodic` and `DailyClosing` kinds, reset flag, multiplier snapshot.
- Daily solar Import energy = (approved daily closing − previous approved daily closing) × multiplier, with the same reset and multi-day-span rules as 7.5.
- **Station energy balance (official data only):** Import = sum of generator energy produced + sum of solar import. Export = sum of feeder energy sent (all feeders, including the solar plant's dedicated feeder, so nothing is counted twice). **[v4]** Subtract internal consumption (7.5). Show the unexplained difference (absolute and %) against the configured threshold, and the solar share of Import (%). Also check each solar source against its dedicated feeder. The energy-balance board must show these lines.
- Solar does not feed the service-due engine and does not use tank, fuel or oil logic.
- Solar screens and balance lines appear only for sites that have at least one `SolarSource` and only when the Solar capability is enabled for the customer.
- The operator screen gets a fourth section, Solar, in the visual style of the designs. If the designs contain no solar element, build it in the same style and list that in the report.

**Open questions. The defaults apply until the owner answers:**

1. Answered by the owner: solar export passes through a feeder, so for now the solar plant has its own dedicated feeder. If solar export later joins a shared feeder, the balance needs a split rule. Default: dedicated feeder.
2. One output meter per solar source, or one per inverter or array? Default: one per source.
3. Is instantaneous solar power (kW) needed in periodic readings? Default: no, energy meters only.

---

## 14. Phase plan

### Phase 0R: Revise discovery (`feature/bitbus-discovery`)

Update `docs/bitbus/00-discovery.md` for this brief. No feature code. Must include: (1) the **legacy removal list** (DailyReading / SubStationReading flows, `InstalledPart` + `SparePartReplacementAlertJob`, old permit numbering, anything else) with reasons and impact on dashboards, reports and alerts that read them; (2) the findings about `docs/design/bitbus/` (files found, what each covers, conflicts with this brief); (3) the migration-to-tenants process; (4) the module/permission strategy; (5) remaining questions. **Acceptance:** the owner can approve without reading code.

### Phase 1: Foundations (`feature/bitbus-foundations`)

Module and permission registration; bilingual lookups with idempotent seeds (departments, part categories, service items); station settings (timezone default `Asia/Aden`, produced-vs-sent threshold, photo-requirement constants; **no reading interval**); attachment/storage abstraction with the 65 KB policy and server validation; shared idempotency store; i18n namespaces and parity test; navigation shells; design-token mapping. Execute the approved legacy removals only if the owner approved them. **Acceptance:** modules toggle by configuration; permissions enforceable; seeds idempotent; scope tests for lookups; photo validation tests.

### Phase 2: Capture (`feature/bitbus-capture`)

Entities and migrations for tanks (calibration), feeders, generator/tank/feeder/meter readings, submissions and attachments; `TankGeometry`; submission API with idempotency and the Pending lifecycle; the operator screen per the design with client-side compression and crop; operator history of their own submissions. **Tests:** geometry, free-time uniqueness/duplicate guard, future-time rejection, idempotency, photo-required rule, server photo validation, scope filtering. **Acceptance:** an operator on a phone submits a mixed set of lines with photos and arbitrary timestamps; retry creates no duplicates; nothing appears in any official view.

### Phase 2.1 [v4]: Corrective patch on delivered Phase 2 (`feature/bitbus-capture-fixes`)

Apply these on top of Phase 2, with tests, and report which items needed schema changes:

- **A.** Central `ReadingApprovalPolicy` and `IsOfficial` (P3): Tier-1 lines `Recorded` on ingest, never in the review queue; void with reason; Tier-2 unchanged. Move any hard-coded Pending logic behind the policy.
- **B.** Tank structure and events (7.3): `TankEventType`, `ParentTankId`, `GeneratorId` (unique per active daily tank), linked Out/In, before/after levels with photos for Tier-2 tank events.
- **C.** Photo stamp (7.6) in the compression pipeline, with test.
- **D.** Reuse-detection scope and multi-line photo reference (7.6).
- **E.** Time handling: minute normalization, submission-level default time, selectable `BusinessDate` for `DailyClosing`, `ClientSentAtUtc` and skew warning (7.1, 7.5, D26, D27).
- **F.** Offline readiness (D28): per-line `ClientLineId` (UUID from the device) in addition to the submission request id; the submission endpoint accepts partial success and returns per-line results (`accepted | duplicate | duplicate_time | rejected` + reasons), up to 50 lines per batch; `AwaitingPhoto` state for offline-queue lines (P2); data-first, photo-second upload allowed.
- **G.** Tank geometry: confirmed vertical cylinder with conical bottom; keep `TankShape` enum + strategy interface, add the setup preview, verify the required fixtures (7.3). Add the optional `PeakLoadKw` column if missing.
- **H.** Confirm Phase 1 removed every trace of the previous organization (logo, manifest names, strings, page titles) and delivered dark/light toggle, colored tab icons and self-hosted font per the designs; list anything remaining.

**Acceptance:** all new tests pass; an hourly line is saved as `Recorded` and visible in monitoring without any review; a Tier-2 line is Pending and appears in no official figure; a closing at 00:05 can belong to the previous business date; two readings seconds apart for the same subject are treated as duplicates. Report format: v3 template plus a table for items A to H (status, files, tests, schema changes, questions). Then stop and wait for `approved phase 2.1`.

### Phase 3: Review, approval and derived data (`feature/bitbus-review`)

**[v4]** Tier-2 only. Review queue and screen (photos with zoom, previous approved value, delta preview, warnings), approve/reject per line and per submission, atomic approval, order-independent monotonicity check, approver ≠ submitter, notifications, derived deltas, daily closing, produced-vs-sent **including internal consumption** and the **energy-balance board** per the design, **average load (7.10)**, history/export, authorized photo endpoint with scope check, and the editing-of-approved rule (7.1). **Tests:** state machine, concurrent approval, reset flag, out-of-order approval, multi-day span, produced-vs-sent math, scope. **Acceptance:** only approved data drives numbers; derived values match worked examples.

### Phase 3b [v4]: Fuel and load safety (`feature/bitbus-fuel-load`)

Sections 7.9, 7.10 and 7.11: `TankVolumeService`, `FuelConsumptionService`, `FuelReconciliationService`, overload events and notifications, operating limits job, consistency warnings, missing-data indicators, operational log views and daily log sheet export, tank configuration screen with setup preview, refill/transfer screen (Screen C). **Tests:** the fuel fixtures in 7.9, dead stock (dead stock 100 L with 650 L measured gives 550 L usable; consumption unchanged; reading at or below dead stock warns; entry by cm and by litres give the same result), geometry fixtures, overload threshold boundary (exactly 100% is not overload, above is), one notification per episode, consistency checks, estimate labelling on the hourly chart, scope. **Acceptance:** consumption matches hand-computed examples; refills are never counted as consumption; an overload creates exactly one alert on receipt.

### Phase 4: Service intervals and due engine (`feature/bitbus-service-intervals`)

Section 8 (including the six seeded service items and the per-generator maintenance table view). **Tests:** calculator boundaries (exactly at interval, one hour before/after, no baseline, hours regression), early-replacement validation, alert de-duplication. **Acceptance:** an oil-set replacement at X hours produces the correct next due state after the next **approved** hours reading.

### Phase 5: Inventory (`feature/bitbus-inventory`)

Section 9. **Tests:** ledger invariant, concurrent approvals (no negative stock, no double issue), partial approval, serialized parts, filtered unique indexes with soft delete, scope. **Acceptance:** receive → request → approve changes stock exactly once.

### Phase 6: Maintenance (`feature/bitbus-maintenance`)

Section 10 (including the defect-log fields). **Tests:** numbering under parallel creation, status transitions, hours mandatory with replacements, early-replacement reason, general-permit rules, multiple assignees, site scoping. **Acceptance:** due alert → permit → parts request → approval → completion with hours → due clock restarts.

### Phase 7: Reports and stocktake (`feature/bitbus-reports`)

Section 11 (items 1 to 8, including the defect and maintenance log report with flexible search and export). **Tests:** hand-computed fixtures, station-time period boundaries, variance math, scope. **Acceptance:** reports reconcile with the ledger to the unit.

### Phase 8: Solar (`feature/bitbus-solar`)

**Specified** in section 13. Depends on Phases 2 and 3; the owner may pull it forward. Tests: solar delta and reset handling, balance with and without solar, Approved-only inclusion, scope. Acceptance: the balance board shows generators + solar as Import against feeder Export, including the solar plant's dedicated feeder.

### Phase 9: Hardening and handover (`feature/bitbus-hardening`)

Permission-matrix review; automated check that every new read service applies the scope filter; i18n parity and hard-coded-string audit; index and performance review at realistic volume; migration review; docs in `docs/bitbus/` (architecture, API summary, operator, reviewer and storekeeper quick guides); dev-only demo seed; known limitations.

### Phase 10 [v4]: Offline queue and sync (`feature/bitbus-offline`)

IndexedDB outbox (`idb`; ask first per rule 7), photo store and reference-data cache (generators, tanks with calibration parameters so the litres preview works offline, feeders, limits, permissions); save-first UX with an instant "saved" state; background batches with exponential backoff; photos uploaded separately after data (`AwaitingPhoto` flow); an expired session never discards queued items (pause, ask to log in, confirm before clearing on logout); sync status chip; Background Sync only if it needs no new dependency (otherwise sync on open and on `online`). **Acceptance:** a reading captured offline arrives exactly once after reconnect even if resent; a late-arriving reading triggers recomputation of neighbouring segments (derived data only); no queued item is lost on session expiry.

---

## 15. Domain glossary (exact Arabic labels for i18n)

| English | Arabic |
| --- | --- |
| Work permit | تصريح عمل |
| General / Generator-specific | عام / خاص بمولد |
| Routine / Preventive / Scheduled | روتيني / وقائي / مبرمج |
| Half-life / Life overhaul / Inspection | نصف عمرية / عمرية / تفقدية |
| Critical / Emergency / Normal | حرجة / اضطراري / عادية |
| Mechanical / Electrical / Instrumentation / Workshop / All | ميكانيك / كهرباء / أجهزة / ورشة / كل الأقسام |
| Oil change set / Oil filter / Air filter / Nozzles | بدلة زيت / فلتر الزيت / فلتر الهواء / النوزلات |
| Supply voucher / Issue request | سند التوريد / طلب صرف |
| Operator / Feeder | المشغل / فيدر |
| Daily tank / Main tank | الخزان اليومي / الخزان الرئيسي |
| Active / Reactive load | الحمل النشط / الحمل غير النشط |
| Power factor / Running hours | معامل القدرة / ساعات التشغيل |
| Stocktake | الجرد |
| Pending / Approved / Rejected | قيد المراجعة / معتمدة / مرفوضة |
| Site / Station | موقع / محطة |
| Overload / Peak load / Average load / Instantaneous load | حمل زائد / أعلى حمل / متوسط الحمل / الحمل اللحظي |
| Refill / Transfer / Level reading | تعبئة / تحويل / قراءة مستوى |
| Recorded / Voided / Awaiting photo | مسجلة / ملغاة / بانتظار الصورة |
| Hourly round / End of day | جولة الساعة / قراءات نهاية اليوم |
| Unexplained difference | فرق غير مفسَّر |

---

## 16. Out of scope

Modbus gateway changes; changes to existing generator status logic; billing and accounting; supplier management and purchase orders; transfers between stations. **[v4]** Native mobile apps remain out of scope. **The offline queue and synchronization are in scope (Phase 10)**, designed in from Phase 2.1. Solar is in scope (section 13). If you believe another excluded item is needed, raise it in a report; do not build it.

---

## 17. Phase report template

```
Phase: <name>   Branch: <name>   Commit: <hash>
1. Done:
2. Not done (and why):
3. Assumptions made / decisions applied:
4. Decisions needed from the owner (including open solar questions):
5. Tests: added <n>; backend / frontend results; pre-existing failures:
6. Migrations: <names>; destructive operations: none/<list>
7. Conflicts found between designs and this brief:
8. Risks and follow-ups:
9. Suggested manual check in the UI:
```
