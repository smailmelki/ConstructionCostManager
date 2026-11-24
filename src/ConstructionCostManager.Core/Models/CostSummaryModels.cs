using ConstructionCostManager.Core.Enums;

namespace ConstructionCostManager.Core.Models;

public class ProjectFinancialSummary
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "DZD";

    public decimal EstimatedBudget { get; set; }

    // Level 2: Subtotals (التكاليف الجزئية)
    public decimal MaterialsSubtotal { get; set; }
    public decimal MaterialTransportCost { get; set; }
    public decimal TotalMaterialsWithTransport => MaterialsSubtotal + MaterialTransportCost;

    public decimal TotalLaborCost { get; set; }
    public decimal TotalOtherTransportCost { get; set; }
    public decimal TotalTransportCost => MaterialTransportCost + TotalOtherTransportCost;
    public decimal TotalEquipmentRentalCost { get; set; }
    public decimal TotalAdditionalExpensesCost { get; set; }

    // Level 3: Total Accrued Cost (التكلفة الإجمالية المستحقة)
    public decimal TotalAccruedCost { get; set; }

    // Cashflow vs Debt separation
    public decimal TotalPaid { get; set; }
    public decimal TotalRemainingDebt { get; set; }

    public decimal RemainingBudget => EstimatedBudget - TotalAccruedCost;
    public decimal BudgetConsumptionPercentage =>
        EstimatedBudget > 0 ? Math.Round((TotalAccruedCost / EstimatedBudget) * 100m, 2, MidpointRounding.AwayFromZero) : 0m;

    public bool IsOverBudget => EstimatedBudget > 0 && TotalAccruedCost > EstimatedBudget;

    public List<PhaseCostSummary> PhaseSummaries { get; set; } = new();
    public List<CategoryBreakdown> ExpenseCategoryBreakdowns { get; set; } = new();
    public List<CategoryBreakdown> MaterialCategoryBreakdowns { get; set; } = new();
    public List<MonthlyExpenseTrend> MonthlyTrends { get; set; } = new();
    public List<ObligationSummary> Obligations { get; set; } = new();
}

public class PhaseCostSummary
{
    public int PhaseId { get; set; }
    public string PhaseName { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public decimal MaterialsCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal TransportAndOtherCost { get; set; }
    public decimal TotalCost { get; set; }

    public decimal PaidAmount { get; set; }
    public decimal RemainingDebt { get; set; }
    public decimal PercentageOfTotal { get; set; }
}

public class CategoryBreakdown
{
    public string CategoryKey { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}

public class MonthlyExpenseTrend
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label => $"{Year:D4}-{Month:D2}";
    public decimal MaterialsCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OtherCost { get; set; }
    public decimal TotalCost => MaterialsCost + LaborCost + OtherCost;
    public decimal PaidCost { get; set; }
}

public class ObligationSummary
{
    public ObligationType ObligationType { get; set; }
    public int ReferenceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public int PhaseId { get; set; }
    public string PhaseName { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount => Math.Round(TotalAmount - PaidAmount, 2, MidpointRounding.AwayFromZero);
    public bool IsFullyPaid => RemainingAmount <= 0m;
    public bool IsOverpaid => RemainingAmount < 0m;

    public string StatusLabel => RemainingAmount switch
    {
        <= 0m => "مدفوع بالكامل",
        _ when PaidAmount > 0m => "مدفوع جزئياً",
        _ => "غير مدفوع"
    };
}

public class ExpenseSearchFilter
{
    public int ProjectId { get; set; }
    public string? SearchText { get; set; }
    public int? PhaseId { get; set; }
    public ObligationType? ObligationType { get; set; }
    public MaterialCategory? MaterialCategory { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public bool OnlyUnpaid { get; set; }
}

public class SearchResultItem
{
    public ObligationType ObligationType { get; set; }
    public int ReferenceId { get; set; }
    public DateTime Date { get; set; }
    public string PhaseName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
}
