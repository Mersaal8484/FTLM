# fueltrack-migration

## Metadata

- **Name**: fueltrack-migration
- **Purpose**: Safe EF Core migration creation, validation, and rollback for FuelTrack
- **Priority**: Required
- **Blocking Level**: Required
- **Version**: 1.0.0
- **Last Updated**: 2026-07-10

## Description

Manages the complete lifecycle of Entity Framework Core migrations in FuelTrack. Ensures every schema change is safe, reversible, and consistent with the Clean Architecture model.

## Trigger Conditions

- Entity model property added, removed, or renamed
- New entity created
- Relationship changed (FK, navigation property)
- Index added or modified
- Fluent API configuration changed in `FuelTrackDbContext`
- Nullable field changed
- Column type changed

## Automatic Invocation Rules

- If entity file modified in `src/FuelTrack.Domain/Models/` → invoke migration skill
- If `FuelTrackDbContext.cs` modified → invoke migration skill
- If `OnModelCreating` changes → invoke migration skill
- If any `*.Designer.cs` file in Migrations/ is modified → invoke migration skill

## Manual Invocation Rules

- User says "create migration", "add migration", "schema change"
- User asks to validate current migration state
- User asks to rollback or revert migration

## Input Requirements

| Input | Required | Description |
|-------|----------|-------------|
| Change description | Yes | What schema change is needed |
| Affected entities | Yes | Which domain models are affected |
| Direction | Yes | Create / Rollback / Validate |

## Output Requirements

| Output | Description |
|--------|-------------|
| Migration file | New `.cs` file in `src/FuelTrack.Infrastructure/Migrations/` |
| Build result | `dotnet build` success confirmation |
| Test result | `dotnet test` pass confirmation |
| Rollback script | SQL rollback command or Down() method verification |

## Sequential Workflow

### Phase 1: Pre-Migration Analysis

1. Read affected entity files in `src/FuelTrack.Domain/Models/`
2. Read current `FuelTrackDbContext.cs` Fluent API configuration
3. Read latest migration file in `src/FuelTrack.Infrastructure/Migrations/`
4. Verify entity-to-table mapping consistency
5. Check for pending model changes

### Phase 2: Entity Validation

1. Verify all PKs use `int` (not `Guid`)
2. Verify all FKs use `int` (not `Guid`)
3. Verify `BaseEntity` inheritance (`Id`, `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`)
4. Verify nullable annotations match database intent
5. Verify navigation properties are correctly configured
6. Verify no duplicate column names across related entities
7. Check for `List<Guid>` remnants → must be `List<int>`

### Phase 3: Fluent API Validation

1. Verify `OnModelCreating` in `FuelTrackDbContext` matches entity config
2. Check FK relationships are explicit (not convention-dependent)
3. Verify cascade delete behavior is intentional
4. Check composite index definitions
5. Verify column types match SQL Server types
6. Validate `HasPrecision()` on decimal fields

### Phase 4: Index Validation

1. Verify new columns have appropriate indexes
2. Check composite index column order (most selective first)
3. Verify no redundant single-column indexes exist
4. Check index naming convention: `IX_{Table}_{Columns}`
5. Verify FK indexes exist for all foreign keys

### Phase 5: Migration Generation

1. Run `dotnet ef migrations add {MigrationName} --project src/FuelTrack.Infrastructure --startup-project src/FuelTrack.API`
2. Review generated migration file
3. Verify `Up()` method matches intended changes
4. Verify `Down()` method can safely revert
5. Check for data migration vs schema migration

### Phase 6: Build & Test

1. Run `dotnet build` — must succeed with 0 errors
2. Run `dotnet test` — all tests must pass
3. Verify no new compiler warnings related to changed code
4. Check Swagger generation still works

### Phase 7: Post-Migration Verification

1. Verify seed data still works (if applicable)
2. Verify `dotnet ef database update` applies cleanly
3. Verify `dotnet ef database update --migration {Previous}` rolls back cleanly
4. Run existing integration tests against updated schema
5. Verify no breaking changes to existing API responses

## Validation Rules

| Rule | Severity | Description |
|------|----------|-------------|
| PK type | CRITICAL | All entity PKs must be `int` |
| FK type | CRITICAL | All FK properties must be `int` |
| No Guid remnants | CRITICAL | No `Guid` or `Guid?` in entity properties |
| Cascade delete | WARNING | Explicit cascade behavior required |
| Index naming | INFO | Must follow `IX_{Table}_{Columns}` |
| Down() method | REQUIRED | Every migration must have reversible Down() |
| Build success | REQUIRED | `dotnet build` must produce 0 errors |
| Test pass | REQUIRED | `dotnet test` must pass all non-skipped tests |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| `Guid` ID remnant | Convert to `int` in entity, DTO, and test |
| Missing `Down()` | Add reverse operation in migration |
| Missing FK index | Add `HasIndex()` in Fluent API |
| Wrong cascade | Configure `.OnDelete(DeleteBehavior.Restrict)` explicitly |
| Missing nullable | Use `int?` for optional FK, `int` for required |
| Data loss migration | Split into schema + data migration steps |
| N+1 from migration | Add `.Include()` after adding new navigation props |

## Performance Considerations

- Avoid adding indexes on large tables during peak hours
- Use `CREATE INDEX CONCURRENTLY` equivalent when possible
- Consider table partitioning for migrations affecting large tables
- Verify new indexes don't duplicate existing ones
- Check query plans after migration application

## Security Considerations

- Never hardcode connection strings in migration files
- Verify migration doesn't expose sensitive data in columns
- Check column-level encryption if adding sensitive fields
- Verify SQL Server permissions allow DDL operations

## Dependencies

- **Domain Layer**: `src/FuelTrack.Domain/Models/`
- **DbContext**: `src/FuelTrack.Infrastructure/FuelTrackDbContext.cs`
- **Migrations**: `src/FuelTrack.Infrastructure/Migrations/`
- **Tools**: `dotnet-ef` CLI

## Related Skills

- `fueltrack-crud-checklist` (for new entity CRUD)
- `fueltrack-code-review` (for migration review)
- `fueltrack-test-fix` (for test updates after schema change)

## Examples

### Example: Add Property to Entity

**Input**: "Add `MaxCapacity` (decimal) to `FuelTank` entity"

**Steps**:
1. Read `FuelTank.cs` — verify current properties
2. Add `public decimal MaxCapacity { get; set; }` to entity
3. Read `FuelTrackDbContext.cs` — add `HasPrecision(18,2)` for MaxCapacity
4. Run `dotnet ef migrations add AddMaxCapacityToFuelTank`
5. Review generated migration — verify column type
6. Run `dotnet build` — 0 errors
7. Run `dotnet test` — all pass
8. Verify `Down()` drops column

### Example: Rollback Migration

**Input**: "Revert the last migration"

**Steps**:
1. Run `dotnet ef migrations remove --project src/FuelTrack.Infrastructure --startup-project src/FuelTrack.API`
2. Verify `dotnet build` succeeds
3. Run `dotnet test` — all pass
4. Verify removed migration file no longer exists

## Completion Checklist

- [ ] Entity changes validated against domain model rules
- [ ] Fluent API configuration updated
- [ ] Index strategy validated
- [ ] Migration generated successfully
- [ ] `Up()` method reviewed
- [ ] `Down()` method reviewed and reversible
- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (0 failures)
- [ ] No new warnings introduced
- [ ] Seed data verified (if applicable)
- [ ] Rollback tested
- [ ] Documentation updated (if public API affected)
