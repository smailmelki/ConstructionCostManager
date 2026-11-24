using ConstructionCostManager.Core.Enums;

namespace ConstructionCostManager.Core.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; } = DateTime.Today;
    public decimal EstimatedBudget { get; set; }
    public string Description { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; } = ProjectStatus.InProgress;
    public string CurrencyCode { get; set; } = "DZD";

    public ICollection<ConstructionPhase> Phases { get; set; } = new List<ConstructionPhase>();
    public ICollection<MaterialPurchase> MaterialPurchases { get; set; } = new List<MaterialPurchase>();
    public ICollection<Worker> Workers { get; set; } = new List<Worker>();
    public ICollection<WorkerWorkEntry> WorkerWorkEntries { get; set; } = new List<WorkerWorkEntry>();
    public ICollection<OtherExpense> OtherExpenses { get; set; } = new List<OtherExpense>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
