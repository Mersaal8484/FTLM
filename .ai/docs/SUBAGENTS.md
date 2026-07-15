# Sub-Agents — FuelTrack-Rebuild

## Overview

Sub-agents are specialized AI agents that handle specific tasks without burning the main context window. They follow the "cavecrew" pattern: small, focused, disposable. Each agent has a defined scope and returns compressed output.

## Table of Contents

- [Agent Definitions](#agent-definitions)
- [Dispatching Agents](#dispatching-agents)
- [Agent Output Format](#agent-output-format)
- [When to Use Each Agent](#when-to-use-each-agent)
- [Quick Reference](#quick-reference)

---

## Agent Definitions

### 1. `cavecrew-investigator`

**File:** `.ai/agents/cavecrew-investigator.md`
**Purpose:** Read-only code locator — finds files, patterns, references without burning context

**Capabilities:**
- Locate files by name, pattern, or content
- Trace import chains and dependencies
- Find type references and usages
- Return absolute paths with line ranges

**Constraints:**
- Read-only — no edits
- No architecture changes
- Compressed output (60% smaller than raw)

**Example dispatch:**
```
@cavecrew-investigator Find all files related to FuelTransaction entity
```

**Returns:**
```
src/FuelTrack.Domain/Models/FuelTransaction.cs (1-45)
src/FuelTrack.Application/DTOs/FuelMovements/FuelTransactionDto.cs (1-30)
src/FuelTrack.Infrastructure/Services/FuelMovementService.cs (1-120)
frontend/src/pages/FuelMovements.js (1-250)
tests/FuelTrack.Tests/Services/FuelMovementServiceTests.cs (1-80)
```

---

### 2. `cavecrew-builder`

**File:** `.ai/agents/cavecrew-builder.md`
**Purpose:** Surgical 1-2 file edits — precise, minimal changes

**Capabilities:**
- Edit existing files (max 2 per dispatch)
- Add/remove properties, methods, imports
- Fix compilation errors
- Update constructor parameters

**Constraints:**
- Max 2 files per dispatch
- No architectural changes
- Cannot add new dependencies
- Must match existing code style
- Cannot create new files

**Example dispatch:**
```
@cavecrew-builder Add CapacityKW property to SubStationListDto.cs and update SubStationService.cs mapping
```

---

### 3. `cavecrew-reviewer`

**File:** `.ai/agents/cavecrew-reviewer.md`
**Purpose:** Code review with severity classification

**Capabilities:**
- Review diffs for security issues
- Check type correctness
- Verify null safety
- Classify issues by severity (CRITICAL/REQUIRED/WARNING/INFO)

**Constraints:**
- Read-only — no edits
- Returns structured review output

**Example dispatch:**
```
@cavecrew-reviewer Review changes in src/FuelTrack.Infrastructure/Services/UserService.cs
```

**Returns:**
```
CRITICAL: Line 45 — SQL injection risk in query string
REQUIRED: Line 78 — Missing null check on User.SubStations
WARNING: Line 102 — Inconsistent naming (subStationId vs SubStationId)
INFO: Line 120 — Could use AsNoTracking() for read query
```

---

### 4. `explore`

**File:** `.ai/agents/explore.md`
**Purpose:** Fast codebase exploration — quick answers about structure

**Capabilities:**
- Answer "where is X defined?"
- Explain "how does Y work?"
- List files in a directory
- Show project structure

**Constraints:**
- Read-only
- Fast response (no deep analysis)

**Example dispatch:**
```
@explore Where is the PermissionMapper defined and what does it do?
```

---

## Dispatching Agents

### In OpenCode

Use the `@` mention syntax:

```
@cavecrew-investigator [task description]
@cavecrew-builder [precise instruction]
@cavecrew-reviewer [review scope]
@explore [question]
```

### Parallel Dispatch

For independent tasks, dispatch multiple agents simultaneously:

```
@cavecrew-investigator Find all DTO files for SubStation entity
@cavecrew-investigator Find all test files referencing SubStation
```

### Sequential Dispatch

For dependent tasks, wait for output before next dispatch:

```
1. @cavecrew-investigator Locate SubStation-related files
2. [Read output]
3. @cavecrew-builder Update SubStationListDto.cs with CapacityKW property
4. [Verify build passes]
```

---

## Agent Output Format

All cavecrew agents return compressed output:

```
[FILE_PATH] (START_LINE-END_LINE)
[brief description of relevant content]

[FILE_PATH] (START_LINE-END_LINE)
[brief description of relevant content]
```

**Key properties:**
- Absolute paths (not relative)
- Line ranges for context
- ~60% smaller than raw file reads
- No architecture opinions (investigator/builder)
- Structured severity (reviewer)

---

## When to Use Each Agent

| Situation | Agent |
|-----------|-------|
| "Where is X defined?" | `explore` |
| "Find all files related to X" | `cavecrew-investigator` |
| "Edit this file to add Y" | `cavecrew-builder` |
| "Review my changes" | `cavecrew-reviewer` |
| "How does the permission system work?" | `explore` |
| "Fix the compilation error in X" | `cavecrew-builder` |
| "Find all references to X" | `cavecrew-investigator` |
| "Check this PR for security issues" | `cavecrew-reviewer` |

---

## Quick Reference

| Agent | Read | Edit | Scope | Output |
|-------|------|------|-------|--------|
| `cavecrew-investigator` | Yes | No | File/pattern location | Paths + line ranges |
| `cavecrew-builder` | Yes | Yes (max 2 files) | Surgical edits | Edit confirmation |
| `cavecrew-reviewer` | Yes | No | Code review | Severity-classified issues |
| `explore` | Yes | No | Quick answers | Text explanation |

## Related Files

- `.ai/agents/cavecrew-investigator.md` — Investigator agent definition
- `.ai/agents/cavecrew-builder.md` — Builder agent definition
- `.ai/agents/cavecrew-reviewer.md` — Reviewer agent definition
- `.ai/agents/explore.md` — Explore agent definition
- `AI_WORKFLOW.md` — When to dispatch each agent
