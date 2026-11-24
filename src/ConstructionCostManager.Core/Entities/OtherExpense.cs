using ConstructionCostManager.Core.Enums;

namespace ConstructionCostManager.Core.Entities;

public class OtherExpense : BaseEntity
{
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int PhaseId { get; set; }
    public ConstructionPhase? Phase { get; set; }

    public ExpenseCategory Category { get; set; } = ExpenseCategory.Additional;
    public string Title { get; set; } = string.Empty;
    public string VendorOrParty { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class Payment : BaseEntity
{
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public ObligationType ObligationType { get; set; }
    public int ReferenceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public string Notes { get; set; } = string.Empty;
}

public class AppSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
