# fueltrack-test-fix

## Metadata

- **Name**: fueltrack-test-fix
- **Purpose**: Fix and maintain FuelTrack tests to match current domain models and patterns
- **Priority**: Required
- **Blocking Level**: Required
- **Version**: 1.0.0
- **Last Updated**: 2026-07-10

## Description

Systematically identifies and fixes failing tests caused by domain model changes (type migrations, field renames, entity restructuring). Ensures test files match current domain models, DTOs, and service signatures. Prevents test drift from accumulated refactoring.

## Trigger Conditions

- `dotnet test` produces failures
- `vitest` produces failures
- Domain model changed (entity added/removed/modified)
- DTO changed (property added/removed/renamed)
- Service signature changed (method added/removed/renamed)
- Test file references stale types

## Automatic Invocation Rules

- If `dotnet test` exits with non-zero code → invoke
- If `vitest` exits with non-zero code → invoke
- If entity type changes (e.g., `Guid` → `int`) → invoke
- If DTO property changes → invoke
- If service method signature changes → invoke

## Manual Invocation Rules

- User says "fix tests", "tests are failing", "test suite broken"
- User says "update tests", "align tests with models"
- User says "test migration", "fix test types"

## Input Requirements

| Input | Required | Description |
|-------|----------|-------------|
| Test output | Yes | Error messages from failing tests |
| Changed files | Yes | Which files were modified recently |
| Test type | Yes | Unit / Integration / E2E |

## Output Requirements

| Output | Description |
|--------|-------------|
| Fixed test files | Updated to match current domain models |
| Test results | All tests passing |
| Regression check | No new failures introduced |

## Current Domain Model Reference

### Entity Types
| Entity | PK Type | Key Fields |
|--------|---------|------------|
| BaseEntity | `int Id` | CreatedBy/UpdatedBy = `int` |
| Lookup | `int` | CategoryId(int), Name, Code?, SortOrder |
| LookupCategory | `int` | Name, DisplayName, Description? |
| User | `int` | IdentityUser<int>, FullName, IsActive |
| Role | `int` | IdentityRole<int>, Permissions? |
| SubStation | `int` | StationId(int), CapacityKW(double?) |
| DailyReading | `int` | GeneratorId(int), PrevEnergyKWh/CurrentEnergyKWh, PrevRunningHours/CurrentRunningHours, FuelLookupId(int), StandardFuelRate(decimal), OilLookupId(int), OilUsedLiters(double), ReadingStatusLookupId(int) |
| SubStationReading | `int` | SubStationId(int), MeterId(int), PrevExportedEnergyKWh/CurrentExportedEnergyKWh |
| PeriodicInventory | `int` | SubStationId(int), Year, Month, PeriodTypeLookupId(int) |
| FuelTransaction | `int` | SubStationId(int), FuelLookupId(int) |
| OilTransaction | `int` | SubStationId(int), OilLookupId(int), TransactionTypeLookupId(int) |
| FuelTank | `int` | SubStationId(int), FuelLookupId(int) |
| OilTank | `int` | SubStationId(int), OilLookupId(int) |
| AnalyticsInsight | `int` | SubStationId(int?), StationId(int?) |
| AuditLog | `int` | UserId(int), EntityId(int) |
| Notification | `int` | SubStationId(int?) |
| Meter | `int` | SubStationId(int?), GeneratorId(int?), FuelTankId(int?) |
| Generator | `int` | SubStationId(int), ActualPowerKW(int), FuelConsumptionRatePerHour(decimal) |

### Models That No Longer Exist
- `StationReading` → use `SubStationReading`
- `FuelMovement` → use `FuelTransaction`
- `OilUsage` → use `OilTransaction`
- `DailyOilConsumption` → removed
- `SubStationMeter` → use `Meter`

### Common Field Mappings
| Old | New |
|-----|-----|
| `Guid Id` | `int Id` |
| `Guid UserId` | `int UserId` |
| `Guid StationId` | `int StationId` |
| `Guid SubStationId` | `int SubStationId` |
| `Guid GeneratorId` | `int GeneratorId` |
| `Guid FuelTankId` | `int FuelTankId` |
| `Guid MeterId` | `int MeterId` |
| `Lookup.Description` | removed (use `Name`) |
| `Lookup.Category` | use `CategoryId` |
| `DailyReading.Shift` | removed |
| `DailyReading.RunningHours` | use `PrevRunningHours`/`CurrentRunningHours` |
| `DailyReading.EnergyKWh` | use `PrevEnergyKWh`/`CurrentEnergyKWh` |
| `DailyReading.LoadKW` | removed |
| `DailyReading.FuelLevelPct` | removed |
| `PeriodicInventory.InventoryType` | use `PeriodTypeLookupId` |
| `Generator.Code` | use `SerialNumber` |
| `Generator.ControlScreenLookupId` | removed |

## Sequential Workflow

### Phase 1: Test Discovery

1. Run `dotnet test --no-build --verbosity normal` — capture output
2. Run `npx vitest run` — capture output
3. Parse error messages for type mismatches
4. Identify files with compilation errors
5. Identify files with runtime assertion failures
6. List all failing tests with error details

### Phase 2: Error Classification

| Error Type | Pattern | Fix Approach |
|-----------|---------|--------------|
| Type mismatch | `Cannot convert Guid to int` | Update type in test |
| Missing member | `Property 'X' does not exist` | Remove reference or find new name |
| Constructor mismatch | `No constructor matches arguments` | Update constructor call |
| Missing type | `Type 'X' not found` | Replace with current type |
| Assertion failure | `Expected X but got Y` | Update expected value or mock |
| Missing mock | `NullReferenceException` | Add mock setup |
| Stale reference | `Assembly reference outdated` | Update namespace or reference |

### Phase 3: Test File Audit

For each failing test file:
1. Read test file completely
2. Identify all type references
3. Cross-reference with current domain models
4. List all mismatches:
   - `Guid` → `int`
   - Removed properties
   - Renamed properties
   - Changed constructor signatures
   - Changed method signatures
   - Changed enum values

### Phase 4: Fix Application

#### Fix Pattern 1: Type Migration (Guid → int)
```csharp
// Before
var id = Guid.NewGuid();
entity.Id = id;

// After
var id = 1; // or use fixture.Create<int>()
entity.Id = id;
```

#### Fix Pattern 2: Property Rename
```csharp
// Before
entity.Description = "test";

// After
entity.Name = "test";
```

#### Fix Pattern 3: Constructor Change
```csharp
// Before
var dto = new CreateFuelTankDto(subStationId: guid, fuelLookupId: guid);

// After
var dto = new CreateFuelTankDto(subStationId: 1, fuelLookupId: 2);
```

#### Fix Pattern 4: Missing Property
```csharp
// Before
entity.Shift = "Day";

// After — property removed, delete the line
```

#### Fix Pattern 5: Changed Service Signature
```csharp
// Before
var result = await service.GetAsync(id);

// After
var result = await service.GetByIdAsync(id, cancellationToken);
```

### Phase 5: Mock Setup

1. Verify all mocked interfaces match current signatures
2. Update mock setups for changed return types
3. Add mock setups for new dependencies
4. Remove mock setups for removed dependencies
5. Verify `CancellationToken` in mock setups
6. Verify `IHttpContextAccessor` mock if user identification needed

### Phase 6: AutoFixture / Test Data

1. Verify `fixture.Create<T>()` works with current types
2. Add customizations for `int` ID generation if needed
3. Verify `FixtureHelper` configurations
4. Remove `Guid` customizations that no longer apply
5. Verify DTO construction with current signatures

### Phase 7: Verification

1. Run `dotnet build` — 0 errors
2. Run `dotnet test` — all pass
3. Run `npx vitest run` — all pass
4. Verify no new warnings introduced
5. Verify test coverage maintained

## Validation Rules

| Rule | Severity | Description |
|------|----------|-------------|
| Type consistency | CRITICAL | Test types must match domain model types |
| Constructor matching | REQUIRED | DTO/entity constructors must match current signatures |
| Property names | REQUIRED | Property references must match current names |
| Mock setup | REQUIRED | Mocked interfaces must match current signatures |
| No stale references | REQUIRED | No references to removed types/properties |
| All tests pass | REQUIRED | `dotnet test` and `vitest` must pass |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| `Guid.NewGuid()` for ID | Use `fixture.Create<int>()` or `1` |
| `entity.Description` | Use `entity.Name` (if renamed) |
| `StationReading` reference | Use `SubStationReading` |
| `FuelMovement` reference | Use `FuelTransaction` |
| `OilUsage` reference | Use `OilTransaction` |
| `InventoryType` string | Use `PeriodTypeLookupId` int |
| `Generator.Code` | Use `Generator.SerialNumber` |
| `Lookup.Description` | Use `Lookup.Name` |
| Missing `.Include()` in mock | Add `.Include()` to mock setup |
| Wrong constructor args | Update to match current DTO constructor |

## Performance Considerations

- Fix tests in batches (by error type) for efficiency
- Use grep to find all instances of a pattern before fixing
- Run tests after each batch to catch regressions early
- Avoid fixing one test at a time (too many round trips)

## Security Considerations

- Don't disable security tests to make them pass
- Don't remove authorization test assertions
- Don't weaken mock security setup
- Verify test data doesn't contain real credentials

## Dependencies

- **Tests**: `tests/FuelTrack.Tests/`
- **Domain**: `src/FuelTrack.Domain/Models/` (source of truth)
- **Application**: `src/FuelTrack.Application/DTOs/`
- **Infrastructure**: `src/FuelTrack.Infrastructure/Services/`
- **Frontend Tests**: `frontend/src/**/*.test.js`, `frontend/e2e/`

## Related Skills

- `fueltrack-migration` (for understanding schema changes)
- `fueltrack-crud-checklist` (for current CRUD patterns)
- `fueltrack-code-review` (for test quality review)

## Examples

### Example: Fix Unit Test After Guid→int Migration

**Failing test**:
```csharp
[Fact]
public async Task GetById_ReturnsFuelTank()
{
    var id = Guid.NewGuid(); // WRONG: Guid
    var tank = new FuelTank { Id = id }; // WRONG: Guid
    // ...
}
```

**Fixed test**:
```csharp
[Fact]
public async Task GetById_ReturnsFuelTank()
{
    var id = 1; // CORRECT: int
    var tank = new FuelTank { Id = id }; // CORRECT: int
    // ...
}
```

### Example: Fix Service Test After Property Rename

**Failing test**:
```csharp
[Fact]
public void Create_ReturnsCorrectFields()
{
    var dto = new CreateLookupDto { Description = "test" }; // WRONG: removed
    // ...
}
```

**Fixed test**:
```csharp
[Fact]
public void Create_ReturnsCorrectFields()
{
    var dto = new CreateLookupDto { Name = "test" }; // CORRECT: current property
    // ...
}
```

## Completion Checklist

- [ ] All failing tests identified and classified
- [ ] Type mismatches fixed (Guid → int)
- [ ] Property renames applied
- [ ] Constructor signatures updated
- [ ] Removed type references cleaned up
- [ ] Mock setups updated
- [ ] AutoFixture customizations updated
- [ ] `dotnet build` passes (0 errors)
- [ ] `dotnet test` passes (all non-skipped)
- [ ] `vitest` passes (all non-skipped)
- [ ] No new warnings introduced
- [ ] Test coverage maintained
