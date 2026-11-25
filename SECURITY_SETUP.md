# إعداد أمان التطبيق

أصبح التطبيق يتطلب تسجيل دخول للمستخدمين، وتحتاج واجهات التقارير والنسخ الاحتياطي إلى جلسة مصادق عليها. تنزيل النسخة الاحتياطية محمي بدور المدير.

## إنشاء hash لكلمة المرور

من جذر المستودع شغّل:

```bash
python3 tools/generate_admin_password_hash.py
```

سيطلب السكربت كلمة المرور مرتين ويطبع قيمة `Admin:PasswordHash`. لا تحفظ كلمة المرور أو قيمة hash في GitHub.

## إعداد محلي عبر User Secrets

```bash
dotnet user-secrets init --project src/ConstructionCostManager.Web
dotnet user-secrets set "Admin:Username" "admin" --project src/ConstructionCostManager.Web
dotnet user-secrets set "Admin:PasswordHash" "v1$..." --project src/ConstructionCostManager.Web
```

استبدل `v1$...` بالقيمة المطبوعة من السكربت.

## إعداد عبر متغيرات البيئة

في بيئة النشر، استخدم:

```bash
Admin__Username=admin
Admin__PasswordHash='v1$...'
```

أو ما يعادلها في مدير الأسرار الخاص بالمنصة. لا تضع كلمة مرور صريحة أو hash حقيقيًا في `appsettings.json` أو في ملفات Git.

## المسارات المحمية

- جميع صفحات Blazor تتطلب جلسة مصادق عليها.
- `/api/reports/pdf` و`/api/reports/excel` يتطلبان تسجيل الدخول.
- `/api/backup/download` يتطلب دور `Administrator`.
- `/account/login` متاح دون تسجيل دخول.
- `/account/logout` ينهي جلسة Cookie الحالية.

إذا لم تتم تهيئة `Admin:Username` و`Admin:PasswordHash`، سيظهر تحذير في السجل ولن يسمح التطبيق بتسجيل الدخول حتى تتم التهيئة.
