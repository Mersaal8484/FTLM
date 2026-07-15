# fueltrack-code-review

## Metadata

- **Name**: fueltrack-code-review
- **Purpose**: Code quality, architecture compliance, and security review for FuelTrack
- **Priority**: Required
- **Blocking Level**: Required
- **Version**: 1.0.0
- **Last Updated**: 2026-07-10

## Description

Reviews code changes against FuelTrack architecture rules, Clean Architecture boundaries, security requirements, and performance standards. Prevents regressions and ensures consistency across the codebase.

## Trigger Conditions

- Any code change in `src/` or `frontend/src/`
- Pull request review
- New feature implementation
- Bug fix
- Refactoring
- Migration or schema change

## Automatic Invocation Rules

- If `git diff` shows changes in `src/` → invoke
- If `git diff` shows changes in `frontend/src/` → invoke
- If new files added → invoke
- If files deleted → invoke

## Manual Invocation Rules

- User says "review code", "check my changes", "code review"
- User says "is this correct", "does this follow patterns"
- User says "security check", "architecture review"

## Input Requirements

| Input | Required | Description |
|-------|----------|-------------|
| Changed files | Yes | List of modified files |
| Change type | Yes | Feature / Bug fix / Refactor / Migration |
| Review scope | Yes | Full review / Focused review |

## Output Requirements

| Output | Description |
|--------|-------------|
| Issue list | Categorized by severity (CRITICAL, REQUIRED, WARNING, INFO) |
| File references | File:line format for each issue |
| Fix suggestions | Specific code changes to resolve each issue |
| Summary | Overall assessment with pass/fail verdict |

## Sequential Workflow

### Phase 1: Architecture Compliance

1. Verify Clean Architecture layer dependencies:
   - Domain → no dependencies
   - Application → depends on Domain only
   - Infrastructure → depends on Domain + Application
   - API → depends on all layers
2. Check for reverse dependencies (e.g., Domain referencing Infrastructure)
3. Verify DTOs are in Application layer (not Domain or Infrastructure)
4. Verify services are in Infrastructure layer (not API)
5. Verify controllers don't contain business logic
6. Check for proper dependency injection registration

### Phase 2: Domain Model Compliance

1. Verify all entities inherit `BaseEntity`
2. Verify PKs are `int` (not `Guid`)
3. Verify FKs are `int` (not `Guid`)
4. Verify no `Guid` remnants in entity properties
5. Verify nullable annotations match database intent
6. Verify naming conventions (PascalCase, no abbreviations)
7. Verify no business logic in entities (keep entities pure)

### Phase 3: Service Layer Review

1. Verify `AsNoTracking()` on read queries
2. Verify `.Include()` for navigation properties
3. Verify `CancellationToken` in all async methods
4. Verify `ILogger<T>` injection and structured logging
5. Verify error handling with specific exception types
6. Verify no N+1 queries
7. Verify pagination on list methods
8. Verify manual DTO mapping (no AutoMapper)
9. Verify `IHttpContextAccessor` for user identification
10. Verify no direct `SaveChangesAsync` without business validation

### Phase 4: Controller Layer Review

1. Verify `[Authorize]` at class level
2. Verify `[Authorize(Roles = "...")]` on write endpoints
3. Verify `[ProducesResponseType]` for Swagger
4. Verify `[ValidateAntiForgeryToken]` on POST/PUT/DELETE
5. Verify `Ok(result)` pattern (not `CreatedAtAction`)
6. Verify error responses (NotFound, BadRequest, Conflict)
7. Verify no business logic in controllers
8. Verify proper HTTP method mapping
9. Verify route naming conventions

### Phase 5: Frontend Review

1. Verify `client` import from `../api/client` (not raw `axios`)
2. Verify `useTranslation()` hook usage (not direct `i18n` import)
3. Verify React Query hooks for data fetching
4. Verify loading/error/empty states
5. Verify form validation (client-side)
6. Verify responsive design
7. Verify accessibility (aria labels, keyboard navigation)
8. Verify i18n translations for all user-facing strings
9. Verify no hardcoded URLs or values
10. Verify proper cleanup in useEffect

### Phase 6: Security Review

1. Verify `[Authorize]` on all protected endpoints
2. Verify no hardcoded secrets or connection strings
3. Verify input validation (server-side)
4. Verify SQL injection prevention (EF Core parameterized queries)
5. Verify XSS prevention (output encoding)
6. Verify CSRF protection (antiforgery tokens)
7. Verify sensitive data not logged
8. Verify HTTPS enforcement
9. Verify HSTS configuration
10. Verify CORS configuration

### Phase 7: Performance Review

1. Check for N+1 query patterns
2. Check for missing indexes on frequently queried columns
3. Check for unnecessary `.Include()` calls
4. Check for missing `AsNoTracking()` on read queries
5. Check for unbounded list queries (missing pagination)
6. Check for synchronous blocking on async methods
7. Check for large object allocations
8. Check for missing caching opportunities
9. Check for inefficient string operations
10. Check for memory leaks in frontend

### Phase 8: Testing Review

1. Verify unit tests exist for changed code
2. Verify tests cover happy path and error cases
3. Verify tests use correct domain models (int PKs, current fields)
4. Verify mock setup is correct
5. Verify assertions are specific
6. Verify no test interdependencies
7. Verify integration tests cover critical paths
8. Verify all tests pass

### Phase 9: Documentation Review

1. Verify XML comments on public methods
2. Verify inline comments for complex logic
3. Verify AGENTS.md updated with session summary
4. Verify Swagger documentation is accurate
5. Verify no outdated comments

## Severity Levels

| Level | Description | Action Required |
|-------|-------------|-----------------|
| CRITICAL | Security vulnerability, data corruption risk, architecture violation | Must fix before merge |
| REQUIRED | Missing best practice, potential bug, performance issue | Must fix before merge |
| WARNING | Code smell, minor inconsistency, improvement opportunity | Should fix, document if skipping |
| INFO | Style preference, minor optimization | Optional, note for future |

## Validation Rules

| Rule | Severity | Description |
|------|----------|-------------|
| No raw axios | CRITICAL | Must use `client` from `../api/client` |
| No Guid IDs | CRITICAL | All IDs must be `int` |
| Authorize required | CRITICAL | All write endpoints must have `[Authorize]` |
| No business logic in controller | REQUIRED | Controllers delegate to services |
| AsNoTracking | REQUIRED | Read queries use `AsNoTracking()` |
| CancellationToken | REQUIRED | All async methods accept CancellationToken |
| Error handling | REQUIRED | All endpoints handle errors gracefully |
| i18n | REQUIRED | All user-facing strings use translation |
| Tests | REQUIRED | Changed code has corresponding tests |
| No N+1 | REQUIRED | No N+1 query patterns |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Raw axios import | Replace with `import client from '../api/client'` |
| Missing `[Authorize]` | Add at class or method level |
| N+1 query | Add `.Include()` or `.Select()` projection |
| Missing error handling | Add try-catch with structured logging |
| Missing i18n | Wrap strings in `t('key')` |
| Missing tests | Add unit and integration tests |
| Business logic in controller | Move to service layer |
| Missing AsNoTracking | Add to read-only queries |

## Performance Considerations

- Review all new queries for N+1 patterns
- Verify pagination on all list endpoints
- Check for missing indexes on new FK columns
- Verify `AsNoTracking()` on read queries
- Check for unnecessary `.Include()` calls
- Review frontend for unnecessary re-renders
- Check for missing React Query cache configuration

## Security Considerations

- Verify `[Authorize]` on every protected endpoint
- Check for hardcoded secrets or connection strings
- Verify input validation on all user inputs
- Check for SQL injection (EF Core should prevent this)
- Verify XSS prevention (output encoding)
- Check for CSRF protection
- Verify sensitive data not logged
- Check for CORS misconfiguration
- Verify HTTPS enforcement

## Dependencies

- **Domain**: `src/FuelTrack.Domain/Models/`
- **Application**: `src/FuelTrack.Application/`
- **Infrastructure**: `src/FuelTrack.Infrastructure/`
- **API**: `src/FuelTrack.API/`
- **Frontend**: `frontend/src/`
- **Tests**: `tests/FuelTrack.Tests/`

## Related Skills

- `fueltrack-migration` (for migration review)
- `fueltrack-crud-checklist` (for CRUD compliance)
- `fueltrack-permission-gate` (for permission review)
- `fueltrack-business-rules` (for business logic review)
- `fueltrack-test-fix` (for test review)

## Examples

### Example: Review Service Change

**Input**: "Review FuelTankService.cs changes"

**Review Output**:
```
CRITICAL: src/FuelTrack.Infrastructure/Services/FuelTankService.cs:45
  Missing AsNoTracking() on read query
  Fix: Add .AsNoTracking() before .FirstOrDefaultAsync()

REQUIRED: src/FuelTrack.Infrastructure/Services/FuelTankService.cs:67
  Missing CancellationToken parameter
  Fix: Add CancellationToken cancellationToken = default parameter

WARNING: src/FuelTrack.Infrastructure/Services/FuelTankService.cs:89
  No structured logging for create operation
  Fix: Add _logger.LogInformation("Creating fuel tank {Code}", dto.Code)

INFO: src/FuelTrack.Infrastructure/Services/FuelTankService.cs:23
  Consider using .Select() projection instead of loading full entity
  Fix: Project only needed properties with .Select()
```

## Completion Checklist

- [ ] Architecture layer dependencies respected
- [ ] Domain model follows conventions (int PKs, BaseEntity, etc.)
- [ ] Service uses AsNoTracking, CancellationToken, structured logging
- [ ] Controller has Authorize, Swagger, error handling
- [ ] Frontend uses client import, i18n, React Query
- [ ] Security: no hardcoded secrets, input validation, CSRF protection
- [ ] Performance: no N+1 queries, pagination, proper indexes
- [ ] Tests exist and pass for changed code
- [ ] Documentation updated
- [ ] All CRITICAL and REQUIRED issues resolved
