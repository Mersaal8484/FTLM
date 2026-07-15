# تحليل نظام FuelTrack-Rebuild — تقرير شامل

> **تاريخ التحليل:** 2026-06-27  
> **المحلل:** Verdent AI — Senior Systems Architect  
> **الإصدار:** .NET 10 LTS + SQL Server 2022 + React 18

---

## 1. نظرة عامة على النظام

FuelTrack هو نظام متكامل لإدارة الوقود والزيت في منشآت توليد الطاقة الكهربائية. يغطي النظام دورة حياة كاملة تشمل:
- إدارة البنية التحتية (فروع → محطات → محطات فرعية → مولدات)
- تتبع مخزون الوقود والزيت
- تسجيل القراءات اليومية للمولدات
- حركات الوقود (استلام، نقل، صرف، إرجاع)
- الجرد الدوري وتقارير الكفاءة
- نظام صلاحيات دقيق (RBAC)
- بوابة Modbus للاتصال بالأجهزة الذكية
- إشعارات فورية عبر SignalR

---

## 2. معمارية النظام

### الطبقات (Clean Architecture)

```
┌─────────────────────────────────────────────────┐
│          FuelTrack.API (ASP.NET Core)            │
│   Controllers │ Middleware │ SignalR Hub         │
└──────────────────────┬──────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────┐
│       FuelTrack.Application (Business)           │
│   DTOs │ Interfaces │ Validators                 │
└──────────┬───────────────────────────────────────┘
           │
┌──────────▼────────────────────────────────────────┐
│      FuelTrack.Infrastructure (Data)               │
│   EF Core │ Services │ Migrations │ Interceptors  │
└──────────┬─────────────────────────────────────────┘
           │
┌──────────▼──────────────────────────┐
│   FuelTrack.Domain (Core Entities)  │
│   Entities │ Enums │ BaseEntity     │
└─────────────────────────────────────┘

     ┌──────────────────────────┐
     │  FuelTrack.ModbusGateway │
     │  Background Worker IoT   │
     └──────────────────────────┘

     ┌──────────────────────────┐
     │  frontend/ (React 18)    │
     │  Arabic RTL SPA          │
     └──────────────────────────┘
```

### التقنيات المستخدمة

| الطبقة | التقنية | الإصدار |
|--------|---------|---------|
| Backend | ASP.NET Core | 10.0 LTS |
| ORM | Entity Framework Core | 10.0.9 |
| Database | SQL Server | 2022 |
| Auth | JWT + HttpOnly Cookies + Identity | — |
| Validation | FluentValidation | 11.x / 12.x |
| Real-time | SignalR | 10.0.9 |
| IoT | NModbus (Modbus TCP) | 3.0.83 |
| Frontend | React 18 | 18.2.0 |
| UI | Bootstrap 5 + React-Bootstrap | 5.2.3 |
| Charts | Chart.js 4 + react-chartjs-2 | 4.3.0 |
| i18n | i18next | 24.2.3 |
| Container | Docker + Docker Compose | — |
| Web Server | nginx | — |
| Tests | xUnit + EF InMemory | 2.9.3 |

---

## 3. نقاط القوة

### 3.1 المعمارية والتصميم

| # | نقطة القوة | التفاصيل |
|---|-----------|---------|
| 1 | **Clean Architecture صارمة** | فصل تام بين الطبقات، Domain لا يعتمد على أي شيء خارجي |
| 2 | **Soft Delete عالمي** | جميع الكيانات (35+) تستخدم `DeletedAt` مع Global Query Filters في EF Core |
| 3 | **Audit Trail تلقائي** | `AuditSaveChangesInterceptor` يسجل كل تغيير (JSON قديم/جديد) تلقائياً دون تدخل يدوي |
| 4 | **RBAC دقيق** | نظام صلاحيات مرن يخزن الإذونات كـ JSON، مع `PermissionGate` component في الواجهة |
| 5 | **DTOs من نوع record** | استخدام `record` types يضمن immutability ويقلل الأخطاء |
| 6 | **Interface-based DI** | جميع الخدمات (25) تُسجَّل عبر interfaces مما يسهل الاختبار والاستبدال |
| 7 | **Fail-fast validation** | التطبيق يرفض الإعدادات الافتراضية الضعيفة في بيئة الإنتاج |

### 3.2 قاعدة البيانات

| # | نقطة القوة | التفاصيل |
|---|-----------|---------|
| 8 | **Unique Indexes** | مؤشرات فريدة على 11 زوج من الأعمدة الحرجة (GeneratorId+ReadingDate إلخ) |
| 9 | **Restrict Delete** | سياسة `OnDelete(Restrict)` تمنع حذف البيانات المرتبطة بالخطأ |
| 10 | **Seed Data** | بذر 10 LookupCategories و~50 Lookup value في المهاجرة مباشرة |
| 11 | **SQL Views** | `v_SubStationFuelStock` و`v_SubStationOilStock` لحساب المخزون المتراكم |

### 3.3 الأمان

| # | نقطة القوة | التفاصيل |
|---|-----------|---------|
| 12 | **HttpOnly Cookies** | رموز JWT في Cookies محمية (لا تُكشف لـ JavaScript) |
| 13 | **Token Refresh** | Axios interceptor يجدد التوكن تلقائياً عند انتهاء الصلاحية |
| 14 | **Rate Limiting** | حد 5 طلبات/دقيقة على `/login` لمنع هجمات التخمين |
| 15 | **Login Attempts** | سجل محاولات تسجيل الدخول مع IP Address |

### 3.4 الواجهة الأمامية

| # | نقطة القوة | التفاصيل |
|---|-----------|---------|
| 16 | **RTL و i18n** | دعم كامل للغة العربية (RTL) مع i18next |
| 17 | **Real-time via SignalR** | تحديثات فورية عبر Socket.IO client |
| 18 | **Analytics Engine** | محرك تحليلات من جانب العميل + خادم لتوليد الرؤى الذكية |
| 19 | **Permission-aware UI** | `PermissionGate` يخفي/يُظهر العناصر بناءً على صلاحيات المستخدم |

### 3.5 التشغيل والنشر

| # | نقطة القوة | التفاصيل |
|---|-----------|---------|
| 20 | **Docker-ready** | `docker-compose.yml` جاهز بـ SQL Server + API + nginx |
| 21 | **Health Checks** | `/healthz/live` و `/healthz/ready` مع EF Core DB check |
| 22 | **Modbus Gateway** | Worker Service مستقل للاتصال بالأجهزة الذكية |

---

## 4. نقاط الضعف والأخطاء

### 4.1 أخطاء حرجة (تمنع الاشتغال)

| # | الخطأ | الملف المتأثر | الأثر |
|---|-------|--------------|-------|
| **C1** | `FuelMovementService.cs` **غير موجود** — الـ interface محدد والـ DI مسجل لكن لا يوجد implementation | `IFuelMovementService` | تعطل صفحة حركات الوقود بالكامل عند التشغيل |
| **C2** | `FuelMovementsController.cs` **غير موجود** — لا يوجد Controller للـ endpoint رغم وجود الـ interface والـ frontend route `/fuel-movements` | — | صفحة حركات الوقود تُعيد 404 لجميع طلبات API |
| **C3** | **Tests لا تُترجَم** — ملف `ReadingServiceTests.cs` يستخدم `Guid.NewGuid()` للـ IDs لكن Domain يستخدم `int` | `tests/.../ReadingServiceTests.cs` | فشل compilation كامل لمشروع الاختبارات |

### 4.2 أخطاء متوسطة (تؤثر على وظائف معينة)

| # | الخطأ | الملف المتأثر | الأثر |
|---|-------|--------------|-------|
| **M1** | **حساب الكفاءة خاطئ** — `totalFuel = Sum(StandardFuelRate)` بدلاً من `Sum(Rate × Hours)` | `EfficiencyService.cs` ≈ line 33 | أرقام تقارير الكفاءة خاطئة تماماً |
| **M2** | **SQL Views غير مُطبقة** — `v_SubStationFuelStock` و`v_SubStationOilStock` ليستا في أي Migration | `SqlViews.sql` | المخزون لا يُحسب صحيحاً (يُعيد null أو خطأ) |
| **M3** | `POST /{id}/correct` في `ReadingsController` **يُعيد `BadRequest` مباشرة** | `ReadingsController.cs` line 66-69 | ميزة تصحيح القراءات معطلة |
| **M4** | `AuthController.Logout` **يتجاوز Service Layer** ويستعلم DbContext مباشرة | `AuthController.cs` line 126-133 | انتهاك معمارية + صعوبة اختبار |
| **M5** | **لا يوجد endpoint لاعتماد الجرد الدوري** — `PeriodicInventory` يحتوي `IsApproved` لكن لا يوجد `POST /{id}/approve` | `InventoryController.cs` | عملية الاعتماد معطلة في الواجهة |
| **M6** | **`GenerateInsightsAsync` غير مكشوف** عبر HTTP — لا يوجد endpoint له | `AnalyticsInsightsController.cs` | توليد الرؤى الذكية لا يعمل من الواجهة |
| **M7** | **`IPeriodicInventoryService` ناقص** — لا يوجد `UpdateAsync`, `DeleteAsync`, `ApproveAsync` | `IPeriodicInventoryService.cs` | لا يمكن تعديل أو حذف أو اعتماد الجرد |
| **M8** | **Validators ناقصة** لـ Controllers, Screens, Users, Roles, Inventory, StationReadings | validators directory | بيانات غير مُتحقق منها قد تُدخل قاعدة البيانات |

### 4.3 مشاكل الواجهة الأمامية (Frontend)

| # | المشكلة | الصفحة | الأثر |
|---|---------|--------|-------|
| **F1** | **صفحة FuelMovements تُظهر 404/خطأ** لغياب الـ Controller والـ Service | `/fuel-movements` | الصفحة لا تعمل إطلاقاً |
| **F2** | **عرض IDs الخام** — بعض الصفحات تعرض `FuelLookupId: 5` بدلاً من اسم الوقود | متعددة | تجربة مستخدم سيئة |
| **F3** | **نماذج الإضافة لا تُرسل بعض الحقول** لبعض الكيانات بسبب أسماء حقول غير متطابقة بين Frontend و DTO | متعددة | فشل عمليات Create/Update |
| **F4** | **عدم تحديث القوائم المنسدلة (dropdowns)** ببيانات Lookup الفعلية من الـ API | نماذج متعددة | اختيارات ثابتة بدلاً من ديناميكية |
| **F5** | **صفحة التقارير** — بعض بيانات Charts تعتمد على SQL Views غير مُطبقة | `/reports` | رسوم بيانية فارغة أو بيانات خاطئة |
| **F6** | **عمليات الحذف لا تُظهر رسالة تأكيد** في بعض الصفحات | متعددة | حذف غير مقصود |
| **F7** | **صفحة التفاصيل (Detail)** لا تُظهر كل الحقول المُعادة من الـ API في بعض الكيانات | متعددة | بيانات مخفية |

### 4.4 مشاكل الكود والديون التقنية

| # | المشكلة | الموقع |
|---|---------|--------|
| **T1** | `temp_models/` — 30+ ملف scaffold من قاعدة بيانات قديمة، **كود ميت** | `Infrastructure/temp_models/` |
| **T2** | **Domain Enums غير مستخدمة** — 9 enums موجودة لكن Domain يستخدم `int` FK للـ Lookups | `Domain/Enums/` |
| **T3** | ملفات log مُضافة لـ Git (`api_stdout.log`, `api_stderr.log`, `fueltrack.db`) | root directory |
| **T4** | ملفات utility مُضافة لـ Git (`verify.js`, `check-unused*.js`) | root directory |
| **T5** | **JWT Key ضعيف** في `appsettings.json` الأساسي (ليس Development فقط) | `appsettings.json` |
| **T6** | **لا يوجد `.gitignore` صحيح** يستبعد الملفات المذكورة | root |
| **T7** | **مشروع Mobile** موجود كمجلد لكن فارغ/غير مكتمل | `mobile/` |

---

## 5. تقييم الأداء المتوقع

| المجال | التقييم | الملاحظة |
|--------|---------|---------|
| **معمارية النظام** | 8.5/10 | Clean Architecture جيدة مع بعض الانتهاكات |
| **قاعدة البيانات** | 7/10 | Schema جيد لكن Views غير مُطبقة |
| **الأمان** | 6.5/10 | JWT صحيح لكن secrets ضعيفة في dev |
| **الواجهة الأمامية** | 5.5/10 | تصميم جيد لكن أخطاء في CRUD وعرض البيانات |
| **الاختبارات** | 2/10 | لا تُترجَم بسبب خطأ أنواع IDs |
| **التوثيق** | 7/10 | README وAGENTS.md جيدة |
| **الجاهزية للإنتاج** | 4/10 | ميزات حرجة ناقصة (FuelMovements) |

---

## 6. التحسينات المقترحة

### 6.1 تحسينات الأداء

| # | التحسين | الأولوية |
|---|---------|---------|
| P1 | إضافة **Caching** (Redis أو In-Memory) للبيانات الثابتة (Lookups, Branches) | عالية |
| P2 | إضافة **Pagination** لجميع قوائم الـ API (بعضها يُعيد كل السجلات) | عالية |
| P3 | إضافة **Indexes** على `ReadingDate`, `TransactionDate` للاستعلامات الزمنية | متوسطة |
| P4 | استخدام **AsNoTracking()** في استعلامات القراءة فقط | متوسطة |
| P5 | **Lazy Loading → Eager Loading** مع Select محدد لتجنب N+1 | متوسطة |

### 6.2 تحسينات الأمان

| # | التحسين | الأولوية |
|---|---------|---------|
| S1 | نقل Secrets إلى **Environment Variables** أو **Azure Key Vault** | حرجة |
| S2 | إضافة **HTTPS Redirection** إجباري في الإنتاج | عالية |
| S3 | إضافة **Account Lockout** بعد عدد من المحاولات الفاشلة | عالية |
| S4 | إضافة **Content Security Policy** headers | متوسطة |
| S5 | تفعيل **Two-Factor Authentication** للمستخدمين ذوي الصلاحيات العالية | متوسطة |

### 6.3 تحسينات وظيفية

| # | التحسين | الأولوية |
|---|---------|---------|
| F1 | إكمال **FuelMovementService + Controller** (ميزة حرجة مفقودة) | حرجة |
| F2 | تطبيق **SQL Views** عبر Migration | حرجة |
| F3 | إصلاح **حساب الكفاءة** في EfficiencyService | عالية |
| F4 | إكمال **PeriodicInventory CRUD** (Update, Delete, Approve) | عالية |
| F5 | إضافة **Validators** للكيانات الناقصة | عالية |
| F6 | إضافة **Export to Excel/PDF** للتقارير (بنية xlsx-js-style موجودة) | متوسطة |
| F7 | إضافة **Bulk Operations** (قراءات جماعية للمولدات) | منخفضة |

### 6.4 تحسينات الكود

| # | التحسين | الأولوية |
|---|---------|---------|
| C1 | حذف `temp_models/` من المشروع | عالية |
| C2 | إصلاح Test Suite (تغيير Guid → int) | عالية |
| C3 | نقل DbContext access من AuthController إلى AuthService | متوسطة |
| C4 | إضافة **Repository Pattern** أو حفظ DbContext في Protected | منخفضة |
| C5 | توحيد معالجة الأخطاء في **Global Exception Middleware** | متوسطة |
| C6 | تحديث `.gitignore` لاستبعاد ملفات Dev | منخفضة |

---

## 7. خريطة الكيانات والعلاقات

```
Branch (1) ──────────────── (*) Station
Station (1) ─────────────── (*) SubStation
SubStation (1) ───────────── (*) Generator
SubStation (1) ───────────── (*) FuelTank
SubStation (1) ───────────── (*) OilTank
SubStation (1) ───────────── (*) Meter
SubStation (1) ───────────── (*) Controller
SubStation (1) ───────────── (*) UserSubStation
SubStation (1) ───────────── (*) SubStationReading
SubStation (1) ───────────── (*) FuelTransaction
SubStation (1) ───────────── (*) OilTransaction
SubStation (1) ───────────── (*) PeriodicInventory

Generator (1) ────────────── (*) DailyReading
Generator (1) ────────────── (*) UserGenerator
Generator (1) ────────────── (*) GeneratorOil
Generator (1) ────────────── (*) GeneratorFuel
Generator (1?) ───────────── (1?) FuelTank (Default)
Generator (1?) ───────────── (1?) OilTank (Default)
Generator (1?) ───────────── (1?) Meter

User (1) ─────────────────── (*) UserSubStation
User (1) ─────────────────── (*) UserGenerator
User (1) ─────────────────── (*) NotificationRecipient
User (1) ─────────────────── (*) RefreshToken
User (1) ─────────────────── (*) AuditLog

Lookup (1) ───────────────── (*) FuelTank (FuelType)
Lookup (1) ───────────────── (*) OilTank (OilType)
Lookup (1) ───────────────── (*) DailyReading (FuelType)
Lookup (1) ───────────────── (*) DailyReading (OilType)
Lookup (1) ───────────────── (*) FuelTransaction (FuelType)
Lookup (1) ───────────────── (*) FuelTransaction (TransactionType)
LookupCategory (1) ────────── (*) Lookup
```

---

## 8. ملخص القرار

النظام يمتلك **أساس معماري ممتاز** وإمكانيات وظيفية ثرية، لكنه يعاني من **فجوات تنفيذية حرجة** تمنع إطلاقه في بيئة الإنتاج. الأولوية القصوى يجب أن تكون:

1. إكمال `FuelMovementService` و`FuelMovementsController`
2. تطبيق SQL Views في Migrations
3. إصلاح Test Suite
4. إصلاح حساب الكفاءة
5. إصلاح عرض البيانات في الواجهة الأمامية
