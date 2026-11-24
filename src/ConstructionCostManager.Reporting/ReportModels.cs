using ConstructionCostManager.Core.Entities;
using ConstructionCostManager.Core.Models;

namespace ConstructionCostManager.Reporting;

public enum FinancialReportType
{
    FullProjectCost = 0,
    Materials = 1,
    Workers = 2,
    Phases = 3,
    DateRangeExpenses = 4,
    DebtsAndRemaining = 5,
    BudgetVsActual = 6
}

public class ReportRequest
{
    public FinancialReportType ReportType { get; set; }
    public Project Project { get; set; } = new();
    public ProjectFinancialSummary Summary { get; set; } = new();
    public List<MaterialPurchase> MaterialPurchases { get; set; } = new();
    public List<WorkerWorkEntry> WorkEntries { get; set; } = new();
    public List<OtherExpense> OtherExpenses { get; set; } = new();
    public List<SearchResultItem> DateRangeItems { get; set; } = new();
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public string GetReportTitle() => ReportType switch
    {
        FinancialReportType.FullProjectCost => "تقرير تكاليف المشروع بالكامل",
        FinancialReportType.Materials => "تقرير مواد البناء",
        FinancialReportType.Workers => "تقرير أجور العمال والحرفيين",
        FinancialReportType.Phases => "تقرير تكاليف مراحل البناء",
        FinancialReportType.DateRangeExpenses => "تقرير المصاريف حسب الفترة الزمنية",
        FinancialReportType.DebtsAndRemaining => "تقرير الديون والمبالغ المتبقية",
        FinancialReportType.BudgetVsActual => "تقرير مقارنة الميزانية بالتكلفة الفعلية",
        _ => "تقرير مالي"
    };
}
