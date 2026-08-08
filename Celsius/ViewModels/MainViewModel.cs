using System.Collections.ObjectModel;
using System.Windows.Threading;
using Celsius.Models;
using Celsius.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Celsius.ViewModels;

/// <summary>
/// Ana ViewModel: servisleri besler, saniyelik anket döngüsünü çalıştırır ve
/// sekmelerin (Dashboard / Disk / Stress) gezinmesini yönetir.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly HardwareMonitorService _hw;
    private readonly AiAdvisorService _advisor;
    private readonly DispatcherTimer _timer;

    private readonly DashboardViewModel _dashboard;
    private readonly DiskViewModel _disk;
    private readonly StressViewModel _stress;

    public ObservableCollection<NavItem> NavItems { get; }

    [ObservableProperty] private int _selectedNavIndex;

    public object CurrentViewModel => NavItems[SelectedNavIndex].ViewModel;

    public MainViewModel(HardwareMonitorService hw, StressTestService stress, AiAdvisorService advisor)
    {
        _hw = hw;
        _advisor = advisor;

        _dashboard = new DashboardViewModel();
        _disk = new DiskViewModel();
        _stress = new StressViewModel(stress);

        NavItems = new ObservableCollection<NavItem>
        {
            new() { Label = "Genel Bakış", Icon = "◉", ViewModel = _dashboard },
            new() { Label = "Disk Sağlığı", Icon = "◈", ViewModel = _disk },
            new() { Label = "Stress Test", Icon = "▲", ViewModel = _stress }
        };
        SelectedNavIndex = 0;

        // İlk okuma + CPU model eşlemesi
        _hw.Poll();
        var first = _hw.Latest;
        _advisor.DetectCpu(first.CpuName);
        RefreshAll(first);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    partial void OnSelectedNavIndexChanged(int value) => OnPropertyChanged(nameof(CurrentViewModel));

    private void Tick()
    {
        _hw.Poll();
        RefreshAll(_hw.Latest);
    }

    private void RefreshAll(SensorSnapshot snap)
    {
        var advice = _advisor.Evaluate(snap);
        _dashboard.Refresh(snap, advice, _advisor.CurrentCpuModel);
        _disk.Refresh(_hw.Disks);
        _stress.Refresh(snap, advice);
    }

    public void Shutdown()
    {
        _timer.Stop();
        _hw.Dispose();
    }
}