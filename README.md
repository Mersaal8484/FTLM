# FuelTrack

نظام لإدارة الوقود مبني على ASP.NET Core وReact.

## تنظيم المشروع

```text
src/                         تطبيقات وخدمات .NET
  FuelTrack.API/             واجهة ASP.NET Core ونقطة تشغيل الخادم
  FuelTrack.Application/     حالات الاستخدام والواجهات وDTOs
  FuelTrack.Domain/          نماذج وقواعد المجال
  FuelTrack.Infrastructure/  قاعدة البيانات وتنفيذ الخدمات
  FuelTrack.ModbusGateway/   بوابة Modbus
frontend/                    تطبيق React وملفاته واختباراته
tests/                       اختبارات .NET
docs/                        التوثيق
scripts/                     أدوات الصيانة والنسخ الاحتياطي
nginx/                       إعدادات الوكيل العكسي
loadtests/                   اختبارات الضغط
FuelTrack.sln                حل مشاريع .NET
docker-compose.yml           إعداد تشغيل الخدمات بالحاويات
```

## التشغيل للتطوير

يتطلب المشروع .NET SDK 10 وNode.js وSQL Server. اضبط سلسلة الاتصال والإعدادات اللازمة في بيئة التطوير أو عبر متغيرات البيئة؛ راجع إعدادات التطبيق و`.env.example` كنقطة بداية.

من جذر المشروع شغّل الخادم:

```powershell
dotnet restore FuelTrack.sln
dotnet run --project src/FuelTrack.API --launch-profile http
```

في طرفية أخرى شغّل الواجهة:

```powershell
cd frontend
npm ci
npm run dev
```

تعمل الواجهة افتراضياً على `http://localhost:3000` والخادم على `http://localhost:5000`. يوجه Vite طلبات `/api` و`/hubs` إلى الخادم.

## التحقق

```powershell
dotnet test FuelTrack.sln
cd frontend
npm run build
npm test -- --run
```

اختبارات الواجهة عبر المتصفح موجودة في `frontend/e2e/`. توثيق المشروع وخطط النسخ الاحتياطي موجودة في `docs/`.

## التشغيل باستخدام Docker

انسخ `.env.example` إلى `.env`، واستبدل القيم المؤقتة بكلمات مرور قوية ومفتاح JWT، ومفتاح تشفير Base64 بطول 32 بايت. احتفظ بمفتاح التشفير نفسه عند إعادة التشغيل حتى تظل البيانات المشفرة قابلة للقراءة. مثال لإنشاء المفتاح باستخدام OpenSSL: `openssl rand -base64 32`.

```powershell
Copy-Item .env.example .env
# عدّل .env قبل المتابعة
docker compose up --build -d
docker compose logs -f
```

افتح `http://localhost:3000` أو المنفذ المحدد في `FRONTEND_PORT`. تمر طلبات API وSignalR عبر الواجهة إلى الخادم داخل شبكة Docker؛ تحفظ قاعدة البيانات في وحدة تخزين `sqlserver_data`. لإيقاف الخدمات استخدم `docker compose down`، ولتحديثها استخدم `docker compose up --build -d`.

إعداد Nginx مستقل في `frontend/nginx.conf` ومربوط بالحاوية للقراءة فقط. بعد تعديله، أعد تشغيل الواجهة باستخدام `docker compose restart frontend` لتطبيق التغييرات.

إذا تعذر الوصول إلى مصدر NuGet الافتراضي أثناء البناء، يمكن ضبط `NUGET_SOURCE` في `.env` على مصدر NuGet متاح.

الإعداد الافتراضي للتطوير المحلي عبر HTTP. عند التشغيل في الإنتاج اضبط `ASPNETCORE_ENVIRONMENT=Production` و`CORS_ORIGIN` على عنوان HTTPS الفعلي، وضع الخدمات خلف وكيل HTTPS مناسب؛ ملفات تعريف الارتباط في الإنتاج تتطلب HTTPS.
