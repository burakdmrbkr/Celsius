using System.Collections.ObjectModel;
using System.Windows.Media;
using Celsius.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Celsius.ViewModels;

/// <summary>"Genel Bakış" sekmesi: canlı sıcaklıklar, çekirdek yükleri ve AI durum özeti.</summary>
public partial class DashboardViewModel : ObservableObject
{
    private static readonly Brush _defaultStatusBrush =
        new SolidColorBrush(Color.FromRgb(139, 148, 158));

    [ObservableProperty] private string _cpuName = "Bekleniyor…";
    [ObservableProperty] private string _cpuTempText = "—";
    [ObservableProperty] private string _cpuMaxTempText = "—";
    [ObservableProperty] private string _cpuLoadText = "—";
    [ObservableProperty] private string _gpuText = "—";
    [ObservableProperty] private string _fanText = "—";
    [ObservableProperty] private string _voltageText = "—";
    [ObservableProperty] private string _statusText = "Sensör verisi bekleniyor";
    [ObservableProperty] private Brush _statusBrush = _defaultStatusBrush;
    [ObservableProperty] private string _statusSummary = "";
    [ObservableProperty] private string _statusSuggestions = "";
    [ObservableProperty] private string _modelMatched = "";
    [ObservableProperty] private string _tempSourceText = "Sıcaklık kaynağı: bekleniyor…";
    [ObservableProperty] private string _coreSectionTitle = "ÇEKİRDEK SICAKLIKLARI";

    public ObservableCollection<CoreTempItem> CoreTemps { get; } = new();

    /// <summary>Her anket döngüsünde UI değerlerini günceller.</summary>
    public void Refresh(SensorSnapshot snap, AdvisorResult advice, CpuModelInfo? model)
    {
        CpuName = snap.CpuName;
        CpuTempText = snap.CpuPackageTemp is { } p ? $"{p:0.0} °C" : "—";
        CpuMaxTempText = snap.CpuMaxCoreTemp is { } m ? $"{m:0.0} °C" : "—";
        CpuLoadText = snap.CpuLoad is { } l ? $"{l:0}%" : "—";
        GpuText = snap.GpuTemp is { } g ? $"{g:0.0} °C" : "—";
        FanText = snap.MaxFanRpm is { } f ? $"{f:0} RPM" : "—";
        VoltageText = snap.Voltage is { } v ? $"{v:0.000} V" : "—";

        StatusText = advice.Summary;
        StatusBrush = StatusToBrush(advice.Status);
        StatusSummary = StatusDetail(advice.Status);
        StatusSuggestions = advice.SuggestionsJoined;

        ModelMatched = model is null ? "Veritabanında eşleşme yok — varsayılan eşikler kullanılıyor."
            : $"Eşleşen model: {model}{(model.Note is { Length: > 0 } n ? " — " + n : "")}";

        TempSourceText = snap.TempSource switch
        {
            "HWiNFO" => "Sıcaklık kaynağı: HWiNFO",
            "LibreHardwareMonitor" => "Sıcaklık kaynağı: LibreHardwareMonitor",
            _ => "Sıcaklık kaynağı: yok — Secure Boot kapalıyken okunur"
        };

        // Çekirdek listesi: per-core sıcaklık varsa çekirdek, yoksa per-CCD sıcaklık göster.
        CoreTemps.Clear();
        if (snap.CoreTemps.Count > 0)
        {
            CoreSectionTitle = "ÇEKİRDEK SICAKLIKLARI";
            var tj = model?.TjMax ?? (snap.CpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase) ? 95 : 100);
            for (var i = 0; i < snap.CoreTemps.Count; i++)
                CoreTemps.Add(new CoreTempItem { Label = $"Çekirdek {i + 1}", TjMax = tj, Temp = snap.CoreTemps[i] });
        }
        else if (snap.CcdTemps.Count > 0)
        {
            CoreSectionTitle = "CCD SICAKLIKLARI";
            var tj = model?.TjMax ?? (snap.CpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase) ? 95 : 100);
            foreach (var ccd in snap.CcdTemps)
                CoreTemps.Add(new CoreTempItem { Label = ccd.Label, TjMax = tj, Temp = ccd.Temp });
        }
        else CoreSectionTitle = "ÇEKİRDEK SICAKLIKLARI";

    }

    private static Brush StatusToBrush(ThermalStatus status) => status switch
    {
        ThermalStatus.Healthy => new SolidColorBrush(Color.FromRgb(63, 185, 80)),
        ThermalStatus.Warm => new SolidColorBrush(Color.FromRgb(88, 166, 255)),
        ThermalStatus.Caution => new SolidColorBrush(Color.FromRgb(210, 153, 34)),
        ThermalStatus.MaintenanceRequired => new SolidColorBrush(Color.FromRgb(255, 145, 0)),
        ThermalStatus.Critical => new SolidColorBrush(Color.FromRgb(248, 81, 73)),
        ThermalStatus.Unavailable => new SolidColorBrush(Color.FromRgb(139, 148, 158)),
        _ => _defaultStatusBrush
    };

    private static string StatusDetail(ThermalStatus status) => status switch
    {
        ThermalStatus.Healthy => "Sağlıklı",
        ThermalStatus.Warm => "Normal",
        ThermalStatus.Caution => "Dikkat",
        ThermalStatus.MaintenanceRequired => "Bakım Gerekli",
        ThermalStatus.Critical => "Kritik",
        ThermalStatus.Unavailable => "Sensör Yok",
        _ => ""
    };
}