using System.Collections.ObjectModel;
using System.Windows.Media;
using Celsius.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Celsius.ViewModels;

/// <summary>"Genel Bakış" sekmesi: canlı sıcaklıklar, gerçek zamanlı grafik ve AI durum özeti.</summary>
public partial class DashboardViewModel : ObservableObject
{
    private const int ChartPoints = 60;
    private readonly ObservableCollection<double> _tempHistory = new();
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

    public ObservableCollection<CoreTempItem> CoreTemps { get; } = new();

    public ISeries[] Series { get; }

    public Axis[] XAxes { get; }
    public Axis[] YAxes { get; }

    public DashboardViewModel()
    {
        Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Name = "CPU Sıcaklık",
                Values = _tempHistory,
                Fill = null,
                GeometrySize = 0,
                LineSmoothness = 0.5,
                Stroke = new SolidColorPaint(SKColor.Parse("#2F81F7")) { StrokeThickness = 2 }
            }
        };
        XAxes = new[]
        {
            new Axis { Name = "Son 60 sn", MinLimit = 0, MaxLimit = ChartPoints - 1,
                       SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#30363D")) { StrokeThickness = 1 } }
        };
        YAxes = new[]
        {
            new Axis { Name = "°C", MinLimit = 0, MaxLimit = 105,
                       Labeler = v => v + "°C",
                       SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#30363D")) { StrokeThickness = 1 } }
        };
    }

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

        ModelMatched = model is null ? "Veritabanında eşleşme yok — varsayılan eşikler kullanılıyor." : $"Eşleşen model: {model}";

        // Çekirdek listesi
        CoreTemps.Clear();
        if (snap.CoreTemps.Count > 0)
        {
            var tj = model?.TjMax ?? (snap.CpuName.Contains("AMD", StringComparison.OrdinalIgnoreCase) ? 95 : 100);
            for (var i = 0; i < snap.CoreTemps.Count; i++)
                CoreTemps.Add(new CoreTempItem { Label = $"Çekirdek {i + 1}", TjMax = tj, Temp = snap.CoreTemps[i] });
        }

        // Grafik: kaydıran pencere
        if (snap.CpuPackageTemp is { } t)
        {
            _tempHistory.Add(t);
            while (_tempHistory.Count > ChartPoints) _tempHistory.RemoveAt(0);
        }
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