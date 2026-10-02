using System.Windows;
using H.NotifyIcon;

namespace Celsius.App.Services;

/// <summary>
/// Owns the system tray icon and its context menu, bridging it to application
/// actions.
/// </summary>
public sealed class TrayIconManager : IDisposable
{
    private TaskbarIcon? _icon;

    /// <summary>Raised when the user chooses "Show".</summary>
    public event EventHandler? ShowRequested;

    /// <summary>Raised when the user chooses "Stress Test".</summary>
    public event EventHandler? StressTestRequested;

    /// <summary>Raised when the user chooses "Exit".</summary>
    public event EventHandler? ExitRequested;

    /// <summary>Creates and shows the tray icon.</summary>
    public void Initialize()
    {
        _icon = new TaskbarIcon
        {
            ToolTipText = "Celsius",
            Icon = TrayIconFactory.CreateIcon(),
        };

        _icon.LeftClickCommand = new RelayCommand(() => ShowRequested?.Invoke(this, EventArgs.Empty));
        _icon.ContextMenu = BuildContextMenu();
        _icon.ForceCreate();
    }

    /// <summary>Updates the tray tooltip text.</summary>
    public void SetTooltip(string text)
    {
        if (_icon is not null)
        {
            _icon.ToolTipText = text;
        }
    }

    private System.Windows.Controls.ContextMenu BuildContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        menu.Items.Add(new System.Windows.Controls.MenuItem
        {
            Header = Localization.LocalizationManager.Get("TrayShow"),
            Command = new RelayCommand(() => ShowRequested?.Invoke(this, EventArgs.Empty)),
        });

        menu.Items.Add(new System.Windows.Controls.MenuItem
        {
            Header = Localization.LocalizationManager.Get("TrayStressTest"),
            Command = new RelayCommand(() => StressTestRequested?.Invoke(this, EventArgs.Empty)),
        });

        menu.Items.Add(new System.Windows.Controls.Separator());

        menu.Items.Add(new System.Windows.Controls.MenuItem
        {
            Header = Localization.LocalizationManager.Get("TrayExit"),
            Command = new RelayCommand(() => ExitRequested?.Invoke(this, EventArgs.Empty)),
        });

        return menu;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _icon?.Dispose();
        _icon = null;
    }
}

/// <summary>Minimal ICommand implementation for tray actions.</summary>
internal sealed class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute) => _execute = execute;

    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute();
}
