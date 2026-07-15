# AI Development Workflow — FuelTrack-Rebuild

## Overview

This document defines the recommended workflow for AI-assisted development on the FuelTrack-Rebuild project. It covers the end-to-end process from task intake through code delivery, leveraging local skills, sub-agents, MCP servers, and checklists.

## Table of Contents

- [Task Intake](#task-intake)
- [Phase 1: Exploration & Planning](#phase-1-exploration--planning)
- [Phase 2: Implementation](#phase-2-implementation)
- [Phase 3: Verification](#phase-3-verification)
- [Phase 4: Code Review](#phase-4-code-review)
- [Phase 5: Delivery](#phase-5-delivery)
- [Quick Reference](#quick-reference)

---

## Task Intake

Before writing any code, classify the task:

| Task Type | Trigger | First Action |
|-----------|---------|--------------|
| New CRUD module | "add X", "create X entity", "build X page" | Load `create-crud-module.md` prompt + `fueltrack-crud-checklist` |
| Bug fix | "fix X", "broken X", "X doesn't work" | Load `caveman-lite` mode, use `cavecrew-investigator` |
| Migration/schema change | "add column", "rename field", "new table" | Load `fueltrack-migration` skill |
| Test failure | "tests failing", "fix tests" | Load `fueltrack-test-fix` skill |
| Code review | "review X", "check PR" | Load `fueltrack-code-review` skill |
| Permission/RBAC change | "role X can/can't Y" | Load `fueltrack-permission-gate` skill |
| Documentation | "document X", "write docs" | Load `documentation-generation-doc-generate` or `readme` skill |

---

## Phase 1: Exploration & Planning

### 1.1 Dispatch `cavecrew-investigator`

Use `cavecrew-investigator` to locate all relevant files without burning context:

```
@cavecrew-investigator Find all files related to [feature/entity]
```

**What it returns:**
- Absolute file paths with line ranges
- Pattern matches and type references
- Import chains and dependencies

### 1.2 Read existing patterns

Before implementing, read neighboring files to understand conventions:

```
Read: src/FuelTrack.Application/Interfaces/I[Entity]Service.cs
Read: src/FuelTrack.Infrastructure/Services/[Entity]Service.cs
Read: src/FuelTrack.API/Controllers/[Entity]Controller.cs
Read: frontend/src/pages/[Entity].js
```

### 1.3 Create task plan

Use `plan-writing` or `writing-plans` skill for multi-step tasks. Break work into atomic steps with verification criteria.

---

## Phase 2: Implementation

### 2.1 Load appropriate skill

| Task | Skill |
|------|-------|
| New CRUD | `fueltrack-crud-checklist` — follow all 70+ steps |
| Migration | `fueltrack-migration` — sequential workflow |
| Business logic | `fueltrack-business-rules` — check domain constraints |
| Permission gate | `fueltrack-permission-gate` — verify JSON array format |

### 2.2 Dispatch `cavecrew-builder`

For surgical edits (1-2 files), dispatch `cavecrew-builder`:

```
@cavecrew-builder [precise instruction]
```

**Constraints:**
- Max 2 files per dispatch
- No architectural changes
- Must match existing code style
- Cannot add new dependencies

### 2.3 Code style rules

- **Backend**: C# conventions, xUnit, FluentAssertions, AutoFixture
- **Frontend**: React functional components, Bootstrap classes, React Query for data, Zustand for state
- **No comments** unless explicitly requested
- **No emojis** unless explicitly requested
- Follow existing import patterns in neighboring files

---

## Phase 3: Verification

### 3.1 Build check

```powershell
dotnet build FuelTrack.sln
```

**Zero errors required.** Warnings from NuGet/SYSLIB are acceptable.

### 3.2 Test suite

```powershell
dotnet test tests/FuelTrack.Tests/FuelTrack.Tests.csproj --logger "console;verbosity=normal"
```

**All existing tests must pass.** No regressions allowed.

### 3.3 Frontend lint

```powershell
cd frontend; npm run lint
```

### 3.4 Load `fueltrack-test-fix` if tests fail

Follow the skill's workflow: categorize failures (syntax vs domain model mismatch vs constructor mismatch), fix root cause, run full suite again.

---

## Phase 4: Code Review

### 4.1 Load `fueltrack-code-review`

Review your own changes against severity levels:

| Severity | Action |
|----------|--------|
| CRITICAL | Fix immediately — security, data loss, silent failures |
| REQUIRED | Must fix before merge — broken features, wrong types |
| WARNING | Should fix — missing null checks, inconsistent patterns |
| INFO | Nice to have — style improvements |

### 4.2 Dispatch `cavecrew-reviewer`

For independent review:

```
@cavecrew-reviewer Review changes in [file/PR]
```

### 4.3 Permission audit

If changes touch roles, users, or authorization:

1. Load `fueltrack-permission-gate`
2. Verify permission strings use JSON array format
3. Check PermissionMapper is called for frontend mapping
4. Verify `InitAsync()` seeds correct permissions
5. Check backward compatibility with old flat strings

---

## Phase 5: Delivery

### 5.1 Commit

Use `caveman-commit` skill for conventional commit messages.

### 5.2 Update AGENTS.md

If the session made significant architectural changes or fixed critical bugs, add a summary to `AGENTS.md` following the existing format:

```markdown
## Summary — Session YYYY-MM-DD

### Goal
- [x] Description

### Changes Made
- File: change description

### Verification
- Build: result
- Tests: result
```

### 5.3 Do NOT

- Commit without explicit user request
- Modify app source code beyond scope
- Invent new architectural patterns
- Add dependencies not already in the project

---

## Quick Reference

| Action | Tool/Skill |
|--------|-----------|
| Locate files | `cavecrew-investigator` |
| Surgical edit | `cavecrew-builder` |
| Code review | `fueltrack-code-review` + `cavecrew-reviewer` |
| New CRUD module | `create-crud-module.md` prompt + `fueltrack-crud-checklist` |
| Migration | `fueltrack-migration` |
| Fix failing tests | `fueltrack-test-fix` |
| Permission changes | `fueltrack-permission-gate` |
| Business rules | `fueltrack-business-rules` |
| Commit message | `caveman-commit` |
| Compress context | `caveman-compress` |
| Token usage stats | `caveman-stats` |

## Related Files

- `AI_GUIDELINES.md` — Constraints and rules for AI
- `CHECKLISTS.md` — All available checklists
- `SKILLS_OVERVIEW.md` — Local skill definitions
- `MCP_SETUP.md` — MCP server configuration
- `SUBAGENTS.md` — Sub-agent definitions
- `PROMPT_LIBRARY.md` — Reusable prompts
