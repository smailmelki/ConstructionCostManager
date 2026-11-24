using ClosedXML.Excel;
using ConstructionCostManager.Core.Helpers;

namespace ConstructionCostManager.Reporting;

public static class ExcelReportGenerator
{
    public static byte[] Generate(ReportRequest request)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("التقرير المالي");
        ws.RightToLeft = true;

        int row = 1;
        ws.Cell(row, 1).Value = request.GetReportTitle();
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 15;
        row++;

        ws.Cell(row, 1).Value = $"المشروع: {request.Project.Name} | المالك: {request.Project.OwnerName} | الموقع: {request.Project.Location}";
        row++;
        ws.Cell(row, 1).Value = $"تاريخ الاستخراج: {DateTime.Now:yyyy-MM-dd HH:mm} | العملة: {request.Project.CurrencyCode}";
        row += 2;

        switch (request.ReportType)
        {
            case FinancialReportType.FullProjectCost:
                WriteKeyValueSection(ws, ref row, "ملخص التكاليف الإجمالية", new[]
                {
                    ("الميزانية التقديرية", request.Summary.EstimatedBudget),
                    ("مجموع مواد البناء", request.Summary.MaterialsSubtotal),
                    ("مجموع نقل المواد", request.Summary.MaterialTransportCost),
                    ("مجموع أجور العمال", request.Summary.TotalLaborCost),
                    ("مجموع النقل الإضافي", request.Summary.TotalOtherTransportCost),
                    ("مجموع كراء المعدات", request.Summary.TotalEquipmentRentalCost),
                    ("مجموع المصاريف الإضافية", request.Summary.TotalAdditionalExpensesCost),
                    ("التكلفة الإجمالية للمشروع", request.Summary.TotalAccruedCost),
                    ("إجمالي المبالغ المدفوعة", request.Summary.TotalPaid),
                    ("الديون والمبالغ المتبقية", request.Summary.TotalRemainingDebt),
                    ("الميزانية المتبقية", request.Summary.RemainingBudget)
                });
                row++;
                WritePhasesTable(ws, ref row, request);
                break;

            case FinancialReportType.Materials:
                WriteHeaders(ws, row++, new[] { "التاريخ", "المرحلة", "المورد", "رقم الفاتورة", "المادة", "التصنيف", "الكمية", "الوحدة", "سعر الوحدة", "النقل", "الإجمالي" });
                foreach (var p in request.MaterialPurchases)
                {
                    foreach (var item in p.Items.Where(i => !i.IsDeleted))
                    {
                        ws.Cell(row, 1).Value = p.PurchaseDate.ToString("yyyy-MM-dd");
                        ws.Cell(row, 2).Value = p.Phase?.Name ?? "";
                        ws.Cell(row, 3).Value = p.SupplierName;
                        ws.Cell(row, 4).Value = p.InvoiceNumber;
                        ws.Cell(row, 5).Value = item.Name;
                        ws.Cell(row, 6).Value = ArabicDisplayHelper.GetMaterialCategoryLabel(item.Category);
                        ws.Cell(row, 7).Value = item.Quantity;
                        ws.Cell(row, 8).Value = ArabicDisplayHelper.GetUnitLabel(item.Unit);
                        ws.Cell(row, 9).Value = item.UnitPrice;
                        ws.Cell(row, 10).Value = p.TransportCost;
                        ws.Cell(row, 11).Value = item.TotalAmount;
                        row++;
                    }
                }
                break;

            case FinancialReportType.Workers:
                WriteHeaders(ws, row++, new[] { "التاريخ", "العامل", "التخصص", "المرحلة", "نوع العمل", "طريقة الحساب", "الوحدات", "السعر", "الإجمالي" });
                foreach (var w in request.WorkEntries)
                {
                    ws.Cell(row, 1).Value = w.WorkDate.ToString("yyyy-MM-dd");
                    ws.Cell(row, 2).Value = w.Worker?.Name ?? "";
                    ws.Cell(row, 3).Value = w.Worker is not null ? ArabicDisplayHelper.GetSpecialtyLabel(w.Worker.Specialty) : "";
                    ws.Cell(row, 4).Value = w.Phase?.Name ?? "";
                    ws.Cell(row, 5).Value = w.WorkType;
                    ws.Cell(row, 6).Value = ArabicDisplayHelper.GetCalculationMethodLabel(w.CalculationMethod);
                    ws.Cell(row, 7).Value = w.UnitsCompleted;
                    ws.Cell(row, 8).Value = w.UnitRate;
                    ws.Cell(row, 9).Value = w.TotalAmount;
                    row++;
                }
                break;

            case FinancialReportType.Phases:
                WritePhasesTable(ws, ref row, request);
                break;

            case FinancialReportType.DateRangeExpenses:
                WriteHeaders(ws, row++, new[] { "التاريخ", "المرحلة", "التصنيف", "البيان", "الطرف / المورد", "المستحق", "المدفوع", "المتبقي" });
                foreach (var item in request.DateRangeItems)
                {
                    ws.Cell(row, 1).Value = item.Date.ToString("yyyy-MM-dd");
                    ws.Cell(row, 2).Value = item.PhaseName;
                    ws.Cell(row, 3).Value = item.CategoryName;
                    ws.Cell(row, 4).Value = item.Title;
                    ws.Cell(row, 5).Value = item.PartyName;
                    ws.Cell(row, 6).Value = item.TotalAmount;
                    ws.Cell(row, 7).Value = item.PaidAmount;
                    ws.Cell(row, 8).Value = item.RemainingAmount;
                    row++;
                }
                break;

            case FinancialReportType.DebtsAndRemaining:
                WriteHeaders(ws, row++, new[] { "التاريخ", "النوع", "البيان", "الطرف / العامل", "المرحلة", "المستحق", "المدفوع", "المتبقي", "الحالة" });
                foreach (var ob in request.Summary.Obligations.Where(o => o.RemainingAmount > 0))
                {
                    ws.Cell(row, 1).Value = ob.Date.ToString("yyyy-MM-dd");
                    ws.Cell(row, 2).Value = ArabicDisplayHelper.GetObligationTypeLabel(ob.ObligationType);
                    ws.Cell(row, 3).Value = ob.Title;
                    ws.Cell(row, 4).Value = ob.PartyName;
                    ws.Cell(row, 5).Value = ob.PhaseName;
                    ws.Cell(row, 6).Value = ob.TotalAmount;
                    ws.Cell(row, 7).Value = ob.PaidAmount;
                    ws.Cell(row, 8).Value = ob.RemainingAmount;
                    ws.Cell(row, 9).Value = ob.StatusLabel;
                    row++;
                }
                break;

            case FinancialReportType.BudgetVsActual:
                WriteKeyValueSection(ws, ref row, "مقارنة الميزانية التقديرية بالتكلفة الفعلية", new[]
                {
                    ("الميزانية التقديرية", request.Summary.EstimatedBudget),
                    ("التكلفة الفعلية المستحقة", request.Summary.TotalAccruedCost),
                    ("المدفوع فعلياً", request.Summary.TotalPaid),
                    ("الالتزامات المتبقية", request.Summary.TotalRemainingDebt),
                    ("الفرق (الميزانية المتبقية)", request.Summary.RemainingBudget),
                    ("نسبة الاستهلاك (%)", request.Summary.BudgetConsumptionPercentage)
                });
                break;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static void WriteHeaders(IXLWorksheet ws, int row, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            cell.Style.Font.FontColor = XLColor.White;
        }
    }

    private static void WriteKeyValueSection(IXLWorksheet ws, ref int row, string title, IEnumerable<(string Label, decimal Value)> items)
    {
        ws.Cell(row, 1).Value = title;
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        WriteHeaders(ws, row++, new[] { "البند", "القيمة" });
        foreach (var (label, value) in items)
        {
            ws.Cell(row, 1).Value = label;
            ws.Cell(row, 2).Value = value;
            ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }
    }

    private static void WritePhasesTable(IXLWorksheet ws, ref int row, ReportRequest request)
    {
        WriteHeaders(ws, row++, new[] { "المرحلة", "المواد", "العمال", "النقل والمصاريف الأخرى", "الإجمالي", "المدفوع", "المتبقي", "النسبة %" });
        foreach (var ph in request.Summary.PhaseSummaries)
        {
            ws.Cell(row, 1).Value = ph.PhaseName;
            ws.Cell(row, 2).Value = ph.MaterialsCost;
            ws.Cell(row, 3).Value = ph.LaborCost;
            ws.Cell(row, 4).Value = ph.TransportAndOtherCost;
            ws.Cell(row, 5).Value = ph.TotalCost;
            ws.Cell(row, 6).Value = ph.PaidAmount;
            ws.Cell(row, 7).Value = ph.RemainingDebt;
            ws.Cell(row, 8).Value = ph.PercentageOfTotal;
            row++;
        }
    }
}
