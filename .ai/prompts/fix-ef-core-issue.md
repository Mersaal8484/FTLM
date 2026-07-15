# Fix EF Core Issue Prompt

## Description
Guide for diagnosing and fixing EF Core issues including N+1 queries, tracking conflicts, migration failures, and FK violations.

## System Prompt
**Role:** EF Core specialist for ASP.NET Core applications.
**Context:** FuelTrack uses EF Core with SQL Server. Context is `FuelTrackDbContext`. Entities inherit `BaseEntity`. `AuditSaveChangesInterceptor` auto-populates audit fields. Common issues: N+1 queries, tracking conflicts after SaveChanges, migration generation failures, FK constraint violations.
**Constraints:**
- Do NOT modify domain entity files unless fixing a structural issue.
- Do NOT remove AsNoTracking() from read queries.
- Do NOT change FK types from int to Guid.
- Always generate migrations via `dotnet ef migrations add` — never hand-edit migration files.
- Use Fluent API configuration, not data annotations.

## User Prompt Template
Diagnose and fix the following EF Core issue:

**Error Message:**
```
{{error_message}}
```

**Relevant Code:**
```csharp
{{code_snippet}}
```

**Expected Behavior:** {{expected}}
**Actual Behavior:** {{actual}}

Provide:
1. Root cause analysis
2. Fix with explanation
3. Verification steps

## Examples

### Example 1: N+1 Query
```
Error: Slow query, 100+ SQL statements for a list of 50 items
Code:
  var stations = await _context.Stations.ToListAsync();
  foreach (var s in stations)
  {
      var subStations = s.SubStations; // Lazy loading triggers N+1
  }

Root Cause: Navigation properties not eagerly loaded. EF Core generates separate query for each Station's SubStations.

Fix:
  var stations = await _context.Stations
      .Include(s => s.SubStations)
      .AsNoTracking()
      .ToListAsync();

Verification: Check SQL log — should show single JOIN query, not 51 separate queries.
```

### Example 2: Tracking Conflict
```
Error: "The instance of entity 'Station' cannot be tracked because another instance with the same key value for {'Id'} is already being tracked."
Code:
  var station = await _context.Stations.FindAsync(id);
  var dto = mapToDto(station);
  var updated = mapToEntity(dto);
  _context.Stations.Update(updated); // Conflict: station already tracked

Root Cause: Entity loaded by FindAsync is tracked, then Update() tries to track another instance with same key.

Fix: Use AsNoTracking() on the initial query, or detach before updating:
  var station = await _context.Stations
      .AsNoTracking()
      .FirstOrDefaultAsync(s => s.Id == id);
  
  // Or detach:
  _context.Entry(station).State = EntityState.Detached;
  _context.Stations.Update(updated);
```

### Example 3: Migration FK Violation
```
Error: "The INSERT statement conflicted with the FOREIGN KEY constraint \"FK_Station_SubStation_StationId\""
Code:
  migration adds SubStationId column with NOT NULL constraint
  existing data has null SubStationId values

Root Cause: Migration adds non-nullable FK column without default value or data backfill.

Fix:
  1. Make FK nullable: entity.SubStationId int? (not int)
  2. Or add default value in migration: .HasDefaultValue(1)
  3. Or backfill data before adding NOT NULL constraint
```

## Completion Checklist
- [ ] Root cause identified with evidence from error/logs
- [ ] Fix uses existing patterns (AsNoTracking, Include, etc.)
- [ ] No domain entity structure changes unless necessary
- [ ] FK types remain `int` (not changed to Guid)
- [ ] Migration generated via `dotnet ef migrations add` if schema changed
- [ ] Fix verified against existing tests
- [ ] No new N+1 queries introduced
- [ ] SaveChanges behavior verified (audit fields still populated)
- [ ] Build passes with 0 errors
