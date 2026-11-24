namespace ConstructionCostManager.Core.Entities;

public class ConstructionPhase : BaseEntity
{
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public ICollection<MaterialPurchase> MaterialPurchases { get; set; } = new List<MaterialPurchase>();
    public ICollection<WorkerWorkEntry> WorkerWorkEntries { get; set; } = new List<WorkerWorkEntry>();
    public ICollection<OtherExpense> OtherExpenses { get; set; } = new List<OtherExpense>();
}
