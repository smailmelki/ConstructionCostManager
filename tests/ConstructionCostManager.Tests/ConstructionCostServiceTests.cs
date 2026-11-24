using ConstructionCostManager.Core.Entities;
using ConstructionCostManager.Core.Enums;
using ConstructionCostManager.Core.Models;
using ConstructionCostManager.Data;
using ConstructionCostManager.Data.Services;
using ConstructionCostManager.Reporting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ConstructionCostManager.Tests;

public class ConstructionCostServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly ConstructionCostService _service;

    public ConstructionCostServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _service = new ConstructionCostService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateProject_Seeds13DefaultPhases_AndPreventsDeletingPhaseWithExpenses()
    {
        var project = await _service.CreateProjectAsync(new Project
        {
            Name = "عمارة النور",
            Location = "الجزائر العاصمة",
            OwnerName = "كريم",
            EstimatedBudget = 12_000_000m
        });

        var phases = await _service.GetPhasesAsync(project.Id);
        Assert.Equal(13, phases.Count);

        var firstPhase = phases.First();
        await _service.AddOtherExpenseAsync(new OtherExpense
        {
            ProjectId = project.Id,
            PhaseId = firstPhase.Id,
            Category = ExpenseCategory.Additional,
            Title = "دراسة التربة",
            Amount = 45_000m
        });

        var (success, error) = await _service.DeletePhaseAsync(firstPhase.Id);
        Assert.False(success);
        Assert.NotEmpty(error);
    }

    [Fact]
    public async Task AddMaterialPurchase_RejectsInitialPaymentAboveTotal_AndDoesNotSaveAnything()
    {
        var project = await _service.CreateProjectAsync(new Project { Name = "مشروع المواد" });
        var phase = (await _service.GetPhasesAsync(project.Id)).First();

        var purchase = new MaterialPurchase
        {
            ProjectId = project.Id,
            PhaseId = phase.Id,
            Items = new List<MaterialItem>
            {
                new() { Name = "إسمنت", Quantity = 1m, UnitPrice = 100m }
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AddMaterialPurchaseAsync(purchase, initialPaidAmount: 101m));

        Assert.Empty(await _service.GetMaterialPurchasesAsync(project.Id));
        Assert.Empty(await _service.GetPaymentsAsync(project.Id));
    }

    [Fact]
    public async Task RecordPayment_RejectsOverpaymentAndCrossProjectReference()
    {
        var project = await _service.CreateProjectAsync(new Project { Name = "المشروع الأول" });
        var otherProject = await _service.CreateProjectAsync(new Project { Name = "المشروع الثاني" });
        var phase = (await _service.GetPhasesAsync(project.Id)).First();

        var purchase = await _service.AddMaterialPurchaseAsync(new MaterialPurchase
        {
            ProjectId = project.Id,
            PhaseId = phase.Id,
            Items = new List<MaterialItem>
            {
                new() { Name = "رمل", Quantity = 1m, UnitPrice = 100m }
            }
        });

        await _service.RecordPaymentAsync(new Payment
        {
            ProjectId = project.Id,
            ObligationType = ObligationType.MaterialPurchase,
            ReferenceId = purchase.Id,
            Amount = 70m
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RecordPaymentAsync(new Payment
            {
                ProjectId = project.Id,
                ObligationType = ObligationType.MaterialPurchase,
                ReferenceId = purchase.Id,
                Amount = 31m
            }));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RecordPaymentAsync(new Payment
            {
                ProjectId = otherProject.Id,
                ObligationType = ObligationType.MaterialPurchase,
                ReferenceId = purchase.Id,
                Amount = 1m
            }));

        var payments = await _service.GetPaymentsAsync(project.Id);
        Assert.Single(payments);
        Assert.Equal(70m, payments[0].Amount);
    }

    [Fact]
    public async Task AddExpenses_RejectsPhaseOrWorkerFromAnotherProject()
    {
        var project = await _service.CreateProjectAsync(new Project { Name = "مشروع أ" });
        var otherProject = await _service.CreateProjectAsync(new Project { Name = "مشروع ب" });
        var projectPhase = (await _service.GetPhasesAsync(project.Id)).First();
        var otherPhase = (await _service.GetPhasesAsync(otherProject.Id)).First();
        var otherWorker = await _service.AddWorkerAsync(new Worker
        {
            ProjectId = otherProject.Id,
            Name = "عامل المشروع ب"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AddOtherExpenseAsync(new OtherExpense
            {
                ProjectId = project.Id,
                PhaseId = otherPhase.Id,
                Title = "مصروف غير صحيح",
                Amount = 50m
            }));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AddWorkEntryAsync(new WorkerWorkEntry
            {
                ProjectId = project.Id,
                WorkerId = otherWorker.Id,
                PhaseId = projectPhase.Id,
                WorkType = "بناء",
                UnitsCompleted = 1m,
                UnitRate = 100m
            }));

        Assert.Empty(await _service.GetOtherExpensesAsync(project.Id));
        Assert.Empty(await _service.GetWorkEntriesAsync(project.Id));
    }

    [Fact]
    public async Task SearchAndReporting_WorkEndToEndWithSqlite()
    {
        var project = await _service.CreateProjectAsync(new Project
        {
            Name = "مشروع تجريبي",
            EstimatedBudget = 2_000_000m
        });
        var phases = await _service.GetPhasesAsync(project.Id);

        var purchase = await _service.AddMaterialPurchaseAsync(new MaterialPurchase
        {
            ProjectId = project.Id,
            PhaseId = phases[1].Id,
            SupplierName = "مؤسسة البناء الحديث",
            InvoiceNumber = "INV-001",
            PurchaseDate = new DateTime(2026, 4, 1),
            TransportCost = 3_000m,
            Items = new List<MaterialItem>
            {
                new() { Name = "إسمنت بورتلاند", Category = MaterialCategory.Cement, Quantity = 100m, Unit = MeasurementUnit.Bag, UnitPrice = 900m }
            }
        }, initialPaidAmount: 50_000m);

        var searchResults = await _service.SearchExpensesAsync(new ExpenseSearchFilter
        {
            ProjectId = project.Id,
            SearchText = "إسمنت",
            OnlyUnpaid = true
        });

        Assert.Single(searchResults);
        Assert.Equal(93_000m, searchResults[0].TotalAmount);
        Assert.Equal(50_000m, searchResults[0].PaidAmount);
        Assert.Equal(43_000m, searchResults[0].RemainingAmount);

        var summary = await _service.GetProjectFinancialSummaryAsync(project.Id);
        Assert.NotNull(summary);

        var req = new ReportRequest
        {
            ReportType = FinancialReportType.FullProjectCost,
            Project = project,
            Summary = summary!,
            MaterialPurchases = await _service.GetMaterialPurchasesAsync(project.Id)
        };

        var excelBytes = ExcelReportGenerator.Generate(req);
        var pdfBytes = PdfReportGenerator.Generate(req);

        Assert.True(excelBytes.Length > 500);
        Assert.True(pdfBytes.Length > 500);
        // PDF magic header %PDF
        Assert.Equal((byte)'%', pdfBytes[0]);
        Assert.Equal((byte)'P', pdfBytes[1]);
        Assert.Equal((byte)'D', pdfBytes[2]);
        Assert.Equal((byte)'F', pdfBytes[3]);
    }
}
