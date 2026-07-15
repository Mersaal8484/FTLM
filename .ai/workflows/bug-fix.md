# Bug Fix Workflow

## Overview
Systematic workflow for reproducing, investigating, fixing, and verifying bugs in FuelTrack. Emphasizes minimal, surgical changes.

## Prerequisites
- Bug description or error message
- Steps to reproduce (if known)
- Affected module identified

## Flow

```
[Reproduce] → [Investigate] → [Fix] → [Test] → [Review] → [Quality Gates]
```

## Steps

### Step 1: Reproduce
- **Invoke:** explore agent
- **Action:** Locate the error in the codebase. Find the exact file, line number, and method where the bug occurs. Verify you can reproduce the issue.
- **Check:** Can the bug be reproduced? Is the error message understood? Does it happen consistently?
- **Output:** Exact location of bug (file:line), reproduction steps, error details.
- **On Failure:** If bug cannot be reproduced, check environment config, database state, or timing issues.

### Step 2: Investigate
- **Invoke:** cavecrew-investigator
- **Action:** Trace the full code path from entry point to failure. Identify root cause, not just symptoms. Check:
  - Data flow (what data enters, what data leaves)
  - State changes (what modifies the data between input and output)
  - External dependencies (database, API calls, configuration)
- **Check:** Root cause identified? Data flow understood? Dependencies checked?
- **Output:** Root cause analysis with exact location and explanation.
- **On Failure:** If root cause unclear, add logging or use investigation techniques to narrow down.

### Step 3: Fix
- **Invoke:** cavecrew-builder
- **Action:** Apply minimal, surgical fix. Do not refactor unrelated code. Follow existing patterns in the codebase.
- **Check:** Fix addresses root cause? Fix is minimal? No unrelated changes? Follows code conventions?
- **Output:** Single focused commit with bug fix.
- **On Failure:** If fix introduces new issues, revert and try alternative approach.

### Step 4: Test
- **Invoke:** fueltrack-test-fix
- **Action:** Write regression test that would have caught this bug. Verify existing tests still pass.
- **Check:** Regression test written? Existing tests pass? Regression test fails without fix, passes with fix?
- **Output:** Regression test added to test suite.
- **On Failure:** Adjust test to properly exercise the bug scenario.

### Step 5: Review
- **Invoke:** fueltrack-code-review + cavecrew-reviewer
- **Action:** Review the fix for:
  - Correctness (does it actually fix the bug?)
  - Safety (does it break anything else?)
  - Completeness (are there related bugs?)
- **Check:** Fix is correct? No regressions? Related areas checked?
- **Output:** Code review report confirming fix is safe and complete.
- **On Failure:** Address review comments before quality gates.

### Step 6: Quality Gates
- **Invoke:** (Build commands + test runner)
- **Action:**
  1. `dotnet build FuelTrack.sln` — zero errors
  2. `dotnet test FuelTrack.Tests` — all tests pass
  3. Verify no new warnings introduced
- **Check:** Build clean? Tests pass? No new warnings?
- **Output:** All gates green, bug fix ready for PR.
- **On Failure:** Fix build errors or test failures before proceeding.

## Quality Gates
- [ ] `dotnet build FuelTrack.sln` — 0 errors
- [ ] `dotnet test FuelTrack.Tests` — all pass
- [ ] Regression test added
- [ ] No unrelated code changes
- [ ] Code review approved
- [ ] No new warnings

## Common Issues
| Issue | Solution |
|-------|----------|
| Fix breaks other tests | Revert, find more targeted fix |
| Cannot reproduce bug | Check environment, database state, configuration |
| Bug is in third-party code | Find workaround or update dependency |
| Multiple root causes | Fix each independently, verify each separately |
