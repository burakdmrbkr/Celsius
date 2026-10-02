using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using Celsius.App.Localization;
using Celsius.Core.Models;
using Celsius.Core.Stress;

namespace Celsius.App;

/// <summary>
/// The stress test window: choose a duration, run a CPU load, watch live
/// temperature, and review a summary when the run ends.
/// </summary>
public partial class StressTestWindow : Window
{
    private const int MaxSamples = 300;

    private readonly ObservableCollection<float> _samples = [];
    private readonly ThermalGuard _guard;
    private readonly DispatcherTimer _uiTimer;
    private readonly DateTime _startTime = DateTime.UtcNow;

    private TimeSpan _selectedDuration;
    private bool _running;
    private bool _suppressCustomSync;

    /// <summary>Creates the stress test window bound to the shared engine.</summary>
    public StressTestWindow()
    {
        InitializeComponent();

        _guard = App.Engine.CreateThermalGuard();
        Chart.Values = _samples;
        Chart.Threshold = _guard.StopThresholdC;
        Chart.Maximum = Math.Max(110, _guard.Profile.TjMaxC + 5);

        ApplyLocalizedText();
        SelectPreset(App.Settings.LastStressDurationMinutes);
        UpdateStats();

        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _uiTimer.Tick += OnUiTick;

        Loaded += (_, _) => UpdateButtonStates();
        Closed += (_, _) => StopIfRunning();
    }

    private static App App => (App)Application.Current;

    private void ApplyLocalizedText()
    {
        Title = LocalizationManager.Get("StressTestTitle");
        DurationLabel.Text = LocalizationManager.Get("DurationLabel");
        CustomLabel.Text = LocalizationManager.Get("CustomMinutesLabel");
        StartButton.Content = LocalizationManager.Get("StartButton");
        StopButton.Content = LocalizationManager.Get("StopButton");
        CloseButton.Content = LocalizationManager.Get("CloseButton");
        ElapsedLabel.Text = LocalizationManager.Get("ElapsedLabel");
        RemainingLabel.Text = LocalizationManager.Get("RemainingLabel");
        CurrentTempLabel.Text = LocalizationManager.Get("CpuTemperature");
        MaxLabel.Text = LocalizationManager.Get("MaxTempLabel");
        MinLabel.Text = LocalizationManager.Get("MinTempLabel");
        AvgLabel.Text = LocalizationManager.Get("AvgTempLabel");
    }

    private void SelectPreset(int minutes)
    {
        _suppressCustomSync = true;

        switch (minutes)
        {
            case 1: Dur1.IsChecked = true; break;
            case 2: Dur2.IsChecked = true; break;
            case 3: Dur3.IsChecked = true; break;
            case 5: Dur5.IsChecked = true; break;
            default:
                Dur1.IsChecked = false;
                Dur2.IsChecked = false;
                Dur3.IsChecked = false;
                Dur5.IsChecked = false;
                CustomMinutesBox.Text = minutes.ToString(CultureInfo.InvariantCulture);
                break;
        }

        _suppressCustomSync = false;
        _selectedDuration = TimeSpan.FromMinutes(minutes);
    }

    private void OnDurationPresetChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressCustomSync)
        {
            return;
        }

        if (Dur1.IsChecked == true) _selectedDuration = TimeSpan.FromMinutes(1);
        else if (Dur2.IsChecked == true) _selectedDuration = TimeSpan.FromMinutes(2);
        else if (Dur3.IsChecked == true) _selectedDuration = TimeSpan.FromMinutes(3);
        else if (Dur5.IsChecked == true) _selectedDuration = TimeSpan.FromMinutes(5);
    }

    private void OnCustomMinutesChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_suppressCustomSync)
        {
            return;
        }

        if (int.TryParse(CustomMinutesBox.Text, out var minutes)
            && minutes >= StressTestOptions.MinDurationMinutes
            && minutes <= StressTestOptions.MaxDurationMinutes)
        {
            _suppressCustomSync = true;
            Dur1.IsChecked = false;
            Dur2.IsChecked = false;
            Dur3.IsChecked = false;
            Dur5.IsChecked = false;
            _suppressCustomSync = false;

            _selectedDuration = TimeSpan.FromMinutes(minutes);
        }
    }

    private void OnStartClick(object sender, RoutedEventArgs e)
    {
        var minutes = (int)Math.Round(_selectedDuration.TotalMinutes);
        minutes = Math.Clamp(minutes, StressTestOptions.MinDurationMinutes, StressTestOptions.MaxDurationMinutes);
        _selectedDuration = TimeSpan.FromMinutes(minutes);

        var confirm = MessageBox.Show(
            LocalizationManager.Get("StressWarningBody"),
            LocalizationManager.Get("StressWarningTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        // Persist the chosen duration for next time.
        var settings = App.Settings.Clone();
        settings.LastStressDurationMinutes = minutes;
        App.ApplySettings(settings);
        new Celsius.Core.Settings.SettingsStore().Save(settings);

        _samples.Clear();
        _running = true;
        UpdateButtonStates();
        _uiTimer.Start();

        var options = new StressTestOptions
        {
            Duration = _selectedDuration,
            WorkerCount = App.Settings.StressWorkerCount,
        };

        App.Engine.Stressor.Start(options, OnThermalAbort);
    }

    private bool OnThermalAbort()
    {
        var snapshot = App.SnapshotProvider.Current;
        if (_guard.Sample(snapshot.CpuTemperatureC))
        {
            return true;
        }

        return false;
    }

    private void OnStopClick(object sender, RoutedEventArgs e) => StopIfRunning(userInitiated: true);

    private void StopIfRunning(bool userInitiated = false)
    {
        if (!_running)
        {
            return;
        }

        App.Engine.Stressor.Stop();
        _running = false;
        _uiTimer.Stop();
        UpdateButtonStates();
    }

    private async void OnUiTick(object? sender, EventArgs e)
    {
        var snapshot = App.SnapshotProvider.Current;

        if (snapshot.CpuTemperatureC is { } temp)
        {
            _samples.Add(temp);
            while (_samples.Count > MaxSamples)
            {
                _samples.RemoveAt(0);
            }

            _guard.Sample(temp);
            CurrentTempValue.Text = $"{MathF.Round(temp)} °C";
        }

        var elapsed = DateTime.UtcNow - _startTime;
        ElapsedValue.Text = FormatDuration(elapsed);

        var remaining = _selectedDuration - elapsed;
        RemainingValue.Text = remaining > TimeSpan.Zero ? FormatDuration(remaining) : "00:00";

        UpdateStats();

        if (_running && !App.Engine.Stressor.IsRunning)
        {
            await FinishAsync();
        }
    }

    private async Task FinishAsync()
    {
        _running = false;
        _uiTimer.Stop();
        UpdateButtonStates();

        var reason = await App.Engine.Stressor.WaitForCompletionAsync();
        var elapsed = DateTime.UtcNow - _startTime;

        var summary = _guard.BuildSummary(
            reason,
            elapsed,
            _selectedDuration,
            App.Engine.Stressor.Iterations);

        ShowSummary(summary);
    }

    private void ShowSummary(StressTestSummary summary)
    {
        var reasonKey = summary.StopReason switch
        {
            StressTestStopReason.Completed => "StopReasonCompleted",
            StressTestStopReason.UserStopped => "StopReasonUserStopped",
            StressTestStopReason.ThermalLimitExceeded => "StopReasonThermal",
            _ => "StopReasonError",
        };

        var temp = summary.MaxTemperatureC is { } t ? $"{MathF.Round(t)} °C" : LocalizationManager.Get("ValueUnavailable");
        var avg = summary.AverageTemperatureC is { } a ? $"{MathF.Round(a)} °C" : LocalizationManager.Get("ValueUnavailable");

        var body =
            $"{LocalizationManager.Get(reasonKey)}\n\n" +
            $"{LocalizationManager.Get("ElapsedLabel")}: {FormatDuration(summary.Elapsed)}\n" +
            $"{LocalizationManager.Get("MaxTempLabel")}: {temp}\n" +
            $"{LocalizationManager.Get("AvgTempLabel")}: {avg}";

        MessageBox.Show(
            body,
            LocalizationManager.Get("StressSummaryTitle"),
            MessageBoxButton.OK,
            summary.StopReason == StressTestStopReason.ThermalLimitExceeded
                ? MessageBoxImage.Warning
                : MessageBoxImage.Information);
    }

    private void UpdateStats()
    {
        if (_samples.Count == 0)
        {
            MaxValue.Text = MinValue.Text = AvgValue.Text = "—";
            return;
        }

        MaxValue.Text = $"{MathF.Round(_samples.Max())} °C";
        MinValue.Text = $"{MathF.Round(_samples.Min())} °C";
        AvgValue.Text = $"{MathF.Round((float)_samples.Average())} °C";
    }

    private static string FormatDuration(TimeSpan span)
    {
        if (span < TimeSpan.Zero)
        {
            span = TimeSpan.Zero;
        }

        return $"{(int)span.TotalMinutes:00}:{span.Seconds:00}";
    }

    private void UpdateButtonStates()
    {
        StartButton.IsEnabled = !_running;
        StopButton.IsEnabled = _running;
        Dur1.IsEnabled = Dur2.IsEnabled = Dur3.IsEnabled = Dur5.IsEnabled = !_running;
        CustomMinutesBox.IsEnabled = !_running;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
