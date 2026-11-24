using ConstructionCostManager.Core.Enums;

namespace ConstructionCostManager.Core.Entities;

public class MaterialPurchase : BaseEntity
{
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int PhaseId { get; set; }
    public ConstructionPhase? Phase { get; set; }

    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; } = DateTime.Today;
    public decimal TransportCost { get; set; }
    public string Notes { get; set; } = string.Empty;

    public ICollection<MaterialItem> Items { get; set; } = new List<MaterialItem>();

    public decimal MaterialsSubtotal =>
        Math.Round(Items.Where(i => !i.IsDeleted).Sum(i => i.TotalAmount), 2, MidpointRounding.AwayFromZero);

    public decimal TotalAmount =>
        Math.Round(MaterialsSubtotal + TransportCost, 2, MidpointRounding.AwayFromZero);
}

public class MaterialItem : BaseEntity
{
    public int MaterialPurchaseId { get; set; }
    public MaterialPurchase? MaterialPurchase { get; set; }

    public string Name { get; set; } = string.Empty;
    public MaterialCategory Category { get; set; } = MaterialCategory.Other;
    public decimal Quantity { get; set; }
    public MeasurementUnit Unit { get; set; } = MeasurementUnit.Piece;
    public decimal UnitPrice { get; set; }
    public string Notes { get; set; } = string.Empty;

    public decimal TotalAmount =>
        Math.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);
}
