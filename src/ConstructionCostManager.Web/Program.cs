using ConstructionCostManager.Core.Models;
using ConstructionCostManager.Data;
using ConstructionCostManager.Data.Services;
using ConstructionCostManager.Reporting;
using ConstructionCostManager.Web.Components;
using ConstructionCostManager.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var dataDir = Path.Combine(builder.Environment.ContentRootPath, "Data");
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "construction_costs.db");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<IConstructionCostService, ConstructionCostService>();
builder.Services.AddSingleton(new DatabaseBackupService(dbPath));
builder.Services.AddScoped<ActiveProjectState>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

// Report & Backup API Endpoints
app.MapGet("/api/reports/excel", async (
    int projectId,
    FinancialReportType type,
    DateTime? from,
    DateTime? to,
    IConstructionCostService service) =>
{
    var request = await BuildReportRequestAsync(projectId, type, from, to, service);
    if (request is null) return Results.NotFound();
    var bytes = ExcelReportGenerator.Generate(request);
    var fileName = $"report_{projectId}_{type}_{DateTime.Now:yyyyMMdd}.xlsx";
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
});

app.MapGet("/api/reports/pdf", async (
    int projectId,
    FinancialReportType type,
    DateTime? from,
    DateTime? to,
    IConstructionCostService service) =>
{
    var request = await BuildReportRequestAsync(projectId, type, from, to, service);
    if (request is null) return Results.NotFound();
    var bytes = PdfReportGenerator.Generate(request);
    var fileName = $"report_{projectId}_{type}_{DateTime.Now:yyyyMMdd}.pdf";
    return Results.File(bytes, "application/pdf", fileName);
});

app.MapGet("/api/backup/download", async (DatabaseBackupService backupService) =>
{
    var bytes = await backupService.ExportBackupBytesAsync();
    return Results.File(bytes, "application/octet-stream", $"construction_costs_backup_{DateTime.Now:yyyyMMdd_HHmm}.db");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static async Task<ReportRequest?> BuildReportRequestAsync(
    int projectId,
    FinancialReportType type,
    DateTime? from,
    DateTime? to,
    IConstructionCostService service)
{
    var project = await service.GetProjectByIdAsync(projectId);
    var summary = await service.GetProjectFinancialSummaryAsync(projectId);
    if (project is null || summary is null) return null;

    var req = new ReportRequest
    {
        ReportType = type,
        Project = project,
        Summary = summary,
        FromDate = from,
        ToDate = to
    };

    if (type == FinancialReportType.Materials)
        req.MaterialPurchases = await service.GetMaterialPurchasesAsync(projectId);
    else if (type == FinancialReportType.Workers)
        req.WorkEntries = await service.GetWorkEntriesAsync(projectId);
    else if (type == FinancialReportType.DateRangeExpenses)
        req.DateRangeItems = await service.SearchExpensesAsync(new ExpenseSearchFilter
        {
            ProjectId = projectId,
            FromDate = from,
            ToDate = to
        });

    return req;
}
