# FuelTrack — نظرة شاملة على النظام

> **تاريخ التحليل**: 2026-07-01  
> **الإصدار الحالي**: 1.0 (Rebuild Phase)  
> **حالة البناء**: يعمل — 79 اختبار ناجح، 10 متجاهلة (SQL Server)

---

## 1. ما هو FuelTrack؟

FuelTrack هو نظام إدارة الوقود والطاقة للمحطات الكهربائية والمحطات الفرعية. يُمكّن المؤسسات الكبيرة من:

- **تتبع المولدات** — البيانات اليومية، معدلات الكفاءة، استهلاك الوقود والزيت
- **إدارة المخزون** — حركات الوقود (وارد/تحويل/خسارة)، المخزون الدوري، خزانات الوقود والزيت
- **قراءات المحطات** — طاقة الإصدار (kWh) من عدادات الربط
- **التحليلات والتقارير** — تقارير يومية/دورية، مؤشرات الأداء الرئيسية، رؤى ذكية
- **إدارة المستخدمين والأدوار** — RBAC دقيق بصلاحيات قابلة للتكوين
- **التكامل مع الأجهزة** — قراءة عدادات Modbus TCP تلقائياً

---

## 2. معمارية النظام

```
┌─────────────────────────────────────────────────────────┐
│                    Frontend (React 18)                  │
│         24 صفحة + SignalR + RBAC + i18n (عربي)         │
└──────────────────────┬──────────────────────────────────┘
                       │ HTTP/WebSocket
┌──────────────────────▼──────────────────────────────────┐
│              FuelTrack.API (.NET 10 LTS)                │
│  25 Controller + SignalR Hub + JWT + Middleware + Rate  │
└──────────┬────────────────────────┬──────────────────────┘
           │                        │
┌──────────▼──────────┐  ┌──────────▼──────────────────────┐
│ FuelTrack.Application│  │    FuelTrack.Infrastructure     │
│ 26 Interface + DTOs  │  │  24 Service + EF Core + Audit  │
│ FluentValidation     │  │  AuditSaveChangesInterceptor   │
└──────────┬───────────┘  └──────────┬───────────────────────┘
           │                          │
┌──────────▼──────────────────────────▼──────────────────┐
│                  FuelTrack.Domain                       │
│           33 Entity + BaseEntity (Soft Delete)         │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│              SQL Server 2022 (Docker)                   │
│   20 Migration + SQL Views + Performance Indexes        │
└─────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│           FuelTrack.ModbusGateway (Background Worker)   │
│      Modbus TCP Polling → SignalR Hub Push Events       │
└──────────────────────────────────────────────────────────┘
```

**نمط المعمارية**: Clean Architecture (Domain → Application → Infrastructure → API)

---

## 3. المكونات الرئيسية

### 3.1 المشاريع (5 projects)

| المشروع | الغرض | التبعيات |
|---------|--------|-----------|
| `FuelTrack.Domain` | النماذج الأساسية، لا تبعيات خارجية | Identity.Stores فقط |
| `FuelTrack.Application` | DTOs، Interfaces، FluentValidation | Domain فقط |
| `FuelTrack.Infrastructure` | EF Core، الخدمات، Migrations | Domain + Application |
| `FuelTrack.API` | Controllers، Middleware، SignalR Hub | Application + Infrastructure |
| `FuelTrack.ModbusGateway` | خدمة IoT للقراءة التلقائية | Domain + Infrastructure |

### 3.2 الكيانات (33 entity)

**التسلسل الهرمي الجغرافي**:
```
Branch → Station → SubStation → Generator
                              → FuelTank
                              → Meter
                              → Device
```

**جداول المعاملات**:
- `DailyReading` — القراءات اليومية للمولدات
- `SubStationReading` — قراءات الطاقة للمحطات الفرعية
- `FuelTransaction` — حركات الوقود (وارد/تحويل/تصحيح)
- `OilTransaction` — حركات الزيت
- `PeriodicInventory` + `PeriodicInventoryFuel` + `PeriodicInventoryOil` — المخزون الدوري

**النظام المساند**:
- `LookupCategory` + `Lookup` — بديل ENUMs (نظام بحث قابل للتوسع)
- `AuditLog` — سجل كامل للتغييرات
- `Notification` + `NotificationRecipient` — نظام إشعارات
- `AnalyticsInsight` + `InsightFeedback` — رؤى ذكية
- `RefreshToken` + `LoginAttempt` — أمان المصادقة

### 3.3 Stack التقني

| الطبقة | التقنية | الإصدار |
|--------|---------|---------|
| Backend | ASP.NET Core | .NET 10.0 LTS |
| ORM | Entity Framework Core | 10.0.9 |
| قاعدة البيانات | SQL Server 2022 | Docker |
| المصادقة | JWT + HttpOnly Cookies + Identity | — |
| Real-time | ASP.NET Core SignalR | 10.0.9 |
| التحقق | FluentValidation | 11-12 |
| IoT | NModbus (Modbus TCP) | 3.0.83 |
| Frontend | React 18 + Bootstrap 5 | CRA |
| HTTP Client | Axios (مع auto-refresh) | 1.4.0 |
| i18n | i18next | 24.2.3 |
| تصدير Excel | xlsx-js-style | 1.2.0 |
| الحاويات | Docker + docker-compose | — |

---

## 4. نقاط القوة

### 4.1 المعمارية والتصميم
- **Clean Architecture صارم** — الفصل الكامل بين الطبقات، Domain لا يعتمد على أي شيء
- **Lookup System بدلاً من ENUMs** — كل القيم الفئوية قابلة للتوسع دون migrations جديدة
- **Soft Delete شامل** — جميع الكيانات الـ33 لها `DeletedAt` مع Global Query Filters تلقائية
- **BaseEntity موحد** — `CreatedAt/By`، `UpdatedAt/By`، `IsActive` على كل كيان
- **Audit Interceptor تلقائي** — كل تغيير في DB يُسجل في `AuditLog` تلقائياً بدون كود إضافي
- **SQL Views** للحسابات المعقدة (`v_SubStationFuelStock`، `v_SubStationOilStock`)

### 4.2 الأمان
- **Dual-token strategy** — JWT قصير الأجل (60 دقيقة) + Refresh Token مخزن مُشفر (SHA-256)
- **Refresh Token Rotation** — كل تجديد يُلغي القديم؛ إعادة الاستخدام تُلغي جميع التوكنات
- **HttpOnly Cookies** — التوكنات محمية من XSS
- **Rate Limiting** على نقاط Auth (20 طلب/دقيقة)
- **Fail-fast guard** — التطبيق يرفض البدء في الإنتاج بإعدادات dev افتراضية
- **Login Attempt Logging** — تسجيل محاولات الدخول مع قفل بعد 5 فشل

### 4.3 Real-time وIoT
- **SignalR مع 25+ حدث** — تحديثات فورية لجميع العمليات
- **Group subscriptions** — المستخدمون يشتركون في groups محددة (station/substation)
- **ModbusGateway** — خدمة خلفية مستقلة للقراءة التلقائية من عدادات Modbus TCP
- **SignalR JWT منفصل** — توكن مخصص للـHub بصلاحيات وانتهاء صلاحية أقصر

### 4.4 جودة الكود
- **FluentValidation** — تحقق صارم لجميع DTOs الرئيسية
- **PaginationResult** موحد في جميع الاستجابات المُقسمة
- **Kebab-case routes** — عبر middleware مخصص
- **Exception Handling Middleware** — معالجة مركزية للأخطاء
- **Health Checks** — EF Core health check مُسجل
- **Performance Indexes** — Composite indexes لأنماط الاستعلام الأكثر شيوعاً
- **AsNoTracking** مُطبق عبر 27+ استعلام قراءة

### 4.5 Frontend
- **RBAC دقيق** — `usePermissions` hook مع `PermissionGate` component
- **Auto token refresh** — queue-based مع منع التجديدات المتزامنة المتعددة
- **i18n** — دعم متعدد اللغات (عربي)
- **تصدير Excel** — دعم تصدير التقارير
- **Analytics Engine** — محرك تحليل جانب العميل + محرك توقع

---

## 5. نقاط الضعف

### 5.1 نقاط ضعف حرجة (Critical)

| # | المشكلة | الموقع | التأثير |
|---|---------|--------|---------|
| W-01 | **مفتاح JWT placeholder** في `appsettings.json` (`"UniqueSecureKey-2026-ReplaceMe-Now!"`) — الـguard لا يتحقق منه | `appsettings.json` | ثغرة أمنية حرجة في الإنتاج |
| W-02 | **`IDeviceService` بلا تطبيق** — تم حذف Device و UserDevice من المشروع بالكامل | — | تم حذفه |
| W-03 | **`ModbusGateway` يُرسل لـHub لكن لا handler على السيرفر** — `SendModbusReading` يذهب إلى الفراغ | `FuelTrackHub.cs` | بيانات IoT تُفقد كلياً |
| W-04 | **لا SSL/TLS في docker-compose** — الاتصال بين الخدمات غير مشفر | `docker-compose.yml` | ثغرة في بيئة الإنتاج |
| W-05 | **`Role.Permissions` كـstring مفصول بفاصلة** — لا type safety، صعب الاستعلام | `Role.cs` | هشاشة في نظام الصلاحيات |

### 5.2 نقاط ضعف عالية الأولوية (High)

| # | المشكلة | الموقع | التأثير |
|---|---------|--------|---------|
| W-06 | **لا Caching** — كل طلب يضرب DB مباشرة، حتى البيانات الثابتة كـLookups | جميع الخدمات | أداء ضعيف تحت الحمل |
| W-07 | **لا Background Jobs** — التنبيهات والرؤى والتنظيف تتم يدوياً | Infrastructure | عمليات يدوية قابلة للأتمتة |
| W-08 | **SQLite في package references الإنتاج** — `Microsoft.EntityFrameworkCore.Sqlite` مُضمن في Infrastructure | `.csproj` | bloat في الإنتاج |
| W-09 | **لا Distributed Logging** — لا Serilog/Seq/ELK، اعتماد على `ILogger` فقط | API | صعوبة تتبع المشاكل في الإنتاج |
| W-10 | **استجابات عربية في API** — `"تم الاعتماد بنجاح"` في `InventoryController` | `InventoryController.cs` | عدم اتساق API |
| W-11 | **`AuditLog.EntityId` كـint** — الـRollback Service يستخدم Reflection للبحث، هش وبطيء | `RollbackService.cs` | صعوبة الصيانة |
| W-12 | **لا API Versioning** — أي تغيير breaking في API يكسر جميع العملاء | `Program.cs` | خطر في التطوير المستقبلي |

### 5.3 نقاط ضعف متوسطة (Medium)

| # | المشكلة | الموقع | التأثير |
|---|---------|--------|---------|
| W-13 | **تغطية الاختبارات محدودة** — 3 frontend tests فقط، لا integration tests حقيقية | `tests/` | خطر انحدار غير مكتشف |
| W-14 | **لا OpenAPI Spec المُصدر** — Swagger موجود لكن لا client generation | `Program.cs` | تطوير Frontend يدوي |
| W-15 | **لا global error boundary في React** — أخطاء JS تُخفق صامتة | `App.js` | تجربة مستخدم سيئة |
| W-16 | **حقل مزدوج للأنواع** — `QuantityLiters` double في Transactions، `CurrentStock` decimal في Oil | Domain | عدم اتساق نوع البيانات |
| W-17 | **`ReadingCorrectionDto` يتيم** — validator موجود، controller endpoint محذوف | Application | كود ميت |
| W-18 | **`Screen` entity بلا استخدام فعلي** — صفحة frontend لا تستهلكه بشكل واضح | API/Frontend | كود غير مستخدم |
| W-19 | **لا Pagination في بعض Endpoints** — بعض endpoints تُعيد جميع السجلات | Controllers | خطر أداء مع البيانات الكبيرة |
| W-20 | **CORS hardcoded** — localhost في CORS بدلاً من Environment Variables | `Program.cs` | تكوين غير مرن |
| W-21 | **CreatedBy = 0 في Register** — Workaround بدلاً من حل صحيح | `AuthController.cs` | بيانات غير صحيحة |

---

## 6. نقاط قابلة للتحسين

### 6.1 أداء وقابلية التوسع

| # | التحسين | الأثر المتوقع |
|---|---------|--------------|
| I-01 | **Redis Caching** — تخزين Lookups, Roles, Permissions مؤقتاً (TTL 5-15 دقيقة) | -70% طلبات DB للبيانات الثابتة |
| I-02 | **Hangfire/Quartz Jobs** — تنظيف التوكنات المنتهية، إنشاء الرؤى، إرسال التنبيهات | أتمتة كاملة للعمليات الدورية |
| I-03 | **Cursor-based Pagination** بدلاً من offset لجداول كبيرة | أداء ثابت مع تزايد البيانات |
| I-04 | **Read Replicas** — فصل read/write لـReports | تقليل الضغط على DB الرئيسية |
| I-05 | **Response Compression** — gzip/br للاستجابات الكبيرة | -60% حجم البيانات المنقولة |

### 6.2 موثوقية وأمان

| # | التحسين | الأثر المتوقع |
|---|---------|--------------|
| I-06 | **Distributed Tracing** — OpenTelemetry + Jaeger/Zipkin | تتبع كامل للطلبات عبر الخدمات |
| I-07 | **Structured Logging** — Serilog → Seq أو ELK Stack | بحث وتحليل السجلات في الوقت الفعلي |
| I-08 | **Secrets Management** — Azure Key Vault / HashiCorp Vault | إزالة كل secrets من config files |
| I-09 | **HTTPS Everywhere** — TLS termination في nginx حتى في dev | حماية البيانات في النقل |
| I-10 | **Input Sanitization** — DOMPurify في React، Regex على API inputs | حماية من XSS/injection |
| I-11 | **API Versioning** — `/api/v1/...` عبر `Asp.Versioning` | تطور API بدون كسر العملاء |
| I-12 | **2FA/MFA Support** — TOTP للمستخدمين الحرجين | مستوى أمان إضافي |

### 6.3 صيانة وجودة الكود

| # | التحسين | الأثر المتوقع |
|---|---------|--------------|
| I-13 | **Result Pattern / OneOf** — بدلاً من exceptions للتحكم بالتدفق | كود أنظف وأقل exceptions |
| I-14 | **AutoMapper / Mapster** — بدلاً من manual mapping في الخدمات | -40% كود mapping |
| I-15 | **Global Exception Filter** في React | منع crash الصفحة |
| I-16 | **E2E Tests** — Playwright أو Cypress | ثقة أعلى في deployments |
| I-17 | **Null-safety Annotations** — `#nullable enable` في جميع المشاريع | منع NRE في وقت التطوير |
| I-18 | **OpenAPI Code Generation** — NSwag/Kiota لـgenerate API client | مزامنة Frontend/Backend تلقائية |

### 6.4 ميزات مفقودة للمؤسسات

| # | الميزة | الأهمية |
|---|--------|---------|
| I-19 | **Multi-tenancy** — عزل بيانات الشركات المختلفة | حرج إذا تعدد العملاء |
| I-20 | **Export to PDF** للتقارير | طلب شائع في المؤسسات |
| I-21 | **Scheduled Reports** — إرسال تقارير دورية بالبريد | أتمتة التقارير |
| I-22 | **Mobile App / PWA** | وصول ميداني |
| I-23 | **Data Retention Policy** — حذف/أرشفة بيانات قديمة تلقائياً | إدارة حجم DB |
| I-24 | **Bulk Import** — رفع Excel/CSV للبيانات التاريخية | Onboarding سريع |
| I-25 | **Geolocation** على المحطات | خرائط وتقارير جغرافية |
| I-26 | **Two-way Modbus** — كتابة على عدادات من النظام | تحكم كامل في الأجهزة |

---

## 7. ملخص التقييم

```
┌──────────────────────────────────────────────────┐
│           تقييم جاهزية الإنتاج                  │
├─────────────────────┬────────────────────────────┤
│ المعمارية           │  ████████░░  80%  ✅        │
│ الأمان             │  ██████░░░░  60%  ⚠️        │
│ الأداء             │  █████░░░░░  50%  ⚠️        │
│ تغطية الاختبارات   │  ████░░░░░░  40%  ❌        │
│ المراقبة/Logging   │  ██░░░░░░░░  20%  ❌        │
│ التوثيق            │  ██████░░░░  60%  ⚠️        │
│ الميزات الوظيفية   │  ████████░░  80%  ✅        │
│ تجربة المستخدم     │  ███████░░░  70%  ✅        │
└─────────────────────┴────────────────────────────┘
  التقييم العام: 57% — قابل للإنتاج بعد معالجة الحرجيات
```

### أولوية العمل المقترحة
1. **فورياً**: W-01 (JWT key)، W-03 (Modbus handler)، W-04 (HTTPS)
2. **قريباً**: I-07 (Logging)، I-01 (Caching)، I-11 (API Versioning)، I-02 (Jobs)
3. **متوسط المدى**: I-06 (Tracing)، I-08 (Secrets)، I-16 (E2E Tests)
4. **طويل المدى**: I-19 (Multi-tenancy)، I-21 (Scheduled Reports)، I-22 (PWA)
