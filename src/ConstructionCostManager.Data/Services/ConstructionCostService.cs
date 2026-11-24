using ConstructionCostManager.Core.Entities;
using ConstructionCostManager.Core.Enums;
using ConstructionCostManager.Core.Helpers;
using ConstructionCostManager.Core.Models;
using ConstructionCostManager.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace ConstructionCostManager.Data.Services;

public class ConstructionCostService : IConstructionCostService
{
    private readonly AppDbContext _db;

    public ConstructionCostService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Project>> GetProjectsAsync() =>
        await _db.Projects.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<Project?> GetProjectByIdAsync(int projectId) =>
        await _db.Projects.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId);

    public async Task<Project> CreateProjectAsync(Project project)
    {
        if (string.IsNullOrWhiteSpace(project.Name))
            throw new ArgumentException("اسم المشروع مطلوب.", nameof(project));

        if (string.IsNullOrWhiteSpace(project.CurrencyCode))
            project.CurrencyCode = await GetSettingAsync("DefaultCurrency", "DZD");

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        // Seed default 13 construction phases for this project
        var phases = ArabicDisplayHelper.DefaultConstructionPhases
            .Select(p => new ConstructionPhase
            {
                ProjectId = project.Id,
                DisplayOrder = p.Order,
                Name = p.Name,
                Description = p.Description,
                IsDefault = true
            }).ToList();

        _db.ConstructionPhases.AddRange(phases);
        await _db.SaveChangesAsync();

        return project;
    }

    public async Task UpdateProjectAsync(Project project)
    {
        var existing = await _db.Projects.FirstOrDefaultAsync(p => p.Id == project.Id)
            ?? throw new InvalidOperationException("المشروع غير موجود.");

        existing.Name = project.Name.Trim();
        existing.Location = project.Location.Trim();
        existing.OwnerName = project.OwnerName.Trim();
        existing.StartDate = project.StartDate;
        existing.EstimatedBudget = project.EstimatedBudget;
        existing.Description = project.Description.Trim();
        existing.Status = project.Status;
        existing.CurrencyCode = string.IsNullOrWhiteSpace(project.CurrencyCode) ? "DZD" : project.CurrencyCode;

        await _db.SaveChangesAsync();
    }

    public async Task SoftDeleteProjectAsync(int projectId)
    {
        var existing = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
        if (existing is null) return;

        existing.IsDeleted = true;
        existing.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<List<ConstructionPhase>> GetPhasesAsync(int projectId) =>
        await _db.ConstructionPhases.AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Id)
            .ToListAsync();

    public async Task<ConstructionPhase> AddPhaseAsync(ConstructionPhase phase)
    {
        if (string.IsNullOrWhiteSpace(phase.Name))
            throw new ArgumentException("اسم المرحلة مطلوب.");

        if (phase.DisplayOrder <= 0)
        {
            var maxOrder = await _db.ConstructionPhases
                .Where(p => p.ProjectId == phase.ProjectId)
                .Select(p => (int?)p.DisplayOrder)
                .MaxAsync() ?? 0;
            phase.DisplayOrder = maxOrder + 1;
        }

        _db.ConstructionPhases.Add(phase);
        await _db.SaveChangesAsync();
        return phase;
    }

    public async Task UpdatePhaseAsync(ConstructionPhase phase)
    {
        var existing = await _db.ConstructionPhases.FirstOrDefaultAsync(p => p.Id == phase.Id)
            ?? throw new InvalidOperationException("المرحلة غير موجودة.");

        existing.Name = phase.Name.Trim();
        existing.DisplayOrder = phase.DisplayOrder;
        existing.Description = phase.Description.Trim();
        await _db.SaveChangesAsync();
    }

    public async Task<(bool Success, string ErrorMessage)> DeletePhaseAsync(int phaseId)
    {
        var phase = await _db.ConstructionPhases.FirstOrDefaultAsync(p => p.Id == phaseId);
        if (phase is null)
            return (false, "المرحلة غير موجودة.");

        var hasMaterials = await _db.MaterialPurchases.AnyAsync(m => m.PhaseId == phaseId);
        var hasWorkers = await _db.WorkerWorkEntries.AnyAsync(w => w.PhaseId == phaseId);
        var hasOther = await _db.OtherExpenses.AnyAsync(o => o.PhaseId == phaseId);

        if (hasMaterials || hasWorkers || hasOther)
            return (false, "لا يمكن حذف هذه المرحلة لوجود مصاريف مرتبطة بها.");

        phase.IsDeleted = true;
        phase.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<List<MaterialPurchase>> GetMaterialPurchasesAsync(int projectId) =>
        await _db.MaterialPurchases.AsNoTracking()
            .Include(p => p.Phase)
            .Include(p => p.Items)
            .Where(p => p.ProjectId == projectId)
            .OrderByDescending(p => p.PurchaseDate)
            .ThenByDescending(p => p.Id)
            .ToListAsync();

    public async Task<MaterialPurchase> AddMaterialPurchaseAsync(
        MaterialPurchase purchase,
        decimal initialPaidAmount = 0m,
        PaymentMethod paymentMethod = PaymentMethod.Cash)
    {
        if (purchase.Items.Count == 0)
            throw new ArgumentException("يجب إضافة مادة واحدة على الأقل في عملية الشراء.");

        foreach (var item in purchase.Items)
        {
            if (item.Quantity <= 0 || item.UnitPrice < 0)
                throw new ArgumentException($"الكمية أو السعر غير صالح للمادة: {item.Name}");
        }

        _db.MaterialPurchases.Add(purchase);
        await _db.SaveChangesAsync();

        if (initialPaidAmount > 0m)
        {
            await RecordPaymentAsync(new Payment
            {
                ProjectId = purchase.ProjectId,
                ObligationType = ObligationType.MaterialPurchase,
                ReferenceId = purchase.Id,
                Amount = initialPaidAmount,
                PaymentDate = purchase.PurchaseDate,
                Method = paymentMethod,
                Notes = $"دفعة أولى - فاتورة {purchase.InvoiceNumber}".Trim()
            });
        }

        return purchase;
    }

    public async Task UpdateMaterialPurchaseAsync(MaterialPurchase purchase)
    {
        var existing = await _db.MaterialPurchases
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == purchase.Id)
            ?? throw new InvalidOperationException("عملية الشراء غير موجودة.");

        existing.PhaseId = purchase.PhaseId;
        existing.SupplierName = purchase.SupplierName.Trim();
        existing.InvoiceNumber = purchase.InvoiceNumber.Trim();
        existing.PurchaseDate = purchase.PurchaseDate;
        existing.TransportCost = purchase.TransportCost;
        existing.Notes = purchase.Notes.Trim();

        await _db.SaveChangesAsync();
    }

    public async Task SoftDeleteMaterialPurchaseAsync(int purchaseId)
    {
        var purchase = await _db.MaterialPurchases
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == purchaseId);
        if (purchase is null) return;

        var now = DateTime.UtcNow;
        purchase.IsDeleted = true;
        purchase.DeletedAt = now;
        foreach (var item in purchase.Items)
        {
            item.IsDeleted = true;
            item.DeletedAt = now;
        }

        var linkedPayments = await _db.Payments
            .Where(p => p.ProjectId == purchase.ProjectId && p.ObligationType == ObligationType.MaterialPurchase && p.ReferenceId == purchase.Id)
            .ToListAsync();
        foreach (var pay in linkedPayments)
        {
            pay.IsDeleted = true;
            pay.DeletedAt = now;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<Worker>> GetWorkersAsync(int projectId) =>
        await _db.Workers.AsNoTracking()
            .Where(w => w.ProjectId == projectId)
            .OrderBy(w => w.Name)
            .ToListAsync();

    public async Task<Worker> AddWorkerAsync(Worker worker)
    {
        if (string.IsNullOrWhiteSpace(worker.Name))
            throw new ArgumentException("اسم العامل مطلوب.");

        _db.Workers.Add(worker);
        await _db.SaveChangesAsync();
        return worker;
    }

    public async Task UpdateWorkerAsync(Worker worker)
    {
        var existing = await _db.Workers.FirstOrDefaultAsync(w => w.Id == worker.Id)
            ?? throw new InvalidOperationException("العامل غير موجود.");

        existing.Name = worker.Name.Trim();
        existing.Specialty = worker.Specialty;
        existing.Phone = worker.Phone.Trim();
        existing.Notes = worker.Notes.Trim();
        await _db.SaveChangesAsync();
    }

    public async Task<List<WorkerWorkEntry>> GetWorkEntriesAsync(int projectId, int? workerId = null)
    {
        var query = _db.WorkerWorkEntries.AsNoTracking()
            .Include(w => w.Worker)
            .Include(w => w.Phase)
            .Where(w => w.ProjectId == projectId);

        if (workerId.HasValue)
            query = query.Where(w => w.WorkerId == workerId.Value);

        return await query
            .OrderByDescending(w => w.WorkDate)
            .ThenByDescending(w => w.Id)
            .ToListAsync();
    }

    public async Task<WorkerWorkEntry> AddWorkEntryAsync(
        WorkerWorkEntry entry,
        decimal initialPaidAmount = 0m,
        PaymentMethod paymentMethod = PaymentMethod.Cash)
    {
        if (entry.UnitsCompleted <= 0 || entry.UnitRate < 0)
            throw new ArgumentException("عدد الوحدات أو السعر غير صالح.");

        _db.WorkerWorkEntries.Add(entry);
        await _db.SaveChangesAsync();

        if (initialPaidAmount > 0m)
        {
            await RecordPaymentAsync(new Payment
            {
                ProjectId = entry.ProjectId,
                ObligationType = ObligationType.WorkerWork,
                ReferenceId = entry.Id,
                Amount = initialPaidAmount,
                PaymentDate = entry.WorkDate,
                Method = paymentMethod,
                Notes = $"دفعة أولى - {entry.WorkType}".Trim()
            });
        }

        return entry;
    }

    public async Task SoftDeleteWorkEntryAsync(int entryId)
    {
        var entry = await _db.WorkerWorkEntries.FirstOrDefaultAsync(w => w.Id == entryId);
        if (entry is null) return;

        var now = DateTime.UtcNow;
        entry.IsDeleted = true;
        entry.DeletedAt = now;

        var linkedPayments = await _db.Payments
            .Where(p => p.ProjectId == entry.ProjectId && p.ObligationType == ObligationType.WorkerWork && p.ReferenceId == entry.Id)
            .ToListAsync();
        foreach (var pay in linkedPayments)
        {
            pay.IsDeleted = true;
            pay.DeletedAt = now;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<OtherExpense>> GetOtherExpensesAsync(int projectId) =>
        await _db.OtherExpenses.AsNoTracking()
            .Include(e => e.Phase)
            .Where(e => e.ProjectId == projectId)
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.Id)
            .ToListAsync();

    public async Task<OtherExpense> AddOtherExpenseAsync(
        OtherExpense expense,
        decimal initialPaidAmount = 0m,
        PaymentMethod paymentMethod = PaymentMethod.Cash)
    {
        if (expense.Amount <= 0)
            throw new ArgumentException("المبلغ يجب أن يكون أكبر من الصفر.");

        _db.OtherExpenses.Add(expense);
        await _db.SaveChangesAsync();

        if (initialPaidAmount > 0m)
        {
            await RecordPaymentAsync(new Payment
            {
                ProjectId = expense.ProjectId,
                ObligationType = ObligationType.OtherExpense,
                ReferenceId = expense.Id,
                Amount = initialPaidAmount,
                PaymentDate = expense.ExpenseDate,
                Method = paymentMethod,
                Notes = $"دفعة - {expense.Title}".Trim()
            });
        }

        return expense;
    }

    public async Task SoftDeleteOtherExpenseAsync(int expenseId)
    {
        var expense = await _db.OtherExpenses.FirstOrDefaultAsync(e => e.Id == expenseId);
        if (expense is null) return;

        var now = DateTime.UtcNow;
        expense.IsDeleted = true;
        expense.DeletedAt = now;

        var linkedPayments = await _db.Payments
            .Where(p => p.ProjectId == expense.ProjectId && p.ObligationType == ObligationType.OtherExpense && p.ReferenceId == expense.Id)
            .ToListAsync();
        foreach (var pay in linkedPayments)
        {
            pay.IsDeleted = true;
            pay.DeletedAt = now;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<Payment>> GetPaymentsAsync(int projectId) =>
        await _db.Payments.AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.Id)
            .ToListAsync();

    public async Task<List<Payment>> GetPaymentsForObligationAsync(int projectId, ObligationType obligationType, int referenceId) =>
        await _db.Payments.AsNoTracking()
            .Where(p => p.ProjectId == projectId && p.ObligationType == obligationType && p.ReferenceId == referenceId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

    public async Task<Payment> RecordPaymentAsync(Payment payment)
    {
        if (payment.Amount <= 0)
            throw new ArgumentException("مبلغ الدفعة يجب أن يكون أكبر من الصفر.");

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return payment;
    }

    public async Task SoftDeletePaymentAsync(int paymentId)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId);
        if (payment is null) return;

        payment.IsDeleted = true;
        payment.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<ProjectFinancialSummary?> GetProjectFinancialSummaryAsync(int projectId)
    {
        var project = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null) return null;

        var phases = await _db.ConstructionPhases.AsNoTracking().Where(p => p.ProjectId == projectId).ToListAsync();
        var purchases = await _db.MaterialPurchases.AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.ProjectId == projectId)
            .ToListAsync();
        var workEntries = await _db.WorkerWorkEntries.AsNoTracking()
            .Include(w => w.Worker)
            .Where(w => w.ProjectId == projectId)
            .ToListAsync();
        var otherExpenses = await _db.OtherExpenses.AsNoTracking().Where(e => e.ProjectId == projectId).ToListAsync();
        var payments = await _db.Payments.AsNoTracking().Where(p => p.ProjectId == projectId).ToListAsync();

        return CostAggregationEngine.CalculateProjectSummary(project, phases, purchases, workEntries, otherExpenses, payments);
    }

    public async Task<List<SearchResultItem>> SearchExpensesAsync(ExpenseSearchFilter filter)
    {
        var purchases = await _db.MaterialPurchases.AsNoTracking()
            .Include(p => p.Phase)
            .Include(p => p.Items)
            .Where(p => p.ProjectId == filter.ProjectId)
            .ToListAsync();

        var workEntries = await _db.WorkerWorkEntries.AsNoTracking()
            .Include(w => w.Worker)
            .Include(w => w.Phase)
            .Where(w => w.ProjectId == filter.ProjectId)
            .ToListAsync();

        var otherExpenses = await _db.OtherExpenses.AsNoTracking()
            .Include(e => e.Phase)
            .Where(e => e.ProjectId == filter.ProjectId)
            .ToListAsync();

        var payments = await _db.Payments.AsNoTracking()
            .Where(p => p.ProjectId == filter.ProjectId)
            .ToListAsync();

        var paidLookup = payments
            .GroupBy(p => (p.ObligationType, p.ReferenceId))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var results = new List<SearchResultItem>();

        foreach (var p in purchases)
        {
            if (filter.MaterialCategory.HasValue && !p.Items.Any(i => i.Category == filter.MaterialCategory.Value))
                continue;

            var paid = paidLookup.GetValueOrDefault((ObligationType.MaterialPurchase, p.Id));
            var itemNames = string.Join("، ", p.Items.Select(i => i.Name));
            results.Add(new SearchResultItem
            {
                ObligationType = ObligationType.MaterialPurchase,
                ReferenceId = p.Id,
                Date = p.PurchaseDate,
                PhaseName = p.Phase?.Name ?? string.Empty,
                CategoryName = "مواد البناء",
                Title = string.IsNullOrWhiteSpace(p.InvoiceNumber) ? itemNames : $"فاتورة #{p.InvoiceNumber} ({itemNames})",
                PartyName = p.SupplierName,
                TotalAmount = p.TotalAmount,
                PaidAmount = paid,
                RemainingAmount = Math.Max(0m, p.TotalAmount - paid),
                Notes = p.Notes
            });
        }

        if (!filter.MaterialCategory.HasValue)
        {
            foreach (var w in workEntries)
            {
                var paid = paidLookup.GetValueOrDefault((ObligationType.WorkerWork, w.Id));
                results.Add(new SearchResultItem
                {
                    ObligationType = ObligationType.WorkerWork,
                    ReferenceId = w.Id,
                    Date = w.WorkDate,
                    PhaseName = w.Phase?.Name ?? string.Empty,
                    CategoryName = "أجور العمال",
                    Title = w.WorkType,
                    PartyName = w.Worker?.Name ?? string.Empty,
                    TotalAmount = w.TotalAmount,
                    PaidAmount = paid,
                    RemainingAmount = Math.Max(0m, w.TotalAmount - paid),
                    Notes = w.Notes
                });
            }

            foreach (var e in otherExpenses)
            {
                var paid = paidLookup.GetValueOrDefault((ObligationType.OtherExpense, e.Id));
                results.Add(new SearchResultItem
                {
                    ObligationType = ObligationType.OtherExpense,
                    ReferenceId = e.Id,
                    Date = e.ExpenseDate,
                    PhaseName = e.Phase?.Name ?? string.Empty,
                    CategoryName = ArabicDisplayHelper.GetExpenseCategoryLabel(e.Category),
                    Title = e.Title,
                    PartyName = e.VendorOrParty,
                    TotalAmount = e.Amount,
                    PaidAmount = paid,
                    RemainingAmount = Math.Max(0m, e.Amount - paid),
                    Notes = e.Notes
                });
            }
        }

        var query = results.AsEnumerable();

        if (filter.ObligationType.HasValue)
            query = query.Where(r => r.ObligationType == filter.ObligationType.Value);

        if (filter.PhaseId.HasValue)
        {
            var phaseName = await _db.ConstructionPhases
                .Where(ph => ph.Id == filter.PhaseId.Value)
                .Select(ph => ph.Name)
                .FirstOrDefaultAsync();
            if (phaseName is not null)
                query = query.Where(r => r.PhaseName == phaseName);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(r =>
                r.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.PartyName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.PhaseName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.CategoryName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.Notes.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.FromDate.HasValue)
            query = query.Where(r => r.Date.Date >= filter.FromDate.Value.Date);

        if (filter.ToDate.HasValue)
            query = query.Where(r => r.Date.Date <= filter.ToDate.Value.Date);

        if (filter.MinAmount.HasValue)
            query = query.Where(r => r.TotalAmount >= filter.MinAmount.Value);

        if (filter.MaxAmount.HasValue)
            query = query.Where(r => r.TotalAmount <= filter.MaxAmount.Value);

        if (filter.OnlyUnpaid)
            query = query.Where(r => r.RemainingAmount > 0m);

        return query.OrderByDescending(r => r.Date).ToList();
    }

    public async Task<string> GetSettingAsync(string key, string defaultValue = "")
    {
        var setting = await _db.AppSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value ?? defaultValue;
    }

    public async Task SetSettingAsync(string key, string value)
    {
        var existing = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (existing is null)
            _db.AppSettings.Add(new AppSetting { Key = key, Value = value });
        else
            existing.Value = value;

        await _db.SaveChangesAsync();
    }
}
