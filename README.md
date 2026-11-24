# Construction Cost Manager — دليل التشغيل

## تشغيل التطبيق

```powershell
# من مجلد المشروع الرئيسي:
cd "d:\Projectes\Apps\Construction Cost Manager"
dotnet run --project src/ConstructionCostManager.Web
```

ثم افتح المتصفح على: **http://localhost:5000** (أو المنفذ الظاهر في وحدة الطرفية)

---

## هيكل المشروع

```
Construction Cost Manager/
├── src/
│   ├── ConstructionCostManager.Core/         ← طبقة النطاق (Entities, Enums, Models, Helpers)
│   │   ├── Entities/                         ← Project, MaterialPurchase, Worker, Payment...
│   │   ├── Enums/                            ← جميع Enums بالتسميات العربية
│   │   ├── Helpers/ArabicDisplayHelper.cs    ← تنسيق الأرقام والتسميات
│   │   ├── Models/CostSummaryModels.cs       ← نماذج التقارير والملخصات
│   │   └── Services/CostAggregationEngine.cs ← محرك تجميع التكاليف متعدد المستويات
│   │
│   ├── ConstructionCostManager.Data/         ← طبقة البيانات (EF Core + SQLite)
│   │   ├── AppDbContext.cs                   ← DbContext مع Soft-Delete و Audit
│   │   └── Services/
│   │       ├── ConstructionCostService.cs    ← جميع عمليات CRUD والبحث
│   │       └── DatabaseBackupService.cs      ← النسخ الاحتياطي والاستعادة
│   │
│   ├── ConstructionCostManager.Reporting/    ← محرك التقارير
│   │   ├── ExcelReportGenerator.cs           ← تصدير Excel (ClosedXML)
│   │   └── PdfReportGenerator.cs             ← تصدير PDF (QuestPDF)
│   │
│   └── ConstructionCostManager.Web/          ← Blazor Server (ASP.NET Core 9)
│       ├── Program.cs                        ← DI + Minimal API endpoints
│       ├── Components/Pages/
│       │   ├── Home.razor                    ← لوحة التحكم الرئيسية Dashboard
│       │   ├── Projects.razor                ← إدارة المشاريع
│       │   ├── Materials.razor               ← تسجيل مواد البناء (فواتير متعددة الأصناف)
│       │   ├── Workers.razor                 ← إدارة العمال والأجور
│       │   ├── Expenses.razor                ← النقل وكراء المعدات والمصاريف
│       │   ├── Phases.razor                  ← مراحل البناء + التكاليف حسب كل مرحلة
│       │   ├── Payments.razor                ← المدفوعات والديون والالتزامات
│       │   ├── Search.razor                  ← البحث والتصفية المتقدمة
│       │   ├── Reports.razor                 ← التقارير وتصدير PDF/Excel/طباعة A4
│       │   └── Settings.razor                ← الإعدادات والنسخ الاحتياطي
│       └── wwwroot/app.css                   ← CSS عربي RTL + Dark/Light Mode
│
└── tests/ConstructionCostManager.Tests/
    ├── CostAggregationEngineTests.cs         ← اختبارات محرك تجميع التكاليف
    └── ConstructionCostServiceTests.cs       ← اختبارات التكامل مع SQLite في الذاكرة
```

---

## نقاط API للتقارير والنسخ الاحتياطي

| النقطة | الوصف |
|--------|-------|
| `GET /api/reports/pdf?projectId=1&type=0` | تصدير PDF لتقرير التكاليف الكلية |
| `GET /api/reports/excel?projectId=1&type=0` | تصدير Excel |
| `GET /api/backup/download` | تحميل نسخة احتياطية SQLite |

**أنواع التقارير (type):**
- 0 = تقرير التكاليف الكلية
- 1 = تقرير المواد
- 2 = تقرير العمال
- 3 = تقرير المراحل
- 4 = تقرير حسب الفترة (يقبل `from` و `to`)
- 5 = تقرير الديون والمبالغ المتبقية
- 6 = مقارنة الميزانية بالتكلفة الفعلية

---

## مبادئ الحساب المالي

```
إجمالي المادة          = الكمية × سعر الوحدة
إجمالي الفاتورة         = مجموع المواد + تكاليف النقل
أجرة العامل            = عدد الوحدات × سعر الوحدة
التكلفة المستحقة (Accrued) = مواد + عمال + نقل + معدات + مصاريف
المدفوع فعلاً          = مجموع الدفعات المسجلة (منفصل عن المستحق)
الديون                  = Σ max(0, مستحق_الالتزام − مدفوع_الالتزام)
الميزانية المتبقية      = الميزانية التقديرية − التكلفة المستحقة
```

> ⚠️ لا يُعدّ المبلغ غير المدفوع كمصروف نقدي — يُظهر كـ"التزام" مستقل

---

## تشغيل الاختبارات

```powershell
dotnet test --no-restore
```

الاختبارات تغطي: حساب التكاليف متعدد المستويات، عزل المشاريع، الحذف الآمن، توليد PDF وExcel، البحث والتصفية.
