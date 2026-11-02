# خطة عمل المرحلة 5: الامتثال المؤسسي، استبقاء وأرشفة البيانات، والتعافي من الكوارث
## FuelTrack — Phase 5: Enterprise Compliance, Data Retention & Disaster Recovery

بناءً على التقرير الشامل لتدقيق النظام ([system_audit_report.md](file:///C:/Users/User/Desktop/FuelTrack-Rebuild/docs/system_audit_report.md))، تهدف **المرحلة 5** إلى استكمال المتطلبات التنظيمية والمؤسسية المتقدمة لضمان استمرارية الأعمال (Business Continuity)، وضع سياسات استبقاء وأرشفة البيانات المؤتمتة (Data Retention & Archival)، وتوثيق خطة النسخ الاحتياطي والتعافي من الكوارث (Disaster Recovery & Backup Strategy).

---

## 🎯 أهداف المرحلة 5 الرئيسية

1. **سياسة وأتمتة استبقاء وأرشفة البيانات (Automated Data Retention & Archival):**
   - تنفيذ خدمة أرشفة مجدولة تنقل السجلات التاريخية القديمة (مثل `AuditLogs` و `DailyReadings` الأقدم من الفترة المحددة) إلى جداول الأرشيف (`AuditLogArchives`, `DailyReadingArchives`).
   - إعداد خيارات تكوين مرنة لفترة الاستبقاء (Data Retention Period) في `appsettings.json`.

2. **استراتيجية النسخ الاحتياطي والتعافي من الكوارث (Backup & Disaster Recovery Strategy):**
   - توثيق وتنفيذ خطة نسخ احتياطي دوري مؤتمتة لقاعدة بيانات SQL Server (Full, Differential, Transaction Log Backups).
   - تحديد معايير أهداف الاسترداد المؤسسية (RPO: 15 minutes, RTO: 1 hour).
   - إنشاء سكريبتات SQL / PowerShell المخصصة للنسخ الاحتياطي واستعادة النظام.

3. **حماية الخصوصية وضوابط الامتثال المؤسسي (GDPR & Data Privacy Controls):**
   - إضافة نقطة نهاية (Endpoint) لتصدير وتطهير بيانات المستخدمين وطلبات النسيان وفق اللوائح التنظيمية.
   - تعزيز تسجيل أحداث الوصول للبيانات الحساسة.

4. **توسيع مراقبة الـ SLA ومقاييس الأداء (SLA & Health Metrics):**
   - تعزيز الـ Health Checks ومقاييس Prometheus بمعدلات الـ Uptime وزمن استجابة العمليات الحرجة.

---

## 📋 تفاصيل المهام المقترحة

### 1. خدمة أرشفة واستبقاء البيانات المؤتمتة (Data Archival Service)
**الملفات:**
- `src/FuelTrack.Application/Interfaces/IDataArchivalService.cs`
- `src/FuelTrack.Infrastructure/Services/DataArchivalService.cs`
- `src/FuelTrack.API/Jobs/DataArchivalJob.cs`
- تحديث [`Program.cs`](file:///C:/Users/User/Desktop/FuelTrack-Rebuild/src/FuelTrack.API/Program.cs) لجدولة مهمة الأرشفة الشهرية عبر Hangfire.

### 2. خطة وسكريبتات النسخ الاحتياطي المؤتمت (Automated Backup Scripts & DR Guide)
**الملفات:**
- `docs/DISASTER_RECOVERY_AND_BACKUP_PLAN.md`
- `scripts/backup-database.sql`
- `scripts/restore-database.sql`

### 3. ضوابط الخصوصية والامتثال (Privacy & Compliance)
**الملفات:**
- `src/FuelTrack.API/Controllers/ComplianceController.cs` (تصدير سجلات المستخدم، تدقيق الخصوصية)

---

## 🗂️ الملفات المتأثرة

### [NEW]
- `src/FuelTrack.Application/Interfaces/IDataArchivalService.cs`
- `src/FuelTrack.Infrastructure/Services/DataArchivalService.cs`
- `src/FuelTrack.API/Jobs/DataArchivalJob.cs`
- `docs/DISASTER_RECOVERY_AND_BACKUP_PLAN.md`
- `scripts/backup-database.sql`

### [MODIFY]
- [`src/FuelTrack.API/Program.cs`](file:///C:/Users/User/Desktop/FuelTrack-Rebuild/src/FuelTrack.API/Program.cs)
- [`src/FuelTrack.Infrastructure/FuelTrackDbContext.cs`](file:///C:/Users/User/Desktop/FuelTrack-Rebuild/src/FuelTrack.Infrastructure/FuelTrackDbContext.cs)

---

## 🧪 خطة التحقق والاعتماد (Verification Plan)

### Automated Tests
1. **اختبارات وحدة لخدمة الأرشفة (Unit Tests):**
   - اختبار نقل السجلات المؤهلة فقط للأرشيف وحذفها من الجداول التشغيلية بنجاح.
   ```powershell
   dotnet test
   dotnet build
   ```
2. **التحقق من سلامة البناء:**
   ```powershell
   npm run build
   ```

### Manual Verification
- التحقق من تنفيذ مهمة الأرشفة بنجاح والتأكد من مطابقة السجلات في جداول الأرشيف.
