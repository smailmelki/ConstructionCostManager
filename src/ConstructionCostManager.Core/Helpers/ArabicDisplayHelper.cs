using ConstructionCostManager.Core.Enums;

namespace ConstructionCostManager.Core.Helpers;

public static class ArabicDisplayHelper
{
    public static readonly IReadOnlyList<(int Order, string Name, string Description)> DefaultConstructionPhases =
    [
        (1, "الدراسات والتحضير", "التصاميم الهندسية، الرخص، وتجهيز الموقع"),
        (2, "الحفر والأساسات", "أعمال الحفر، الردم، والأساسات الخرسانية"),
        (3, "الخرسانة والهيكل", "الأعمدة، الجسور، والهيكل الخرساني"),
        (4, "البناء والجدران", "بناء الجدران الخارجية والداخلية بالآجر أو الطوب"),
        (5, "السقف", "تسقيف البناية والخرسانة العلوية"),
        (6, "العزل", "العزل المائي والحراري للأسطح والجدران"),
        (7, "الكهرباء", "التمديدات الكهربائية، اللوحات، والمقابس"),
        (8, "السباكة", "تمديدات المياه الصالحة للشرب والصرف الصحي"),
        (9, "التلبيس واللياسة", "أعمال التلبيس الداخلي والخارجي"),
        (10, "الأرضيات والبلاط", "تبليط الأرضيات والجدران والرخام"),
        (11, "النجارة والأبواب والنوافذ", "تركيب الأبواب، النوافذ، والنجارة الخشبية أو الألمنيوم"),
        (12, "الدهان والتشطيبات", "أعمال الطلاء، الجبس، واللمسات النهائية"),
        (13, "الأشغال الخارجية", "السور الخارجي، الفناء، والمداخل")
    ];

    public static string GetStatusLabel(ProjectStatus status) => status switch
    {
        ProjectStatus.InProgress => "قيد الإنجاز",
        ProjectStatus.Paused => "متوقف",
        ProjectStatus.Completed => "مكتمل",
        _ => "غير محدد"
    };

    public static string GetMaterialCategoryLabel(MaterialCategory category) => category switch
    {
        MaterialCategory.Cement => "إسمنت",
        MaterialCategory.Steel => "حديد",
        MaterialCategory.Sand => "رمل",
        MaterialCategory.Gravel => "حصى",
        MaterialCategory.Bricks => "طوب وآجر",
        MaterialCategory.Wood => "خشب",
        MaterialCategory.Paint => "دهان",
        MaterialCategory.Electrical => "كهرباء",
        MaterialCategory.Plumbing => "سباكة",
        MaterialCategory.Tiles => "بلاط وأرضيات",
        MaterialCategory.Insulation => "عزل",
        MaterialCategory.Other => "مواد أخرى",
        _ => "أخرى"
    };

    public static string GetUnitLabel(MeasurementUnit unit) => unit switch
    {
        MeasurementUnit.Bag => "كيس",
        MeasurementUnit.Ton => "طن",
        MeasurementUnit.CubicMeter => "م³",
        MeasurementUnit.SquareMeter => "م²",
        MeasurementUnit.LinearMeter => "م.ط",
        MeasurementUnit.Piece => "قطعة",
        MeasurementUnit.Liter => "لتر",
        MeasurementUnit.Kilogram => "كغ",
        MeasurementUnit.Roll => "لفة",
        MeasurementUnit.Box => "صندوق",
        MeasurementUnit.Other => "وحدة",
        _ => "وحدة"
    };

    public static string GetSpecialtyLabel(WorkerSpecialty specialty) => specialty switch
    {
        WorkerSpecialty.Mason => "بنّاء",
        WorkerSpecialty.Carpenter => "نجار",
        WorkerSpecialty.SteelFixer => "حداد",
        WorkerSpecialty.Electrician => "كهربائي",
        WorkerSpecialty.Plumber => "سباك",
        WorkerSpecialty.Painter => "دهان",
        WorkerSpecialty.Tiler => "بلاّط",
        WorkerSpecialty.Plasterer => "مليّس",
        WorkerSpecialty.Helper => "عامل مساعد",
        WorkerSpecialty.Engineer => "مهندس / مشرف",
        WorkerSpecialty.Other => "تخصص آخر",
        _ => "أخرى"
    };

    public static string GetCalculationMethodLabel(LaborCalculationMethod method) => method switch
    {
        LaborCalculationMethod.Daily => "يومي",
        LaborCalculationMethod.Hourly => "بالساعة",
        LaborCalculationMethod.ByTask => "بالمهمة",
        LaborCalculationMethod.ByMeter => "بالمتر",
        LaborCalculationMethod.ByAgreement => "بالاتفاق",
        _ => "أخرى"
    };

    public static string GetExpenseCategoryLabel(ExpenseCategory category) => category switch
    {
        ExpenseCategory.Transport => "نقل",
        ExpenseCategory.EquipmentRental => "كراء معدات",
        ExpenseCategory.Additional => "مصاريف إضافية",
        _ => "أخرى"
    };

    public static string GetPaymentMethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "نقداً",
        PaymentMethod.BankTransfer => "تحويل بنكي",
        PaymentMethod.Check => "شيك",
        PaymentMethod.Other => "طريقة أخرى",
        _ => "أخرى"
    };

    public static string GetObligationTypeLabel(ObligationType type) => type switch
    {
        ObligationType.MaterialPurchase => "فاتورة مواد",
        ObligationType.WorkerWork => "أجرة عامل",
        ObligationType.OtherExpense => "مصروف آخر",
        _ => "أخرى"
    };

    public static string GetCurrencyDisplay(string currencyCode) => currencyCode.ToUpperInvariant() switch
    {
        "DZD" => "د.ج",
        "SAR" => "ر.س",
        "AED" => "د.إ",
        "MAD" => "د.م",
        "TND" => "د.ت",
        "EGP" => "ج.م",
        "USD" => "$",
        "EUR" => "€",
        _ => currencyCode
    };

    public static string FormatMoney(decimal amount, string currencyCode = "DZD")
    {
        var symbol = GetCurrencyDisplay(currencyCode);
        return $"{amount:N2} {symbol}";
    }
}
