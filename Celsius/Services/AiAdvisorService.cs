using Celsius.Models;

namespace Celsius.Services;

/// <summary>
/// "AI asistan": veritabanındaki işlemciye özgü kritik sıcaklık eşiklerine göre
/// durumu değerlendirir ve bakım önerileri üretir. Trend analizi yalnızca
/// oturum boyunca bellekte tutulur — kalıcı geçmiş yazılmaz.
/// </summary>
public class AiAdvisorService
{
    private readonly CpuDatabaseService _db;
    private CpuModelInfo? _cpuModel;
    private readonly Queue<double> _recentTemps = new();
    private const int TrendWindow = 30;

    public AiAdvisorService(CpuDatabaseService db) => _db = db;

    /// <summary>Tespit edilen işlemci adını DB'deki modele bağlar.</summary>
    public void DetectCpu(string? cpuName)
    {
        _cpuModel = _db.FindBestMatch(cpuName);
    }

    public CpuModelInfo? CurrentCpuModel => _cpuModel;

    /// <summary>Bir okumayı değerlendirip durum + öneriler döndürür.</summary>
    public AdvisorResult Evaluate(SensorSnapshot snap)
    {
        var temp = snap.CpuPackageTemp ?? snap.CpuMaxCoreTemp;
        if (temp is null)
        {
            return new AdvisorResult(ThermalStatus.Unavailable,
                "İşlemci sıcaklık sensörü okunamadı",
                null,
                new[]
                {
                    "LibreHardwareMonitor bu anakart + işlemci kombinasyonunda AMD SMU sıcaklığını 0 okuyor; " +
                    "ölü okumalar gösterilmez. Yük, GPU ve disk verileri güncel.",
                    "Değeri HWiNFO64 ile karşılaştırabilirsin — farklı bir okuma yöntemi kullanır."
                });
        }

        var model = _cpuModel;
        var tjMax = model?.TjMax ?? (snap.CpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase) ? 95 : 100);
        var sustained = model?.SustainedMaxTemp ?? tjMax - 10;

        // Oturum içi trend penceresine ekle
        _recentTemps.Enqueue(temp.Value);
        while (_recentTemps.Count > TrendWindow) _recentTemps.Dequeue();

        ThermalStatus status;
        if (temp >= tjMax) status = ThermalStatus.Critical;
        else if (temp >= tjMax - 8) status = ThermalStatus.MaintenanceRequired;
        else if (temp >= sustained) status = ThermalStatus.Caution;
        else if (temp >= tjMax * 0.6) status = ThermalStatus.Warm;
        else status = ThermalStatus.Healthy;

        var suggestions = new List<string>();
        TrackTrend(status, suggestions);
        BuildStatusMessages(status, suggestions, temp.Value, tjMax, sustained);

        var summary = status switch
        {
            ThermalStatus.Healthy => "Sağlıklı — sıcaklık güvenli aralıkta.",
            ThermalStatus.Warm => "Normal — hafif ısınma mevcut, izleniyor.",
            ThermalStatus.Caution => "Dikkat — önerilen sürekli sıcaklığın üzerinde.",
            ThermalStatus.MaintenanceRequired => "Bakım gerekli — sıcaklık kritik eşiğe yaklaşıyor.",
            ThermalStatus.Critical => "KRİTİK — işlemci kendini korumaya aldı (throttle).",
            _ => ""
        };

        return new AdvisorResult(status, summary, temp, suggestions);
    }

    private void TrackTrend(ThermalStatus status, List<string> suggestions)
    {
        if (_recentTemps.Count < 15) return;
        var half = _recentTemps.Count / 2;
        var firstAvg = _recentTemps.Take(half).Average();
        var secondAvg = _recentTemps.Skip(half).Average();
        var delta = secondAvg - firstAvg;

        if (delta >= 5 && (status == ThermalStatus.Caution || status == ThermalStatus.MaintenanceRequired))
            suggestions.Add($"Oturum boyunca sıcaklık {delta:0.0}°C arttı — kasa hava akışı ve fan temizliği kontrol edilmeli.");
        else if (delta <= -5)
            suggestions.Add("Sıcaklık oturum boyunca düşüyor — soğutma iyi durumda görünüyor.");
    }

    private static void BuildStatusMessages(ThermalStatus status, List<string> suggestions,
        double temp, double tjMax, double sustained)
    {
        switch (status)
        {
            case ThermalStatus.Critical:
                suggestions.Add($"KRİTİK: {temp:0}°C — TjMax ({tjMax:0}°C) aşıldı/erişildi. Yükü durdur, kapağı aç, soğutucuyu kontrol et.");
                break;
            case ThermalStatus.MaintenanceRequired:
                suggestions.Add($"Sıcaklık {temp:0}°C — {tjMax - 8:0}°C üzerinde. Termal macun yenilenmesi ve soğutucu bakımı önerilir.");
                break;
            case ThermalStatus.Caution:
                suggestions.Add($"Sıcaklık {temp:0}°C, önerilen sürekli üst limit ({sustained:0}°C) üzerinde. Uzun süre bu seviyede tutmayın.");
                break;
        }
    }
}