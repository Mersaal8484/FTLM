# استراتيجية النسخ الاحتياطي والتعافي من الكوارث
## FuelTrack — Disaster Recovery & Backup Plan

---

## 1. الأهداف المؤسسية للاسترداد (SLA Targets)

| المعيار | الهدف | الوصف |
|---|---|---|
| **RPO (Recovery Point Objective)** | **15 دقيقة** | الحد الأقصى لفقدان البيانات المقبول عند وقوع كارثة |
| **RTO (Recovery Time Objective)** | **1 ساعة** | الحد الأقصى للوقت المستغرق لإعادة تشغيل النظام بالكامل |
| **Availability Target** | **99.9%** | نسبة توافر النظام والخدمات التشغيلية |

---

## 2. جدول النسخ الاحتياطي المؤتمت (Backup Schedule)

```
┌─────────────────┬──────────────┬───────────────────────────────┬─────────────────┐
│ نوع النسخ       │ التكرار      │ التوقيت                       │ فترة الاستبقاء  │
├─────────────────┼──────────────┼───────────────────────────────┼─────────────────┤
│ Full Backup     │ أسبوعي       │ كل يوم جمعة الساعة 01:00 صباحاً │ 30 يوماً        │
│ Differential    │ يومي         │ يومياً الساعة 02:00 صباحاً     │ 14 يوماً        │
│ Transaction Log │ كل 15 دقيقة  │ على مدار الساعة               │ 7 أيام          │
└─────────────────┴──────────────┴───────────────────────────────┴─────────────────┘
```

---

## 3. سياسة استبقاء وأرشفة البيانات (Data Retention & Archival Policy)

- **السجلات التشغيلية المباشرة (Active Ledger)**:
  - تحتفظ جداول `DailyReadings` و `FuelTransactions` و `OilTransactions` بالسجلات النشطة للفترة الحالية (حتى سنة واحدة).
- **أرشفة سجلات التدقيق (`AuditLogArchives`)**:
  - تُنقل السجلات الأقدم من **90 يوماً** تلقائياً إلى جداول الأرشيف عبر مهمة `DataArchivalJob` الشهرية.
- **أرشفة قراءات المولدات (`DailyReadingArchives`)**:
  - تُنقل القراءات المعتمدة الأقدم من **365 يوماً** إلى جداول الأرشيف لتقليل حجم الفهارس وتسريع الاستعلامات اليومية.

---

## 4. خطوات الاسترداد السريع عند الطوارئ (Disaster Recovery Runbook)

### الخطوة 1: التحقق من جاهزية الخادم وقاعدة البيانات
```powershell
# التحقق من تشغيل خدمة SQL Server
Get-Service -Name "MSSQLSERVER"
```

### الخطوة 2: استعادة النسخة الاحتياطية الكاملة (Full Backup)
```sql
RESTORE DATABASE [FuelTrack]
FROM DISK = 'D:\Backups\FuelTrack_Full.bak'
WITH NORECOVERY, REPLACE;
```

### الخطوة 3: تطبيق النسخة التفاضلية وسجلات المعاملات (Differential & Logs)
```sql
RESTORE DATABASE [FuelTrack]
FROM DISK = 'D:\Backups\FuelTrack_Diff.bak'
WITH NORECOVERY;

RESTORE LOG [FuelTrack]
FROM DISK = 'D:\Backups\FuelTrack_Log.trn'
WITH RECOVERY;
```

### الخطوة 4: التحقق من سلامة البيانات وإعادة بناء الفهارس
```sql
DBCC CHECKDB ('FuelTrack') WITH NO_INFOMSGS;
EXEC sp_updatestats;
```

### الخطوة 5: التحقق من فحص صحة النظام (Health Check)
```bash
curl -i http://localhost:5000/health/ready
```
يجب أن تعود الاستجابة بـ `HTTP 200 OK` مع حالة `"Healthy"`.
