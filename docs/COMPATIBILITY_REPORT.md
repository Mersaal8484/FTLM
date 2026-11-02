# تقرير التوافق — FuelTrack + نظام الصيانة والمخازن

> **السؤال:** هل سيعمل النظامان معاً؟  
> **الجواب المختصر:** نعم — لكن يوجد **5 نقاط حرجة** تحتاج معالجة دقيقة قبل التنفيذ.

---

## أولاً: ما سيعمل تلقائياً دون أي تعديل ✅

| العنصر | السبب |
|--------|-------|
| **المصادقة (JWT + Cookies)** | كل API call جديد سيمر عبر `client.js` الذي يُضيف الـ token تلقائياً |
| **Audit Trail** | `AuditSaveChangesInterceptor` يُطبَّق على أي entity يرث من `BaseEntity` — ستُسجَّل كل تغييرات الكيانات الجديدة تلقائياً |
| **Soft Delete** | `DeletedAt` موجود في `BaseEntity` — يكفي فقط إضافة `HasQueryFilter` في `OnModelCreating` للكيانات الجديدة |
| **SignalR Hub** | نفس الـ hub موجود — يكفي إرسال events جديدة باسم مختلف |
| **RBAC / Permissions** | نظام الصلاحيات JSON-based يمكنه استيعاب مفاتيح جديدة بدون تعديل في قاعدة البيانات |
| **FluentValidation** | يُسجَّل بالـ Assembly — أي Validator جديد في نفس الـ Assembly يُفعَّل تلقائياً |
| **Health Checks** | يفحص `FuelTrackDbContext` — الكيانات الجديدة مُضافة لنفس الـ Context |
| **نظام الترجمة (i18next)** | ملفات الترجمة مستقلة — يكفي إضافة مفاتيح جديدة |

---

## ثانياً: تعديلات مطلوبة في ملفات موجودة (بسيطة وآمنة) ⚠️

هذه التعديلات **لا تكسر** أي وظيفة قائمة — هي إضافات صرفة:

### 1. `FuelTrackDbContext.cs`
**إضافة:**
```csharp
// DbSets الجديدة (ضمن الكلاس)
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

// في OnModelCreating: إضافة filters وعلاقات الكيانات الجديدة
```
**لا يُمسّ** أي إعداد قائم.

### 2. `Program.cs`
**إضافة في كتلة الـ services:**
```csharp
builder.Services.AddScoped<ISparePartService, SparePartService>();
builder.Services.AddScoped<IWarehouseStockService, WarehouseStockService>();
builder.Services.AddScoped<IWarehouseSupplyService, WarehouseSupplyService>();
builder.Services.AddScoped<IDispatchRequestService, DispatchRequestService>();
builder.Services.AddScoped<IWarehouseTransactionService, WarehouseTransactionService>();
builder.Services.AddScoped<IWarehouseReportService, WarehouseReportService>();
builder.Services.AddScoped<IWorkPermitService, WorkPermitService>();
builder.Services.AddScoped<IInstalledPartService, InstalledPartService>();
builder.Services.AddScoped<IMaintenanceDashboardService, MaintenanceDashboardService>();
```
**لا يُمسّ** أي service قائم.

### 3. `frontend/src/App.js`
**إضافة Routes جديدة:**
```jsx
const WarehouseParts    = lazy(() => import('./pages/WarehouseParts'));
const WarehouseStock    = lazy(() => import('./pages/WarehouseStock'));
const WarehouseSupply   = lazy(() => import('./pages/WarehouseSupply'));
const WarehouseDispatch = lazy(() => import('./pages/WarehouseDispatch'));
const WarehouseReports  = lazy(() => import('./pages/WarehouseReports'));
const WorkPermits       = lazy(() => import('./pages/WorkPermits'));
const InstalledParts    = lazy(() => import('./pages/InstalledParts'));
const MaintenanceDash   = lazy(() => import('./pages/MaintenanceDashboard'));

// في Routes:
<Route path="/warehouse/parts"    element={<Protected {...guardProps} module="warehouse"><WarehouseParts /></Protected>} />
<Route path="/warehouse/stock"    element={<Protected {...guardProps} module="warehouse"><WarehouseStock /></Protected>} />
<Route path="/warehouse/supply"   element={<Protected {...guardProps} module="warehouse"><WarehouseSupply /></Protected>} />
<Route path="/warehouse/dispatch" element={<Protected {...guardProps} module="warehouse"><WarehouseDispatch /></Protected>} />
<Route path="/warehouse/reports"  element={<Protected {...guardProps} module="warehouse"><WarehouseReports /></Protected>} />
<Route path="/maintenance"        element={<Protected {...guardProps} module="maintenance"><MaintenanceDash /></Protected>} />
<Route path="/maintenance/permits" element={<Protected {...guardProps} module="maintenance"><WorkPermits /></Protected>} />
<Route path="/maintenance/installed-parts" element={<Protected {...guardProps} module="maintenance"><InstalledParts /></Protected>} />
```

### 4. `frontend/src/components/Sidebar.js`
**إضافة قسمَين جديدَين في `mainNav`:**
```jsx
// قسم المخازن
{ to: '/warehouse/dispatch', icon: PackageCheck, label: ar ? 'طلبات الصرف' : 'Dispatch Requests', show: can('warehouse') },
{ to: '/warehouse/supply',   icon: Boxes,        label: ar ? 'توريد المخازن'  : 'Warehouse Supply',  show: can('warehouse') },

// قسم الصيانة
{ to: '/maintenance',         icon: Wrench,       label: ar ? 'لوحة الصيانة'  : 'Maintenance',       show: can('maintenance') },
{ to: '/maintenance/permits', icon: FileText,     label: ar ? 'تصاريح العمل'  : 'Work Permits',      show: can('maintenance') },
```

---

## ثالثاً: النقاط الحرجة — يجب معالجتها أو ستفشل ❌

---

### [حرج-1] تعارض معرّفات Lookup (سيُسبّب فشل Migration)

**المشكلة:**
الـ Lookups الحالية تصل إلى ID=54 والـ LookupCategories إلى ID=18. أي Migration جديدة تستخدم IDs أقل من ذلك ستفشل بخطأ Primary Key violation.

**الملاحظة الخطرة:** LookupCategory رقم 11 غير مستخدم (قفزت من 10 إلى 12). يجب التحقق قبل استخدامه.

**الحل:** بدء Seed للكيانات الجديدة من:
```csharp
// LookupCategories: تبدأ من ID = 19
// Lookups:          تبدأ من ID = 55
```

جدول التوزيع المقترح للـ Lookups الجديدة:
| الفئة | CategoryId | IDs المقترحة |
|-------|-----------|-------------|
| PartCategory (6 قيم) | 19 | 55–60 |
| PartUnit (6 قيم) | 20 | 61–66 |
| MaintenanceType (7 قيم) | 21 | 67–73 |
| MaintenanceDepartment (5 قيم) | 22 | 74–78 |
| MaintenancePriority (4 قيم) | 23 | 79–82 |
| MaintenanceStatus (7 قيم) | 24 | 83–89 |
| DispatchStatus (6 قيم) | 25 | 90–95 |
| WarehouseSupplyStatus (2 قيم) | 26 | 96–97 |
| WarehouseTransactionType (5 قيم) | 27 | 98–102 |
| ReplacementPriority (4 قيم) | 28 | 103–106 |

---

### [حرج-2] Unique Index على WarehouseStock يتعارض مع Soft Delete

**المشكلة:**
```csharp
// هذا سيفشل عند إنشاء سجل جديد بعد soft-delete للقديم:
.HasIndex(s => new { s.SparePartId, s.StationId }).IsUnique()
// السجل القديم (DeletedAt ≠ null) لا يزال في قاعدة البيانات
// → PK violation عند إضافة نفس الزوج مرة أخرى
```

**الحل الصحيح:**
```csharp
modelBuilder.Entity<WarehouseStock>()
    .HasIndex(s => new { s.SparePartId, s.StationId })
    .IsUnique()
    .HasFilter("[DeletedAt] IS NULL"); // Filtered Index — يتجاهل السجلات المحذوفة
```

نفس المشكلة تنطبق على `SparePart.PartNumber`:
```csharp
modelBuilder.Entity<SparePart>()
    .HasIndex(s => s.PartNumber)
    .IsUnique()
    .HasFilter("[DeletedAt] IS NULL");
```

---

### [حرج-3] تعارض متعدد FKs على نفس الجدول (Multiple FK to Same Table)

**المشكلة:**
`DispatchRequest` يحتوي على:
- `SubStationId` → `SubStations` 
- `StationId` → `Stations`

`WorkPermit` يحتوي على:
- `RequestedById` → `Users`
- `AssignedToId` → `Users`
- `ApprovedById` → `Users`

عند وجود أكثر من FK يشير إلى نفس الجدول، EF Core يطلب تحديداً صريحاً للعلاقة وإلا ستحدث أخطاء `InvalidOperationException`:

**الحل الصريح الذي يجب كتابته:**
```csharp
// في OnModelCreating — لكل FK بشكل منفصل:

modelBuilder.Entity<WorkPermit>()
    .HasOne(wp => wp.RequestedBy)
    .WithMany()
    .HasForeignKey(wp => wp.RequestedById)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<WorkPermit>()
    .HasOne(wp => wp.AssignedTo)
    .WithMany()
    .HasForeignKey(wp => wp.AssignedToId)
    .OnDelete(DeleteBehavior.SetNull);

modelBuilder.Entity<WorkPermit>()
    .HasOne(wp => wp.ApprovedBy)
    .WithMany()
    .HasForeignKey(wp => wp.ApprovedById)
    .OnDelete(DeleteBehavior.SetNull);

// وكذلك لـ DispatchRequest:
modelBuilder.Entity<DispatchRequest>()
    .HasOne(dr => dr.SubStation)
    .WithMany()
    .HasForeignKey(dr => dr.SubStationId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<DispatchRequest>()
    .HasOne(dr => dr.Station)  // المخزن
    .WithMany()
    .HasForeignKey(dr => dr.StationId)
    .OnDelete(DeleteBehavior.Restrict);
```

---

### [حرج-4] ضمان الترقيم التسلسلي بدون Race Condition

**المشكلة:**
إذا أرسل مستخدمان طلب إنشاء `WorkPermit` في نفس الوقت:
```csharp
// كلاهما يجلب آخر رقم = WP-2026-001
// كلاهما يحسب التالي = WP-2026-002
// → تعارض Unique Key
```

**الحل في SQL Server:**
استخدام `SEQUENCE` objects بدلاً من MAX+1:

```csharp
// في Migration جديدة:
protected override void Up(MigrationBuilder migrationBuilder)
{
    // تسلسل واحد لكل محطة فرعية (يُنشأ ديناميكياً بالكود)
    migrationBuilder.Sql(@"
        CREATE TABLE PermitNumberCounters (
            SubStationId INT NOT NULL PRIMARY KEY,
            LastSequence INT NOT NULL DEFAULT 0
        );
    ");
}
```

```csharp
// في WorkPermitService.GeneratePermitNumberAsync:
public async Task<string> GeneratePermitNumberAsync(int subStationId, CancellationToken ct = default)
{
    // قفل pessimistic على السطر
    await _db.Database.ExecuteSqlRawAsync(
        "UPDATE PermitNumberCounters SET LastSequence = LastSequence + 1 WHERE SubStationId = {0}; " +
        "INSERT INTO PermitNumberCounters (SubStationId, LastSequence) " +
        "SELECT {0}, 1 WHERE NOT EXISTS (SELECT 1 FROM PermitNumberCounters WHERE SubStationId = {0});",
        subStationId);
    
    var seq = await _db.Database.SqlQuery<int>(
        $"SELECT LastSequence AS Value FROM PermitNumberCounters WHERE SubStationId = {subStationId}")
        .FirstAsync(ct);
    
    var code = await _db.SubStations
        .Where(s => s.Id == subStationId)
        .Select(s => s.Code)
        .FirstAsync(ct);
    
    return $"WP-{DateTime.UtcNow.Year}-{code}-{seq:D4}";
}
```

---

### [حرج-5] إدارة ReservedQuantity بدون Concurrency Issues

**المشكلة:**
إذا طلب موقعان في نفس اللحظة نفس القطعة بكمية تساوي المخزون المتاح:
```
المخزون المتاح = 10
طلب A: يحجز 10 ← المحجوز = 10
طلب B: يحجز 10 ← يرى المحجوز = 0 (لم يتحدث بعد) ← يحجز 10
النتيجة: محجوز 20 من مخزون 10 → سالب!
```

**الحل:** استخدام Database-level locking عند تحديث الـ ReservedQuantity:
```csharp
public async Task CreateAsync(CreateDispatchRequestDto dto, int userId)
{
    // استخدام transaction مع lock صريح
    await using var tx = await _db.Database.BeginTransactionAsync(
        System.Data.IsolationLevel.Serializable);
    try
    {
        foreach (var item in dto.Items)
        {
            // قراءة + قفل في آن واحد
            var stock = await _db.WarehouseStocks
                .FromSqlRaw("SELECT * FROM WarehouseStocks WITH (UPDLOCK) WHERE SparePartId = {0} AND StationId = {1}",
                    item.SparePartId, dto.StationId)
                .FirstOrDefaultAsync();

            var available = stock.CurrentQuantity - stock.ReservedQuantity;
            // لا نمنع الطلب — فقط نرصد الكمية المتاحة ليراها المعتمد
            stock.ReservedQuantity += item.RequestedQuantity;
        }
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }
}
```

---

## رابعاً: نقاط لا تحتاج تعديل في الكيانات الموجودة ✅

**السؤال الشائع:** هل يجب إضافة navigation properties للـ `Generator`, `Station`, `SubStation`؟

**الجواب:** لا. EF Core يسمح بتعريف العلاقة من جهة واحدة فقط:

```csharp
// يكفي هذا في OnModelCreating — لا حاجة لإضافة ICollection في Generator.cs:
modelBuilder.Entity<InstalledPart>()
    .HasOne(ip => ip.Generator)
    .WithMany()  // ← WithMany() بدون parameter = لا navigation على الطرف الآخر
    .HasForeignKey(ip => ip.GeneratorId)
    .OnDelete(DeleteBehavior.Restrict);
```

هذا يعني أن `Generator.cs` و`Station.cs` و`SubStation.cs` **لا تحتاج أي تعديل**.

---

## خامساً: ملخص التوافق النهائي

```
✅ يعمل تلقائياً              (8 عناصر)
⚠️ تعديلات بسيطة وآمنة        (4 ملفات — إضافات فقط)
❌ نقاط حرجة تحتاج معالجة      (5 نقاط — مُوثَّقة بحلولها أعلاه)
```

### الخلاصة:
**النظامان سيعملان معاً بشكل كامل** إذا تم الالتزام بـ:
1. IDs جديدة للـ Lookups تبدأ من 55 للقيم ومن 19 للفئات
2. Filtered Unique Index على `WarehouseStock`
3. تعريف صريح لكل FK عند تعدد العلاقات لنفس الجدول
4. `UPDLOCK` في Serializable transaction لمنع حجز زائد للمخزون
5. Counter Table بدلاً من MAX+1 لتوليد أرقام التصاريح

جميع الكيانات الحالية، الخدمات، الـ controllers، والواجهة الأمامية **لن تتأثر** لأن التغييرات كلها إضافية وليست تعديلية.
