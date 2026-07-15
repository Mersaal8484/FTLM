# RBAC + UserScope Redesign Plan

**Date:** 2026-07-09
**Goal:** Complete RBAC + UserScope redesign using Module Registry as Single Source of Truth

---

## Architecture: Module Registry (Single Source of Truth)

Every page in the system is registered ONCE in a Module Registry. This registry is the single source of truth that auto-generates:

- Sidebar menu items
- Permissions (View/Create/Update/Delete/Print/Export/Approve/Reject)
- Permission tree
- API permissions
- UserScope type requirements
- Audit log registration
- General search registration
- Dashboard widget permissions

### Registry Structure

```typescript
interface ModuleDefinition {
  name: { en: string; ar: string };
  route: string;
  icon: string;
  parent: string | null;
  template: 'CRUD' | 'Approval' | 'Report' | 'Dashboard';
  scopeType: 'None' | 'Branch' | 'Station' | 'SubStation';
  dashboardWidget: boolean;
  widgetIds?: string[];  // e.g., ['fuel_stock', 'running_generators']
  customActions?: string[];
  order?: number;
}
```

### Template → Permission Mapping

| Template | Generated Permissions |
|----------|----------------------|
| CRUD | View, Create, Update, Delete |
| Approval | View, Create, Update, Delete, Approve, Reject |
| Report | View, Print, Export |
| Dashboard | View |

### ScopeType → UserScope Enforcement

| ScopeType | Enforcement |
|-----------|-------------|
| None | No scope filtering |
| Branch | User must have Branch scope |
| Station | User must have Station scope |
| SubStation | User must have SubStation scope |

---

## Implementation Phases

### Phase 1: Domain Layer (Foundation)

**Create:**
- `src/FuelTrack.Domain/Models/ModuleDefinition.cs` — Module definition entity
- `src/FuelTrack.Domain/Models/ModuleTemplate.cs` — Enum: CRUD, Approval, Report, Dashboard
- `src/FuelTrack.Domain/Models/ScopeType.cs` — Enum: None, Branch, Station, SubStation
- `src/FuelTrack.Domain/Models/PagePermission.cs` — Auto-generated permission entity
- `src/FuelTrack.Domain/Models/DashboardWidgetPermission.cs` — Widget-level permission

**Modify:**
- `src/FuelTrack.Domain/Models/UserScope.cs` — Add GeneratorId, FuelTankId, OilTankId, MeterId

### Phase 2: Infrastructure Layer (Seed & Migration)

**Create:**
- `src/FuelTrack.Infrastructure/Data/ModuleRegistrySeed.cs` — Defines ALL pages
- `src/FuelTrack.Infrastructure/Data/PermissionGenerator.cs` — Generates permissions from registry
- `src/FuelTrack.Infrastructure/Data/DashboardWidgetGenerator.cs` — Generates widget permissions
- `src/FuelTrack.Infrastructure/Migrations/YYYYMMDDHHMMSS_ModuleRegistryAndPermissions.cs`

**Modify:**
- `src/FuelTrack.Infrastructure/FuelTrackDbContext.cs` — Add DbSets

### Phase 3: Application Layer (Services)

**Create:**
- `src/FuelTrack.Infrastructure/Services/UserScopeService.cs` — Centralized scope filtering

**Modify:**
- `src/FuelTrack.Infrastructure/Services/PermissionService.cs` — Query from registry
- `src/FuelTrack.Infrastructure/Services/UserScopeFilterService.cs` — Use UserScopeService
- `src/FuelTrack.Infrastructure/Services/RoleService.cs` — System Owner protection

### Phase 4: API Layer (Enforcement)

**Create:**
- `src/FuelTrack.API/Middleware/PermissionEnforcementMiddleware.cs`

**Modify:**
- `src/FuelTrack.API/Controllers/BaseApiController.cs` — Add CanAccess helper
- ALL controllers — Replace role-name checks with permission checks

### Phase 5: Frontend (Registry & Components)

**Modify:**
- `frontend/src/permissions/registry.js` — Expand to match backend
- `frontend/src/permissions/usePermissions.js` — Update for new structure
- `frontend/src/components/roles/PermissionMatrix.jsx` — Support nested modules

### Phase 6: Frontend Pages (Scope-Aware)

**Modify:**
- ALL page components — Add scope filtering UI
- `frontend/src/pages/UsersManagement.jsx` — Scope assignment

### Phase 7: Testing

**Create:**
- `src/FuelTrack.Tests/Integration/PermissionEnforcementTests.cs`
- `src/FuelTrack.Tests/Integration/ScopeEnforcementTests.cs`

---

## Migration Strategy (Clean Rebuild)

1. Drop existing permission tables
2. Create new schema
3. Seed ModuleRegistry
4. Auto-generate permissions
5. Auto-assign to System Owner
6. Migrate existing roles (best effort)
7. Update UserScope with new fields

---

## Key Design Decisions

| Decision | Choice |
|----------|--------|
| Permission source | Module Registry (Single Source of Truth) |
| Auto-generation | Yes — all from registry |
| System Owner | Auto-assigned ALL, protected |
| Scope hierarchy | Branch → Station → SubStation |
| Dashboard widgets | Independent View per widget |
| Reports | Independent View/Print/Export |
| Migration | Clean rebuild |
