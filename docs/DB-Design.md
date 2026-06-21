# Database Design – Phase 1

## 1. فئات الجداول
| الفئة | الوصف | مثال
|-------|-------|------|
| **Master (تأسيسية)** | كيان ثابت، لا يُعدل قيمه تراكمية مباشرة | `Branch`, `Station`, `SubStation`, `Generator`, `FuelTank`, `Oil`, `Controller`, `User`, `Role`, `Meter`
| **Ledger (سجل إلحاق)** | Append‑only, يدعم تصحيح عبر `CorrectedFromId` أو `SupersededById` | `DailyReading`, `FuelMovement`, `OilTransaction`, `PeriodicInventory`, `InventoryFuelReading`, `InventoryOilReading`, `StationReading`
| **Support (مساندة)** | عمليات مساندة، لا تُكتب قيم تجميعية | `Notification`, `AuditLog`, `AnalyticsInsight`, `RefreshToken`, `LoginAttempt`

## 2. قواعد أساسية
- **لا أعمدة تراكمية مخزَّنة** في جداول الـ Master (مثل `TotalOperatingHours`). القيم تُشتق عبر Views.
- **جداول Ledger** تحتوي على حقول تصحيح: `CorrectedFromId` (nullable) و `SupersededById` (nullable).
- **قيد فريد** على تركيبة `Generator + Date + Shift` في `DailyReading` لمنع التكرار بعد الاعتماد.
- **CHECK constraints** لضمان سعة الخزان لا تتجاوز الحد الأقصى.
- **العلاقات**: `Branch 1‑* Station`, `Station 1‑* SubStation`, `SubStation 1‑* Generator`, `Generator 1‑* DailyReading`, الخ.

## 3. مخطط الكيانات (C#)
كل كيان يُعرَّف كـ `record` أو `class` مع الخصائص الأساسية فقط (المفاتيح، العلاقات، الحقول).
- **Master** لا تحتوي على خصائص تجميعية؛ فقط مفاتيح وهوية.
- **Ledger** تشمل `Id`, `CreatedAt`, `ApprovedAt`, `CorrectedFromId`, `SupersededById` وغيرها.
- **Support** تشمل `Id`, `Timestamp`, `Message`, إلخ.

## 4. DbContext
`FuelTrackDbContext` يدمج جميع الـ DbSet، ويُطبق Fluent API لتحديد:
- المفاتيح الأساسية والأجنبية
- الفهارس الفريدة
- القيود (CHECK)
- العلاقات (One‑to‑Many, Many‑to‑Many عبر جداول وصل)
- إعدادات الـ Table‑Per‑Hierarchy للـ inheritance إذا لزم.

## 5. خطوات تنفيذية (ملاحظة للـ CI)
1. إنشاء ملفات الكيانات في `src/FuelTrack.Domain/Models/`.
2. إنشاء ملف `FuelTrackDbContext.cs` في `src/FuelTrack.Infrastructure/`.
3. إضافة EF Core Fluent Configurations إذا أحببت (أو داخل `OnModelCreating`).
4. تشغيل `dotnet ef migrations add InitialCreate` لإنشاء ملف Migration أولي.
5. تشغيل `dotnet ef database update` على قاعدة SQL Server محلية للتحقق.
6. كتابة اختبار وحدة بسيط يضيف سجلين `DailyReading` متتاليين ثم يصحح الأول ويتحقق من تحديث الـ View (اختبار لاحق في Phase 2).

---
**ملاحظة**: لا تنفّذ أي كود إنتاجي الآن، فقط هيكل الملفات والوثائق المطلوبة للـ Phase 1.
