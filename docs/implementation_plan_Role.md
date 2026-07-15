# إعادة بناء نظام الصلاحيات — Permissions System Rebuild

## الهدف / Goal

إعادة بناء نظام الصلاحيات بالكامل بحيث يعتمد على الجداول الثلاثة الجديدة:
- **`ModuleDefinitions`** — تعريف جميع الصفحات/الوحدات (مصدر الحقيقة الوحيد)
- **`PagePermissions`** — الصلاحيات المتاحة لكل وحدة (view, create, edit, delete, approve, reject, export, print...)
- **`RolePermissions`** — ربط الأدوار بالصلاحيات (Role → PagePermission → IsGranted)

وإزالة النظام القديم القائم على:
- `PermissionHelper.cs` (JSON serialization/deserialization)
- `Permissions.cs` (`PermissionModule` class + `PermissionModules.AllModules`)
- `registry.js` (frontend static MODULES object)
- حقل `Role.Permissions` (JSON string في قاعدة البيانات)
- `normalizePermissions` / `ConvertDictJsonToModuleArray` و كل منطق التحويل

---

## User Review Required

> [!IMPORTANT]
> **تغيير جوهري في هيكل البيانات**: النظام الجديد يستبدل حقل `Permissions` النصي في جدول `Roles` بجداول علائقية. يجب تشغيل Migration جديد وترحيل البيانات الحالية.

> [!WARNING]
> **لا تحديد أدوار بعينها في الكود**: الالتزام الكامل بمبدأ عدم وجود `role === 'admin'` hardcoded. كل الصلاحيات تأتي من قاعدة البيانات عبر `RolePermissions`. الإدارة فقط للـ SystemOwner يمكن أن تكون استثناءً.

> [!CAUTION]
> **ترحيل الأدوار الحالية**: الأدوار المخزنة حالياً بصيغة JSON في `Role.Permissions` يجب ترحيلها إلى `RolePermissions` أثناء Migration.

---

## Open Questions

> [!IMPORTANT]
> **هل يجب الإبقاء على `Role.Permissions` JSON كـ fallback مؤقت؟**
> الخطة الحالية: حذفه تماماً وترحيل كل البيانات.

> [!IMPORTANT]
> **هل SystemOwner يتجاوز نظام الصلاحيات دائماً؟**
> الخطة: نعم — SystemOwner يرى كل شيء دون الحاجة لسجلات في `RolePermissions`.

---

## خريطة الصفحات الكاملة (ModuleDefinitions Seed)

| Key | NameAr | NameEn | Route | Template | Group |
|-----|--------|--------|-------|----------|-------|
| `dashboard` | لوحة التحكم | Dashboard | `/` | Dashboard | main |
| `generators` | المولدات | Generators | `/generators` | Approval | main |
| `tanks` | الخزانات | Tanks | `/tanks` | CRUD | main |
| `oil_inventory` | مخزون الزيوت | Oil Inventory | `/oils` | CRUD | main |
| `fuel_movements` | حركات الوقود | Fuel Movements | `/fuel-movements` | Approval | main |
| `readings` | القراءات | Readings | `/my-generators` | Approval | main |
| `station_reading` | قراءات المحطة | Station Reading | `/stations/:id/reading` | Approval | main |
| `inventory` | الجرد الدوري | Periodic Inventory | `/inventory` | Approval | main |
| `reports` | التقارير | Reports | `/reports` | Report | main |
| `notifications` | التنبيهات | Notifications | `/notifications` | ReadOnly | main |
| `users` | المستخدمون | Users | `/users` | Settings | system |
| `roles` | الأدوار والصلاحيات | Roles & Permissions | `/roles` | Settings | system |
| `locations` | المواقع | Locations | `/locations` | Settings | system |
| `meters` | العدادات | Meters | `/meters` | Settings | system |
| `controllers` | المتحكمات | Controllers | `/controllers` | Settings | system |
| `setup_oils` | إعداد الزيوت | Setup Oils | `/setup-oils` | Settings | system |
| `audits` | سجل التدقيق | Audit Log | `/audits` | ReadOnly | system |
| `rollback` | مراجعة التغييرات | Review & Rollback | `/rollback` | Settings | system |

---

## Proposed Changes

### ─── Layer 1: Domain ───

#### [MODIFY] [Permissions.cs](file:///c:/Users/User/Desktop/FuelTrack-Rebuild/src/FuelTrack.Domain/Models/Permissions.cs)
- حذف `PermissionModule` class و `PermissionModules` static class بالكامل (النظام القديم)
- الاستعاضة بـ `PagePermission` و `ModuleDefinition` الموجودين مسبقاً

#### [MODIFY] [Role.cs](file:///c:/Users/User/Desktop/FuelTrack-Rebuild/src/FuelTrack.Domain/Models/Role.cs)
- إزالة حقل `Permissions` (النص JSON)
- إضافة navigation property: `ICollection<RolePermission> RolePermissions`

---

### ─── Layer 2: Infrastructure ───

#### [MODIFY] [FuelTrackDbContext.cs](file:///c:/Users/User/Desktop/FuelTrack-Rebuild/src/FuelTrack.Infrastructure/FuelTrackDbContext.cs)
- إزالة تهيئة `Role.Permissions` كـ JSON column
- إضافة تهيئة علاقات `RolePermission → Role` و `RolePermission → PagePermission`
- إضافة تهيئة `ModuleDefinition` hierarchy (self-referencing FK)
- إضافة Seed data لـ `ModuleDefinitions` و `PagePermissions`

#### [NEW] Migration: `AddModulePermissionSystem`
- إنشاء جداول `ModuleDefinitions`, `PagePermissions`, `RolePermissions`
- ترحيل بيانات `Role.Permissions` JSON إلى `RolePermissions`
- حذف عمود `Permissions` من جدول `Roles`

#### [MODIFY] [RoleService.cs](file:///c:/Users/User/Desktop/FuelTrack-Rebuild/src/FuelTrack.Infrastructure/Services/RoleService.cs)
- **إزالة** كل منطق: `NormalizeStoredPermissions`, `ConvertPermissions`, `ToFlatKeys`, `PermissionHelper` references
- **إعادة كتابة** `GetAllAsync()`: يجلب الأدوار مع `RolePermissions → PagePermission → ModuleDefinition`
- **إعادة كتابة** `CreateAsync()` / `UpdateAsync()`: يستقبل `List<RolePermissionAssignDto>` ويكتب في `RolePermissions`
- **إعادة كتابة** `GetRolePermissionsAsync()`: يُرجع `List<PagePermissionDto>` مع حالة `IsGranted`
- **إعادة كتابة** `InitAsync()`: يستخدم `ModuleDefinitions` من DB لملء `RolePermissions` للأدوار الافتراضية
- **إعادة كتابة** `CanUserAsync()`: يستعلم `RolePermissions` مباشرةً

#### [MODIFY] [PermissionHelper.cs](file:///c:/Users/User/Desktop/FuelTrack-Rebuild/src/FuelTrack.Application/Helpers/PermissionHelper.cs)
- **حذف** كل الملف (**200+ سطر من منطق JSON التحويل القديم**)
- استبداله بـ `PermissionChecker.cs` بسيط: `HasPermission(List<RolePermission>, moduleKey, actionKey)`

---

### ─── Layer 3: Application ───

#### [DELETE] [Permissions.cs](file:///c:/Users/User/Desktop/FuelTrack-Rebuild/src/FuelTrack.Domain/Models/Permissions.cs)
- ملف `PermissionModule` / `PermissionModules` يُحذف كلياً

#### [MODIFY] [IRoleService.cs](file:///c:/Users/User/Desktop/FuelTrack-Rebuild/src/FuelTrack.Application/Interfaces/IRoleService.cs)
- تحديث signatures لتتوافق مع الجداول الجديدة:
  - `GetModuleDefinitionsAsync()` → ترجع كل الوحدات مع صلاحياتها
  - `AssignPermissionsAsync(int roleId, List<AssignPermissionDto>)` → يستبدل `UpdateRolePermissionsAsync`

#### [NEW] DTOs جديدة في `/DTOs/RBAC/`:
```csharp
// ModuleDefinitionDto.cs
record ModuleDefinitionDto(int Id, string Key, string NameAr, string NameEn, string Route, string Icon, string? ParentKey, string Template, int SortOrder, bool IsVisible, List<PagePermissionDto> Permissions);

// PagePermissionDto.cs
record PagePermissionDto(int Id, int ModuleDefinitionId, string ActionKey, string NameAr, string NameEn, bool IsGranted);

// AssignPermissionsDto.cs
record AssignPermissionsDto(List<PagePermissionGrantDto> Permissions);
record PagePermissionGrantDto(int PagePermissionId, bool IsGranted);
```

#### [MODIFY] [RoleDto.cs](file:///c:/Users/User/Desktop/FuelTrack-Rebuild/src/FuelTrack.Application/DTOs/RBAC/RoleDto.cs)
- تغيير `Permissions` من `string?` إلى `List<PagePermissionDto>` أو `Dictionary<string, Dictionary<string, bool>>`

---

### ─── Layer 4: API Controllers ───

#### [MODIFY] [RolesController.cs](file:///c:/Users/User/Desktop/FuelTrack-Rebuild/src/FuelTrack.API/Controllers/RolesController.cs)
- إضافة endpoint: `GET /api/modules` → يُرجع كل `ModuleDefinitions` مع `PagePermissions`
- تعديل: `GET /api/roles/{id}/permissions` → يُرجع الصلاحيات بصيغة موحدة
- تعديل: `PUT /api/roles/{id}/permissions` → يستقبل `AssignPermissionsDto`
- إزالة: `POST /api/roles/init` أو إعادة كتابتها لتستخدم DB

#### [NEW] [ModulesController.cs](file:///c:/Users/User/Desktop/FuelTrack-Rebuild/src/FuelTrack.API/Controllers/ModulesController.cs)
```
GET /api/modules        → List<ModuleDefinitionDto> (كل الوحدات + صلاحياتها)
GET /api/modules/seed   → POST: زرع/تحديث ModuleDefinitions (Admin only)
```

---

### ─── Layer 5: Auth — تغيير طريقة تحميل الصلاحيات ───

#### [MODIFY] [AuthController.cs / AuthService.cs](file:///c:/Users/User/Desktop/FuelTrack-Rebuild/src/FuelTrack.API/Controllers/AuthController.cs)
- عند تحميل `/api/auth/me`: استعلام `RolePermissions` للمستخدم وإرجاعها بصيغة:
```json
{
  "permissions": {
    "generators": { "view": true, "create": false, "edit": true, ... },
    "dashboard": { "view": true },
    ...
  }
}
```
- تُبنى هذه الـ map من `RolePermissions → PagePermission → ModuleDefinition.Key + ActionKey`

---

### ─── Layer 6: Frontend ───

#### [DELETE] [registry.js](file:///c:/Users/User/Desktop/FuelTrack-Rebuild/frontend/src/permissions/registry.js)
- حذف ملف `registry.js` (يحتوي على MODULES, ACTIONS_META, DEPENDENCIES الثابتة)
- استبداله بـ API call يجلب `ModuleDefinitions` من `/api/modules`

#### [MODIFY] [usePermissions.js](file:///c:/Users/User/Desktop/FuelTrack-Rebuild/frontend/src/permissions/usePermissions.js)
- إزالة `MODULE_ALIASES`, `ACTION_ALIASES` الثابتة
- تبسيط: permissions تأتي من `authStore.user.permissions` كـ flat map مباشر من DB
- `can(module, action)` → تستعلم `permissions[module][action] === true`
- إزالة hardcoded `role === 'admin'` check → استبداله بـ system permission

#### [NEW] `useModules.js` — React hook
```js
// يجلب ModuleDefinitions من /api/modules ويُخزنها في state
// يُستخدم في RolesManagement لبناء مصفوفة الصلاحيات ديناميكياً
export default function useModules() { ... }
```

#### [REWRITE] [RolesManagement.jsx](file:///c:/Users/User/Desktop/FuelTrack-Rebuild/frontend/src/pages/RolesManagement.jsx)
- **إزالة**: `normalizePermissions`, `BACKEND_MODULE_TO_FRONTEND`, `MODULE_ALIASES`, `ACTION_ALIASES` (400 سطر من منطق التحويل)
- **إعادة الكتابة الكاملة** مع:
  - تحميل `ModuleDefinitions` من `GET /api/modules`
  - مصفوفة صلاحيات ديناميكية (لا hard-coded `MODULES` object)
  - `PermissionMatrix` يُبنى من البيانات المُجلبة وليس من `registry.js`
  - حفظ الصلاحيات عبر `PUT /api/roles/{id}/permissions` بصيغة `AssignPermissionsDto`

#### [MODIFY] [App.jsx](file:///c:/Users/User/Desktop/FuelTrack-Rebuild/frontend/src/App.jsx)
- إزالة `OilSetupProtected` والـ hardcoded `module="settings" action="manage"` 
- استبدالها بـ `Protected` عام يستخدم مفاتيح الوحدات المُعرَّفة في `ModuleDefinitions`
- كل route يحصل على `module` key مطابق لـ `ModuleDefinition.Key`

---

## خطة Seeding البيانات (ModuleDefinitions)

سيتم إنشاء Migration يُدرج تلقائياً `ModuleDefinitions` + `PagePermissions` المقابلة:

```
dashboard       → [view]
generators      → [view, create, edit, delete, approve, reject, export, print]
tanks           → [view, create, edit, delete, export, print]
oil_inventory   → [view, create, edit, delete, export, print]
fuel_movements  → [view, create, edit, delete, approve, reject, export, print]
readings        → [view, create, edit, delete, approve, reject, export, print]
station_reading → [view, create, edit, delete, approve, reject]
inventory       → [view, create, edit, delete, approve, reject, export, print]
reports         → [view, export, print]
notifications   → [view, manage]
users           → [view, create, edit, delete, manage]
roles           → [view, create, edit, delete, manage]
locations       → [view, create, edit, delete, manage]
meters          → [view, create, edit, delete, manage]
controllers     → [view, create, edit, delete, manage]
setup_oils      → [view, create, edit, delete, manage]
audits          → [view, export]
rollback        → [view, manage]
```

---

## الملفات المُزالة (Old System Cleanup)

| الملف | السبب |
|-------|-------|
| `src/FuelTrack.Application/Helpers/PermissionHelper.cs` | كل منطق JSON التحويل القديم |
| `src/FuelTrack.Domain/Models/Permissions.cs` | `PermissionModule` + `PermissionModules` القديم |
| `frontend/src/permissions/registry.js` | MODULES الثابتة تُحل بـ API |
| `frontend/src/permissions/useDashboardVisibility.js` | Dashboard widgets تنتقل لـ ModuleDefinitions |
| `frontend/src/permissions/useReportsVisibility.js` | Reports tabs تنتقل لـ ModuleDefinitions |

---

## Verification Plan

### Automated Tests
```bash
# Backend build
dotnet build src/FuelTrack.API

# Run migrations
dotnet ef database update --project src/FuelTrack.Infrastructure --startup-project src/FuelTrack.API

# Frontend build
cd frontend && npm run build
```

### Manual Verification
1. تسجيل الدخول كـ SystemOwner → يرى كل الصفحات
2. إنشاء دور جديد بصلاحيات محدودة → تحقق من ظهور الصفحات المسموحة فقط
3. تعديل صلاحيات دور موجود → تحقق من التطبيق الفوري
4. التحقق من عدم ظهور أي `role === 'admin'` hardcoded في الكود
5. مراجعة جداول قاعدة البيانات: `ModuleDefinitions`, `PagePermissions`, `RolePermissions`

---

## ترتيب التنفيذ (Phases)

```
Phase 1 → Domain: تحديث Role.cs، إزالة Permissions.cs
Phase 2 → Infrastructure: DbContext + Migration + Seed
Phase 3 → Application: DTOs + IRoleService + PermissionChecker
Phase 4 → Infrastructure Services: RoleService rewrite
Phase 5 → API: RolesController + ModulesController + AuthService permissions loading
Phase 6 → Frontend: useModules.js + usePermissions.js + RolesManagement.jsx rewrite + App.jsx
Phase 7 → Cleanup: حذف registry.js + PermissionHelper + القديم
Phase 8 → Build & Verify
```
