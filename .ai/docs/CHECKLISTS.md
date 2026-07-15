# Checklists — FuelTrack-Rebuild

## Overview

All checklists available for AI-assisted development. Each checklist is defined in a skill file under `.ai/skills/`. Use these to ensure completeness when performing specific task types.

## Table of Contents

- [CRUD Module Checklist](#crud-module-checklist)
- [Migration Checklist](#migration-checklist)
- [Permission Gate Checklist](#permission-gate-checklist)
- [Code Review Checklist](#code-review-checklist)
- [Test Fix Checklist](#test-fix-checklist)
- [Business Rules Checklist](#business-rules-checklist)
- [Quick Reference](#quick-reference)

---

## CRUD Module Checklist

**Source:** `.ai/skills/fueltrack-crud-checklist.md`
**Use when:** Creating or modifying a full CRUD module (entity + DTO + service + controller + frontend + tests)

### Domain Layer
- [ ] Entity inherits `BaseEntity` (int Id, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
- [ ] All FK properties use `int` (not `Guid`)
- [ ] Navigation properties defined
- [ ] No business logic in entity (keep in service)

### Application Layer
- [ ] Create DTO with positional constructor
- [ ] Update DTO with positional constructor
- [ ] List DTO (flat, no navigation props)
- [ ] Detail DTO (includes navigation data)
- [ ] Service interface (`I[Entity]Service`)
- [ ] FluentValidation validators for Create/Update

### Infrastructure Layer
- [ ] DbContext `DbSet<[Entity]>` registered
- [ ] Fluent API configuration (if needed)
- [ ] Service implementation with async/await
- [ ] Use `AsNoTracking()` for read queries
- [ ] Include navigation properties where needed

### API Layer
- [ ] Controller with `[Route("api/[controller]")]`
- [ ] GET list (paginated)
- [ ] GET by id
- [ ] POST create
- [ ] PUT update
- [ ] DELETE (Admin only)
- [ ] Proper authorization attributes
- [ ] Error handling (try/catch or global middleware)

### Frontend Layer
- [ ] Page component in `frontend/src/pages/`
- [ ] API client calls use `client` (not raw `axios`)
- [ ] React Query hooks for data fetching
- [ ] Bootstrap styling consistent with existing pages
- [ ] Loading/error/empty states

### Tests
- [ ] Service unit tests with AutoFixture
- [ ] All tests use `int` for IDs
- [ ] Constructor parameters match current DTOs
- [ ] Full test suite passes

---

## Migration Checklist

**Source:** `.ai/skills/fueltrack-migration.md`
**Use when:** Adding/modifying database schema

### Pre-Migration
- [ ] Back up current migration state
- [ ] Identify affected entities
- [ ] Check for breaking changes to existing migrations

### Migration Creation
- [ ] Use `dotnet ef migrations add [Name]`
- [ ] Review generated migration SQL
- [ ] Verify column types match domain model
- [ ] Verify FK relationships correct
- [ ] Add indexes for new query patterns

### Post-Migration
- [ ] `dotnet build` — zero errors
- [ ] `dotnet test` — all pass
- [ ] Verify data integrity
- [ ] Update AGENTS.md if significant change

### Rollback Plan
- [ ] Identify previous migration for rollback
- [ ] Test `dotnet ef database update [PreviousMigration]`
- [ ] Verify no data loss

---

## Permission Gate Checklist

**Source:** `.ai/skills/fueltrack-permission-gate.md`
**Use when:** Modifying roles, permissions, or authorization

### Permission Storage
- [ ] Permissions stored as JSON array format
- [ ] NOT flat comma-separated strings
- [ ] Format: `[{"module":"X","canView":true,"canCreate":true}]`

### Permission Mapping
- [ ] `PermissionMapper` called for frontend display
- [ ] Domain module names map to frontend names (e.g., `FuelMovements` → `fuel_movements`)
- [ ] `Stations`/`Roles`/`Settings` all map to frontend `settings`

### Role Service
- [ ] `InitAsync()` seeds roles as JSON
- [ ] `ConvertPermissions()` normalizes to array format
- [ ] Auto-converts old flat strings via `NormalizeToJson()`

### Backward Compatibility
- [ ] Old flat string permissions still readable
- [ ] `ConvertFromFlatString()` maps legacy keys
- [ ] No data loss on re-save cycle

### Verification
- [ ] `POST /api/roles/init` re-seeds correctly
- [ ] Frontend permission tree displays correctly
- [ ] No `[[[]]]` corruption on re-fetch/re-save

---

## Code Review Checklist

**Source:** `.ai/skills/fueltrack-code-review.md`
**Use when:** Reviewing any code changes

### Severity Levels

| Level | Action Required |
|-------|----------------|
| CRITICAL | Fix immediately — security, data loss, silent failures |
| REQUIRED | Must fix before merge — broken features, wrong types |
| WARNING | Should fix — missing null checks, inconsistent patterns |
| INFO | Nice to have — style improvements |

### Review Areas
- [ ] Security: no secrets in code, proper auth attributes
- [ ] Correctness: types match, null checks present
- [ ] Performance: no N+1 queries, proper indexing
- [ ] Consistency: follows existing patterns
- [ ] Tests: coverage adequate, no regressions

---

## Test Fix Checklist

**Source:** `.ai/skills/fueltrack-test-fix.md`
**Use when:** Tests are failing

### Categorize Failures
- [ ] Syntax errors (missing semicolons, wrong method names)
- [ ] Domain model mismatch (field renamed/removed)
- [ ] Constructor mismatch (DTO parameters changed)
- [ ] Type mismatch (Guid vs int, string vs int)

### Fix Strategy
- [ ] Fix root cause, not symptom
- [ ] Update test data to match current entities
- [ ] Update assertions to match current behavior
- [ ] Run full suite after each fix batch
- [ ] No regressions

### Verification
- [ ] `dotnet test` — all pass
- [ ] `npm run lint` — no new errors (frontend)
- [ ] No test exclusions added without justification

---

## Business Rules Checklist

**Source:** `.ai/skills/fueltrack-business-rules.md`
**Use when:** Implementing or modifying business logic

### Domain Rules
- [ ] All PKs are `int` (not `Guid`)
- [ ] `CreatedBy`/`UpdatedBy` are `int` (user IDs)
- [ ] Lookup fields use `int` FK to `Lookup` table
- [ ] No business logic in entities (keep in services)

### Naming Rules
- [ ] Use current entity names (e.g., `FuelTransaction`, not `FuelMovement`)
- [ ] Use current field names (e.g., `PeriodTypeLookupId`, not `InventoryType`)
- [ ] Use current DTO constructor signatures

### Data Integrity
- [ ] FK relationships enforced
- [ ] Required fields validated
- [ ] No orphaned records on delete

---

## Quick Reference

| Checklist | Skill File | Use When |
|-----------|-----------|----------|
| CRUD Module | `fueltrack-crud-checklist.md` | New entity/module |
| Migration | `fueltrack-migration.md` | Schema changes |
| Permission Gate | `fueltrack-permission-gate.md` | Role/permission changes |
| Code Review | `fueltrack-code-review.md` | Any code review |
| Test Fix | `fueltrack-test-fix.md` | Failing tests |
| Business Rules | `fueltrack-business-rules.md` | Business logic changes |

## Related Files

- `SKILLS_OVERVIEW.md` — Full skill definitions
- `AI_WORKFLOW.md` — When to use each checklist
- `AI_GUIDELINES.md` — Constraints that apply to all checklists
