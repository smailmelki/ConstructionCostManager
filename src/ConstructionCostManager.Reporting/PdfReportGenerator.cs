using ConstructionCostManager.Core.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ConstructionCostManager.Reporting;

public static class PdfReportGenerator
{
    static PdfReportGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Generate(ReportRequest request)
    {
        var currency = request.Project.CurrencyCode;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));
                page.ContentFromRightToLeft();

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(request.GetReportTitle()).FontSize(16).Bold().FontColor("#1E3A5F");
                            c.Item().Text($"المشروع: {request.Project.Name} | المالك: {request.Project.OwnerName}").FontSize(10);
                            c.Item().Text($"الموقع: {request.Project.Location} | العملة: {currency}").FontSize(9).FontColor(Colors.Grey.Darken2);
                        });
                        row.ConstantItem(120).AlignLeft().Column(c =>
                        {
                            c.Item().Text($"التاريخ: {DateTime.Now:yyyy-MM-dd}").FontSize(9);
                            c.Item().Text($"الحالة: {ArabicDisplayHelper.GetStatusLabel(request.Project.Status)}").FontSize(9);
                        });
                    });
                    col.Item().PaddingVertical(6).LineHorizontal(1).LineColor("#CBD5E1");
                });

                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Spacing(10);
                    switch (request.ReportType)
                    {
                        case FinancialReportType.FullProjectCost:
                            BuildSummaryGrid(col, request, currency);
                            BuildPhasesTable(col, request, currency);
                            break;

                        case FinancialReportType.Phases:
                            BuildPhasesTable(col, request, currency);
                            break;

                        case FinancialReportType.BudgetVsActual:
                            BuildSummaryGrid(col, request, currency);
                            break;

                        case FinancialReportType.Materials:
                            BuildMaterialsTable(col, request, currency);
                            break;

                        case FinancialReportType.Workers:
                            BuildWorkersTable(col, request, currency);
                            break;

                        case FinancialReportType.DebtsAndRemaining:
                            BuildDebtsTable(col, request, currency);
                            break;

                        case FinancialReportType.DateRangeExpenses:
                            BuildDateRangeTable(col, request, currency);
                            break;
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("صفحة ");
                    x.CurrentPageNumber();
                    x.Span(" من ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void BuildSummaryGrid(ColumnDescriptor col, ReportRequest request, string currency)
    {
        var s = request.Summary;
        var rows = new (string Label, string Val)[]
        {
            ("الميزانية التقديرية", ArabicDisplayHelper.FormatMoney(s.EstimatedBudget, currency)),
            ("مجموع مواد البناء", ArabicDisplayHelper.FormatMoney(s.MaterialsSubtotal, currency)),
            ("مجموع نقل المواد", ArabicDisplayHelper.FormatMoney(s.MaterialTransportCost, currency)),
            ("مجموع أجور العمال", ArabicDisplayHelper.FormatMoney(s.TotalLaborCost, currency)),
            ("مجموع النقل الإضافي", ArabicDisplayHelper.FormatMoney(s.TotalOtherTransportCost, currency)),
            ("مجموع كراء المعدات", ArabicDisplayHelper.FormatMoney(s.TotalEquipmentRentalCost, currency)),
            ("مجموع المصاريف الإضافية", ArabicDisplayHelper.FormatMoney(s.TotalAdditionalExpensesCost, currency)),
            ("التكلفة الإجمالية للمشروع", ArabicDisplayHelper.FormatMoney(s.TotalAccruedCost, currency)),
            ("إجمالي المبالغ المدفوعة", ArabicDisplayHelper.FormatMoney(s.TotalPaid, currency)),
            ("الديون والمبالغ المتبقية", ArabicDisplayHelper.FormatMoney(s.TotalRemainingDebt, currency)),
            ("الميزانية المتبقية", ArabicDisplayHelper.FormatMoney(s.RemainingBudget, currency)),
            ("نسبة استهلاك الميزانية", $"{s.BudgetConsumptionPercentage:N2} %")
        };

        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2);
                c.RelativeColumn(1);
            });

            table.Header(h =>
            {
                h.Cell().Background("#1E3A5F").Padding(5).Text("البند المالي").FontColor(Colors.White).Bold();
                h.Cell().Background("#1E3A5F").Padding(5).Text("القيمة").FontColor(Colors.White).Bold();
            });

            foreach (var (label, val) in rows)
            {
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(label);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(5).Text(val).Bold();
            }
        });
    }

    private static void BuildPhasesTable(ColumnDescriptor col, ReportRequest request, string currency)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2);
                c.RelativeColumn(1);
                c.RelativeColumn(1);
                c.RelativeColumn(1);
                c.RelativeColumn(1);
                c.RelativeColumn(1);
            });

            table.Header(h =>
            {
                foreach (var title in new[] { "المرحلة", "المواد", "العمال", "النقل/أخرى", "الإجمالي", "المتبقي" })
                    h.Cell().Background("#1E3A5F").Padding(4).Text(title).FontColor(Colors.White).Bold().FontSize(9);
            });

            foreach (var ph in request.Summary.PhaseSummaries)
            {
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ph.PhaseName).FontSize(9);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ArabicDisplayHelper.FormatMoney(ph.MaterialsCost, currency)).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ArabicDisplayHelper.FormatMoney(ph.LaborCost, currency)).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ArabicDisplayHelper.FormatMoney(ph.TransportAndOtherCost, currency)).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ArabicDisplayHelper.FormatMoney(ph.TotalCost, currency)).Bold().FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ArabicDisplayHelper.FormatMoney(ph.RemainingDebt, currency)).FontSize(8);
            }
        });
    }

    private static void BuildMaterialsTable(ColumnDescriptor col, ReportRequest request, string currency)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1);
                c.RelativeColumn(2);
                c.RelativeColumn(1);
                c.RelativeColumn(1);
                c.RelativeColumn(1);
                c.RelativeColumn(1);
            });

            table.Header(h =>
            {
                foreach (var title in new[] { "التاريخ", "المادة", "المورد", "الكمية", "سعر الوحدة", "الإجمالي" })
                    h.Cell().Background("#1E3A5F").Padding(4).Text(title).FontColor(Colors.White).Bold().FontSize(9);
            });

            foreach (var p in request.MaterialPurchases)
            {
                foreach (var item in p.Items.Where(i => !i.IsDeleted))
                {
                    table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(p.PurchaseDate.ToString("yyyy-MM-dd")).FontSize(8);
                    table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(item.Name).FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(p.SupplierName).FontSize(8);
                    table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text($"{item.Quantity:N2} {ArabicDisplayHelper.GetUnitLabel(item.Unit)}").FontSize(8);
                    table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ArabicDisplayHelper.FormatMoney(item.UnitPrice, currency)).FontSize(8);
                    table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ArabicDisplayHelper.FormatMoney(item.TotalAmount, currency)).Bold().FontSize(8);
                }
            }
        });
    }

    private static void BuildWorkersTable(ColumnDescriptor col, ReportRequest request, string currency)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1);
                c.RelativeColumn(1.5f);
                c.RelativeColumn(1.5f);
                c.RelativeColumn(1);
                c.RelativeColumn(1);
                c.RelativeColumn(1);
            });

            table.Header(h =>
            {
                foreach (var title in new[] { "التاريخ", "العامل", "العمل", "الحساب", "الكمية", "الإجمالي" })
                    h.Cell().Background("#1E3A5F").Padding(4).Text(title).FontColor(Colors.White).Bold().FontSize(9);
            });

            foreach (var w in request.WorkEntries)
            {
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(w.WorkDate.ToString("yyyy-MM-dd")).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(w.Worker?.Name ?? "").FontSize(9);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(w.WorkType).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ArabicDisplayHelper.GetCalculationMethodLabel(w.CalculationMethod)).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text($"{w.UnitsCompleted:N2}").FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ArabicDisplayHelper.FormatMoney(w.TotalAmount, currency)).Bold().FontSize(8);
            }
        });
    }

    private static void BuildDebtsTable(ColumnDescriptor col, ReportRequest request, string currency)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1);
                c.RelativeColumn(1.5f);
                c.RelativeColumn(1.5f);
                c.RelativeColumn(1);
                c.RelativeColumn(1);
                c.RelativeColumn(1);
            });

            table.Header(h =>
            {
                foreach (var title in new[] { "التاريخ", "البيان", "الطرف", "المستحق", "المدفوع", "المتبقي" })
                    h.Cell().Background("#1E3A5F").Padding(4).Text(title).FontColor(Colors.White).Bold().FontSize(9);
            });

            foreach (var ob in request.Summary.Obligations.Where(o => o.RemainingAmount > 0))
            {
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ob.Date.ToString("yyyy-MM-dd")).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ob.Title).FontSize(9);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ob.PartyName).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ArabicDisplayHelper.FormatMoney(ob.TotalAmount, currency)).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ArabicDisplayHelper.FormatMoney(ob.PaidAmount, currency)).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ArabicDisplayHelper.FormatMoney(ob.RemainingAmount, currency)).Bold().FontColor("#B91C1C").FontSize(8);
            }
        });
    }

    private static void BuildDateRangeTable(ColumnDescriptor col, ReportRequest request, string currency)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1);
                c.RelativeColumn(1.2f);
                c.RelativeColumn(1.8f);
                c.RelativeColumn(1);
                c.RelativeColumn(1);
                c.RelativeColumn(1);
            });

            table.Header(h =>
            {
                foreach (var title in new[] { "التاريخ", "المرحلة", "البيان", "المستحق", "المدفوع", "المتبقي" })
                    h.Cell().Background("#1E3A5F").Padding(4).Text(title).FontColor(Colors.White).Bold().FontSize(9);
            });

            foreach (var item in request.DateRangeItems)
            {
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(item.Date.ToString("yyyy-MM-dd")).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(item.PhaseName).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(item.Title).FontSize(9);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ArabicDisplayHelper.FormatMoney(item.TotalAmount, currency)).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ArabicDisplayHelper.FormatMoney(item.PaidAmount, currency)).FontSize(8);
                table.Cell().BorderBottom(1).BorderColor("#E2E8F0").Padding(4).Text(ArabicDisplayHelper.FormatMoney(item.RemainingAmount, currency)).FontSize(8);
            }
        });
    }
}
