# Phase 4 completion audit — read-only

**Audit date:** 2026-11-11  
**Audited commit:** `32275c010acb666e1ed8788dd4ee4f6b75e23d0e`  
**Base:** `f5a15cb511966ba9660aa6bc5e8e3ac7078e7067`  
**Scope:** source and Git metadata only. No source, migration, dependency, database, branch, or remote was changed by this audit.

## 1. Verdict

**Incomplete.** Commit `32275c0` implements a substantial Phase 4 surface: four domain tables, a tenant upgrade, calculator, controllers, scheduler integration, UI pages, and tests. It does not meet important mandatory requirements from the Phase 4 prompt: service APIs and writes do not enforce user scope, the required operational-data feature gate is absent, replacements are not idempotent, suggested intervals are used as real definitions, and notifications are hard-coded English rather than localized keys. The committed report also identifies the wrong commit and makes claims contradicted by its own text/history.

## 2. Push status and provenance

- The Phase 4 worktree is on `feature/bitbus-service-intervals`, tracking `personal/feature/bitbus-service-intervals`; it is clean (`git status --porcelain` produced no output).
- Local remote-tracking ref `personal/feature/bitbus-service-intervals` equals `32275c0`; `git diff --stat personal/feature/bitbus-service-intervals HEAD` produced no output. Thus, **the local tracking data says the commit was pushed to the personal feature branch**.
- Live `git ls-remote personal …` could not contact GitHub (`Failed to connect to github.com:443`). Remote equality is therefore **UNVERIFIED live**, not disproved. Re-run the command below when network access is available.
- The local `personal/development` tracking ref is `f5a15cb`, not `32275c0`; no evidence in the local refs shows Phase 4 merged into `development`. `origin/master` remains `b08555f`.
- `32275c0` is one Conventional Commit (`feat(maintenance): …`) with parent `f5a15cb`. It changes 54 files / 4,751 insertions and does not itself create a merge commit.
- `008_phase4_service_intervals.sql` and `04-service-intervals.md` are tracked by `32275c0`, not ignored. This audit report is intentionally untracked.

Live verification:

```powershell
Set-Location 'D:\FTM\.worktrees\bitbus-service-intervals'
git ls-remote personal refs/heads/feature/bitbus-service-intervals refs/heads/development refs/heads/master
git fetch personal
git diff --stat personal/feature/bitbus-service-intervals HEAD
```

## 3. Requirement traceability matrix

| Requirement | Prompt section | Status | Evidence | Notes |
|---|---:|---|---|---|
| Dedicated feature branch based on `f5a15cb` | §0 | DONE | `32275c0^ = f5a15cb`; branch tracks `personal/feature/bitbus-service-intervals` | Remote status live is UNVERIFIED because network was blocked. |
| One Conventional Commit; no merge into development | §0 | DONE / UNVERIFIED | `git show -s 32275c0`; local `personal/development = f5a15cb` | No live GitHub verification possible. |
| Read and report design-folder findings | §0, §8 | DEVIATES | `docs/bitbus/04-service-intervals.md:115-117`; current repo root has `docs/design/` | Report says design folder was absent. It is not tracked by this commit, so historical presence is UNVERIFIED; the statement is not reliable. |
| Service items, definitions, records, states exist | §1, §4 | DONE | `ServiceItem.cs`, `ServiceIntervalDefinition.cs`, `ServiceReplacementRecord.cs`, `ServiceDueState.cs`; upgrade 008 | All inherit `BaseEntity`; DbContext config has active filtered indexes. |
| Six defaults, independent table, Arabic names, admin CRUD/deactivation | P4-1 | PARTIAL | `008_phase4_service_intervals.sql:22-44`; `ServiceItemsController.cs`; `ServiceItemService.cs` | Seed data is idempotent and does not overwrite edits. Arabic literals are mojibake in the committed SQL when decoded as UTF-8. Delete returns 409 when referenced. Legacy lookup remains but “unused” is not established. |
| Suggested intervals are hints only, never actual definitions | P4-2 | DEVIATES | `ServiceDueService.cs:133-161`; `ServiceReplacementService.cs:50-54,173-175` | When no definition exists, code uses `SuggestedIntervalHours` (or 250) for due state and validation. This is explicitly prohibited. |
| Single model-key normalization function | P4-3 | DONE | `Domain/Services/GeneratorModelKey.cs`; service uses `GeneratorModelKey.Normalize` | No contrary comparison found in new Phase 4 services. |
| Reset invalidates prior baseline; rebaseline required | P4-4 | PARTIAL | `ServiceDueCalculator.cs:46-56`; due/replacement services query official resets | Reset is evaluated from official readings, but requirements concerning all reset boundary cases have no dedicated integration test. |
| Immediate immutable records; void with reason | P4-5 | PARTIAL | `ServiceReplacementService.cs:87-106,234-275` | Records are appended and voided, but no idempotency key is used; duplicate retries create duplicate records. |
| Exact statuses and default warning lead | P4-6 | DONE | `ServiceDueCalculator.cs:26-115`; calculator tests | Boundaries shown as `<0`, `==0`, `<= lead`, `> lead`; default `max(50,10%)`. |
| Escalation-only, one alert per cycle | P4-7 | PARTIAL | `ServiceDueEvaluationService.cs:124-190`; `ServiceDueCalculator.ShouldNotify` | Sequential de-duplication exists, but state is saved **after** `PublishScopedAsync`, so publish failure/concurrent evaluators can duplicate or lose state. No concurrency test. |
| Monotonic hours and warning/early-reason validation | P4-8, P4-9 | PARTIAL | `ServiceDueCalculator.cs:117+`; calculator tests | Server validation exists. The prompt’s named warning constant and all boundary tests are not evidenced. |
| Operational-data new-mode feature gate; legacy unchanged | P4-10 | DEVIATES | `AlertGeneratorService.cs:73-84`; all four controllers | Only legacy installed-part alert is gated. New endpoints/pages/services have no feature-mode gate; a legacy customer can call them if it has maintenance permission. |
| No Phase 5 `SparePartId` | §1, §4 | DONE | `ServiceIntervalDefinition.cs`; `008_phase4_service_intervals.sql` | No `SparePartId` found in Phase 4 model/upgrade. |
| No legacy installed-part deletion / generator status / Modbus changes | §1 | DONE | `git diff f5a15cb..32275c0 --name-only` | No such files altered except additive alert gating. |
| Additive/idempotent PostgreSQL upgrade 008; registered and embedded | §4 | DONE | `008_phase4_service_intervals.sql`; `FuelTrack.Infrastructure.csproj`; `TenantSchemaUpgradeCatalog.cs:12-18` | Quoted identifiers and `IF NOT EXISTS` are used. |
| Every upgrade enumeration updated; 001–007 byte-identical | §4 | PARTIAL / UNVERIFIED | Catalog includes 2–8; diff shows only catalog and new 008 | Search supports catalog registration, but byte-by-byte historical comparison and all enumerations were not independently proven in this audit. |
| EF drift measured before/after; no catch-up migration | §4 | UNVERIFIED | No EF migration added in commit | No observed `has-pending-model-changes` output supplied for this exact remote commit. |
| Pure calculator | §5 | DONE | `ServiceDueCalculator.cs`; `ServiceDueCalculatorTests.cs` | Pure static implementation. |
| Separate item/definition/replacement/due/evaluation services | §5 | DONE | interfaces and infrastructure services added by `32275c0` | Replacement service is the apparent production writer. |
| Scope filter in every read and write | §5 | MISSING | `ServiceDueService.cs:20-90`; `ServiceReplacementService.cs:28-320`; controllers do not pass user ID | New services inject no `IUserScopeFilterService`; any authorized maintenance user can address another site/generator. This is a security boundary defect. |
| Projection reads (`AsNoTracking` + `Select`) | §5 | DEVIATES | `ServiceDueService.cs:24-27,42-55,125-131`; `ServiceReplacementService.cs:283-297` | Uses `Include` and materialization, not required projections. |
| Idempotent replacement/baseline command | §5 | MISSING | `IIdempotencyService` has no Phase 4 caller; `ServiceReplacementsController.cs` | Duplicate POSTs are not protected. |
| Bulk partial success | §5 | MISSING | `ServiceReplacementService.cs:166-193` | It aborts on first missing item and otherwise inserts all rows; it returns no per-row success/error result. |
| Official running hours, `CapturedAtUtc` ordering, as-of timestamp | §5 | PARTIAL | `ServiceDueService.cs:100-122`; `ServiceDueEvaluationService.cs:60-83` | Official status and captured-time ordering are present. It falls back to **any** official running-hours reading if no DailyClosing, contrary to the required official closing source; no explicit as-of request/value is implemented. |
| `NotConfigured` behavior | §5 | PARTIAL | enum `ServiceDueStatus.cs:10`; `ServiceDueService.cs:133-161` | Enum exists but no-definition fallback creates normal rows instead of returning `NotConfigured`. |
| Localized notification keys and scoped publish | §5 | DEVIATES | `ServiceDueEvaluationService.cs:154-166,193-216` | `PublishScopedAsync(...,["maintenance"],…)` is used, but title/message are hard-coded English (Arabic value computed but discarded). |
| Hourly tenant evaluation with lock; no Hangfire in MT | §5 | DONE | `TenantMaintenanceScheduler.cs`; `TenantArchivalRunner.cs:17-102`; `Program.cs` | Uses tenant eligibility and PostgreSQL advisory lock. |
| Immediate evaluation after replacement and Tier-2 approval | §5 | PARTIAL | `ServiceReplacementService.cs:108-116`; `OperationalReviewService.cs` change | Replacement runs after durable record save. Approval hook exists, but the audit did not prove its exact daily-closing/Tier-2 predicate. |
| Legacy removal at one decision point | §5 | PARTIAL | `AlertGeneratorService.cs:73-84` | Legacy alert is gated, but this is not enough to gate the new path for legacy customers. |
| Granular permission actions, module seed/role grants | §5 | MISSING | `PermissionEnforcementMiddleware.cs:60-66` | Controllers map only to broad `maintenance`; no Phase 4 granular action mapping, module-seed extension, or role grant documentation found. |
| Operator lacks configure/record by default | §5 | UNVERIFIED | no dedicated Phase 4 role test/change found | Existing role defaults were not traced sufficiently. |
| Per-generator table and replacement UI | §6 | DONE | `GeneratorMaintenanceTable.jsx`; `RecordReplacement.jsx` | Page/test added. |
| Bulk baseline and definitions administration UI | §6 | DONE | `BulkBaseline.jsx`; `ServiceDefinitionsAdmin.jsx` | Screens exist. |
| Dashboard summary | §6 | DONE | `MaintenanceDashboard.jsx` change | Additive card is present. |
| RTL/mobile/loading/empty/error/accessibility | §6 | PARTIAL | Phase 4 pages and `i18n.js` | Existing visual patterns are reused, but automated coverage only has one generator-table test; no focused accessibility/mobile verification. |
| i18n ar/en parity and translated notifications | §6 | PARTIAL | `i18n.js`; `i18n.test.js`; `Notifications.jsx` | Parity test passed according to owner output. Notification payload text remains English, not translation keys. |
| No new dependencies | §6 | DONE | `git diff --name-only f5a15cb..32275c0` | No package manifest/lockfile in Phase 4 commit. |
| Calculator mandatory cases | §7 | PARTIAL | `ServiceDueCalculatorTests.cs:37-273` | Exact interval, one hour before/after, no baseline, regression and early-reason cases exist. |
| Alert de-dupe, scope, idempotency, late approval, Postgres proof tests | §7 | MISSING / PARTIAL | `ServiceDueCalculatorTests.cs:273`; `ServiceDueServiceTests.cs` | Only a pure `ShouldNotify` test is present. No Phase 4 scope/idempotency/concurrency/late-approval test; `tests/PostgresModel.Proof` unchanged by `32275c0`. |
| Report follows §17 and required extras | §8 | DEVIATES | `docs/bitbus/04-service-intervals.md` | Wrong commit (`ba317f8` at line 5), contradicts itself on verification (lines 6 vs 68), lacks a full legacy-removal/lost-history list and verified EF-drift evidence. It correctly stops awaiting approval. |

## 4. Defects and gaps (ordered by severity)

| ID | Severity | Evidence | Description and suggested fix |
|---|---|---|---|
| P4-A01 | **Blocker** | `ServiceDueService.cs:20-90`; `ServiceReplacementService.cs:28-320`; controllers | No `IUserScopeFilterService`/current-user enforcement exists in Phase 4 service reads or writes. A permitted user can query, replace, baseline, void, or evaluate another scope’s generator by ID. Add scoped generator/substation queries and pass identity through every service/controller; add API scope-denial tests. |
| P4-A02 | **High** | `ServiceDueService.cs:133-161`; `ServiceReplacementService.cs:50-54,173-175` | Suggested intervals become operational intervals whenever a model definition is absent. Return `NotConfigured` and forbid recording against undefined model/item definitions; use suggestions only to prefill admin UI. |
| P4-A03 | **High** | `ServiceReplacementsController.cs`; `ServiceReplacementService.cs:87-106,176-195` | Record and bulk-baseline commands do not use `IIdempotencyService`. Network retries create duplicate service history. Require a tenant/user-scoped client request ID and replay stored response. |
| P4-A04 | **High** | `ServiceDueEvaluationService.cs:152-190,193-216` | Alert state is persisted after publishing, and notification text is hard-coded English. Commit state before/atomically with an outbox or protected state transition, publish after commit, and store i18n keys/parameters rather than English strings. |
| P4-A05 | **High** | All Phase 4 controllers; `AlertGeneratorService.cs:73-84` | `operational_data` gate only suppresses the old alert. New Phase 4 endpoints and UI remain available in legacy mode. Gate the whole new path at one server-side decision point and hide navigation/routes appropriately. |
| P4-A06 | **Medium** | `ServiceDueService.cs:109-116`; `ServiceDueEvaluationService.cs:69-75` | Source falls back to any official running-hour meter reading instead of the required official `DailyClosing` data. Remove fallback or explicitly document/approve it. |
| P4-A07 | **Medium** | `ServiceReplacementService.cs:140-231` | Bulk baseline is all-or-nothing and lacks row-level outcomes, not the required partial-success result. |
| P4-A08 | **Medium** | `ServiceDueService.cs:24-27,42-55,125-131`; `ServiceReplacementService.cs:283-297` | Required projection-only query convention is not followed; Includes materialize navigation graphs. Replace with `AsNoTracking().Select(...)`. |
| P4-A09 | **Medium** | `PermissionEnforcementMiddleware.cs:60-66`; no related module/role changes | Broad module mapping is not granular actions; no new role grants/default operator-denial evidence. Add explicit action permissions and tests. |
| P4-A10 | **Medium** | `008_phase4_service_intervals.sql:23,27,31,35,39,43`; `04-service-intervals.md` | Arabic seed/report content is mojibake in the committed file representation. Recreate affected text as real UTF-8 Arabic and prove with a migration-safe correction strategy; never edit applied upgrade 008. |
| P4-A11 | **Low** | `04-service-intervals.md:5-6,68,97-103,116` | Report names non-existent commit `ba317f8`, says both VERIFIED and unverified, and says design folder absent. Correct report facts in a follow-up documentation commit. |

## 5. Tests and verification

### Observed owner-supplied results

The supplied log records these commands as successful for the Phase 4 worktree:

- Backend tests: **397 total; 387 passed; 0 failed; 10 skipped** (`dotnet test FuelTrack.sln --no-build --no-restore`).
- Frontend tests: **18 files / 64 tests passed** (`npm.cmd run test -- --run`).
- Frontend production build: passed (`npm.cmd run build`), with existing Rollup chunk-size and PURE-comment warnings.

The log does **not** prove restore/build/EF-drift commands for `32275c0`, so those are unverified. The committed report claims the same test counts but its top-level verification claim is internally inconsistent.

### Test quality assessment

- Strong enough: pure calculator boundary tests (no baseline, exact due, one hour before/after, regression, early reason); lifecycle/void tests.
- Weak/incomplete: `AlertDeduplication_ShouldNotify_OnUpwardEscalationOnly` tests a pure helper, not persistence, transaction ordering, or concurrent hourly/immediate evaluation.
- Missing: scope-denial tests, idempotency replay/conflict tests, partial bulk outcomes, no-definition `NotConfigured`, legacy/new feature-mode gates, approved-vs-pending/rejected/voided source isolation, notification localization/publish-after-commit, and a Phase 4 PostgreSQL proof extension.

### Commands for the owner (not run by this audit)

```powershell
Set-Location 'D:\FTM\.worktrees\bitbus-service-intervals'
dotnet restore FuelTrack.sln
dotnet build FuelTrack.sln --no-restore
dotnet ef migrations has-pending-model-changes --project src/FuelTrack.Infrastructure --startup-project src/FuelTrack.API
dotnet test FuelTrack.sln --no-build --no-restore
Set-Location frontend
npm.cmd run test -- --run
npm.cmd run build
```

PostgreSQL proof remains **UNVERIFIED** without an owner-provided disposable connection string:

```powershell
$env:ConnectionStrings__Platform = '<disposable PostgreSQL connection string>'
dotnet run --project tests/PostgresModel.Proof
```

## 6. Arabic summary (ملخص بالعربية)

- **الحكم: Phase 4 غير مكتملة** رغم أن جزءًا كبيرًا من الجداول والواجهات والمنطق موجود.
- أخطر خطأ: خدمات Phase 4 لا تتحقق من نطاق المستخدم؛ يمكن لمستخدم لديه صلاحية عامة استهداف مولد خارج موقعه عبر المعرّف.
- القيم المقترحة للخدمة تُستخدم كفترات حقيقية عند غياب تعريف الموديل، وهذا يخالف القرار P4-2.
- حفظ الاستبدال وBaseline ليسا Idempotent؛ إعادة الطلب قد تنشئ سجلات مكررة.
- بوابة `operational_data` لا تمنع المسار الجديد؛ هي توقف التنبيه القديم فقط.
- إشعارات الاستحقاق نصوص إنجليزية ثابتة وحفظ حالة منع التكرار يأتي بعد النشر.
- التقرير المرفوع يذكر hash غير صحيح ويجمع بين عبارة «تم التحقق» و«لم تُشغّل الاختبارات».
- نتائج الاختبارات المرسلة جيدة: 387 ناجحًا في الخلفية و64 في الواجهة، لكنها لا تغطي فجوات النطاق وidempotency والتزامن.
- يجب إصلاح أمن النطاق أولًا، ثم P4-2/idempotency/gating، ثم إعادة الاختبارات وPostgreSQL proof.
