# Create CRUD Module Prompt

## Description
Step-by-step guide to implement a full CRUD module from domain entity through React frontend, following FuelTrack's Clean Architecture conventions.

## System Prompt
**Role:** Senior full-stack developer specializing in ASP.NET Core Clean Architecture with React frontends.
**Context:** FuelTrack project uses ASP.NET Core 10, EF Core, SQL Server, React 18, Vite, Bootstrap, React Query, Zustand, SignalR. Architecture: Domain → Application → Infrastructure → API. All IDs are `int` (not Guid). Audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy) auto-populated via `AuditSaveChangesInterceptor`.
**Constraints:**
- Do NOT modify existing app source code outside the new module files.
- Use only existing libraries: FluentValidation, Automapper (or manual mapping), React Query, Zustand, Bootstrap.
- Follow existing file naming and folder conventions exactly.
- FK properties must be `int`, never `Guid`.
- Always include `[PermissionGate]` or equivalent permission checks on React pages.
- Never hardcode connection strings, secrets, or environment-specific values.

## User Prompt Template
Create a complete CRUD module for {{entity_name}} with the following:

**Entity Description:** {{description}}
**FK Relationships:** {{foreign_keys}}
**List Columns:** {{list_columns}}
**Form Fields:** {{form_fields}}

Follow the full dependency order:
1. Domain entity (inherit BaseEntity)
2. EF Core configuration (Fluent API in DbContext)
3. DTOs (List, Detail, Create, Update)
4. FluentValidation validators
5. Repository / service layer
6. API controller
7. React page with table, create/edit dialog
8. React Query hooks
9. Unit tests

## Examples

### Example 1: FuelTank Module
```
Entity: FuelTank
FK: SubStationId (int), FuelLookupId (int)
List Columns: Name, SubStation name, Fuel type, Capacity
Form Fields: Name, SubStationId (dropdown), FuelLookupId (dropdown), Capacity, Notes

Domain: FuelTank : BaseEntity { SubStationId int, FuelLookupId int, Capacity decimal, Name string, Notes string? }
DbContext: entity.HasOne<SubStation>().WithMany().HasForeignKey(e => e.SubStationId)
DTOs: FuelTankListDto { Id, Name, SubStationName, FuelTypeName, Capacity }
Service: FuelTankService : IFuelTankService
Controller: [ApiController] [Route("api/fuel-tanks")] [Authorize]
React: FuelTanks.js with <PermissionGate permission="fuel_tanks.view">
```

### Example 2: OilTank Module
```
Entity: OilTank
FK: SubStationId (int), OilLookupId (int)
List Columns: Name, SubStation name, Oil type, Capacity
Form Fields: Name, SubStationId (dropdown), OilLookupId (dropdown), Capacity, Notes

Domain: OilTank : BaseEntity { SubStationId int, OilLookupId int, Capacity decimal, Name string }
DbContext: entity.HasOne<SubStation>().WithMany().HasForeignKey(e => e.SubStationId)
DTOs: OilTankListDto { Id, Name, SubStationName, OilTypeName, Capacity }
Service: OilTankService : IOilTankService
Controller: [ApiController] [Route("api/oil-tanks")] [Authorize]
React: OilTanks.js with <PermissionGate permission="oil_tanks.view">
```

## Completion Checklist
- [ ] Entity inherits `BaseEntity` with `int` Id
- [ ] FK properties are `int` (not Guid, not string)
- [ ] Navigation properties are nullable where appropriate
- [ ] EF Core Fluent API configured in `FuelTrackDbContext.OnModelCreating`
- [ ] DTOs created: List, Detail, Create, Update
- [ ] FluentValidation validators for Create and Update DTOs
- [ ] Service interface + implementation registered in DI
- [ ] Controller has `[Authorize]`, `[PermissionGate]` or role-based checks
- [ ] Controller returns `Ok(result)` not `CreatedAtAction` (avoids NRE)
- [ ] Service uses `AsNoTracking()` for read queries
- [ ] Service includes related entities with `.Include()` where needed
- [ ] React page imports `client` from `../api/client` (not raw axios)
- [ ] React page has `<PermissionGate>` wrapper
- [ ] React Query hooks use proper `queryKey` and invalidation
- [ ] Form uses existing Bootstrap components and validation patterns
- [ ] Tests use `int` IDs, correct field names, valid DTO constructors
- [ ] Build passes with 0 errors
- [ ] All existing tests still pass
