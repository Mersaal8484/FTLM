# برومبت نظام الصيانة وإدارة المخازن — FuelTrack Extension

---

## دورك ومهمتك

أنت مهندس برمجيات أول وخبير في تحليل وتصميم الأنظمة بخبرة تزيد عن 20 عاماً في بناء أنظمة ERP وأنظمة إدارة العمليات الصناعية. لديك إتقان عميق في Clean Architecture وDomain-Driven Design وEntity Framework Core وASP.NET Core وReact.

مطلوب منك **تصميم وبناء امتداد كامل** لنظام FuelTrack القائم يضيف إليه نظامَي **إدارة المخازن** و**الصيانة** بشكل متكامل. يجب أن يكون الامتداد الجديد **متوافقاً تماماً** مع المعمارية الحالية وغير كاسر لأي وظيفة قائمة.

---

## السياق الكامل للنظام القائم

### المعمارية
- **نمط:** Clean Architecture (4 طبقات صارمة)
- **Backend:** ASP.NET Core 10.0 LTS + EF Core 10 + SQL Server 2022
- **Frontend:** React 18 SPA — Arabic RTL — i18next — Bootstrap 5 — react-bootstrap
- **Auth:** JWT Bearer + HttpOnly Cookies + ASP.NET Core Identity<User, Role, int>
- **Real-time:** SignalR Hub على `/hubs/fuel-track`
- **PKs:** جميع الكيانات تستخدم `int` (ليس Guid)
- **Soft Delete:** جميع الكيانات تحتوي `DeletedAt?` مع Global Query Filter في EF Core
- **Audit:** `AuditSaveChangesInterceptor` يسجل كل تعديل تلقائياً في جدول `AuditLogs`
- **DTOs:** جميعها من نوع C# `record`
- **Validation:** FluentValidation مع auto-registration

### هيكل الطبقات
```
FuelTrack.Domain/Models/         ← Entities فقط — لا dependencies خارجية
FuelTrack.Application/           ← DTOs + Interfaces + Validators
FuelTrack.Infrastructure/Services/ ← تنفيذ الـ Services + EF Core
FuelTrack.API/Controllers/       ← Controllers + Middleware + SignalR
frontend/src/pages/              ← صفحات React
frontend/src/api/client.js       ← Axios instance مع interceptors
```

### الكيانات الحالية ذات الصلة
```
Branch (فرع/محافظة) 
  └── Station (محطة) ← وحدة التحكم في المخازن
        └── SubStation (موقع/محطة فرعية)
              └── Generator (مولد)
                    └── DailyReading (قراءات يومية)
User (مستخدم: IdentityUser<int>)
Role (دور: IdentityRole<int>) — يحتوي Permissions كـ JSON string
Lookup / LookupCategory ← نظام القوائم المرجعية المركزي
BaseEntity ← { Id:int, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, DeletedAt? }
```

### نمط الـ BaseEntity (يجب الالتزام به في كل كيان جديد)
```csharp
public abstract class BaseEntity
{
    public int Id { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}
```

### نمط تسجيل الخدمات (Program.cs)
```csharp
builder.Services.AddScoped<IXxxService, XxxService>();
```

### نمط الـ Controller الأساسي
```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected string[] GetUserRoles() => User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
}
```

### Authorization Policies الموجودة
- `AdminOnly` — Admin فقط
- `AdminOrOperator` — Admin أو Operator
- `AdminOrManager` — Admin أو Manager

---

## المتطلبات الوظيفية الكاملة

---

### النظام الأول: إدارة المخازن (Warehouse Management)

**قاعدة العمل الأساسية:** المخزن يعمل على مستوى المحطة (Station) — التوريد مركزي من المحطة — الصرف للمواقع (SubStation) — المحاسبة على المولد.

#### 1.1 كتالوج قطع الغيار (Spare Parts Catalog)
نظام مركزي لتسجيل جميع أنواع القطع المستخدمة في صيانة المولدات.

**البيانات المطلوبة لكل قطعة:**
- الاسم العربي والإنجليزي
- رقم القطعة (Part Number) — مميز
- نوع القطعة (Lookup: ميكانيكي/كهربائي/إلكتروني/أجهزة قياس/ورشة/أخرى)
- الوحدة (Lookup: قطعة/لتر/كيلوغرام/متر/طقم/علبة)
- الوصف التفصيلي
- رقم القطعة من الشركة المصنعة (اختياري)
- الباركود (اختياري)
- الحد الأدنى من المخزون (يُطلق تنبيه عند الوصول إليه)
- ساعات الاستبدال (اختياري — مثال: فلتر الزيت كل 500 ساعة)
- أهمية التغيير عند وصول ساعات التشغيل (Lookup: حرجة/عالية/متوسطة/منخفضة)
- هل مرتبطة بساعات تشغيل المولد؟ (Boolean)
- الصلاحية على مستوى: المحطة فقط أم عام لجميع المحطات
- ملاحظات

#### 1.2 المخزون الفعلي (Stock)
- ربط كل قطعة بمخزون كل محطة
- الكمية الحالية — الكمية المحجوزة (لطلبات معلقة) — الكمية المتاحة = الحالية - المحجوزة
- حساب تلقائي عند كل توريد أو صرف

#### 1.3 سندات التوريد (Supply Vouchers)
توريد مركزي للمحطة يشمل:
- رقم السند (تلقائي تسلسلي: SUP-2026-001)
- تاريخ التوريد
- اسم المورد
- رقم الفاتورة وتاريخها
- المبلغ الإجمالي
- قائمة القطع الواردة (SparePartId, Quantity, UnitPrice, SerialNumber?)
- الحالة (Draft → Completed)
- عند Confirm: يُضاف المخزون تلقائياً + يُنشأ سجل في `WarehouseTransaction`

#### 1.4 طلبات الصرف (Dispatch Requests)
يطلبها الموقع (SubStation) من المحطة (Station):
- رقم الطلب (تلقائي: DRQ-2026-001)
- الموقع الطالب (SubStationId)
- تاريخ الطلب
- الغرض (مرتبط بتصريح عمل WorkPermitId أو طلب مستقل)
- قائمة القطع المطلوبة مع الكميات
- سير الموافقة: (Pending → Approved/PartialApproved/Rejected → Issued/Completed)
- عند الموافقة: يُعرض المخزون المتبقي لكل قطعة بجانب الكمية المطلوبة
- عند الصرف: يُخفَّض المخزون تلقائياً + يُنشأ سجل WarehouseTransaction

#### 1.5 سجل حركات المخزون (Warehouse Transactions)
- يُنشأ تلقائياً عند كل توريد أو صرف أو إرجاع
- يحتفظ بـ BalanceBefore و BalanceAfter
- قابل للاستعراض والتصدير

---

### النظام الثاني: إدارة الصيانة (Maintenance Management)

**قاعدة العمل:** الصيانة تعمل على مستوى الموقع (SubStation) والمولد (Generator) تحديداً.

#### 2.1 تصاريح العمل (Work Permits)

**الترقيم:** تلقائي تسلسلي لكل موقع (مثال: WP-2026-SUB001-0001)

**البيانات الأساسية:**
- نوع الصيانة (Lookup):
  - روتيني (Routine)
  - وقائي (Preventive)
  - مبرمج (Scheduled)
  - نصف عمرية (HalfLife)
  - عمرية (FullLife)
  - تفقدية (Inspection)
  - طارئة (Emergency)
- المولد المعني (GeneratorId?) أو "عام" (غير مرتبط بمولد)
- القسم المكلف (Lookup): الكل / ميكانيكي / كهربائي / أجهزة / ورشة
- وصف المشكلة / العمل المطلوب تنفيذه
- سبب المشكلة (تحليل السبب الجذري)
- مستوى الأولوية (Lookup): حرجة / اضطراري / عادية / منخفضة
- الحالة (Lookup): مسودة → مقدم → نشط → مكتمل → معتمد → مرفوض → ملغى
- التواريخ: تاريخ الطلب، المخطط للبدء، المخطط للانتهاء، الفعلي للبدء، الفعلي للانتهاء
- ساعات التشغيل عند فتح التصريح (من DailyReadings)
- الفني المكلف (UserId)
- المشرف المعتمد (UserId)

**المهام الفرعية (WorkTasks):**
- قائمة المهام ضمن التصريح مع حالة إتمام كل مهمة
- تاريخ ووقت إتمام كل مهمة + من أتمها

**ربط بالمخازن:**
- ربط طلبات الصرف (DispatchRequests) بتصريح العمل
- عرض القطع المُصرفة لهذا التصريح

**ربط بالقراءات:**
- عرض آخر قراءة للمولد (ساعات التشغيل) في تفاصيل التصريح

#### 2.2 القطع المُركَّبة (Installed Parts)
- عند تركيب قطعة على مولد: تُسجَّل ببيانات التركيب
- InstallationHours: ساعات المولد عند التركيب
- EstimatedReplacementHours: ساعات التشغيل المتبقية للتغيير
- عند وصول المولد إلى ساعات الاستبدال → تنبيه تلقائي
- إذا تم تركيب قطعة قبل انتهاء عمرها الافتراضي → إلزامي إدخال سبب التغيير المبكر

#### 2.3 سجل الصيانة (Maintenance Log)
- سجل تاريخي لكل العمليات على كل مولد
- يُنشأ تلقائياً عند كل تحديث لتصريح العمل
- يحتوي: التاريخ + الساعات + الإجراء المنفذ + من نفذه

---

## المواصفات التقنية الكاملة (Technical Specifications)

---

### أولاً: نماذج البيانات الجديدة (Domain Models)

أنشئ الملفات التالية في `src/FuelTrack.Domain/Models/`:

---

#### `SparePart.cs`
```
يرث من BaseEntity
الخصائص:
- string Name                         // اسم القطعة
- string? NameEn                      // الاسم بالإنجليزية
- string PartNumber                   // رقم القطعة (Unique)
- string? ManufacturerPartNumber      // رقم الشركة المصنعة
- string? Barcode
- int PartCategoryLookupId           // نوع القطعة
- int UnitLookupId                   // وحدة القياس
- string? Description
- decimal UnitPrice                   // السعر الافتراضي
- int MinStockQuantity                // الحد الأدنى
- int? ReplacementIntervalHours       // ساعات الاستبدال
- int? ReplacementPriorityLookupId   // أهمية التغيير
- bool IsHoursTracked                // مرتبطة بساعات؟
- string? Notes
Navigation:
- Lookup PartCategoryLookup
- Lookup UnitLookup
- Lookup? ReplacementPriorityLookup
- ICollection<WarehouseStock> Stocks
- ICollection<WarehouseSupplyItem> SupplyItems
- ICollection<DispatchRequestItem> DispatchItems
- ICollection<InstalledPart> InstalledParts
```

---

#### `WarehouseStock.cs`
```
يرث من BaseEntity
الخصائص:
- int SparePartId
- int StationId                      // المخزن على مستوى المحطة
- decimal CurrentQuantity
- decimal ReservedQuantity           // محجوز لطلبات معلقة
[NotMapped]
- decimal AvailableQuantity => CurrentQuantity - ReservedQuantity
Navigation:
- SparePart SparePart
- Station Station
Unique Index: (SparePartId, StationId)
```

---

#### `WarehouseSupply.cs`
```
يرث من BaseEntity
الخصائص:
- string SupplyNumber                // SUP-2026-001 (auto)
- int StationId
- DateTime SupplyDate
- string? SupplierName
- string? InvoiceNumber
- DateTime? InvoiceDate
- decimal? TotalAmount
- int StatusLookupId                // Draft / Completed
- string? Notes
Navigation:
- Station Station
- Lookup StatusLookup
- ICollection<WarehouseSupplyItem> Items
```

---

#### `WarehouseSupplyItem.cs`
```
يرث من BaseEntity
الخصائص:
- int WarehouseSupplyId
- int SparePartId
- decimal Quantity
- decimal UnitPrice
- decimal TotalPrice                // = Quantity * UnitPrice
- string? SerialNumber             // رقم تسلسلي للقطعة إن وجد
- string? Notes
Navigation:
- WarehouseSupply WarehouseSupply
- SparePart SparePart
```

---

#### `DispatchRequest.cs`
```
يرث من BaseEntity
الخصائص:
- string RequestNumber              // DRQ-2026-001 (auto)
- int SubStationId                 // الموقع الطالب
- int StationId                    // المخزن المعني
- DateTime RequestDate
- int RequestedById
- int? ApprovedById
- DateTime? ApprovalDate
- int? WorkPermitId                // مرتبط بتصريح عمل (اختياري)
- int StatusLookupId               // Pending/Approved/PartialApproved/Rejected/Completed
- string? Purpose                  // الغرض من الصرف
- string? Notes
Navigation:
- SubStation SubStation
- Station Station
- User RequestedBy
- User? ApprovedBy
- WorkPermit? WorkPermit
- Lookup StatusLookup
- ICollection<DispatchRequestItem> Items
```

---

#### `DispatchRequestItem.cs`
```
يرث من BaseEntity
الخصائص:
- int DispatchRequestId
- int SparePartId
- decimal RequestedQuantity
- decimal? ApprovedQuantity
- decimal? IssuedQuantity
- string? Notes
- bool IsApproved                  // على مستوى السطر
Navigation:
- DispatchRequest DispatchRequest
- SparePart SparePart
- ICollection<InstalledPart> InstalledParts
```

---

#### `WarehouseTransaction.cs`
```
يرث من BaseEntity
الخصائص:
- int SparePartId
- int StationId
- int TransactionTypeLookupId      // Supply/Dispatch/Return/Adjustment
- DateTime TransactionDate
- decimal Quantity                 // موجب = إضافة، سالب = صرف
- decimal BalanceBefore
- decimal BalanceAfter
- int? ReferenceId                 // SupplyId أو DispatchRequestId
- string? ReferenceType            // "Supply" | "Dispatch" | "Return"
- string? Notes
Navigation:
- SparePart SparePart
- Station Station
- Lookup TransactionTypeLookup
```

---

#### `WorkPermit.cs`
```
يرث من BaseEntity
الخصائص:
- string PermitNumber              // WP-2026-SUB001-0001 (auto per SubStation)
- int SubStationId
- int? GeneratorId                 // null = تصريح عام
- int MaintenanceTypeLookupId     // Routine/Preventive/Scheduled/HalfLife/FullLife/Inspection/Emergency
- int DepartmentLookupId          // All/Mechanical/Electrical/Instrumentation/Workshop
- int PriorityLookupId            // Critical/Emergency/Normal/Low
- int StatusLookupId              // Draft/Submitted/Active/Completed/Approved/Rejected/Cancelled
- DateTime PermitDate
- string ProblemDescription       // المشكلة / العمل المطلوب
- string? ProblemCause            // سبب المشكلة
- DateTime? PlannedStartDate
- DateTime? PlannedEndDate
- DateTime? ActualStartDate
- DateTime? ActualEndDate
- double? RunningHoursAtCreation  // ساعات التشغيل عند فتح التصريح
- int RequestedById
- int? AssignedToId               // الفني المكلف
- int? ApprovedById
- string? CompletionNotes         // ملاحظات الإغلاق
- string? RejectionReason
- string? Notes
Navigation:
- SubStation SubStation
- Generator? Generator
- Lookup MaintenanceTypeLookup
- Lookup DepartmentLookup
- Lookup PriorityLookup
- Lookup StatusLookup
- User RequestedBy
- User? AssignedTo
- User? ApprovedBy
- ICollection<WorkTask> Tasks
- ICollection<DispatchRequest> DispatchRequests
- ICollection<InstalledPart> InstalledParts
- ICollection<MaintenanceLog> Logs
```

---

#### `WorkTask.cs`
```
يرث من BaseEntity
الخصائص:
- int WorkPermitId
- string TaskDescription
- int SortOrder
- bool IsCompleted
- DateTime? CompletedAt
- int? CompletedById
- string? Notes
Navigation:
- WorkPermit WorkPermit
- User? CompletedBy
```

---

#### `InstalledPart.cs`
```
يرث من BaseEntity
الخصائص:
- int GeneratorId
- int SparePartId
- int? WorkPermitId               // تصريح العمل الذي تم التركيب فيه
- int? DispatchRequestItemId      // سطر الصرف المصدر للقطعة
- DateTime InstallationDate
- double InstallationHours        // ساعات المولد عند التركيب
- int? ReplacementIntervalHours   // تُنسخ من SparePart عند التركيب
- double? EstimatedReplacementAtHours // InstallationHours + ReplacementIntervalHours
- bool IsActive                   // هل لا تزال مركبة على المولد؟
- DateTime? RemovedAt
- double? RemovedAtHours          // ساعات المولد عند الإزالة
- string? EarlyReplacementReason  // سبب التغيير المبكر (إلزامي إذا قبل الوقت)
- string? Notes
Navigation:
- Generator Generator
- SparePart SparePart
- WorkPermit? WorkPermit
- DispatchRequestItem? DispatchRequestItem
```

---

#### `MaintenanceLog.cs`
```
يرث من BaseEntity
الخصائص:
- int WorkPermitId
- int? GeneratorId
- DateTime LogDate
- double? RunningHoursAtLog
- string Action                   // الإجراء المنفذ
- string? Notes
- int LoggedById
Navigation:
- WorkPermit WorkPermit
- Generator? Generator
- User LoggedBy
```

---

### ثانياً: بيانات Lookup الجديدة المطلوبة

أضف في `SeedLookupData` Migration (أو Migration جديدة) الفئات التالية:

```
LookupCategory: PartCategory (فئة القطعة)
  - Mechanical (ميكانيكي)
  - Electrical (كهربائي)
  - Electronic (إلكتروني)
  - Instrumentation (أجهزة قياس)
  - Workshop (ورشة)
  - Other (أخرى)

LookupCategory: PartUnit (وحدة القطعة)
  - Piece (قطعة)
  - Liter (لتر)
  - Kilogram (كيلوغرام)
  - Meter (متر)
  - Set (طقم)
  - Pack (علبة)

LookupCategory: MaintenanceType (نوع الصيانة)
  - Routine (روتيني)
  - Preventive (وقائي)
  - Scheduled (مبرمج)
  - HalfLife (نصف عمرية)
  - FullLife (عمرية)
  - Inspection (تفقدية)
  - Emergency (طارئة)

LookupCategory: MaintenanceDepartment (القسم المكلف)
  - All (الكل)
  - Mechanical (ميكانيكي)
  - Electrical (كهربائي)
  - Instrumentation (أجهزة)
  - Workshop (ورشة)

LookupCategory: MaintenancePriority (أولوية الصيانة)
  - Critical (حرجة)
  - Emergency (اضطراري)
  - Normal (عادية)
  - Low (منخفضة)

LookupCategory: MaintenanceStatus (حالة التصريح)
  - Draft (مسودة)
  - Submitted (مقدم)
  - Active (نشط)
  - Completed (مكتمل)
  - Approved (معتمد)
  - Rejected (مرفوض)
  - Cancelled (ملغى)

LookupCategory: DispatchStatus (حالة طلب الصرف)
  - Pending (قيد الانتظار)
  - Approved (معتمد)
  - PartialApproved (موافقة جزئية)
  - Rejected (مرفوض)
  - Issued (تم الصرف)
  - Completed (مكتمل)

LookupCategory: WarehouseSupplyStatus (حالة سند التوريد)
  - Draft (مسودة)
  - Completed (مكتمل)

LookupCategory: WarehouseTransactionType (نوع حركة المخزون)
  - Supply (توريد)
  - Dispatch (صرف)
  - Return (إرجاع)
  - Adjustment (تسوية)
  - Transfer (نقل)

LookupCategory: ReplacementPriority (أهمية الاستبدال)
  - Critical (حرجة — يجب التوقف فوراً)
  - High (عالية — تغيير في أقرب وقت)
  - Medium (متوسطة — ضمن الصيانة القادمة)
  - Low (منخفضة — مراقبة)
```

---

### ثالثاً: EF Core Configuration

أضف في `FuelTrackDbContext.OnModelCreating`:

```csharp
// DbSets الجديدة
public DbSet<SparePart> SpareParts => Set<SparePart>();
public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
public DbSet<WarehouseSupply> WarehouseSupplies => Set<WarehouseSupply>();
public DbSet<WarehouseSupplyItem> WarehouseSupplyItems => Set<WarehouseSupplyItem>();
public DbSet<DispatchRequest> DispatchRequests => Set<DispatchRequest>();
public DbSet<DispatchRequestItem> DispatchRequestItems => Set<DispatchRequestItem>();
public DbSet<WarehouseTransaction> WarehouseTransactions => Set<WarehouseTransaction>();
public DbSet<WorkPermit> WorkPermits => Set<WorkPermit>();
public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
public DbSet<InstalledPart> InstalledParts => Set<InstalledPart>();
public DbSet<MaintenanceLog> MaintenanceLogs => Set<MaintenanceLog>();

// Global Soft Delete Filters (أضف لجميع الكيانات الجديدة)
modelBuilder.Entity<SparePart>().HasQueryFilter(e => e.DeletedAt == null);
modelBuilder.Entity<WarehouseStock>().HasQueryFilter(e => e.DeletedAt == null);
// ... إلخ لجميع الكيانات

// Unique Indexes
modelBuilder.Entity<WarehouseStock>()
    .HasIndex(s => new { s.SparePartId, s.StationId }).IsUnique();

modelBuilder.Entity<SparePart>()
    .HasIndex(s => s.PartNumber).IsUnique();

// Relationships
// SparePart → Lookups (Restrict)
// WarehouseStock → SparePart (Restrict), Station (Restrict)
// WarehouseSupply → Station (Restrict), StatusLookup (Restrict)
// WarehouseSupplyItem → Supply (Cascade), SparePart (Restrict)
// DispatchRequest → SubStation/Station/RequestedBy/WorkPermit (Restrict), StatusLookup (Restrict)
// DispatchRequestItem → DispatchRequest (Cascade), SparePart (Restrict)
// WarehouseTransaction → SparePart/Station/TypeLookup (Restrict)
// WorkPermit → SubStation/Generator?/Lookups/Users (Restrict)
//   DeleteBehavior.SetNull على Generator (اختياري)
// WorkTask → WorkPermit (Cascade)
// InstalledPart → Generator/SparePart/WorkPermit?/DispatchRequestItem? (Restrict)
// MaintenanceLog → WorkPermit/Generator?/LoggedBy (Restrict)

// Decimal precision
modelBuilder.Entity<SparePart>().Property(s => s.UnitPrice).HasColumnType("decimal(18,4)");
modelBuilder.Entity<WarehouseSupplyItem>().Property(s => s.UnitPrice).HasColumnType("decimal(18,4)");
modelBuilder.Entity<WarehouseSupplyItem>().Property(s => s.TotalPrice).HasColumnType("decimal(18,4)");
modelBuilder.Entity<WarehouseStock>().Property(s => s.CurrentQuantity).HasColumnType("decimal(18,3)");
modelBuilder.Entity<WarehouseStock>().Property(s => s.ReservedQuantity).HasColumnType("decimal(18,3)");
```

---

### رابعاً: الـ DTOs المطلوبة

#### للمخازن:

```
SparePartListDto(Id, PartNumber, Name, PartCategoryName, UnitName, MinStockQuantity,
    ReplacementIntervalHours?, IsHoursTracked, UnitPrice, Notes)

SparePartDetailDto(Id, PartNumber, Name, NameEn?, ManufacturerPartNumber?, Barcode?,
    PartCategoryLookupId, PartCategoryName, UnitLookupId, UnitName, Description?,
    UnitPrice, MinStockQuantity, ReplacementIntervalHours?, IsHoursTracked,
    ReplacementPriorityLookupId?, ReplacementPriorityName?, Notes, CreatedAt)

CreateSparePartDto(Name, NameEn?, PartNumber, ManufacturerPartNumber?, Barcode?,
    PartCategoryLookupId, UnitLookupId, Description?, UnitPrice, MinStockQuantity,
    ReplacementIntervalHours?, IsHoursTracked, ReplacementPriorityLookupId?, Notes?)

WarehouseStockDto(SparePartId, PartNumber, SparePartName, StationId, StationName,
    CurrentQuantity, ReservedQuantity, AvailableQuantity, MinStockQuantity,
    IsLowStock, UnitName)

WarehouseSupplyListDto(Id, SupplyNumber, StationName, SupplyDate, SupplierName?,
    InvoiceNumber?, TotalAmount?, StatusName, ItemCount, CreatedAt)

WarehouseSupplyDetailDto(+ Items: List<SupplyItemDto>)

CreateWarehouseSupplyDto(StationId, SupplyDate, SupplierName?, InvoiceNumber?,
    InvoiceDate?, TotalAmount?, Notes?, Items: List<CreateSupplyItemDto>)

CreateSupplyItemDto(SparePartId, Quantity, UnitPrice, SerialNumber?, Notes?)

DispatchRequestListDto(Id, RequestNumber, SubStationName, StationName, RequestDate,
    StatusName, RequestedByName, ItemCount, WorkPermitNumber?, CreatedAt)

DispatchRequestDetailDto(+ Items: List<DispatchItemDetailDto>)

DispatchItemDetailDto(Id, SparePartId, SparePartName, PartNumber, UnitName,
    RequestedQuantity, ApprovedQuantity?, IssuedQuantity?,
    AvailableInStock, IsApproved, Notes?)

CreateDispatchRequestDto(SubStationId, StationId, WorkPermitId?, Purpose?,
    Notes?, Items: List<CreateDispatchItemDto>)

CreateDispatchItemDto(SparePartId, RequestedQuantity, Notes?)

ApproveDispatchRequestDto(List<ApproveDispatchItemDto> Items, string? Notes)

ApproveDispatchItemDto(DispatchRequestItemId, ApprovedQuantity, bool IsApproved)

WarehouseTransactionDto(Id, SparePartName, PartNumber, TransactionTypeName,
    TransactionDate, Quantity, BalanceBefore, BalanceAfter, ReferenceId?,
    ReferenceType?, Notes, CreatedByName, CreatedAt)

WarehouseReportDto(StationId, StationName, StartDate, EndDate,
    TotalSupplied, TotalDispatched, NetChange,
    LowStockItems: List<SparePartListDto>,
    Items: List<WarehouseTransactionDto>)
```

#### للصيانة:

```
WorkPermitListDto(Id, PermitNumber, SubStationName, GeneratorName?, MaintenanceTypeName,
    DepartmentName, PriorityName, StatusName, PermitDate, RequestedByName,
    AssignedToName?, PlannedStartDate?, ActualStartDate?, TasksTotal, TasksCompleted)

WorkPermitDetailDto(+ ProblemDescription, ProblemCause?, CompletionNotes?,
    RejectionReason?, RunningHoursAtCreation?,
    Tasks: List<WorkTaskDto>,
    DispatchRequests: List<DispatchRequestListDto>,
    InstalledParts: List<InstalledPartDto>,
    Logs: List<MaintenanceLogDto>)

CreateWorkPermitDto(SubStationId, GeneratorId?, MaintenanceTypeLookupId,
    DepartmentLookupId, PriorityLookupId, PermitDate,
    ProblemDescription, ProblemCause?, PlannedStartDate?, PlannedEndDate?,
    AssignedToId?, Notes?, Tasks: List<CreateWorkTaskDto>)

CreateWorkTaskDto(TaskDescription, SortOrder)

WorkTaskDto(Id, TaskDescription, SortOrder, IsCompleted,
    CompletedAt?, CompletedByName?)

UpdateWorkPermitStatusDto(StatusLookupId, Notes?, RejectionReason?,
    ActualStartDate?, ActualEndDate?)

InstalledPartDto(Id, GeneratorId, GeneratorName, SparePartId, SparePartName,
    PartNumber, WorkPermitNumber?, InstallationDate, InstallationHours,
    ReplacementIntervalHours?, EstimatedReplacementAtHours?,
    CurrentGeneratorHours, HoursUntilReplacement?,
    IsOverdue, IsActive, RemovedAt?, RemovedAtHours?,
    EarlyReplacementReason?, Notes?)

CreateInstalledPartDto(GeneratorId, SparePartId, WorkPermitId?,
    DispatchRequestItemId?, InstallationDate, InstallationHours,
    ReplacementIntervalHours?, Notes?)

RemoveInstalledPartDto(RemovedAtHours, EarlyReplacementReason?, Notes?)

MaintenanceLogDto(Id, WorkPermitNumber, GeneratorName?, LogDate,
    RunningHoursAtLog?, Action, Notes?, LoggedByName, CreatedAt)

MaintenanceDashboardDto(
    TotalOpenPermits, CriticalPermits, OverdueInstalledParts,
    LowStockPartsCount, ActiveWorkPermits,
    RecentPermits: List<WorkPermitListDto>,
    UpcomingReplacements: List<InstalledPartDto>)
```

---

### خامساً: الـ Services المطلوبة

أنشئ كل Interface في `src/FuelTrack.Application/Interfaces/` وكل Implementation في `src/FuelTrack.Infrastructure/Services/`:

```
ISparePartService:
  - CreateAsync(dto, userId) → SparePartDetailDto
  - UpdateAsync(id, dto, userId) → SparePartDetailDto
  - DeleteAsync(id) → void
  - GetByIdAsync(id) → SparePartDetailDto?
  - GetAllAsync(stationId?) → List<SparePartListDto>
  - GetPagedAsync(page, pageSize, search?, categoryId?) → PagedResult<SparePartListDto>

IWarehouseStockService:
  - GetByStationAsync(stationId) → List<WarehouseStockDto>
  - GetByPartAsync(sparePartId) → List<WarehouseStockDto>
  - GetLowStockAsync(stationId?) → List<WarehouseStockDto>
  - AdjustStockAsync(sparePartId, stationId, delta, reason, userId) → void (manual adjustment)

IWarehouseSupplyService:
  - CreateAsync(dto, userId) → WarehouseSupplyDetailDto
  - UpdateAsync(id, dto, userId) → WarehouseSupplyDetailDto  (Draft only)
  - ConfirmAsync(id, userId) → WarehouseSupplyDetailDto  (Draft → Completed, يضيف للمخزون)
  - DeleteAsync(id) → void  (Draft only)
  - GetByIdAsync(id) → WarehouseSupplyDetailDto?
  - GetByStationAsync(stationId, page, pageSize) → PagedResult<WarehouseSupplyListDto>
  - GetAllAsync(page, pageSize) → PagedResult<WarehouseSupplyListDto>

IDispatchRequestService:
  - CreateAsync(dto, userId) → DispatchRequestDetailDto
  - UpdateAsync(id, dto, userId) → DispatchRequestDetailDto  (Pending only)
  - ApproveAsync(id, dto, userId) → DispatchRequestDetailDto  (Pending → Approved/Partial)
  - RejectAsync(id, reason, userId) → DispatchRequestDetailDto
  - IssueAsync(id, userId) → DispatchRequestDetailDto  (Approved → Issued, يخفض المخزون)
  - DeleteAsync(id) → void  (Pending only)
  - GetByIdAsync(id) → DispatchRequestDetailDto?
  - GetBySubStationAsync(subStationId, page, pageSize) → PagedResult<DispatchRequestListDto>
  - GetByStationAsync(stationId, page, pageSize) → PagedResult<DispatchRequestListDto>
  - GetAllAsync(page, pageSize, status?) → PagedResult<DispatchRequestListDto>

IWarehouseTransactionService:
  - GetByPartAsync(sparePartId, page, pageSize) → PagedResult<WarehouseTransactionDto>
  - GetByStationAsync(stationId, startDate, endDate, page, pageSize) → PagedResult<WarehouseTransactionDto>

IWarehouseReportService:
  - GetStockReportAsync(stationId?) → List<WarehouseStockDto>
  - GetMovementReportAsync(stationId, startDate, endDate) → WarehouseReportDto
  - GetLowStockAlertAsync() → List<WarehouseStockDto>
  - GetDispatchByGeneratorAsync(generatorId, startDate?, endDate?) → List<WarehouseTransactionDto>
  - GetDispatchBySubStationAsync(subStationId, startDate?, endDate?) → List<WarehouseTransactionDto>
  - GetSupplyReportAsync(stationId, startDate, endDate) → List<WarehouseSupplyListDto>
  - GetPeriodicInventoryReportAsync(stationId, type: monthly|quarterly|semiannual|annual, year, period) → WarehouseReportDto

IWorkPermitService:
  - CreateAsync(dto, userId) → WorkPermitDetailDto
  - UpdateAsync(id, dto, userId) → WorkPermitDetailDto
  - UpdateStatusAsync(id, dto, userId) → WorkPermitDetailDto
  - DeleteAsync(id) → void  (Draft only)
  - GetByIdAsync(id) → WorkPermitDetailDto?
  - GetBySubStationAsync(subStationId, page, pageSize, status?) → PagedResult<WorkPermitListDto>
  - GetByGeneratorAsync(generatorId, page, pageSize) → PagedResult<WorkPermitListDto>
  - GetAllAsync(page, pageSize, status?, priority?, subStationId?) → PagedResult<WorkPermitListDto>
  - CompleteTaskAsync(permitId, taskId, userId) → WorkTaskDto
  - AddLogAsync(permitId, action, notes, userId) → MaintenanceLogDto
  - GeneratePermitNumberAsync(subStationId) → string  (WP-2026-SUB001-0001)

IInstalledPartService:
  - InstallAsync(dto, userId) → InstalledPartDto
  - RemoveAsync(id, dto, userId) → InstalledPartDto
  - GetByGeneratorAsync(generatorId, activeOnly) → List<InstalledPartDto>
  - GetOverdueAsync(subStationId?) → List<InstalledPartDto>  (تجاوزت ساعات التغيير)
  - GetUpcomingReplacementsAsync(subStationId?, hoursThreshold) → List<InstalledPartDto>
  - CheckEarlyReplacementAsync(sparePartId, generatorId) → bool

IMaintenanceDashboardService:
  - GetDashboardAsync(subStationId?, stationId?) → MaintenanceDashboardDto
```

---

### سادساً: الـ Controllers المطلوبة

أنشئ في `src/FuelTrack.API/Controllers/`:

```
Route: api/spare-parts
SparePartsController → ISparePartService
  GET    /                     → GetPaged (search, categoryId, stationId)
  GET    /{id}                 → GetById
  POST   /                     → Create [AdminOrManager]
  PUT    /{id}                 → Update [AdminOrManager]
  DELETE /{id}                 → Delete [Admin]

Route: api/warehouse/stock
WarehouseStockController → IWarehouseStockService
  GET    /by-station/{stationId}  → GetByStation
  GET    /by-part/{partId}        → GetByPart
  GET    /low-stock               → GetLowStock (stationId?)
  POST   /{partId}/adjust         → AdjustStock [Admin] (manual adj.)

Route: api/warehouse/supply
WarehouseSupplyController → IWarehouseSupplyService
  GET    /                     → GetAll (paged)
  GET    /by-station/{id}      → GetByStation (paged)
  GET    /{id}                 → GetById
  POST   /                     → Create [AdminOrManager]
  PUT    /{id}                 → Update [AdminOrManager] (Draft only)
  POST   /{id}/confirm         → Confirm [AdminOrManager]
  DELETE /{id}                 → Delete [Admin] (Draft only)

Route: api/warehouse/dispatch
DispatchRequestController → IDispatchRequestService
  GET    /                     → GetAll (paged, status?)
  GET    /by-substation/{id}   → GetBySubStation (paged)
  GET    /by-station/{id}      → GetByStation (paged)
  GET    /{id}                 → GetById
  POST   /                     → Create [Authorize]
  PUT    /{id}                 → Update [Authorize] (Pending only)
  POST   /{id}/approve         → Approve [AdminOrManager]
  POST   /{id}/reject          → Reject [AdminOrManager]
  POST   /{id}/issue           → Issue [AdminOrManager] (executes dispatch)
  DELETE /{id}                 → Delete [Admin]

Route: api/warehouse/transactions
WarehouseTransactionsController → IWarehouseTransactionService
  GET    /by-part/{partId}     → GetByPart (paged)
  GET    /by-station/{id}      → GetByStation (startDate, endDate, paged)

Route: api/warehouse/reports
WarehouseReportsController → IWarehouseReportService
  GET    /stock                → GetStockReport (stationId?)
  GET    /movements            → GetMovementReport (stationId, startDate, endDate)
  GET    /low-stock-alert      → GetLowStockAlert
  GET    /by-generator/{id}    → GetDispatchByGenerator
  GET    /by-substation/{id}   → GetDispatchBySubStation
  GET    /supply               → GetSupplyReport (stationId, startDate, endDate)
  GET    /periodic             → GetPeriodicInventory (stationId, type, year, period)

Route: api/work-permits
WorkPermitsController → IWorkPermitService
  GET    /                     → GetAll (paged, status?, priority?, subStationId?)
  GET    /by-substation/{id}   → GetBySubStation (paged, status?)
  GET    /by-generator/{id}    → GetByGenerator (paged)
  GET    /{id}                 → GetById
  POST   /                     → Create [Authorize]
  PUT    /{id}                 → Update [Authorize] (Draft only)
  POST   /{id}/status          → UpdateStatus [AdminOrManager]
  POST   /{id}/tasks/{taskId}/complete → CompleteTask [Authorize]
  POST   /{id}/log             → AddLog [Authorize]
  DELETE /{id}                 → Delete [Admin]

Route: api/installed-parts
InstalledPartsController → IInstalledPartService
  GET    /by-generator/{id}    → GetByGenerator (activeOnly?)
  GET    /overdue              → GetOverdue (subStationId?)
  GET    /upcoming             → GetUpcomingReplacements (subStationId?, hoursThreshold=200)
  POST   /install              → Install [Authorize]
  POST   /{id}/remove          → Remove [Authorize]

Route: api/maintenance/dashboard
MaintenanceDashboardController → IMaintenanceDashboardService
  GET    /                     → GetDashboard (subStationId?, stationId?)
```

---

### سابعاً: الصفحات الأمامية (React Pages)

أنشئ في `frontend/src/pages/`:

---

#### `WarehouseParts.js` — كتالوج قطع الغيار
```
عنوان: "كتالوج قطع الغيار"
الوظائف:
- جدول مقسم بالصفحات مع بحث وفرز (PartNumber, Name, Category, Unit)
- فلتر بالفئة (ميكانيكي/كهربائي/إلكتروني/أجهزة/ورشة)
- مؤشر بصري للقطع التي وصلت للحد الأدنى (صف أحمر)
- Modal إضافة/تعديل قطعة مع جميع الحقول
- Modal تفاصيل يعرض: المخزون الحالي في كل محطة + القطع المركبة على المولدات
- زر حذف مع تأكيد
- تصدير Excel للكتالوج
API: GET/POST/PUT/DELETE /api/spare-parts
```

---

#### `WarehouseStock.js` — المخزون الفعلي
```
عنوان: "المخزون الفعلي"
الوظائف:
- فلتر بالمحطة
- جدول يعرض: رقم القطعة، الاسم، الفئة، المخزون الحالي، المحجوز، المتاح، الحد الأدنى
- تلوين الصفوف: أخضر (فوق الحد)، أصفر (قريب من الحد)، أحمر (وصل أو أقل من الحد)
- زر "طلب توريد" للقطع الناقصة
- تبويب "تنبيهات المخزون المنخفض"
API: GET /api/warehouse/stock/by-station/{id}, /low-stock
```

---

#### `WarehouseSupply.js` — سندات التوريد
```
عنوان: "سندات التوريد"
الوظائف:
- قائمة سندات التوريد مع حالة كل سند
- Modal إنشاء سند جديد:
  * اختيار المحطة
  * بيانات الفاتورة (رقم، تاريخ، مورد، مبلغ)
  * جدول القطع: اختيار القطعة + الكمية + سعر الوحدة + رقم تسلسلي
  * إضافة/حذف صفوف ديناميكياً
  * حساب المجموع تلقائياً
- زر "تأكيد السند" لنقله من Draft → Completed
- عرض تفاصيل السند مع جميع بنوده
API: GET/POST/PUT /api/warehouse/supply, POST /{id}/confirm
```

---

#### `WarehouseDispatch.js` — طلبات الصرف
```
عنوان: "طلبات الصرف"
الوظائف:
- تبويبان: "الطلبات الواردة" (للمخزن) | "طلباتي" (للموقع)
- جدول مع فلتر الحالة (Pending/Approved/Rejected/Completed)
- Modal إنشاء طلب:
  * اختيار المحطة الفرعية + المخزن
  * ربط بتصريح عمل (اختياري)
  * جدول القطع المطلوبة مع الكميات
- Modal الموافقة (للمخزن):
  * يعرض الكمية المطلوبة + المخزون المتاح لكل قطعة
  * تحديد الكمية المعتمدة لكل سطر
  * زر "موافق كلياً" و "موافق جزئياً" و "رفض"
- زر "تنفيذ الصرف" عند الموافقة النهائية (Issued)
API: GET/POST/PUT /api/warehouse/dispatch, POST /{id}/approve, /{id}/reject, /{id}/issue
```

---

#### `WarehouseReports.js` — تقارير المخازن
```
عنوان: "تقارير المخازن"
التبويبات:
1. تقرير المخزون: عرض المخزون الكامل مع حالة كل قطعة
2. حركات المخزون: فلتر بالمحطة والتاريخ، جدول الحركات مع BalanceBefore/After
3. المخزون المنخفض: قائمة القطع التي وصلت للحد الأدنى
4. توريد الفترة: فاتورة التوريد في فترة زمنية محددة
5. صرف بالموقع: القطع المنصرفة لموقع معين
6. صرف بالمولد: القطع المنصرفة لمولد معين
7. الجرد الدوري: شهري/ربع سنوي/نصف سنوي/سنوي
كل تقرير: فلاتر متقدمة + تصدير Excel + طباعة
```

---

#### `WorkPermits.js` — تصاريح العمل
```
عنوان: "تصاريح العمل"
الوظائف:
- جدول بتصاريح الموقع مع ألوان الأولوية (أحمر=حرجة، برتقالي=اضطراري، أصفر=عادية)
- فلتر بالحالة / النوع / القسم / المولد
- بطاقة إحصائية: مفتوحة / مكتملة / معتمدة / ملغاة
- Modal إنشاء تصريح:
  * نوع الصيانة (dropdown)
  * المولد أو "عام" (toggle)
  * القسم المكلف
  * وصف المشكلة (textarea)
  * سبب المشكلة (textarea)
  * الأولوية
  * التواريخ المخططة
  * قائمة المهام (WorkTasks) - إضافة ديناميكية
- صفحة تفاصيل تصريح (WorkPermitDetail.js):
  * جميع بيانات التصريح
  * قائمة المهام مع checkboxes
  * القطع المستخدمة (من طلبات الصرف)
  * سجل التصريح (MaintenanceLog)
  * أزرار العمل: بدء / إكمال / اعتماد / رفض / إلغاء
API: GET/POST/PUT /api/work-permits, POST /{id}/status
```

---

#### `InstalledParts.js` — القطع المركبة
```
عنوان: "القطع المركبة على المولدات"
الوظائف:
- فلتر بالموقع / المولد / نوع القطعة
- جدول يعرض: القطعة، المولد، تاريخ التركيب، ساعات التركيب، الساعات المتبقية
- مؤشر بصري: أخضر (أقل من 80% من العمر)، أصفر (80-100%)، أحمر (تجاوز)
- تبويب "القطع المستحقة التغيير" (الأحمر)
- تبويب "القطع القريبة من الاستبدال" (الأصفر)
- Modal "تركيب قطعة جديدة":
  * اختيار المولد
  * اختيار القطعة
  * ساعات التشغيل الحالية (تُملأ تلقائياً من آخر قراءة)
  * ربط بطلب صرف (اختياري)
  * ربط بتصريح عمل (اختياري)
- Modal "إزالة قطعة":
  * ساعات المولد عند الإزالة
  * سبب التغيير المبكر (إلزامي إذا لم تنته الساعات)
API: GET /api/installed-parts/by-generator/{id}, /overdue, /upcoming
     POST /install, /{id}/remove
```

---

#### `MaintenanceDashboard.js` — لوحة تحكم الصيانة
```
الوظائف:
- KPI Cards: تصاريح مفتوحة / تصاريح حرجة / قطع متأخرة التغيير / مخزون منخفض
- جدول آخر التصاريح المفتوحة
- جدول القطع المستحقة التغيير
- Chart: توزيع التصاريح حسب النوع (Pie) وحسب الأولوية (Bar)
- Chart: حركات المخزون خلال 30 يوم
- تحديث تلقائي عبر SignalR عند أحداث الصيانة
API: GET /api/maintenance/dashboard
```

---

### ثامناً: نظام الإشعارات التلقائية

ادمج مع نظام الإشعارات القائم (Notification + NotificationRecipient) لإطلاق إشعارات SignalR في الحالات التالية:

```
1. قطعة في المولد وصلت إلى 80% من ساعات الاستبدال → إشعار لمدير الصيانة
2. قطعة في المولد تجاوزت ساعات الاستبدال → إشعار حرج لمدير الصيانة
3. مخزون قطعة وصل للحد الأدنى → إشعار لأمين المخزن
4. طلب صرف جديد → إشعار لأمين المخزن
5. طلب صرف معتمد/مرفوض → إشعار لمقدم الطلب
6. تصريح عمل جديد → إشعار للمسؤول
7. تصريح عمل اكتملت مهامه → إشعار للمشرف للاعتماد

الإشعار يحتوي: Title, Message, NotificationType("Maintenance"/"Warehouse"), Severity,
                SubStationId?, مرسل إلى: أمناء المخازن / مدراء الصيانة حسب الدور
```

---

### تاسعاً: الصلاحيات (RBAC)

أضف للنظام الجديد صلاحيات في `registry.js` للواجهة وفي `PermissionMapper` للـ Backend:

```
warehouse.parts.read       — عرض كتالوج القطع
warehouse.parts.write      — إضافة/تعديل القطع
warehouse.supply.read      — عرض سندات التوريد
warehouse.supply.write     — إنشاء/تعديل سندات التوريد
warehouse.supply.confirm   — تأكيد سندات التوريد
warehouse.dispatch.read    — عرض طلبات الصرف
warehouse.dispatch.create  — إنشاء طلبات الصرف
warehouse.dispatch.approve — اعتماد/رفض طلبات الصرف
warehouse.dispatch.issue   — تنفيذ الصرف الفعلي
warehouse.reports.view     — عرض تقارير المخازن
maintenance.permits.read   — عرض تصاريح العمل
maintenance.permits.create — إنشاء تصاريح العمل
maintenance.permits.manage — إدارة حالات التصاريح (اعتماد/رفض/إلغاء)
maintenance.parts.read     — عرض القطع المركبة
maintenance.parts.install  — تركيب وإزالة القطع
maintenance.dashboard.view — عرض لوحة تحكم الصيانة
```

---

### عاشراً: Migrations المطلوبة

```
Migration 1: AddMaintenanceWarehouseLookups
  - إضافة LookupCategories الجديدة وقيمها

Migration 2: AddWarehouseEntities
  - إنشاء جداول: SpareParts, WarehouseStocks, WarehouseSupplies,
                  WarehouseSupplyItems, DispatchRequests, DispatchRequestItems,
                  WarehouseTransactions

Migration 3: AddMaintenanceEntities
  - إنشاء جداول: WorkPermits, WorkTasks, InstalledParts, MaintenanceLogs

Migration 4: AddPerformanceIndexes
  - إضافة indexes على: PermitDate, InstallationDate, TransactionDate,
                        GeneratorId+IsActive (InstalledParts),
                        SparePartId+StationId (WarehouseStock - unique)
```

---

## قواعد صارمة يجب الالتزام بها

### قواعد الكود
1. **كل كيان جديد يرث من `BaseEntity`** — لا استثناء
2. **جميع الـ IDs من نوع `int`** — لا Guid
3. **جميع الـ DTOs من نوع `record`** — C# record types
4. **إضافة Global Query Filter** للـ soft-delete على كل كيان جديد في `OnModelCreating`
5. **استخدام `AsNoTracking()`** في كل استعلام قرائي
6. **استخدام `AddRange` + `SaveChangesAsync` مرة واحدة** — لا N+1 pattern
7. **Fluent Validation** لكل Create/Update DTO

### قواعد Business Logic
8. **WarehouseSupply.Confirm** — يجب التحقق من وجود Items قبل التأكيد
9. **DispatchRequest.Approve** — يجب التحقق أن `ApprovedQuantity <= AvailableInStock` لكل قطعة
10. **DispatchRequest.Issue** — يُنشئ `WarehouseTransaction` لكل سطر معتمد، يُخفّض `WarehouseStock.CurrentQuantity`، يُحرر `ReservedQuantity`
11. **DispatchRequest.Create** — يُزيد `WarehouseStock.ReservedQuantity` عند الإنشاء، يُحرره عند الرفض أو الإلغاء
12. **InstalledPart.Install** — إذا `ReplacementIntervalHours` > 0 في SparePart، تُحسب `EstimatedReplacementAtHours = InstallationHours + ReplacementIntervalHours`
13. **InstalledPart.Remove** — إذا `RemovedAtHours < EstimatedReplacementAtHours` → `EarlyReplacementReason` إلزامي
14. **WorkPermit.PermitNumber** — يُولَّد تلقائياً بتنسيق `WP-{YYYY}-{SubStationCode}-{SEQ:0000}` مع قفل تزامن (Concurrency)
15. **WarehouseSupply.SupplyNumber** — يُولَّد: `SUP-{YYYY}-{StationCode}-{SEQ:0000}`
16. **DispatchRequest.RequestNumber** — يُولَّد: `DRQ-{YYYY}-{SubStationCode}-{SEQ:0000}`

### قواعد الواجهة الأمامية
17. **Arabic RTL** — جميع الصفحات والمودالات يجب أن تدعم RTL
18. **i18next** — إضافة مفاتيح الترجمة لجميع النصوص الجديدة
19. **PermissionGate** — تغليف جميع أزرار الإضافة/التعديل/الحذف/الاعتماد
20. **Loading states** — كل fetch يجب أن يعرض Spinner أثناء التحميل
21. **Error handling** — كل API call يجب أن يعالج الخطأ ويعرض رسالة للمستخدم
22. **Confirmation modal** — قبل كل حذف أو اعتماد أو رفض
23. **Real-time updates** — الاستماع لـ SignalR events المتعلقة بالصيانة والمخازن
24. **لا استخدام axios مباشرة** — استخدم `client` (الـ axios instance من `frontend/src/api/client.js`)

---

## ترتيب التنفيذ المقترح

```
Phase 1: Domain Models + EF Configuration + Migrations (Lookup Seed → Entities)
Phase 2: Application Layer (DTOs + Interfaces + Validators)
Phase 3: Infrastructure (Service Implementations)
Phase 4: API Controllers + DI Registration في Program.cs
Phase 5: Frontend — WarehouseParts.js + WarehouseStock.js
Phase 6: Frontend — WarehouseSupply.js + WarehouseDispatch.js
Phase 7: Frontend — WarehouseReports.js
Phase 8: Frontend — WorkPermits.js + WorkPermitDetail.js
Phase 9: Frontend — InstalledParts.js + MaintenanceDashboard.js
Phase 10: SignalR Notifications + RBAC Integration
Phase 11: Testing شامل لكل الصفحات والـ endpoints
```

---

## نقطة التحقق النهائية

قبل إعلان اكتمال أي Phase، يجب:
```
□ dotnet build src\FuelTrack.sln → 0 errors
□ dotnet ef database update → ينجح بدون أخطاء
□ جميع الـ endpoints تُعيد استجابات صحيحة
□ جميع الـ validations تعمل
□ لا console errors في المتصفح
□ SignalR events تصل بشكل صحيح
□ الصلاحيات تُطبَّق على الأزرار والـ API
□ البيانات العربية تظهر بشكل صحيح (RTL)
```
