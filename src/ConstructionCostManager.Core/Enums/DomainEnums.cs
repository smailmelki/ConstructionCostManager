namespace ConstructionCostManager.Core.Enums;

public enum ProjectStatus
{
    InProgress = 0,
    Paused = 1,
    Completed = 2
}

public enum MaterialCategory
{
    Cement = 0,
    Steel = 1,
    Sand = 2,
    Gravel = 3,
    Bricks = 4,
    Wood = 5,
    Paint = 6,
    Electrical = 7,
    Plumbing = 8,
    Tiles = 9,
    Insulation = 10,
    Other = 99
}

public enum MeasurementUnit
{
    Bag = 0,
    Ton = 1,
    CubicMeter = 2,
    SquareMeter = 3,
    LinearMeter = 4,
    Piece = 5,
    Liter = 6,
    Kilogram = 7,
    Roll = 8,
    Box = 9,
    Other = 99
}

public enum WorkerSpecialty
{
    Mason = 0,
    Carpenter = 1,
    SteelFixer = 2,
    Electrician = 3,
    Plumber = 4,
    Painter = 5,
    Tiler = 6,
    Plasterer = 7,
    Helper = 8,
    Engineer = 9,
    Other = 99
}

public enum LaborCalculationMethod
{
    Daily = 0,
    Hourly = 1,
    ByTask = 2,
    ByMeter = 3,
    ByAgreement = 4
}

public enum ExpenseCategory
{
    Transport = 0,
    EquipmentRental = 1,
    Additional = 2
}

public enum PaymentMethod
{
    Cash = 0,
    BankTransfer = 1,
    Check = 2,
    Other = 99
}

public enum ObligationType
{
    MaterialPurchase = 0,
    WorkerWork = 1,
    OtherExpense = 2
}
