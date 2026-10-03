using System.Windows;
using Celsius.App.Localization;
using Celsius.Core.Models;
using Celsius.Core.Settings;

namespace Celsius.App;

/// <summary>
/// The main dashboard window: a simple list of metrics plus buttons to open the
/// stress test and settings windows.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>Creates the main window and wires it to the shared engine.</summary>
    public MainWindow()
    {
        InitializeComponent();

        ApplyLocalizedText();

        Loaded += OnLoaded;
        Closed += OnClosed;

        var app = (App)Application.Current;
        app.SnapshotProvider.SnapshotChanged += OnSnapshotChanged;
        LocalizationManager.LanguageChanged += OnLanguageChanged;
    }

    private static App App => (App)Application.Current;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        CpuNameText.Text = App.Engine.Cpu.Name;
        UpdateThermalDisplay();
        RenderSnapshot(App.SnapshotProvider.Current);

        var elevated = IsElevated();
        AdminWarningBorder.Visibility = elevated ? Visibility.Collapsed : Visibility.Visible;

        // If we're elevated but still have no temperature sensors, the kernel
        // driver (PawnIO) is almost certainly missing or blocked — show the
        // explicit sensor warning so the user isn't left wondering.
        var sensorsMissing = !App.Engine.HasTemperatureSensors;
        SensorWarningBorder.Visibility = elevated && sensorsMissing
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        App.SnapshotProvider.SnapshotChanged -= OnSnapshotChanged;
        LocalizationManager.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => ApplyLocalizedText();

    private void ApplyLocalizedText()
    {
        CpuHeader.Text = LocalizationManager.Get("CpuSection");
        MemoryHeader.Text = LocalizationManager.Get("MemorySection");
        StorageHeader.Text = LocalizationManager.Get("StorageSection");
        GpuHeader.Text = LocalizationManager.Get("GpuSection");

        CpuTempLabel.Text = LocalizationManager.Get("CpuTemperature");
        CpuClockLabel.Text = LocalizationManager.Get("CpuClock");
        CpuLoadLabel.Text = LocalizationManager.Get("CpuLoad");
        ThermalLabel.Text = LocalizationManager.Get("ThermalLimit");
        MemoryLabel.Text = LocalizationManager.Get("MemoryUsed");

        AdminWarningText.Text = LocalizationManager.Get("AdminWarning");
        SensorWarningText.Text = LocalizationManager.Get("SensorWarning");
        StressButton.Content = LocalizationManager.Get("StressTestButton");
        SettingsButton.Content = LocalizationManager.Get("SettingsButton");

        UpdateThermalDisplay();
    }

    private void UpdateThermalDisplay()
    {
        var profile = App.Engine.ThermalProfile;
        var sourceKey = profile.Source switch
        {
            ThermalSource.Hardware => "ThermalSourceHardware",
            ThermalSource.LookupTable => "ThermalSourceTable",
            _ => "ThermalSourceDefault",
        };

        var sourceLabel = LocalizationManager.Get(sourceKey);
        ThermalValue.Text =
            $"{MathF.Round(profile.TjMaxC)} °C ({sourceLabel}) → {MathF.Round(profile.StopThresholdC)} °C";
    }

    private void OnSnapshotChanged(object? sender, SystemSnapshot snapshot)
    {
        // Marshalled onto the UI thread because the provider fires from a worker.
        Dispatcher.BeginInvoke(() => RenderSnapshot(snapshot));
    }

    private void RenderSnapshot(SystemSnapshot snapshot)
    {
        CpuTempValue.Text = Format(snapshot.CpuTemperatureC, "°C");
        CpuTempValue.Foreground = ResolveTemperatureBrush(snapshot.CpuTemperatureC);
        CpuClockValue.Text = Format(snapshot.CpuClockMhz, "MHz");
        CpuLoadValue.Text = Format(snapshot.CpuLoadPercent, "%");
        MemoryValue.Text = Format(snapshot.MemoryUsedPercent, "%");

        DriveList.ItemsSource = snapshot.Drives.ToList();

        var gpus = snapshot.Gpus.Select(g => new GpuRow
        {
            Name = g.Name,
            TemperatureC = g.TemperatureC,
            ClockMhz = g.ClockMhz,
            TempLabel = LocalizationManager.Get("GpuTemperature"),
            ClockLabel = LocalizationManager.Get("GpuClock"),
        }).ToList();

        GpuList.ItemsSource = gpus;
    }

    private static string Format(float? value, string unit) =>
        value is { } v && !float.IsNaN(v)
            ? $"{MathF.Round(v)} {unit}"
            : LocalizationManager.Get("ValueUnavailable");

    /// <summary>
    /// Picks the temperature colour brush adaptively: green normally, amber as it
    /// approaches the warm threshold and red near the thermal limit.
    /// </summary>
    private static System.Windows.Media.Brush ResolveTemperatureBrush(float? value)
    {
        // Thresholds are fixed for the dashboard; GPU rows keep their own converter
        // defaults so the two views can be tuned independently.
        const double hotThreshold = 85;
        const double warmThreshold = 70;

        var color = Converters.TemperatureBrushConverter.ResolveColor(
            value,
            hotThreshold,
            warmThreshold,
            "#22C55E",
            "#F59E0B",
            "#EF4444");

        return (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter()
            .ConvertFromString(color)!;
    }

    private void OnStressClick(object sender, RoutedEventArgs e) => App.ShowStressTest();

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            ApplyLocalizedText();
            RenderSnapshot(App.SnapshotProvider.Current);
        }
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var behavior = App.Settings.CloseBehavior;

        if (behavior == CloseBehavior.MinimizeToTray && !App.IsExiting)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        App.ExitApplication();
    }

    private static bool IsElevated()
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    /// <summary>View model row for a GPU in the list.</summary>
    private sealed class GpuRow
    {
        public required string Name { get; init; }

        public float? TemperatureC { get; init; }

        public float? ClockMhz { get; init; }

        public required string TempLabel { get; init; }

        public required string ClockLabel { get; init; }
    }
}