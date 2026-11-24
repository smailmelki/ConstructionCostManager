using ConstructionCostManager.Core.Entities;
using ConstructionCostManager.Core.Enums;
using ConstructionCostManager.Core.Models;

namespace ConstructionCostManager.Data.Services;

public interface IConstructionCostService
{
    // Projects
    Task<List<Project>> GetProjectsAsync();
    Task<Project?> GetProjectByIdAsync(int projectId);
    Task<Project> CreateProjectAsync(Project project);
    Task UpdateProjectAsync(Project project);
    Task SoftDeleteProjectAsync(int projectId);

    // Phases
    Task<List<ConstructionPhase>> GetPhasesAsync(int projectId);
    Task<ConstructionPhase> AddPhaseAsync(ConstructionPhase phase);
    Task UpdatePhaseAsync(ConstructionPhase phase);
    Task<(bool Success, string ErrorMessage)> DeletePhaseAsync(int phaseId);

    // Material Purchases
    Task<List<MaterialPurchase>> GetMaterialPurchasesAsync(int projectId);
    Task<MaterialPurchase> AddMaterialPurchaseAsync(MaterialPurchase purchase, decimal initialPaidAmount = 0m, PaymentMethod paymentMethod = PaymentMethod.Cash);
    Task UpdateMaterialPurchaseAsync(MaterialPurchase purchase);
    Task SoftDeleteMaterialPurchaseAsync(int purchaseId);

    // Workers & Labor
    Task<List<Worker>> GetWorkersAsync(int projectId);
    Task<Worker> AddWorkerAsync(Worker worker);
    Task UpdateWorkerAsync(Worker worker);
    Task<List<WorkerWorkEntry>> GetWorkEntriesAsync(int projectId, int? workerId = null);
    Task<WorkerWorkEntry> AddWorkEntryAsync(WorkerWorkEntry entry, decimal initialPaidAmount = 0m, PaymentMethod paymentMethod = PaymentMethod.Cash);
    Task SoftDeleteWorkEntryAsync(int entryId);

    // Other Expenses (Transport, Equipment Rental, Additional)
    Task<List<OtherExpense>> GetOtherExpensesAsync(int projectId);
    Task<OtherExpense> AddOtherExpenseAsync(OtherExpense expense, decimal initialPaidAmount = 0m, PaymentMethod paymentMethod = PaymentMethod.Cash);
    Task SoftDeleteOtherExpenseAsync(int expenseId);

    // Payments & Debts
    Task<List<Payment>> GetPaymentsAsync(int projectId);
    Task<List<Payment>> GetPaymentsForObligationAsync(int projectId, ObligationType obligationType, int referenceId);
    Task<Payment> RecordPaymentAsync(Payment payment);
    Task SoftDeletePaymentAsync(int paymentId);

    // Aggregations & Search
    Task<ProjectFinancialSummary?> GetProjectFinancialSummaryAsync(int projectId);
    Task<List<SearchResultItem>> SearchExpensesAsync(ExpenseSearchFilter filter);

    // Settings
    Task<string> GetSettingAsync(string key, string defaultValue = "");
    Task SetSettingAsync(string key, string value);
}
