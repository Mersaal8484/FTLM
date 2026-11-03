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

خطة الانتقال إلى PostgreSQL وتعدد المستأجرين ولوحة الإدارة موثقة في [docs/POSTGRES_MULTITENANT_PLAN.md](docs/POSTGRES_MULTITENANT_PLAN.md).

تتوفر لوحة منصة أولية للتطوير في `src/FuelTrack.Admin`، مع قاعدة PostgreSQL مستقلة في `docker-compose.platform.yml`. في البيئة المحلية الحالية تُشغّل بالأمر:

```powershell
docker compose --env-file .env.platform -p ftm-platform -f docker-compose.platform.yml -f docker-compose.platform.local-secrets.yml up --build -d
```

عنوان اللوحة `http://localhost:3010`. ملف `.env.platform` المحلي يحتوي إعدادات تسجيل الدخول ولا يُضاف إلى Git. يخزن إعداد التطوير المحلي مفتاح تشفير اعتمادات المستأجرين في وحدة Docker المسماة `ftm-platform_tenant_keys`؛ احتفظ بنسخة آمنة منها لأن فقدان المفتاح يمنع فك الاعتمادات. إعداد العملاء الافتراضي يستخدم SQL Server؛ إعداد PostgreSQL أدناه يقتصر على الديمو المحلي حتى اكتمال العزل. لإعداد مفتاح خارجي مختلف انظر `docker-compose.platform.provisioning.yml`.

لتشغيل الديمو المحلي بواجهة React وAPI وPostgreSQL ضمن مجموعة Docker واحدة، بعد تجهيز `.env.platform` و`.env.demo` ومجلد مفاتيح المستأجرين، استخدم:

```powershell
docker compose --env-file .env.platform --env-file .env.demo -p ftm-platform -f docker-compose.platform.yml -f docker-compose.platform.local-secrets.yml -f docker-compose.demo.yml up -d --build
```

افتح `http://demo.localhost:3000/login`. عند إضافة مستأجر محلي في لوحة الإدارة، أدخل اسماً للنطاق الفرعي مثل `company`؛ يصبح عنوانه `http://company.localhost:3000` بعد تجهيز المستأجر وإضافة اشتراك فعال وتفعيله. يستخدم جميع المستأجرين واجهة React نفسها، وتُحدد قاعدة البيانات بحسب النطاق المسجل. ملف `.env.demo` يحتوي مفاتيح الديمو المحلية ولا يُحفظ في Git. هذا الإعداد للتجربة المحلية فقط، والبث المباشر والمهام الخلفية معطّلان فيه؛ لا يصلح لنشر العملاء عبر الإنترنت.

من قائمة المستأجرين في لوحة الإدارة يمكن تعديل الاسم الظاهر للعميل ورفع شعاره أو حذفه، دون تغيير اسم المستأجر الإداري أو الاشتراك. تقبل اللوحة صور PNG وJPEG وWebP بحجم أقصاه 1 ميجابايت، وتظهر الهوية على واجهة العميل بحسب نطاقه.

في جهاز تطوير جديد، تُنشأ وحدة المفتاح مرة واحدة قبل تشغيل إعداد الأسرار المحلي. الأمر التالي يحافظ على المفتاح إن كان موجوداً:

```powershell
docker volume create ftm-platform_tenant_keys
docker run --rm --user 0 -v 'ftm-platform_tenant_keys:/keys' alpine:latest sh -c 'set -eu; if [ ! -e /keys/local-v1.key ]; then umask 077; head -c 32 /dev/urandom | base64 > /keys/local-v1.key; chown 1654:1654 /keys/local-v1.key; chmod 400 /keys/local-v1.key; fi; chown 1654:1654 /keys; chmod 700 /keys'
```

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

إذا تعذر تنزيل صورة SDK الرسمية وكانت صورة SDK موثوقة ومتوافقة متاحة محلياً، يمكن تحديدها عند البناء باستخدام الوسيط `DOTNET_SDK_IMAGE`. مثال للوحة المنصة:

```powershell
docker compose --env-file .env.platform -p ftm-platform -f docker-compose.platform.yml -f docker-compose.platform.local-secrets.yml build --build-arg DOTNET_SDK_IMAGE=fueltrack-local-sdk:10.0 admin
docker compose --env-file .env.platform -p ftm-platform -f docker-compose.platform.yml -f docker-compose.platform.local-secrets.yml up -d --no-build admin
```

الصورة البديلة في هذا المثال محلية وليست صورة Microsoft الرسمية؛ يلزم التحقق من مصدرها وتوافقها قبل استخدامها.

الإعداد الافتراضي للتطوير المحلي عبر HTTP. عند التشغيل في الإنتاج اضبط `ASPNETCORE_ENVIRONMENT=Production` و`CORS_ORIGIN` على عنوان HTTPS الفعلي، وضع الخدمات خلف وكيل HTTPS مناسب؛ ملفات تعريف الارتباط في الإنتاج تتطلب HTTPS.
