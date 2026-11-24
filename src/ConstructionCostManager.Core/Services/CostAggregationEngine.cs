using ConstructionCostManager.Core.Entities;
using ConstructionCostManager.Core.Enums;
using ConstructionCostManager.Core.Helpers;
using ConstructionCostManager.Core.Models;

namespace ConstructionCostManager.Core.Services;

/// <summary>
/// Pure, deterministic multi-level cost aggregation engine.
/// Ensures zero double-counting and strict separation between accrued costs and cashflow payments.
/// </summary>
public static class CostAggregationEngine
{
    public static ProjectFinancialSummary CalculateProjectSummary(
        Project project,
        IEnumerable<ConstructionPhase> phases,
        IEnumerable<MaterialPurchase> purchases,
        IEnumerable<WorkerWorkEntry> workEntries,
        IEnumerable<OtherExpense> otherExpenses,
        IEnumerable<Payment> payments)
    {
        var activePhases = phases.Where(p => !p.IsDeleted && p.ProjectId == project.Id)
            .OrderBy(p => p.DisplayOrder)
            .ToList();
        var phaseLookup = activePhases.ToDictionary(p => p.Id, p => p);

        var activePurchases = purchases.Where(p => !p.IsDeleted && p.ProjectId == project.Id).ToList();
        var activeWorkEntries = workEntries.Where(w => !w.IsDeleted && w.ProjectId == project.Id).ToList();
        var activeOtherExpenses = otherExpenses.Where(e => !e.IsDeleted && e.ProjectId == project.Id).ToList();
        var activePayments = payments.Where(p => !p.IsDeleted && p.ProjectId == project.Id).ToList();

        // Group payments by (ObligationType, ReferenceId)
        var paidByObligation = activePayments
            .GroupBy(p => (p.ObligationType, p.ReferenceId))
            .ToDictionary(g => g.Key, g => Math.Round(g.Sum(x => x.Amount), 2, MidpointRounding.AwayFromZero));

        decimal GetPaid(ObligationType type, int refId) =>
            paidByObligation.TryGetValue((type, refId), out var amt) ? amt : 0m;

        // Subtotals
        var materialsSubtotal = Math.Round(activePurchases.Sum(p => p.MaterialsSubtotal), 2, MidpointRounding.AwayFromZero);
        var materialTransportCost = Math.Round(activePurchases.Sum(p => p.TransportCost), 2, MidpointRounding.AwayFromZero);
        var totalLaborCost = Math.Round(activeWorkEntries.Sum(w => w.TotalAmount), 2, MidpointRounding.AwayFromZero);

        var otherTransportCost = Math.Round(
            activeOtherExpenses.Where(e => e.Category == ExpenseCategory.Transport).Sum(e => e.Amount), 2, MidpointRounding.AwayFromZero);
        var equipmentRentalCost = Math.Round(
            activeOtherExpenses.Where(e => e.Category == ExpenseCategory.EquipmentRental).Sum(e => e.Amount), 2, MidpointRounding.AwayFromZero);
        var additionalExpensesCost = Math.Round(
            activeOtherExpenses.Where(e => e.Category == ExpenseCategory.Additional).Sum(e => e.Amount), 2, MidpointRounding.AwayFromZero);

        var totalAccruedCost = Math.Round(
            materialsSubtotal + materialTransportCost + totalLaborCost + otherTransportCost + equipmentRentalCost + additionalExpensesCost,
            2, MidpointRounding.AwayFromZero);

        // Obligations list (exact per-record accrued vs paid vs remaining)
        var obligations = new List<ObligationSummary>();

        foreach (var purchase in activePurchases)
        {
            var paid = GetPaid(ObligationType.MaterialPurchase, purchase.Id);
            obligations.Add(new ObligationSummary
            {
                ObligationType = ObligationType.MaterialPurchase,
                ReferenceId = purchase.Id,
                Title = string.IsNullOrWhiteSpace(purchase.InvoiceNumber)
                    ? $"شراء مواد ({purchase.Items.Count(i => !i.IsDeleted)} صنف)"
                    : $"فاتورة #{purchase.InvoiceNumber}",
                PartyName = string.IsNullOrWhiteSpace(purchase.SupplierName) ? "مورد غير محدد" : purchase.SupplierName,
                PhaseId = purchase.PhaseId,
                PhaseName = phaseLookup.TryGetValue(purchase.PhaseId, out var ph) ? ph.Name : "مرحلة غير محددة",
                Date = purchase.PurchaseDate,
                TotalAmount = purchase.TotalAmount,
                PaidAmount = paid
            });
        }

        foreach (var work in activeWorkEntries)
        {
            var paid = GetPaid(ObligationType.WorkerWork, work.Id);
            obligations.Add(new ObligationSummary
            {
                ObligationType = ObligationType.WorkerWork,
                ReferenceId = work.Id,
                Title = string.IsNullOrWhiteSpace(work.WorkType) ? "عمل عمال" : work.WorkType,
                PartyName = work.Worker?.Name ?? $"عامل #{work.WorkerId}",
                PhaseId = work.PhaseId,
                PhaseName = phaseLookup.TryGetValue(work.PhaseId, out var ph) ? ph.Name : "مرحلة غير محددة",
                Date = work.WorkDate,
                TotalAmount = work.TotalAmount,
                PaidAmount = paid
            });
        }

        foreach (var expense in activeOtherExpenses)
        {
            var paid = GetPaid(ObligationType.OtherExpense, expense.Id);
            obligations.Add(new ObligationSummary
            {
                ObligationType = ObligationType.OtherExpense,
                ReferenceId = expense.Id,
                Title = expense.Title,
                PartyName = string.IsNullOrWhiteSpace(expense.VendorOrParty)
                    ? ArabicDisplayHelper.GetExpenseCategoryLabel(expense.Category)
                    : expense.VendorOrParty,
                PhaseId = expense.PhaseId,
                PhaseName = phaseLookup.TryGetValue(expense.PhaseId, out var ph) ? ph.Name : "مرحلة غير محددة",
                Date = expense.ExpenseDate,
                TotalAmount = Math.Round(expense.Amount, 2, MidpointRounding.AwayFromZero),
                PaidAmount = paid
            });
        }

        var totalPaid = Math.Round(obligations.Sum(o => o.PaidAmount), 2, MidpointRounding.AwayFromZero);
        var totalRemainingDebt = Math.Round(obligations.Sum(o => Math.Max(0m, o.RemainingAmount)), 2, MidpointRounding.AwayFromZero);

        // Phase summaries
        var phaseSummaries = activePhases.Select(phase =>
        {
            var pPurchases = activePurchases.Where(p => p.PhaseId == phase.Id).ToList();
            var pWork = activeWorkEntries.Where(w => w.PhaseId == phase.Id).ToList();
            var pOther = activeOtherExpenses.Where(e => e.PhaseId == phase.Id).ToList();

            var matCost = Math.Round(pPurchases.Sum(p => p.MaterialsSubtotal), 2, MidpointRounding.AwayFromZero);
            var labCost = Math.Round(pWork.Sum(w => w.TotalAmount), 2, MidpointRounding.AwayFromZero);
            var otherCost = Math.Round(
                pPurchases.Sum(p => p.TransportCost) + pOther.Sum(e => e.Amount), 2, MidpointRounding.AwayFromZero);
            var phaseTotal = Math.Round(matCost + labCost + otherCost, 2, MidpointRounding.AwayFromZero);

            var phasePaid = Math.Round(
                pPurchases.Sum(p => GetPaid(ObligationType.MaterialPurchase, p.Id)) +
                pWork.Sum(w => GetPaid(ObligationType.WorkerWork, w.Id)) +
                pOther.Sum(e => GetPaid(ObligationType.OtherExpense, e.Id)),
                2, MidpointRounding.AwayFromZero);

            return new PhaseCostSummary
            {
                PhaseId = phase.Id,
                PhaseName = phase.Name,
                DisplayOrder = phase.DisplayOrder,
                MaterialsCost = matCost,
                LaborCost = labCost,
                TransportAndOtherCost = otherCost,
                TotalCost = phaseTotal,
                PaidAmount = phasePaid,
                RemainingDebt = Math.Max(0m, Math.Round(phaseTotal - phasePaid, 2, MidpointRounding.AwayFromZero)),
                PercentageOfTotal = totalAccruedCost > 0
                    ? Math.Round((phaseTotal / totalAccruedCost) * 100m, 2, MidpointRounding.AwayFromZero)
                    : 0m
            };
        }).ToList();

        // Expense category breakdowns
        var categoryBreakdowns = new List<CategoryBreakdown>();
        void AddCat(string key, string name, decimal amt)
        {
            if (amt <= 0) return;
            categoryBreakdowns.Add(new CategoryBreakdown
            {
                CategoryKey = key,
                CategoryName = name,
                Amount = amt,
                Percentage = totalAccruedCost > 0 ? Math.Round((amt / totalAccruedCost) * 100m, 2, MidpointRounding.AwayFromZero) : 0m
            });
        }

        AddCat("Materials", "مواد البناء", materialsSubtotal);
        AddCat("Labor", "أجور العمال", totalLaborCost);
        AddCat("Transport", "النقل والشحن", materialTransportCost + otherTransportCost);
        AddCat("Equipment", "كراء المعدات", equipmentRentalCost);
        AddCat("Additional", "مصاريف إضافية", additionalExpensesCost);

        // Material sub-category breakdown
        var materialCategoryBreakdowns = activePurchases
            .SelectMany(p => p.Items.Where(i => !i.IsDeleted))
            .GroupBy(i => i.Category)
            .Select(g =>
            {
                var amt = Math.Round(g.Sum(i => i.TotalAmount), 2, MidpointRounding.AwayFromZero);
                return new CategoryBreakdown
                {
                    CategoryKey = g.Key.ToString(),
                    CategoryName = ArabicDisplayHelper.GetMaterialCategoryLabel(g.Key),
                    Amount = amt,
                    Percentage = materialsSubtotal > 0
                        ? Math.Round((amt / materialsSubtotal) * 100m, 2, MidpointRounding.AwayFromZero)
                        : 0m
                };
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        // Monthly trends
        var monthlyBuckets = new Dictionary<(int Year, int Month), MonthlyExpenseTrend>();
        MonthlyExpenseTrend GetBucket(DateTime date)
        {
            var key = (date.Year, date.Month);
            if (!monthlyBuckets.TryGetValue(key, out var bucket))
            {
                bucket = new MonthlyExpenseTrend { Year = date.Year, Month = date.Month };
                monthlyBuckets[key] = bucket;
            }
            return bucket;
        }

        foreach (var p in activePurchases)
        {
            var b = GetBucket(p.PurchaseDate);
            b.MaterialsCost += p.MaterialsSubtotal;
            b.OtherCost += p.TransportCost;
        }
        foreach (var w in activeWorkEntries)
        {
            var b = GetBucket(w.WorkDate);
            b.LaborCost += w.TotalAmount;
        }
        foreach (var e in activeOtherExpenses)
        {
            var b = GetBucket(e.ExpenseDate);
            b.OtherCost += e.Amount;
        }
        foreach (var pay in activePayments)
        {
            var b = GetBucket(pay.PaymentDate);
            b.PaidCost += pay.Amount;
        }

        var monthlyTrends = monthlyBuckets.Values
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToList();

        return new ProjectFinancialSummary
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            CurrencyCode = project.CurrencyCode,
            EstimatedBudget = project.EstimatedBudget,
            MaterialsSubtotal = materialsSubtotal,
            MaterialTransportCost = materialTransportCost,
            TotalLaborCost = totalLaborCost,
            TotalOtherTransportCost = otherTransportCost,
            TotalEquipmentRentalCost = equipmentRentalCost,
            TotalAdditionalExpensesCost = additionalExpensesCost,
            TotalAccruedCost = totalAccruedCost,
            TotalPaid = totalPaid,
            TotalRemainingDebt = totalRemainingDebt,
            PhaseSummaries = phaseSummaries,
            ExpenseCategoryBreakdowns = categoryBreakdowns,
            MaterialCategoryBreakdowns = materialCategoryBreakdowns,
            MonthlyTrends = monthlyTrends,
            Obligations = obligations.OrderByDescending(o => o.Date).ToList()
        };
    }
}
