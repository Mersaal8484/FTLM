# Refactoring Workflow

## Overview
Workflow for improving code structure without changing behavior. Emphasizes incremental changes with test safety nets.

## Prerequisites
- Code smell or improvement target identified
- Existing test coverage available (or write tests first)
- No pending feature work on affected code

## Flow

```
[Analyze] → [Plan] → [Execute] → [Verify] → [Review] → [Quality Gates]
```

## Steps

### Step 1: Analysis
- **Invoke:** explore agent
- **Action:** Identify code smells in the target area. Common smells in FuelTrack:
  - Duplicate logic across services
  - God classes or methods
  - Dead code
  - Tight coupling between layers
  - Missing abstractions
- **Check:** Code smells documented? Impact scope defined? Risk level assessed?
- **Output:** Analysis report listing specific code smells and affected files.
- **On Failure:** If target area unclear, use explore to map the full dependency graph.

### Step 2: Plan
- **Invoke:** cavecrew-reviewer
- **Action:** Review current code state and plan refactoring steps. Ensure refactoring:
  - Does not change external behavior
  - Can be done incrementally (small steps)
  - Has test coverage at each step
  - Follows existing Clean Architecture patterns
- **Check:** Plan is incremental? Tests exist? Behavior preserved? No scope creep?
- **Output:** Step-by-step refactoring plan with test checkpoints.
- **On Failure:** If plan too large, break into smaller refactorings.

### Step 3: Execute
- **Invoke:** cavecrew-builder
- **Action:** Execute refactoring in small, incremental steps:
  1. Extract method/class
  2. Rename for clarity
  3. Remove dead code
  4. Simplify conditionals
  5. Apply SOLID principles
  Each step must compile and pass tests before proceeding.
- **Check:** Each step compiles? Tests pass after each step? Behavior unchanged?
- **Output:** Refactored code with all tests passing.
- **On Failure:** If any step breaks tests, revert that step and try alternative approach.

### Step 4: Verify
- **Invoke:** fueltrack-test-fix
- **Action:** Run full test suite. Verify no behavior changes. Add tests for any untested paths exposed by refactoring.
- **Check:** All tests pass? No behavior changes? New paths tested?
- **Output:** Full test suite green, new tests added if needed.
- **On Failure:** Identify which test failed and determine if it's a behavior change (bad) or test needs updating (acceptable).

### Step 5: Review
- **Invoke:** fueltrack-code-review + cavecrew-reviewer
- **Action:** Review refactoring for:
  - Behavior preservation (no functional changes)
  - Code improvement (readability, maintainability)
  - Architecture compliance (Clean Architecture boundaries)
  - Test adequacy (coverage maintained or improved)
- **Check:** Behavior preserved? Code improved? Architecture clean? Tests adequate?
- **Output:** Code review report confirming refactoring is safe and beneficial.
- **On Failure:** Address review comments, potentially revert problematic changes.

### Step 6: Quality Gates
- **Invoke:** (Build commands + test runner)
- **Action:**
  1. `dotnet build FuelTrack.sln` — zero errors, fewer warnings
  2. `dotnet test FuelTrack.Tests` — all tests pass
  3. Verify no performance regressions
  4. Check that public API surface unchanged
- **Check:** Build clean? Tests pass? No perf regressions? API unchanged?
- **Output:** All gates green, refactoring ready for PR.
- **On Failure:** Fix build errors or investigate test/performance failures.

## Quality Gates
- [ ] `dotnet build FuelTrack.sln` — 0 errors
- [ ] `dotnet test FuelTrack.Tests` — all pass
- [ ] No functional behavior changes
- [ ] Public API surface unchanged
- [ ] Code review approved
- [ ] No performance regressions

## Common Issues
| Issue | Solution |
|-------|----------|
| Tests fail after refactor | Revert last step, find safer approach |
| Behavior accidentally changed | Revert, verify test coverage, re-plan |
| Refactoring too large | Break into smaller, independently verifiable steps |
| Performance regression | Profile before/after, identify hot path |
| Merge conflicts | Rebase frequently, coordinate with team |
