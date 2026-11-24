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
