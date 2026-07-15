# fueltrack-business-rules

## Metadata

- **Name**: fueltrack-business-rules
- **Purpose**: Business logic validation and domain rule enforcement for FuelTrack entities
- **Priority**: Required
- **Blocking Level**: Required
- **Version**: 1.0.0
- **Last Updated**: 2026-07-10

## Description

Enforces business rules, invariants, and domain-specific validation across FuelTrack entities. Ensures all CRUD operations respect domain constraints beyond basic type/required validation. Prevents data corruption from invalid state transitions and business logic violations.

## Trigger Conditions

- Entity has business invariants (e.g., fuel level 0-100%)
- State transition rules exist (e.g., reading status workflow)
- Cross-entity validation needed (e.g., sub-station belongs to station)
- Aggregation logic exists (e.g., inventory calculations)
- Time-based rules apply (e.g., periodic inventory periods)

## Automatic Invocation Rules

- If entity has validation attributes beyond basic required/string-length → invoke
- If service contains conditional logic based on entity state → invoke
- If multiple entities interact (FK validation, aggregation) → invoke
- If enum-based state machine exists → invoke

## Manual Invocation Rules

- User says "business rule", "domain validation", "invariant check"
- User says "validate logic", "check business constraints"
- User says "state transition", "workflow rule"

## Input Requirements

| Input | Required | Description |
|-------|----------|-------------|
| Entity name | Yes | Which entity has business rules |
| Rule description | Yes | What constraint must be enforced |
| Rule type | Yes | Invariant / State transition / Cross-entity / Aggregation |

## Output Requirements

| Output | Description |
|--------|-------------|
| Validator rule | FluentValidation rule added to validator |
| Service guard | Business logic check in service method |
| Test case | Unit test verifying rule enforcement |
| Documentation | Rule documented in code comments |

## Sequential Workflow

### Phase 1: Rule Identification

1. Read entity file for validation attributes and comments
2. Read service file for conditional logic and guards
3. Read DTO for validation attributes
4. Check for enum state properties
5. Check for computed/derived properties
6. Check for FK relationships that imply constraints

### Phase 2: Rule Classification

| Category | Examples | Enforcement Point |
|----------|----------|-------------------|
| Value range | Fuel level 0-100%, capacity > 0 | Validator + Service guard |
| State transition | ReadingStatus: Pending → Verified → Approved | Service method |
| Cross-entity | SubStation belongs to Station | Service method + Validator |
| Uniqueness | Station code must be unique | Service method + DB constraint |
| Temporal | PeriodicInventory month 1-12, year valid | Validator |
| Aggregation | DailyReading calculations must be consistent | Service method |
| Authorization | Only assigned users can access sub-station data | Service method + Policy |

### Phase 3: Rule Implementation

#### Value Range Rules
1. Add `InclusiveBetween(min, max)` to validator
2. Add guard clause in service `Create/Update` methods
3. Add unit test with boundary values (min-1, min, min+1, max-1, max, max+1)
4. Verify error message is user-friendly

#### State Transition Rules
1. Identify valid state machine transitions
2. Add `IsValidTransition(from, to)` method in service
3. Add guard clause before state change
4. Add unit test for each valid transition
5. Add unit test for each invalid transition
6. Verify error message explains allowed transitions

#### Cross-Entity Rules
1. Verify FK relationship exists in domain model
2. Add `.Include()` to load related entity when needed
3. Add existence check in service method
4. Add ownership/access validation if needed
5. Add unit test verifying constraint
6. Add integration test verifying DB constraint

#### Uniqueness Rules
1. Add unique index in migration
2. Add existence check in service before create/update
3. Add error message for duplicate detection
4. Add unit test for duplicate scenario
5. Add integration test for DB constraint

#### Temporal Rules
1. Add date range validation in validator
2. Add business logic for period validation
3. Add timezone handling if applicable
4. Add unit test for boundary dates
5. Add test for DST transitions if relevant

#### Aggregation Rules
1. Identify source data for aggregation
2. Add calculation method in service
3. Add validation that source data exists
4. Add precision handling for decimals
5. Add unit test with known inputs/outputs
6. Add integration test with real data flow

### Phase 4: Guard Clause Patterns

```csharp
// Null check
if (entity is null)
    throw new NotFoundException(nameof(Entity), id);

// State transition
if (!IsValidTransition(entity.Status, newStatus))
    throw new BusinessRuleException(
        $"Cannot transition from {entity.Status} to {newStatus}");

// Value range
if (value < MinValue || value > MaxValue)
    throw new ValidationException(
        $"{nameof(value)} must be between {MinValue} and {MaxValue}");

// Ownership
if (entity.UserId != currentUser.Id && !currentUser.IsAdmin)
    throw new ForbiddenException("Not authorized to modify this entity");

// Uniqueness
if (await _context.Entities.AnyAsync(e => e.Code == code && e.Id != id))
    throw new ConflictException($"Entity with code '{code}' already exists");
```

### Phase 5: Testing

1. Create test for each business rule
2. Test boundary conditions (min, max, just outside)
3. Test state transitions (valid and invalid)
4. Test cross-entity constraints
5. Test uniqueness violations
6. Test temporal edge cases
7. Test aggregation with known values
8. Verify error messages are correct

### Phase 6: Documentation

1. Add XML comments to business rule methods
2. Add inline comments for complex logic
3. Document state machine transitions
4. Document aggregation formulas
5. Update AGENTS.md with session summary

## Validation Rules

| Rule | Severity | Description |
|------|----------|-------------|
| Guard clauses | REQUIRED | All service methods must validate preconditions |
| State machine | REQUIRED | State transitions must be explicit and tested |
| Boundary values | REQUIRED | Value range rules must be tested at boundaries |
| Error messages | REQUIRED | Must be user-friendly and explain the constraint |
| Cross-entity | REQUIRED | FK relationships must be validated |
| Aggregation | REQUIRED | Calculation precision must be verified |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Missing guard clause | Add null/state/value check at method entry |
| Invalid state transition | Add `IsValidTransition()` check |
| Missing uniqueness check | Add `AnyAsync()` before create/update |
| Wrong aggregation precision | Use `decimal` with `HasPrecision(18,2)` |
| Missing boundary test | Add tests at min, max, and just outside range |
| Poor error message | Explain what went wrong and how to fix it |

## Performance Considerations

- Cache frequently accessed business rules
- Use database constraints for uniqueness (don't rely only on app-level checks)
- Batch validation for bulk operations
- Avoid loading full entity for simple existence checks

## Security Considerations

- Business rules must be enforced server-side (never trust client-side validation)
- Authorization checks must happen before business rule checks
- Audit business rule violations with structured logging
- Prevent business rule bypass through direct API calls

## Dependencies

- **Domain**: `src/FuelTrack.Domain/Models/`
- **Application**: `src/FuelTrack.Application/DTOs/`
- **Infrastructure**: `src/FuelTrack.Infrastructure/Services/`
- **Tests**: `tests/FuelTrack.Tests/`

## Related Skills

- `fueltrack-crud-checklist` (for CRUD implementation)
- `fueltrack-permission-gate` (for authorization rules)
- `fueltrack-code-review` (for rule review)
- `fueltrack-test-fix` (for test updates)

## Examples

### Example: DailyReading Business Rules

**Rules**:
1. `CurrentEnergyKWh` must be >= `PrevEnergyKWh` (meter can't go backwards)
2. `CurrentRunningHours` must be >= `PrevRunningHours`
3. `GeneratorId` must reference an active generator
4. `ReadingDate` must not be in the future
5. `FuelUsedLiters` must be >= 0
6. `ReadingStatusLookupId` must be a valid status

**Implementation**:
```csharp
// Validator
RuleFor(x => x.CurrentEnergyKWh)
    .GreaterThanOrEqualTo(x => x.PrevEnergyKWh)
    .WithMessage("Current energy cannot be less than previous energy");

RuleFor(x => x.ReadingDate)
    .LessThanOrEqualTo(DateTime.UtcNow)
    .WithMessage("Reading date cannot be in the future");
```

## Completion Checklist

- [ ] All business rules identified from entity/service analysis
- [ ] Rules classified by type (value, state, cross-entity, etc.)
- [ ] Guard clauses added to service methods
- [ ] FluentValidation rules added to validators
- [ ] State machine transitions documented
- [ ] Uniqueness constraints enforced (app + DB)
- [ ] Aggregation precision verified
- [ ] Unit tests created for each rule
- [ ] Boundary conditions tested
- [ ] Error messages are user-friendly
- [ ] Business rules documented
