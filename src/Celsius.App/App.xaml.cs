using System.Windows;
using System.Windows.Threading;
using Celsius.App.Localization;
using Celsius.App.Services;
using Celsius.Core;
using Celsius.Core.Settings;

namespace Celsius.App;

/// <summary>
/// Application entry point. Owns the engine, the tray icon and the background
/// polling timer, and coordinates window lifetime.
/// </summary>
public partial class App : Application
{
    private CelsiusEngine? _engine;
    private TrayIconManager? _tray;
    private DispatcherTimer? _pollTimer;
    private MainWindow? _mainWindow;
    private bool _isExiting;

    /// <summary>The shared application engine.</summary>
    public CelsiusEngine Engine =>
        _engine ?? throw new InvalidOperationException("Engine not initialized.");

    /// <summary>The live snapshot provider, updated by the background poll.</summary>
    public SystemSnapshotProvider SnapshotProvider { get; } = new();

    /// <summary>Current settings.</summary>
    public AppSettings Settings { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Settings = new SettingsStore().Load();
        LocalizationManager.ApplyAndRemember(Settings.Language);

        _engine = new CelsiusEngine();
        _engine.Initialize();

        // Tray icon.
        _tray = new TrayIconManager();
        _tray.ShowRequested += (_, _) => ShowMainWindow();
        _tray.StressTestRequested += (_, _) => ShowStressTest();
        _tray.ExitRequested += (_, _) => ExitApplication();
        _tray.Initialize();

        // Background polling.
        SnapshotProvider.Refresh(_engine);
        StartPolling();

        // Main window.
        _mainWindow = new MainWindow();
        if (!Settings.StartMinimized)
        {
            _mainWindow.Show();
        }

        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    private void StartPolling()
    {
        _pollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(Settings.PollIntervalSeconds),
        };

        _pollTimer.Tick += (_, _) =>
        {
            if (_engine is null)
            {
                return;
            }

            SnapshotProvider.Refresh(_engine);
            UpdateTrayTooltip();
        };

        _pollTimer.Start();
    }

    /// <summary>Reconfigures the polling timer after a settings change.</summary>
    public void ApplySettings(AppSettings settings)
    {
        Settings = settings;
        LocalizationManager.ApplyAndRemember(settings.Language);

        if (_pollTimer is not null)
        {
            _pollTimer.Interval = TimeSpan.FromSeconds(settings.PollIntervalSeconds);
        }
    }

    private void UpdateTrayTooltip()
    {
        if (_tray is null)
        {
            return;
        }

        var snapshot = SnapshotProvider.Current;
        var temp = snapshot.CpuTemperatureC is { } t
            ? $"{MathF.Round(t)} °C"
            : LocalizationManager.Get("ValueUnavailable");

        _tray.SetTooltip($"Celsius — CPU {temp}");
    }

    /// <summary>Shows (or restores) the main window.</summary>
    public void ShowMainWindow()
    {
        _mainWindow ??= new MainWindow();

        if (!_mainWindow.IsVisible)
        {
            _mainWindow.Show();
        }

        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        _mainWindow.Activate();
    }

    /// <summary>Opens the stress test window.</summary>
    public void ShowStressTest()
    {
        var window = new StressTestWindow { Owner = _mainWindow?.IsVisible == true ? _mainWindow : null };
        window.ShowDialog();
    }

    /// <summary>Requests application exit.</summary>
    public void ExitApplication()
    {
        _isExiting = true;
        Shutdown();
    }

    /// <summary>True when the app is shutting down for real (not hiding to tray).</summary>
    public bool IsExiting => _isExiting;

    protected override void OnExit(ExitEventArgs e)
    {
        _pollTimer?.Stop();
        _tray?.Dispose();
        _engine?.Dispose();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            new Celsius.Core.Logging.FileLogger().Critical("Unhandled UI exception.", e.Exception);
        }
        catch
        {
            // ignore
        }

        MessageBox.Show(
            e.Exception.Message,
            "Celsius",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }
}

