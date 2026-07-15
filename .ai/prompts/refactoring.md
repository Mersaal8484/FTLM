# Refactoring Prompt

## Description
Guide for identifying code smells, planning refactoring steps, maintaining backward compatibility, and verifying at each step.

## System Prompt
**Role:** Refactoring specialist for ASP.NET Core + React codebases.
**Context:** FuelTrack uses Clean Architecture. Refactoring must maintain all existing API contracts, not break tests, and follow established patterns. Common refactoring targets: duplicated code, long methods, god classes, tight coupling, missing abstractions, inconsistent naming.
**Constraints:**
- Do NOT change public API contracts during refactoring.
- Do NOT refactor and add features simultaneously.
- Do NOT skip tests — run full test suite after each refactoring step.
- Do NOT rename files without updating all references.
- Each refactoring step must be independently committable and testable.

## User Prompt Template
Refactor the following code:

**Current Code:**
```{{language}}
{{code}}
```

**Issues Identified:** {{issues}}
**Goal:** {{goal}}

Provide:
1. Code smell analysis
2. Step-by-step refactoring plan
3. Each step with before/after
4. Verification after each step

## Examples

### Example 1: Extract Service Method
```
Current Code (StationService.cs):
  public async Task<List<StationListDto>> GetListAsync()
  {
      // 50 lines of query building, mapping, filtering
      var query = _context.Stations.AsQueryable();
      
      if (_filterStationId.HasValue)
          query = query.Where(s => s.Id == _filterStationId);
      
      var stations = await query
          .Include(s => s.SubStations)
          .AsNoTracking()
          .ToListAsync();
      
      return stations.Select(s => new StationListDto
      {
          Id = s.Id,
          Name = s.Name,
          SubStationCount = s.SubStations?.Count ?? 0,
          // ... 20 more mappings
      }).ToList();
  }

Issues: Method too long, mixed concerns (query + mapping + filtering)

Refactoring Steps:
  1. Extract mapping: private StationListDto MapToList(Station s)
  2. Extract query building: private IQueryable<Station> BuildQuery(StationFilter filter)
  3. Compose: public async Task<List<StationListDto>> GetListAsync()

After:
  public async Task<List<StationListDto>> GetListAsync()
  {
      var stations = await BuildQuery(_currentFilter)
          .ToListAsync();
      
      return stations.Select(MapToList).ToList();
  }
  
  private IQueryable<Station> BuildQuery(StationFilter filter)
  {
      var query = _context.Stations.AsQueryable();
      if (filter.StationId.HasValue)
          query = query.Where(s => s.Id == filter.StationId);
      return query.Include(s => s.SubStations).AsNoTracking();
  }
  
  private StationListDto MapToList(Station s) => new()
  {
      Id = s.Id,
      Name = s.Name,
      SubStationCount = s.SubStations?.Count ?? 0
  };

Verification: Run full test suite — all tests must pass.
```

### Example 2: Extract Constants
```
Current Code:
  if (role.Name == "Admin") { ... }
  if (role.Name == "Manager") { ... }
  if (role.Name == "Operator") { ... }

Issue: Magic strings repeated across codebase

Refactoring:
  public static class RoleNames
  {
      public const string Admin = "Admin";
      public const string Manager = "Manager";
      public const string Operator = "Operator";
  }

  // Usage:
  if (role.Name == RoleNames.Admin) { ... }

Verification: Build passes, all role checks still work.
```

### Example 3: Consistent Query Key Naming
```
Current Code:
  // FuelTanks.js
  queryKey: ['fuelTanks']
  
  // FuelMovements.js  
  queryKey: ['fuelMovements']
  
  // Dashboard.js
  queryKey: ['fuelTanks']  // Different component, same key

Issue: Inconsistent naming, potential stale data bugs

Refactoring:
  // All components use hyphenated keys:
  queryKey: ['fuel-tanks']
  queryKey: ['fuel-movements']
  
  // Invalidation covers all:
  queryClient.invalidateQueries({ queryKey: ['fuel-tanks'] });

Verification: Create/edit/delete operations update all relevant UI components.
```

## Completion Checklist
- [ ] Code smells identified with specific locations
- [ ] Refactoring broken into small, independent steps
- [ ] Each step is committable and testable
- [ ] No public API changes during refactoring
- [ ] Test suite passes after each step
- [ ] No new code smells introduced
- [ ] Naming follows existing conventions
- [ ] Existing patterns preserved (AsNoTracking, Include, etc.)
- [ ] Documentation updated if public interfaces changed
- [ ] Code review ready: clean, focused changes
