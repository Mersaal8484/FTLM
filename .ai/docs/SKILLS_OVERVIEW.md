# Skills Overview — FuelTrack-Rebuild

## Overview

Local skills defined specifically for this project. Each skill is a markdown file in `.ai/skills/` that provides specialized instructions for a specific task type. Global skills (caveman, graphify, etc.) are installed separately via `~/.config/opencode/skills/`.

## Table of Contents

- [Local Skills](#local-skills)
- [Global Skills](#global-skills)
- [Skill Selection Guide](#skill-selection-guide)
- [Quick Reference](#quick-reference)

---

## Local Skills

### 1. `fueltrack-migration`

**File:** `.ai/skills/fueltrack-migration.md`
**Purpose:** Database schema migration workflow

**Triggers:**
- Adding new columns/tables
- Renaming fields
- Changing FK relationships
- Modifying indexes

**Key Rules:**
- Sequential workflow: backup → create → verify → test → rollback plan
- Never modify committed migrations
- Always verify generated SQL before applying
- Back up before destructive changes

---

### 2. `fueltrack-crud-checklist`

**File:** `.ai/skills/fueltrack-crud-checklist.md`
**Purpose:** End-to-end CRUD module creation checklist

**Triggers:**
- "Add [entity]" / "Create [module]"
- "Build [feature] page"

**Key Rules:**
- 70+ steps across 6 layers (Domain → Application → Infrastructure → API → Frontend → Tests)
- All IDs must be `int` (not `Guid`)
- Follow existing file naming conventions
- Verify constructor signatures match DTOs
- Full test suite must pass

---

### 3. `fueltrack-permission-gate`

**File:** `.ai/skills/fueltrack-permission-gate.md`
**Purpose:** Permission/RBAC enforcement and auditing

**Triggers:**
- Modifying role permissions
- Adding new permission modules
- Changing authorization logic
- Permission corruption bugs

**Key Rules:**
- Permissions stored as JSON array format (not flat strings)
- Format: `[{"module":"Dashboard","canView":true}]`
- `PermissionMapper` required for frontend mapping
- `InitAsync()` seeds roles as JSON
- Auto-converts old flat strings via `NormalizeToJson()`
- Known bug (2026-07-05): `ConvertPermissions()` must call `ConvertDictToModules()`

---

### 4. `fueltrack-business-rules`

**File:** `.ai/skills/fueltrack-business-rules.md`
**Purpose:** Domain-specific business logic enforcement

**Triggers:**
- Implementing calculations
- Adding validation rules
- Modifying workflow logic

**Key Rules:**
- All PKs are `int`
- Lookup fields use `int` FK to `Lookup` table
- Use current entity/field names (check AGENTS.md for renames)
- No business logic in entities (keep in services)
- Business rules reference:
  - `FuelTransaction` (not `FuelMovement`)
  - `OilTransaction` (not `OilUsage`)
  - `PeriodTypeLookupId` (not `InventoryType`)
  - `SubStationReading` (not `StationReading`)

---

### 5. `fueltrack-code-review`

**File:** `.ai/skills/fueltrack-code-review.md`
**Purpose:** Structured code review with severity levels

**Triggers:**
- Reviewing changes before commit
- PR review
- Self-review

**Severity Levels:**
| Level | Description | Action |
|-------|-------------|--------|
| CRITICAL | Security, data loss, silent failures | Fix immediately |
| REQUIRED | Broken features, wrong types | Must fix before merge |
| WARNING | Missing null checks, inconsistent patterns | Should fix |
| INFO | Style improvements | Nice to have |

---

### 6. `fueltrack-test-fix`

**File:** `.ai/skills/fueltrack-test-fix.md`
**Purpose:** Systematic test failure resolution

**Triggers:**
- `dotnet test` failures
- `npm test` / `vitest` failures
- Test compilation errors

**Key Rules:**
- Categorize before fixing (syntax / domain mismatch / constructor mismatch)
- Fix root cause, not symptom
- Backend: xUnit, FluentAssertions, AutoFixture, NSubstitute
- Frontend: Vitest
- All IDs must be `int` (not `Guid`)
- Run full suite after each fix batch

---

## Global Skills

Installed at `~/.config/opencode/skills/`:

| Skill | Purpose |
|-------|---------|
| `caveman` | Ultra-compressed communication mode (75% token savings) |
| `caveman-commit` | Conventional commit message generator |
| `caveman-compress` | Compress memory files to caveman format |
| `caveman-help` | Quick reference for all caveman commands |
| `caveman-review` | Ultra-compressed code review comments |
| `caveman-stats` | Real token usage statistics |
| `cavecrew` | Delegate to subagents (investigator/builder/reviewer) |
| `graphify` | Persistent knowledge graph from codebase |

---

## Skill Selection Guide

| Task | Load This Skill |
|------|----------------|
| Add new entity/module | `fueltrack-crud-checklist` |
| Database migration | `fueltrack-migration` |
| Role/permission change | `fueltrack-permission-gate` |
| Business logic | `fueltrack-business-rules` |
| Code review | `fueltrack-code-review` |
| Fix failing tests | `fueltrack-test-fix` |
| Compress context | `caveman-compress` |
| Generate commit | `caveman-commit` |
| Delegate to subagent | `cavecrew` |
| Build knowledge graph | `graphify` |

---

## Quick Reference

| Skill | File | Lines |
|-------|------|-------|
| `fueltrack-migration` | `.ai/skills/fueltrack-migration.md` | Migration workflow |
| `fueltrack-crud-checklist` | `.ai/skills/fueltrack-crud-checklist.md` | 70+ CRUD steps |
| `fueltrack-permission-gate` | `.ai/skills/fueltrack-permission-gate.md` | Permission/RBAC |
| `fueltrack-business-rules` | `.ai/skills/fueltrack-business-rules.md` | Domain constraints |
| `fueltrack-code-review` | `.ai/skills/fueltrack-code-review.md` | Severity-based review |
| `fueltrack-test-fix` | `.ai/skills/fueltrack-test-fix.md` | Test failure resolution |

## Related Files

- `AI_WORKFLOW.md` — When to load each skill
- `CHECKLISTS.md` — Detailed checklists from each skill
- `SUBAGENTS.md` — Sub-agent definitions for delegation
