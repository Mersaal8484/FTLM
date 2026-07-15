# إعادة تصميم جدول الصلاحيات — نظام تبويبات احترافي

## الوصف

جدول الصلاحيات الحالي في صفحة `RolesManagement` يعرض **جميع الوحدات (modules) في قائمة مسطحة** مقسّمة فقط إلى مجموعتين (`Main Modules` / `System Settings`). هذا يجعل الجدول طويلاً وصعب التنقل.

الهدف هو **إعادة تنظيم الجدول بنظام تبويبات (tabs)** بحيث تُجمَّع الوحدات ضمن فئات واضحة، مع **الحفاظ الكامل على آلية حفظ وتحديث الصلاحيات كما هي**.

---

## تحليل البنية الحالية

### الوحدات المتاحة (من `ModuleRegistrySeed.cs`)

| الفئة | المفتاح | الاسم |
|-------|---------|-------|
| **لوحة التحكم** | `dashboard` | Dashboard |
| **الإدارة** | `users` | المستخدمين |
| **الإدارة** | `roles` | الأدوار |
| **الإدارة** | `branches` | الفروع |
| **الإدارة** | `stations` | المحطات |
| **الإدارة** | `sub_stations` | المحطات الفرعية |
| **الإدارة** | `generators` | المولدات |
| **الإدارة** | `fuel_tanks` | خزانات الوقود |
| **الإدارة** | `oil_tanks` | خزانات الزيت |
| **الإدارة** | `meters` | العدادات |
| **الإدارة** | `controllers` | وحدات التحكم |
| **الإدارة** | `lookups` | القوائم المرجعية |
| **العمليات** | `daily_readings` | القراءات اليومية |
| **العمليات** | `station_readings` | قراءات المحطة |
| **العمليات** | `fuel_movements` | حركة الوقود |
| **العمليات** | `oil_transactions` | حركات الزيت |
| **العمليات** | `periodic_inventory` | الجرد الدوري |
| **العمليات** | `notifications` | الإشعارات |
| **العمليات** | `rollback_approval` | موافقة التراجع |
| **العمليات** | `review_changes` | مراجعة التغييرات |
| **التقارير** | `reports` | التقارير (رئيسي) |
| **التقارير** | `fuel_report` | تقرير الوقود |
| **التقارير** | `oil_report` | تقرير الزيت |
| **التقارير** | `efficiency_report` | تقرير الكفاءة |
| **التقارير** | `consumption_report` | تقرير الاستهلاك |
| **التقارير** | `inventory_report` | تقرير الجرد |
| **النظام** | `audit_logs` | سجلات التدقيق |
| **النظام** | `settings` | الإعدادات |

---

## هيكل التبويبات المقترح

```
┌─────────────┬──────────────┬──────────────┬──────────────┬──────────────┐
│  Dashboard  │   الإدارة    │   العمليات   │   التقارير   │    النظام    │
└─────────────┴──────────────┴──────────────┴──────────────┴──────────────┘
```

| التبويب | المفاتيح |
|---------|----------|
| 🖥️ **Dashboard** | `dashboard` + widgets |
| ⚙️ **الإدارة** | `users`, `roles`, `branches`, `stations`, `sub_stations`, `generators`, `fuel_tanks`, `oil_tanks`, `meters`, `controllers`, `lookups` |
| 🔧 **العمليات** | `daily_readings`, `station_readings`, `fuel_movements`, `oil_transactions`, `periodic_inventory`, `notifications`, `rollback_approval`, `review_changes` |
| 📊 **التقارير** | `reports`, `fuel_report`, `oil_report`, `efficiency_report`, `consumption_report`, `inventory_report` |
| 🛡️ **النظام** | `audit_logs`, `settings` |

---

## التغييرات المقترحة

### Component: `PermissionMatrix.jsx`

#### [MODIFY] [PermissionMatrix.jsx](file:///c:/Users/User/Desktop/FuelTrack-Rebuild/frontend/src/components/roles/PermissionMatrix.jsx)

**التعديلات الأساسية:**

1. **إضافة `TAB_GROUPS`** — خريطة ثابتة تحدد أي وحدة تنتمي لأي تبويب:
   ```js
   const TAB_GROUPS = [
     { key: 'dashboard', labelAr: 'لوحة التحكم', labelEn: 'Dashboard', icon: '🖥️',
       modules: ['dashboard'] },
     { key: 'admin',    labelAr: 'الإدارة',      labelEn: 'Admin',     icon: '⚙️',
       modules: ['users','roles','branches','stations','sub_stations',
                 'generators','fuel_tanks','oil_tanks','meters','controllers','lookups'] },
     { key: 'ops',      labelAr: 'العمليات',     labelEn: 'Operations', icon: '🔧',
       modules: ['daily_readings','station_readings','fuel_movements',
                 'oil_transactions','periodic_inventory','notifications',
                 'rollback_approval','review_changes'] },
     { key: 'reports',  labelAr: 'التقارير',     labelEn: 'Reports',   icon: '📊',
       modules: ['reports','fuel_report','oil_report','efficiency_report',
                 'consumption_report','inventory_report'] },
     { key: 'system',   labelAr: 'النظام',       labelEn: 'System',    icon: '🛡️',
       modules: ['audit_logs','settings'] },
   ];
   ```

2. **إضافة `activeTab` state** — للتحكم في التبويب النشط.

3. **إضافة شريط التبويبات (Tab Bar)** — قبل الجدول، يعرض:
   - اسم التبويب + أيقونة
   - عداد صغير بعدد الصلاحيات المُفعَّلة في كل تبويب
   - Highlight للتبويب النشط

4. **تصفية الوحدات** — `visibleModules` تُصفَّى بناءً على `activeTab` **وليس** فقط البحث.

5. **زر "تحديد الكل" يعمل على التبويب الحالي** فقط (وليس كل الوحدات).

6. **احتفاظ بـ `buildPermissionEntries`** في الـ parent يبقى كما هو — التغيير في العرض فقط.

7. **تحسينات بصرية:**
   - Tab bar بتصميم pill/underline أنيق
   - Badge عداد صغير على كل تبويب
   - انتقال سلس (CSS transition) عند تغيير التبويب
   - Sub-grouping داخل التبويب (مثلاً داخل "الإدارة": "المواقع" ثم "المعدات")

---

### تحسينات بصرية إضافية

- **رأس الجدول sticky** — يبقى مرئياً عند التمرير
- **Module row** — لون خلفية متناوب لتحسين القراءة
- **Badge "كل الصلاحيات مُفعَّلة"** — إظهار ✓ خضراء عند تفعيل كل صلاحيات وحدة
- **"تحديد كل التبويب" + "إلغاء تحديد كل التبويب"** — أزرار سريعة

---

## ما لن يتغير (مضمون)

- ✅ `buildPermissionEntries()` في `RolesManagement.jsx` — لا تغيير
- ✅ `handleSavePermissions()` و `handleSave()` — لا تغيير
- ✅ API calls (`PUT /api/roles/{id}/permissions`) — لا تغيير
- ✅ منطق `DEPENDENCIES` (view يُفعَّل تلقائياً) — يُنقل ويبقى كما هو
- ✅ `toggleModule`, `toggleColumn`, `selectAll`, `deselectAll` — تبقى لكن تعمل على التبويب النشط
- ✅ `RolesManagement.jsx` الشكل العام — لا تغيير (فقط `PermissionMatrix` يتغير)

---

## خطة التحقق

### البناء
```bash
cd frontend && npm run build
```

### اختبار يدوي
1. فتح صفحة إدارة الأدوار
2. التحقق من ظهور التبويبات بشكل صحيح (5 تبويبات)
3. تغيير الصلاحيات في كل تبويب والتحقق من الحفظ
4. التحقق من أن الصلاحيات المُعطاة تؤثر فعلياً على الوصول في النظام
5. التحقق من عمل البحث ضمن التبويب

---

## أسئلة مفتوحة

> [!IMPORTANT]
> **ترتيب الوحدات داخل تبويب "الإدارة":** هل تريد تقسيمها إلى مجموعات فرعية؟ مثلاً:
> - "إدارة المستخدمين": users, roles
> - "إدارة المواقع": branches, stations, sub_stations  
> - "المعدات": generators, fuel_tanks, oil_tanks, meters, controllers
> - "إعدادات": lookups

> [!NOTE]
> **dashboard widgets** — حاليًا في قاعدة البيانات يوجد وحدات `widget_*` وهي مرتبطة بـ `dashboard` كـ parent. هل تريد عرضها في تبويب Dashboard أم إخفاءها؟ (الأرجح إخفاؤها لأنها غير مرئية `IsVisible = false`)
