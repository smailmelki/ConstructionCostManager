using ConstructionCostManager.Core.Entities;
using ConstructionCostManager.Core.Enums;
using ConstructionCostManager.Core.Services;

namespace ConstructionCostManager.Tests;

public class CostAggregationEngineTests
{
    [Fact]
    public void CalculateProjectSummary_ComputesMultiLevelCostsAndSeparatesAccruedVsPaidVsDebt()
    {
        var project = new Project { Id = 1, Name = "فيلا سكنية", EstimatedBudget = 5_000_000m, CurrencyCode = "DZD" };
        var phase1 = new ConstructionPhase { Id = 10, ProjectId = 1, Name = "الحفر والأساسات", DisplayOrder = 1 };
        var phase2 = new ConstructionPhase { Id = 11, ProjectId = 1, Name = "الخرسانة والهيكل", DisplayOrder = 2 };

        var purchase = new MaterialPurchase
        {
            Id = 100,
            ProjectId = 1,
            PhaseId = 10,
            SupplierName = "مؤسسة الأمل",
            PurchaseDate = new DateTime(2026, 3, 10),
            TransportCost = 5_000m,
            Items = new List<MaterialItem>
            {
                new() { Id = 1, Name = "إسمنت", Category = MaterialCategory.Cement, Quantity = 50m, UnitPrice = 950m }, // 47,500
                new() { Id = 2, Name = "حديد", Category = MaterialCategory.Steel, Quantity = 2m, UnitPrice = 140_000m } // 280,000
            }
        }; // MaterialsSubtotal = 327,500; Total = 332,500

        var worker = new Worker { Id = 20, ProjectId = 1, Name = "أحمد بن علي", Specialty = WorkerSpecialty.Mason };
        var workEntry = new WorkerWorkEntry
        {
            Id = 200,
            ProjectId = 1,
            WorkerId = 20,
            Worker = worker,
            PhaseId = 11,
            WorkType = "صب الأعمدة",
            CalculationMethod = LaborCalculationMethod.Daily,
            UnitsCompleted = 10m,
            UnitRate = 4_000m,
            WorkDate = new DateTime(2026, 3, 15)
        }; // 40,000

        var equipment = new OtherExpense
        {
            Id = 300,
            ProjectId = 1,
            PhaseId = 10,
            Category = ExpenseCategory.EquipmentRental,
            Title = "كراء حفارة",
            Amount = 60_000m,
            ExpenseDate = new DateTime(2026, 3, 5)
        };

        var payments = new List<Payment>
        {
            new() { Id = 1, ProjectId = 1, ObligationType = ObligationType.MaterialPurchase, ReferenceId = 100, Amount = 200_000m, PaymentDate = new DateTime(2026, 3, 10) },
            new() { Id = 2, ProjectId = 1, ObligationType = ObligationType.WorkerWork, ReferenceId = 200, Amount = 15_000m, PaymentDate = new DateTime(2026, 3, 16) },
            new() { Id = 3, ProjectId = 1, ObligationType = ObligationType.WorkerWork, ReferenceId = 200, Amount = 10_000m, PaymentDate = new DateTime(2026, 3, 20) }
        };

        var summary = CostAggregationEngine.CalculateProjectSummary(
            project,
            new[] { phase1, phase2 },
            new[] { purchase },
            new[] { workEntry },
            new[] { equipment },
            payments);

        Assert.Equal(327_500m, summary.MaterialsSubtotal);
        Assert.Equal(5_000m, summary.MaterialTransportCost);
        Assert.Equal(40_000m, summary.TotalLaborCost);
        Assert.Equal(60_000m, summary.TotalEquipmentRentalCost);
        Assert.Equal(432_500m, summary.TotalAccruedCost);

        // Cashflow vs Debt separation
        Assert.Equal(225_000m, summary.TotalPaid);
        Assert.Equal(207_500m, summary.TotalRemainingDebt); // 132,500 material + 15,000 worker + 60,000 equipment
        Assert.Equal(4_567_500m, summary.RemainingBudget);
        Assert.Equal(8.65m, summary.BudgetConsumptionPercentage);

        var p1Summary = summary.PhaseSummaries.Single(p => p.PhaseId == 10);
        Assert.Equal(392_500m, p1Summary.TotalCost);
        Assert.Equal(200_000m, p1Summary.PaidAmount);
        Assert.Equal(192_500m, p1Summary.RemainingDebt);

        var p2Summary = summary.PhaseSummaries.Single(p => p.PhaseId == 11);
        Assert.Equal(40_000m, p2Summary.TotalCost);
        Assert.Equal(25_000m, p2Summary.PaidAmount);
        Assert.Equal(15_000m, p2Summary.RemainingDebt);
    }

    [Fact]
    public void CalculateProjectSummary_IsolatesProjectsAndIgnoresSoftDeletedRecords()
    {
        var project1 = new Project { Id = 1, Name = "مشروع 1", EstimatedBudget = 1_000_000m };
        var phase1 = new ConstructionPhase { Id = 1, ProjectId = 1, Name = "مرحلة 1", DisplayOrder = 1 };

        var otherExpenses = new[]
        {
            new OtherExpense { Id = 1, ProjectId = 1, PhaseId = 1, Category = ExpenseCategory.Additional, Title = "رخصة", Amount = 10_000m },
            new OtherExpense { Id = 2, ProjectId = 1, PhaseId = 1, Category = ExpenseCategory.Additional, Title = "محذوف", Amount = 50_000m, IsDeleted = true },
            new OtherExpense { Id = 3, ProjectId = 2, PhaseId = 99, Category = ExpenseCategory.Additional, Title = "مشروع آخر", Amount = 90_000m }
        };

        var summary = CostAggregationEngine.CalculateProjectSummary(
            project1,
            new[] { phase1 },
            Array.Empty<MaterialPurchase>(),
            Array.Empty<WorkerWorkEntry>(),
            otherExpenses,
            Array.Empty<Payment>());

        Assert.Equal(10_000m, summary.TotalAccruedCost);
        Assert.Equal(0m, summary.TotalPaid);
        Assert.Equal(10_000m, summary.TotalRemainingDebt);
    }
}
