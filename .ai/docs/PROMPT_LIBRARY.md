# Prompt Library — FuelTrack-Rebuild

## Overview

Reusable prompts for common development tasks. Each prompt is a markdown file in `.ai/prompts/` that provides structured instructions for AI to follow. Prompts are templates — fill in the placeholders with project-specific values.

## Table of Contents

- [Available Prompts](#available-prompts)
- [Using Prompts](#using-prompts)
- [Creating New Prompts](#creating-new-prompts)
- [Quick Reference](#quick-reference)

---

## Available Prompts

### 1. `create-crud-module.md`

**File:** `.ai/prompts/create-crud-module.md`
**Purpose:** End-to-end CRUD module creation with all layers

**Placeholders:**
- `[Entity]` — Entity name (e.g., `FuelTransaction`, `Generator`)
- `[ plural]` — Plural form (e.g., `FuelTransactions`, `Generators`)
- `[ feature]` — Feature directory name (e.g., `FuelMovements`, `Generators`)
- `[ description]` — Brief description of what the entity represents

**Usage:**
```
Load create-crud-module.md and fill in:
- Entity: SubStationReading
- Plural: SubStationReadings
- Feature: StationReadings
- Description: Daily readings from sub-station meters
```

**What it generates:**
- Domain entity with BaseEntity inheritance
- Application DTOs (Create, Update, List, Detail)
- Service interface and implementation
- API controller with full CRUD endpoints
- Frontend page component
- Unit tests

---

## Using Prompts

### Step 1: Load the prompt

Read the prompt file to understand the template:

```
Read: .ai/prompts/create-crud-module.md
```

### Step 2: Fill placeholders

Replace `[Entity]`, `[plural]`, `[feature]`, `[description]` with actual values.

### Step 3: Follow the instructions

Execute each step in the prompt sequentially. Load relevant skills as needed:

| Prompt Step | Load Skill |
|-------------|-----------|
| Domain entity | — |
| DTOs | `fueltrack-crud-checklist` |
| Service | `fueltrack-business-rules` |
| Controller | `fueltrack-code-review` |
| Frontend page | — |
| Tests | `fueltrack-test-fix` |

### Step 4: Verify

Run build and tests after completing all steps:

```powershell
dotnet build FuelTrack.sln
dotnet test tests/FuelTrack.Tests/FuelTrack.Tests.csproj
cd frontend; npm run lint
```

---

## Creating New Prompts

### Prompt Structure

```markdown
# [Prompt Name]

## Overview
[What this prompt does]

## Placeholders
- `[placeholder1]` — Description
- `[placeholder2]` — Description

## Steps

### Step 1: [Step name]
[Instructions]

### Step 2: [Step name]
[Instructions]

...

## Verification
[How to verify the work is complete]
```

### Naming Convention

- Use kebab-case: `create-crud-module.md`, `fix-permission-bug.md`
- Start with action verb: `create-`, `fix-`, `update-`, `add-`
- Be specific: `add-substation-reading-entity.md` not `add-entity.md`

### Where to Store

All prompts go in `.ai/prompts/` directory.

---

## Prompt Templates

### Template: Create Entity

```markdown
# Create [Entity] Entity

## Placeholders
- `[Entity]` — Entity name
- `[ table]` — Database table name

## Steps

1. Create `src/FuelTrack.Domain/Models/[Entity].cs`
   - Inherit `BaseEntity`
   - Add properties with correct types
   - All FKs must be `int`

2. Add `DbSet<[Entity]>` to `FuelTrackDbContext.cs`

3. Create migration:
   ```powershell
   dotnet ef migrations add Add[Entity]
   ```

4. Verify build passes
```

### Template: Fix Bug

```markdown
# Fix [Bug Description]

## Placeholders
- `[Entity]` — Affected entity
- `[ symptom]` — What's broken
- `[ root cause]` — Why it's broken

## Steps

1. Load `fueltrack-test-fix` skill
2. Locate failing test
3. Identify root cause
4. Fix the issue
5. Run full test suite
6. Verify no regressions
```

---

## Quick Reference

| Prompt | File | Use When |
|--------|------|----------|
| `create-crud-module` | `.ai/prompts/create-crud-module.md` | New entity/module |

## Related Files

- `AI_WORKFLOW.md` — When to use prompts
- `SKILLS_OVERVIEW.md` — Skills to load during prompt execution
- `CHECKLISTS.md` — Checklists to verify completeness
