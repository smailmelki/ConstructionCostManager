using ConstructionCostManager.Core.Enums;

namespace ConstructionCostManager.Core.Entities;

public class Worker : BaseEntity
{
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Name { get; set; } = string.Empty;
    public WorkerSpecialty Specialty { get; set; } = WorkerSpecialty.Mason;
    public string Phone { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public ICollection<WorkerWorkEntry> WorkEntries { get; set; } = new List<WorkerWorkEntry>();
}

public class WorkerWorkEntry : BaseEntity
{
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int WorkerId { get; set; }
    public Worker? Worker { get; set; }

    public int PhaseId { get; set; }
    public ConstructionPhase? Phase { get; set; }

    public string WorkType { get; set; } = string.Empty;
    public LaborCalculationMethod CalculationMethod { get; set; } = LaborCalculationMethod.Daily;
    public decimal UnitsCompleted { get; set; }
    public decimal UnitRate { get; set; }
    public DateTime WorkDate { get; set; } = DateTime.Today;
    public string Notes { get; set; } = string.Empty;

    public decimal TotalAmount =>
        Math.Round(UnitsCompleted * UnitRate, 2, MidpointRounding.AwayFromZero);
}
