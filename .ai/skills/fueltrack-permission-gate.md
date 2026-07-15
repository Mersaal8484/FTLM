# fueltrack-permission-gate

## Metadata

- **Name**: fueltrack-permission-gate
- **Purpose**: RBAC permission management, validation, and corruption prevention
- **Priority**: Required
- **Blocking Level**: Required
- **Version**: 1.0.0
- **Last Updated**: 2026-07-10

## Description

Manages the hierarchical permission system in FuelTrack. Prevents the permission corruption bug (Session 2026-07-05) where dict-format permissions caused `[[[]]]` corruption. Ensures all permission operations use the correct JSON array format and domain-to-frontend mapping.

## Trigger Conditions

- Role permissions modified
- Permission JSON format validated
- Frontend permission mapping checked
- New module permissions added
- User role assignment changed

## Automatic Invocation Rules

- If `Permissions` field in `Role.cs` is modified → invoke
- If `RoleService.ConvertPermissions()` is modified → invoke
- If `PermissionHelper` methods are called → invoke
- If `PermissionMapper.cs` is modified → invoke
- If frontend permission checks added → invoke

## Manual Invocation Rules

- User says "fix permissions", "permission issue", "role access"
- User says "check RBAC", "validate permissions"
- User says "permission corruption", "role not working"

## Input Requirements

| Input | Required | Description |
|-------|----------|-------------|
| Operation type | Yes | Read / Modify / Validate / Fix |
| Role or permission | Yes | Which role or permission is affected |
| Scope | Yes | Backend only / Frontend only / Both |

## Output Requirements

| Output | Description |
|--------|-------------|
| Validation result | Permission format is correct |
| Corruption check | No `[[[]]]` or dict format detected |
| Mapping verification | Domain-to-frontend mapping is consistent |
| Fix actions | Steps to resolve any issues found |

## Sequential Workflow

### Phase 1: Permission Format Validation

1. Read `Role.cs` — verify `Permissions` field is `string?`
2. Read `PermissionHelper.cs` — verify serialization format
3. Check database for permission values matching these patterns:
   - **VALID**: `[{"module":"Dashboard","canView":true}]`
   - **CORRUPTED**: `[[[]]]`, `{}`, `{"dashboard":{"view":true}}`
4. If corrupted format found → trigger normalization
5. Verify `IsJsonFormat()` correctly detects format

### Phase 2: Permission Structure Validation

1. Verify all permission modules match `PermissionModules.AllModules`:
   - Dashboard, Readings, FuelMovements, OilTransactions, Inventory
   - Reports, Users, Roles, Stations, SubStations, Generators
   - FuelTanks, OilTanks, Lookups, Notifications, Settings
2. Verify each module has valid actions: `canView`, `canCreate`, `canUpdate`, `canDelete`
3. Verify no unknown modules exist in permission data
4. Verify no duplicate modules in same permission array

### Phase 3: Domain-to-Frontend Mapping

1. Read `PermissionMapper.cs` — verify `DomainToFrontend` dictionary
2. Verify all domain modules have frontend mappings:
   - `Dashboard` → `dashboard`
   - `Readings` → `readings`
   - `FuelMovements` → `fuel_movements`
   - `OilTransactions` → `oil_transactions`
   - `Inventory` → `inventory`
   - `Reports` → `reports`
   - `Users` → `users`
   - `Roles` → `roles`
   - `Stations` → `stations`
   - `SubStations` → `sub_stations`
   - `Generators` → `generators`
   - `FuelTanks` → `fuel_tanks`
   - `OilTanks` → `oil_tanks`
   - `Lookups` → `lookups`
   - `Notifications` → `notifications`
   - `Settings` → `settings`
3. Verify frontend permission keys match backend module names
4. Check for unmapped modules on either side

### Phase 4: Corruption Prevention

1. Verify `ConvertPermissions()` always normalizes to array format
2. Verify `ConvertDictToModules()` is called in Object branch
3. Verify `PermissionHelper.Serialize()` produces correct JSON
4. Test round-trip: serialize → deserialize → verify equality
5. Check for dict format remnants in any role permissions
6. Verify `InitAsync()` seeds correct format

### Phase 5: API Authorization Validation

1. Verify `RoleService.InitAsync()` seeds roles with correct permissions
2. Verify `POST /api/roles` validates permission format before save
3. Verify `PUT /api/roles/{id}` normalizes permission format on update
4. Verify `GET /api/roles` returns correctly formatted permissions
5. Test permission check flow: token → claims → policy → endpoint

### Phase 6: Frontend Permission Validation

1. Verify `PermissionMapper.MapToModules()` correctly maps domain to frontend
2. Verify `PermissionMapper.HasPermission()` checks correctly
3. Verify menu visibility respects permissions
4. Verify page access respects permissions
5. Verify button visibility respects permissions
6. Test permission denied UI shows correct message

### Phase 7: Seed Data Validation

1. Verify `POST /api/roles/init` seeds correct format
2. Verify default roles (Admin, Manager, Operator, Viewer) have correct permissions
3. Verify Admin role has full access to all modules
4. Verify Manager role has appropriate restricted access
5. Verify Operator role has operational access only
6. Verify Viewer role has read-only access

## Validation Rules

| Rule | Severity | Description |
|------|----------|-------------|
| Array format | CRITICAL | Permissions MUST be JSON array format |
| No dict format | CRITICAL | Dict format causes corruption on re-save |
| No `[[[]]]` | CRITICAL | Corrupted format must be fixed immediately |
| Module names | REQUIRED | Must match `PermissionModules.AllModules` |
| Action names | REQUIRED | Must be `canView`, `canCreate`, `canUpdate`, `canDelete` |
| Mapping complete | REQUIRED | All domain modules must have frontend mappings |
| Seed data | REQUIRED | Default roles must be seeded correctly |
| No duplicates | REQUIRED | No duplicate modules in same permission array |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Dict format saved | Call `ConvertDictToModules()` before serializing |
| Missing normalization | Always call `PermissionHelper.NormalizeToJson()` on save |
| Wrong module name | Use exact names from `PermissionModules.AllModules` |
| Missing frontend mapping | Add to `DomainToFrontend` dictionary |
| Permission check bypass | Verify `[Authorize]` + policy check on endpoint |
| Corrupted seed data | Call `POST /api/roles/init` to re-seed |

## Performance Considerations

- Cache role permissions after first load
- Use in-memory permission check for frequently accessed endpoints
- Batch permission checks for bulk operations
- Avoid re-serializing permissions on every request

## Security Considerations

- Never expose raw permission JSON to unauthorized users
- Verify permission checks on every API endpoint
- Audit permission changes with structured logging
- Prevent privilege escalation through permission manipulation
- Verify JWT claims include correct role information
- Check for role removal after permission check

## Dependencies

- **Domain**: `src/FuelTrack.Domain/Models/Permissions.cs`, `Role.cs`
- **Application**: `src/FuelTrack.Application/DTOs/PermissionModuleDto.cs`
- **Infrastructure**: `src/FuelTrack.Infrastructure/Services/RoleService.cs`
- **API**: `src/FuelTrack.API/Helpers/PermissionMapper.cs`

## Related Skills

- `fueltrack-crud-checklist` (for role CRUD)
- `fueltrack-code-review` (for permission review)
- `fueltrack-test-fix` (for permission tests)

## Examples

### Example: Validate All Roles

**Input**: "Check all roles for permission corruption"

**Steps**:
1. Query all roles from database
2. For each role, check `Permissions` field format
3. If `IsJsonFormat()` returns false → flag as corrupted
4. If dict format detected → normalize with `ConvertDictToModules()`
5. If `[[[]]]` detected → re-seed with `POST /api/roles/init`
6. Report findings with role names and corruption types

### Example: Add New Module Permission

**Input**: "Add 'Analytics' module to permission system"

**Steps**:
1. Add `Analytics` to `PermissionModules.AllModules`
2. Add `analytics` to `DomainToFrontend` dictionary in `PermissionMapper`
3. Verify frontend permission key matches
4. Update default role seeds with Analytics permissions
5. Test Admin role has Analytics access
6. Test Viewer role has Analytics read-only

## Completion Checklist

- [ ] All role permissions in array format (not dict or corrupted)
- [ ] All modules match `PermissionModules.AllModules`
- [ ] All actions are `canView`, `canCreate`, `canUpdate`, `canDelete`
- [ ] Domain-to-frontend mapping is complete and correct
- [ ] Seed data produces correct format
- [ ] `ConvertPermissions()` always normalizes
- [ ] API endpoints validate permission format
- [ ] Frontend permission checks work correctly
- [ ] No `[[[]]]` corruption in database
- [ ] Permission changes are audited
