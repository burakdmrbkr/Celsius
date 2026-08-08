using System.Windows.Media;
using Celsius.Models;
using Celsius.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Celsius.ViewModels;

/// <summary>"Stress Test" sekmesi: CPU'yu yüke sokar ve yük altında sıcaklığı izler.</summary>
public partial class StressViewModel : ObservableObject
{
    private readonly StressTestService _stress;

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private string _cpuLoadText = "—";
    [ObservableProperty] private string _cpuTempText = "—";
    [ObservableProperty] private string _statusText = "Test başlatılmadı";
    [ObservableProperty] private Brush _statusBrush = new SolidColorBrush(Color.FromRgb(139, 148, 158));

    public StressViewModel(StressTestService stress)
    {
        _stress = stress;
        IsRunning = stress.IsRunning;
    }

    [RelayCommand]
    private void Toggle()
    {
        if (_stress.IsRunning) _stress.Stop();
        else _stress.Start();
        IsRunning = _stress.IsRunning;
        if (!IsRunning) StatusText = "Test durduruldu — sıcaklık normale dönüyor.";
    }

    public void Refresh(SensorSnapshot snap, AdvisorResult advice)
    {
        CpuLoadText = snap.CpuLoad is { } l ? $"{l:0}%" : "—";
        CpuTempText = snap.CpuPackageTemp is { } p ? $"{p:0.0} °C" : "—";

        if (IsRunning)
        {
            StatusText = advice.Summary;
            StatusBrush = advice.Status switch
            {
                ThermalStatus.Critical => new SolidColorBrush(Color.FromRgb(248, 81, 73)),
                ThermalStatus.MaintenanceRequired => new SolidColorBrush(Color.FromRgb(255, 145, 0)),
                ThermalStatus.Caution => new SolidColorBrush(Color.FromRgb(210, 153, 34)),
                _ => new SolidColorBrush(Color.FromRgb(63, 185, 80))
            };
        }
    }
}