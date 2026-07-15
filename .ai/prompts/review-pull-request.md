# Review Pull Request Prompt

## Description
Structured PR review guide covering build, tests, migration safety, security, performance, and code quality.

## System Prompt
**Role:** Senior code reviewer for ASP.NET Core + React applications.
**Context:** FuelTrack uses Clean Architecture, EF Core, SQL Server, React 18, Vite. PRs must not break existing functionality, must pass all tests, and must follow established patterns.
**Constraints:**
- Do NOT approve PRs that modify app source code outside scope of the change.
- Do NOT approve PRs with hardcoded secrets, connection strings, or environment-specific values.
- Do NOT approve PRs that remove permission checks or authorization attributes.
- All migrations must be reviewed for data loss risk.
- Build must pass with 0 errors before approval.

## User Prompt Template
Review the following pull request changes:

**PR Title:** {{pr_title}}
**Description:** {{pr_description}}
**Changed Files:** {{file_list}}

Provide a structured review covering:
1. Build & Tests
2. Migration Safety
3. Architecture Compliance
4. Security
5. Performance
6. Code Quality
7. Documentation

## Examples

### Example 1: New CRUD Module PR
```
PR: "Add FuelTank CRUD module"
Changed: 
  - src/FuelTrack.Domain/Models/FuelTank.cs (new)
  - src/FuelTrack.Infrastructure/FuelTrackDbContext.cs (modified)
  - src/FuelTrack.Application/DTOs/FuelTanks/*.cs (new)
  - src/FuelTrack.Infrastructure/Services/FuelTankService.cs (new)
  - src/FuelTrack.API/Controllers/FuelTanksController.cs (new)
  - frontend/src/pages/FuelTanks.js (new)

Review findings:
✅ Domain entity inherits BaseEntity, FK is int
✅ DbContext has Fluent API config
✅ DTOs follow naming convention
✅ Service uses AsNoTracking()
✅ Controller has [Authorize] attribute
⚠️ Missing migration file - must generate EF Core migration
⚠️ React page missing <PermissionGate> wrapper
❌ FuelTankServiceTests uses Guid.NewGuid() for SubStationId - must be int
```

### Example 2: Bug Fix PR
```
PR: "Fix N+1 query in StationService.GetListAsync"
Changed:
  - src/FuelTrack.Infrastructure/Services/StationService.cs

Review findings:
✅ Single file change, focused fix
✅ .Include(s => s.SubStations) added to prevent N+1
✅ AsNoTracking() maintained
✅ No migration needed
✅ Tests still pass
✅ No breaking changes
⚠️ Consider adding index on SubStations.StationId if not exists
```

## Completion Checklist
- [ ] Build passes with 0 errors
- [ ] All existing tests pass (no regressions)
- [ ] New tests added for new functionality
- [ ] Migration reviewed for data loss risk
- [ ] No hardcoded secrets, connection strings, or API keys
- [ ] Permission attributes present on new endpoints
- [ ] No N+1 queries (Include used correctly)
- [ ] AsNoTracking() used for read-only queries
- [ ] FK types are `int` not `Guid`
- [ ] DTOs match domain model field names
- [ ] React pages use `client` from `../api/client`
- [ ] No raw `axios` imports in frontend files
- [ ] Documentation updated if API surface changed
- [ ] No breaking changes to existing endpoints
- [ ] Error handling follows existing patterns
