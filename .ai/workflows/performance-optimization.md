# Performance Optimization Workflow

## Overview
Workflow for identifying and fixing performance bottlenecks in FuelTrack. Focuses on data-driven optimization with measurable results.

## Prerequisites
- Performance issue reported or suspected
- Baseline metrics available (or establish first)
- Access to profiling tools

## Flow

```
[Profile] → [Analyze] → [Optimize] → [Verify] → [Review] → [Quality Gates]
```

## Steps

### Step 1: Profile
- **Invoke:** explore agent
- **Action:** Identify performance bottlenecks. Common areas in FuelTrack:
  - Slow database queries (N+1, missing indexes)
  - Unbounded data loading
  - Unnecessary EF Core includes
  - Missing AsNoTracking for read-only queries
  - Large payload serialization
  - SignalR message storms
- **Check:** Bottleneck identified? Metrics collected? Baseline established?
- **Output:** Profiling report with specific bottleneck locations and metrics.
- **On Failure:** If bottleneck unclear, add logging/timing to narrow down.

### Step 2: Analyze
- **Invoke:** cavecrew-investigator
- **Action:** Deep-dive into the bottleneck:
  - SQL query analysis (check generated queries from EF Core)
  - Execution plan review
  - Memory allocation patterns
  - Hot path identification
- **Check:** Root cause understood? Query plan analyzed? Fix strategy defined?
- **Output:** Analysis report with specific optimization recommendations.
- **On Failure:** If analysis unclear, use SQL Profiler or EF Core logging to capture actual queries.

### Step 3: Optimize
- **Invoke:** cavecrew-builder
- **Action:** Apply targeted optimizations:
  - Add missing indexes (composite for common query patterns)
  - Apply AsNoTracking() for read-only queries
  - Optimize Include() chains
  - Add pagination for large result sets
  - Cache frequent read-only data
  - Optimize serialization settings
- **Check:** Optimization applied? Minimal code change? No behavior change?
- **Output:** Optimized code with specific performance improvements.
- **On Failure:** If optimization introduces bugs, revert and try alternative approach.

### Step 4: Verify
- **Invoke:** fueltrack-test-fix
- **Action:** Run full test suite to ensure no regressions. Measure performance improvement.
- **Check:** All tests pass? Performance improved? No behavior changes?
- **Output:** Test results confirming no regressions, performance metrics showing improvement.
- **On Failure:** If tests fail, revert optimization and try safer approach.

### Step 5: Review
- **Invoke:** fueltrack-code-review + cavecrew-reviewer
- **Action:** Review optimization for:
  - Correctness (no behavior changes)
  - Effectiveness (measurable improvement)
  - Safety (no new bugs)
  - Maintainability (code still readable)
- **Check:** Optimization correct? Improvement measurable? Code maintainable?
- **Output:** Code review report with performance metrics.
- **On Failure:** If optimization not effective, revert and try different approach.

### Step 6: Quality Gates
- **Invoke:** (Build commands + test runner)
- **Action:**
  1. `dotnet build FuelTrack.sln` — zero errors
  2. `dotnet test FuelTrack.Tests` — all tests pass
  3. Verify performance metrics improved
  4. Check no memory leaks introduced
- **Check:** Build clean? Tests pass? Performance improved? No memory issues?
- **Output:** All gates green, optimization ready for PR.
- **On Failure:** Fix build errors or revert optimization if metrics worse.

## Quality Gates
- [ ] `dotnet build FuelTrack.sln` — 0 errors
- [ ] `dotnet test FuelTrack.Tests` — all pass
- [ ] Performance metrics improved (before/after comparison)
- [ ] No behavior changes
- [ ] No memory regressions
- [ ] Code review approved

## Common Issues
| Issue | Solution |
|-------|----------|
| Optimization makes code less readable | Add comments explaining why, keep changes minimal |
| Performance improvement not measurable | Use proper profiling, establish clear baseline |
| Optimization breaks tests | Revert, verify test coverage, try safer approach |
| Index slows down writes | Analyze read/write ratio, use covering indexes carefully |
| Caching introduces stale data | Use appropriate cache invalidation strategy |
