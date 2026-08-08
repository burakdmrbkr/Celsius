namespace Celsius.Models;

/// <summary>
/// SQLite referans veritabanında tutulan bir işlemci modeli kaydı.
/// Uygulama anlık ölçümleri bu eşik değerlerine göre değerlendirir.
/// </summary>
public class CpuModelInfo
{
    public int Id { get; set; }

    /// <summary>Intel / AMD</summary>
    public string Manufacturer { get; set; } = "";

    /// <summary>Ör. "Core i7-12700K", "Ryzen 7 7800X3D"</summary>
    public string ModelName { get; set; } = "";

    /// <summary>Maksimum junction sıcaklığı (TjMax) — °C. Bu değere yaklaşınca throttle başlar.</summary>
    public double TjMax { get; set; } = 100;

    /// <summary>Uzun süreli yük altında önerilen üst sıcaklık — °C.</summary>
    public double SustainedMaxTemp { get; set; } = 90;

    /// <summary>Ek not (isteğe bağlı).</summary>
    public string? Note { get; set; }

    public override string ToString() => $"{Manufacturer} {ModelName}";
}