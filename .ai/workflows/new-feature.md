# New Feature Workflow

## Overview
End-to-end workflow for building a new feature in FuelTrack. Covers entity design through CRUD to frontend and testing.

## Prerequisites
- Feature requirements documented
- Database schema impact understood
- Affected modules identified

## Flow

```
[Investigate] → [Design Entity] → [Create Migration] → [Implement CRUD] → [Build Frontend] → [Write Tests] → [Code Review] → [Quality Gates]
```

## Steps

### Step 1: Investigation
- **Invoke:** explore agent
- **Action:** Analyze existing patterns in similar modules (e.g., Station, SubStation, Generator). Identify domain models, services, DTOs, controllers, and frontend pages that follow the same pattern.
- **Check:** Domain model exists? DTOs exist? Service interface exists? Controller exists? Frontend page exists?
- **Output:** Investigation report listing all existing patterns and files to follow as templates.
- **On Failure:** If module is entirely new, note that and proceed with generic Clean Architecture template.

### Step 2: Entity Design
- **Invoke:** fueltrack-business-rules
- **Action:** Define domain entity following FuelTrack conventions: BaseEntity inheritance, int PKs, proper navigation properties, FK conventions. Validate against existing domain models.
- **Check:** Entity inherits BaseEntity? All IDs are int? Navigation properties correct? No circular references?
- **Output:** Domain entity file (e.g., `NewEntity.cs`) with proper configuration.
- **On Failure:** Review similar entities in domain layer for correct patterns.

### Step 3: Create Migration
- **Invoke:** fueltrack-migration
- **Action:** Create EF Core migration for the new entity. Add DbSet to FuelTrackDbContext, configure fluent API, generate migration.
- **Check:** Migration compiles? Schema matches domain entity? Indexes added for FKs? Composite indexes for common queries?
- **Output:** Migration file + updated FuelTrackDbContext.
- **On Failure:** If migration fails, verify domain entity configuration and DbContext setup.

### Step 4: CRUD Implementation
- **Invoke:** fueltrack-crud-checklist
- **Action:** Implement full CRUD stack:
  1. DTOs (Create, Update, List, Detail) in Application layer
  2. Service interface + implementation in Infrastructure layer
  3. Controller with proper auth attributes and response types
  4. Validators (FluentValidation) for create/update DTOs
- **Check:** All CRUD operations work? Auth attributes correct? Response types documented? Validators applied?
- **Output:** DTOs, Service, Controller, Validators — all in correct layers.
- **On Failure:** Use fueltrack-code-review to verify layer boundaries.

### Step 5: Frontend
- **Invoke:** (Manual — frontend not covered by fueltrack skills)
- **Action:**
  1. Create React page component following existing patterns
  2. Add query hooks for GET endpoints
  3. Add mutation hooks for POST/PUT/DELETE
  4. Integrate SignalR for real-time updates if applicable
  5. Add route to navigation
- **Check:** Page renders? CRUD operations work? Loading/error states handled? Responsive layout?
- **Output:** React page + query hooks + route configuration.
- **On Failure:** Check browser console, verify API endpoints, check auth token.

### Step 6: Testing
- **Invoke:** fueltrack-test-fix
- **Action:** Write unit tests for service layer and integration tests for API endpoints. Follow existing test patterns in FuelTrack.Tests.
- **Check:** Unit tests pass? Integration tests pass? Coverage reasonable? Edge cases covered?
- **Output:** Test files in FuelTrack.Tests project.
- **On Failure:** Run tests individually, fix compilation errors, adjust test fixtures.

### Step 7: Code Review
- **Invoke:** fueltrack-code-review + cavecrew-reviewer
- **Action:** Review all new code for:
  - Clean Architecture compliance (layer boundaries)
  - Security (auth attributes, input validation)
  - Performance (AsNoTracking, proper indexing)
  - Conventions (naming, structure matching existing code)
- **Check:** No layer violations? Auth required on write endpoints? No N+1 queries? Naming matches conventions?
- **Output:** Code review report with issues found and fixes applied.
- **On Failure:** Fix issues before proceeding to quality gates.

### Step 8: Quality Gates
- **Invoke:** (Build commands + test runner)
- **Action:**
  1. `dotnet build FuelTrack.sln` — zero errors
  2. `dotnet test FuelTrack.Tests` — all tests pass
  3. Verify migration applies cleanly
  4. Check Swagger docs if API changes
  5. Update relevant documentation
- **Check:** Build clean? Tests pass? Migration applies? Swagger correct? Docs updated?
- **Output:** All gates green, feature ready for PR.
- **On Failure:** Fix build errors, fix failing tests, resolve migration issues.

## Quality Gates
- [ ] `dotnet build FuelTrack.sln` — 0 errors
- [ ] `dotnet test FuelTrack.Tests` — all pass
- [ ] Migration applies and reverts cleanly
- [ ] Swagger documentation reflects new endpoints
- [ ] Code review approved
- [ ] No regressions in existing tests

## Common Issues
| Issue | Solution |
|-------|----------|
| Entity not found in DbContext | Add DbSet to FuelTrackDbContext.cs |
| Migration scaffold fails | Verify entity has parameterless constructor or correct configuration |
| Service N+1 queries | Use `.Include()` or `.AsNoTracking()` as appropriate |
| Frontend 401 errors | Check auth token, verify `[Authorize]` attributes match |
| Integration test failures | Verify test database connection string, check test fixtures |
