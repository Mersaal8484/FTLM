# fueltrack-crud-checklist

## Metadata

- **Name**: fueltrack-crud-checklist
- **Purpose**: Standardized CRUD implementation across all FuelTrack entities
- **Priority**: Required
- **Blocking Level**: Required
- **Version**: 1.0.0
- **Last Updated**: 2026-07-10

## Description

Ensures every new CRUD module follows the established FuelTrack patterns across backend (Domain, Application, Infrastructure, API) and frontend (React pages, API client, React Query hooks). Prevents inconsistency across the 25+ controllers and 30+ services.

## Trigger Conditions

- New entity CRUD module created
- New controller added
- New service added
- New frontend page created for entity management
- Existing CRUD module refactored

## Automatic Invocation Rules

- If new file created in `src/FuelTrack.API/Controllers/` → invoke
- If new file created in `src/FuelTrack.Infrastructure/Services/` → invoke
- If new file created in `src/FuelTrack.Application/DTOs/` → invoke
- If new file created in `frontend/src/pages/` → invoke

## Manual Invocation Rules

- User says "create CRUD", "add entity module", "new page for X"
- User says "scaffold controller", "build CRUD for"

## Input Requirements

| Input | Required | Description |
|-------|----------|-------------|
| Entity name | Yes | Name of the domain entity |
| Entity properties | Yes | All properties with types |
| Relationships | Yes | FKs, navigation properties |
| Operations needed | Yes | Create, Read, Update, Delete, List |
| Auth requirements | Yes | Roles allowed for each operation |

## Output Requirements

| Output | Description |
|--------|-------------|
| Domain entity | Updated or new entity in `src/FuelTrack.Domain/Models/` |
| DTOs | Create, Update, List, Detail DTOs |
| Validators | FluentValidation validators for Create/Update |
| Service interface | `I{Name}Service.cs` in Application layer |
| Service implementation | `{Name}Service.cs` in Infrastructure layer |
| Controller | `{Name}Controller.cs` in API layer |
| Frontend page | `{Name}.jsx` in `frontend/src/pages/` |
| Unit tests | Service and controller tests |
| Integration tests | End-to-end API tests |

## Sequential Workflow

### Phase 1: Domain Layer

1. Create/verify entity in `src/FuelTrack.Domain/Models/`
2. Verify inherits `BaseEntity` (int Id, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
3. Verify all PKs and FKs are `int` (not `Guid`)
4. Verify nullable annotations match database intent
5. Add navigation properties with explicit configuration
6. Add enum properties if applicable
7. Verify entity follows naming conventions (PascalCase, no abbreviations)

### Phase 2: Application Layer — DTOs

1. Create `{Entity}Dto.cs` — List response DTO
2. Create `{Entity}DetailDto.cs` — Single item response DTO
3. Create `Create{Entity}Dto.cs` — Create request DTO
4. Create `Update{Entity}Dto.cs` — Update request DTO
5. Verify DTO property types match entity types
6. Verify DTO uses `int` for all IDs
7. Add `[Required]` attributes where appropriate
8. Add `[StringLength]` for string fields
9. Add `[Range]` for numeric fields
10. Verify DTO doesn't expose internal fields

### Phase 3: Application Layer — Validators

1. Create `Create{Entity}Validator.cs` using FluentValidation
2. Create `Update{Entity}Validator.cs` using FluentValidation
3. Verify all required fields have `NotEmpty()` rule
4. Verify string length limits have `MaximumLength()` rule
5. Verify numeric ranges have `InclusiveBetween()` rule
6. Add custom validation rules for business logic
7. Verify validator inherits `AbstractValidator<T>`

### Phase 4: Application Layer — Service Interface

1. Create `I{Name}Service.cs` in `src/FuelTrack.Application/Interfaces/`
2. Define methods: `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`
3. Add `CancellationToken` parameter to all async methods
4. Add pagination parameters to `GetAllAsync`
5. Add filtering parameters where needed
6. Define return types using DTOs

### Phase 5: Infrastructure Layer — Service Implementation

1. Create `{Name}Service.cs` in `src/FuelTrack.Infrastructure/Services/`
2. Inject `FuelTrackDbContext` via constructor
3. Use `IHttpContextAccessor` for user identification (CreatedBy/UpdatedBy)
4. Use `AsNoTracking()` for read operations
5. Use `.Include()` for navigation properties
6. Use `PredicateBuilder` for dynamic filtering
7. Implement pagination with `.Skip().Take()`
8. Map entity to DTO manually (no AutoMapper)
9. Use `SaveChangesAsync` with `CancellationToken`
10. Handle `DbUpdateException` for constraint violations
11. Add structured logging via `ILogger<T>`
12. Verify no N+1 queries

### Phase 6: API Layer — Controller

1. Create `{Name}Controller.cs` in `src/FuelTrack.API/Controllers/`
2. Inherit `BaseApiController`
3. Add `[Authorize]` at class level
4. Add `[Authorize(Roles = "...")]` for write operations
5. Map HTTP methods: GET (list), GET/{id} (detail), POST (create), PUT/{id} (update), DELETE/{id} (delete)
6. Add `[ProducesResponseType]` attributes for Swagger
7. Add `[ValidateAntiForgeryToken]` for POST/PUT/DELETE
8. Return `Ok(result)` for success (not `CreatedAtAction` to avoid NRE)
9. Return `NoContent()` for delete
10. Return `NotFound()` when entity not found
11. Return `BadRequest()` for validation errors
12. Add `[ServiceFilter(typeof(ValidationFilter))]` if applicable

### Phase 7: SignalR Hub

1. Verify hub exists in `src/FuelTrack.API/Hubs/`
2. Add notification method for entity changes
3. Verify hub connection authorized
4. Verify clients receive correct group notifications
5. Test hub connection lifecycle

### Phase 8: Frontend — API Client

1. Import `client` from `../api/client` (NOT raw axios)
2. Define API functions for each endpoint
3. Verify base URL and auth headers are handled by client interceptor
4. Add error handling for each API call

### Phase 9: Frontend — React Query Hooks

1. Create `{entity}Keys.js` for query key management
2. Create `use{Entity}s()` hook for list query
3. Create `use{Entity}(id)` hook for detail query
4. Create `useCreate{Entity}()` mutation hook
5. Create `useUpdate{Entity}()` mutation hook
6. Create `useDelete{Entity}()` mutation hook
7. Add query invalidation on mutations
8. Add optimistic updates where appropriate
9. Verify stale time and cache time configuration

### Phase 10: Frontend — Page Component

1. Create `{Name}.jsx` in `frontend/src/pages/`
2. Use `useTranslation()` hook (not direct `i18n` import)
3. Use Bootstrap components for layout
4. Add loading states with skeleton/spinner
5. Add error states with retry button
6. Add empty states with helpful message
7. Add table with sorting, filtering, pagination
8. Add create/edit modal/dialog
9. Add delete confirmation
10. Add form validation (client-side)
11. Add success/error toast notifications
12. Verify responsive design
13. Add navigation link in `App.jsx` router
14. Add i18n translations to `i18n.js`

### Phase 11: Testing

1. Create unit test for service CRUD operations
2. Create unit test for controller endpoints
3. Create integration test for API endpoints
4. Create Playwright test for critical user flows
5. Verify mock data follows current domain models
6. Verify DTO constructors match current signatures
7. Run `dotnet test` — all pass
8. Run `vitest` — all pass
9. Run `playwright test` — critical flows pass

### Phase 12: Documentation

1. Verify Swagger documentation updated
2. Verify API documentation reflects new endpoints
3. Add inline comments for complex business logic
4. Update AGENTS.md with session summary

## Validation Rules

| Rule | Severity | Description |
|------|----------|-------------|
| Client import | CRITICAL | Must use `client` from `../api/client`, not raw `axios` |
| ID type | CRITICAL | All IDs must be `int` in entity, DTO, and frontend |
| Authorization | CRITICAL | All write endpoints must have `[Authorize]` |
| AsNoTracking | REQUIRED | Read queries must use `AsNoTracking()` |
| CancellationToken | REQUIRED | All async methods must accept and pass CancellationToken |
| N+1 check | REQUIRED | Verify no N+1 queries in service methods |
| DTO mapping | REQUIRED | Manual mapping, no AutoMapper |
| Error handling | REQUIRED | All endpoints must handle errors gracefully |
| Localization | REQUIRED | All user-facing strings must use i18n |
| Testing | REQUIRED | All CRUD operations must have tests |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Raw axios import | Replace with `import client from '../api/client'` |
| `Guid` IDs | Convert to `int` everywhere |
| Missing `AsNoTracking()` | Add to all read-only queries |
| Missing `.Include()` | Add for navigation properties |
| N+1 queries | Use `.Include()` or `.Select()` projection |
| Missing error handling | Add try-catch with structured logging |
| Missing i18n | Wrap all strings in `t('key')` |
| Missing pagination | Add `.Skip().Take()` with page/size params |
| Missing authorization | Add `[Authorize]` attributes |

## Performance Considerations

- Use `AsNoTracking()` for all read queries
- Use `.Select()` projection to load only needed columns
- Use `.Include()` only when navigation data is needed
- Implement pagination for all list endpoints
- Use `PredicateBuilder` for dynamic filtering instead of string concatenation
- Cache frequently accessed lookup data
- Use `Take(1)` for existence checks instead of loading full entity

## Security Considerations

- Verify `[Authorize]` on all write endpoints
- Use `[Authorize(Roles = "Admin")]` for dangerous operations
- Validate user has access to requested entity (ownership check)
- Sanitize search inputs to prevent injection
- Verify CSRF protection on mutating endpoints
- Don't expose internal IDs or sensitive fields in DTOs

## Dependencies

- **Domain**: `src/FuelTrack.Domain/Models/`
- **Application**: `src/FuelTrack.Application/`
- **Infrastructure**: `src/FuelTrack.Infrastructure/Services/`
- **API**: `src/FuelTrack.API/Controllers/`
- **Frontend**: `frontend/src/pages/`, `frontend/src/api/`
- **Tests**: `tests/FuelTrack.Tests/`

## Related Skills

- `fueltrack-migration` (for entity schema changes)
- `fueltrack-permission-gate` (for authorization)
- `fueltrack-business-rules` (for business validation)
- `fueltrack-code-review` (for quality review)
- `fueltrack-test-fix` (for test maintenance)

## Examples

### Example: Create FuelTank CRUD

**Input**: "Create full CRUD for FuelTank entity"

**Steps**:
1. Domain: Verify `FuelTank.cs` has int PK, FKs, navigation props
2. DTOs: Create `FuelTankDto`, `FuelTankDetailDto`, `CreateFuelTankDto`, `UpdateFuelTankDto`
3. Validators: Create `CreateFuelTankValidator`, `UpdateFuelTankValidator`
4. Interface: Create `IFuelTankService.cs` with CRUD methods
5. Service: Create `FuelTankService.cs` with `AsNoTracking()`, `.Include()`, pagination
6. Controller: Create `FuelTanksController.cs` with `[Authorize]`, Swagger attrs
7. Frontend: Create `TanksManagement.jsx` with React Query hooks
8. Tests: Create service + controller tests
9. Build: `dotnet build` → 0 errors
10. Test: `dotnet test` → all pass

## Completion Checklist

- [ ] Domain entity follows all conventions (int PKs, BaseEntity, etc.)
- [ ] DTOs created with correct types and validation attributes
- [ ] Validators created with FluentValidation rules
- [ ] Service interface defined with CancellationToken
- [ ] Service implementation uses AsNoTracking, Include, pagination
- [ ] Controller has Authorize, Swagger, error handling
- [ ] SignalR notifications configured (if real-time needed)
- [ ] Frontend uses `client` import (not raw axios)
- [ ] React Query hooks created with proper cache management
- [ ] Frontend page has loading, error, empty states
- [ ] i18n translations added
- [ ] Unit tests created
- [ ] Integration tests created
- [ ] `dotnet build` passes
- [ ] `dotnet test` passes
- [ ] Swagger documentation verified
- [ ] AGENTS.md updated
