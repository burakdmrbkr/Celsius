using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Celsius.App.Localization;
using Celsius.Core.Settings;

namespace Celsius.App;

/// <summary>
/// A simple settings dialog: polling interval, default stress duration, worker
/// count, close behaviour and language.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly AppSettings _working;
    private bool _suppressLanguageChange;

    private static readonly (string? Culture, string LabelKey)[] LanguageOptions =
    [
        (null, "LanguageSystem"),
        ("en", "LanguageEnglish"),
        ("tr", "LanguageTurkish"),
    ];

    /// <summary>Creates the settings window seeded from the current settings.</summary>
    public SettingsWindow()
    {
        InitializeComponent();

        _working = ((App)Application.Current).Settings.Clone();

        ApplyLocalizedText();
        LoadIntoControls();
    }

    private static App App => (App)Application.Current;

    private void ApplyLocalizedText()
    {
        Title = LocalizationManager.Get("SettingsTitle");
        SettingsTitle.Text = LocalizationManager.Get("SettingsTitle");
        LanguageLabel.Text = LocalizationManager.Get("LanguageLabel");
        PollLabel.Text = LocalizationManager.Get("PollIntervalLabel");
        DefaultDurationLabel.Text = LocalizationManager.Get("DefaultDurationLabel");
        WorkerLabel.Text = LocalizationManager.Get("WorkerCountLabel");
        CloseBehaviorLabel.Text = LocalizationManager.Get("CloseBehaviorLabel");
        CloseTrayRadio.Content = LocalizationManager.Get("CloseBehaviorTray");
        CloseExitRadio.Content = LocalizationManager.Get("CloseBehaviorExit");
        CancelButton.Content = LocalizationManager.Get("CancelButton");
        SaveButton.Content = LocalizationManager.Get("SaveButton");
    }

    private void LoadIntoControls()
    {
        _suppressLanguageChange = true;
        LanguageCombo.Items.Clear();
        var selectedIndex = 0;
        for (var i = 0; i < LanguageOptions.Length; i++)
        {
            LanguageCombo.Items.Add(LocalizationManager.Get(LanguageOptions[i].LabelKey));
            if (LanguageOptions[i].Culture == _working.Language)
            {
                selectedIndex = i;
            }
        }

        LanguageCombo.SelectedIndex = selectedIndex;
        _suppressLanguageChange = false;

        PollBox.Text = _working.PollIntervalSeconds.ToString(CultureInfo.InvariantCulture);
        DefaultDurationBox.Text = _working.LastStressDurationMinutes.ToString(CultureInfo.InvariantCulture);
        WorkerBox.Text = _working.StressWorkerCount.ToString(CultureInfo.InvariantCulture);

        if (_working.CloseBehavior == CloseBehavior.Exit)
        {
            CloseExitRadio.IsChecked = true;
        }
        else
        {
            CloseTrayRadio.IsChecked = true;
        }
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressLanguageChange || LanguageCombo.SelectedIndex < 0)
        {
            return;
        }

        var culture = LanguageOptions[LanguageCombo.SelectedIndex].Culture;
        _working.Language = culture;
        LocalizationManager.ApplyAndRemember(culture);
        ApplyLocalizedText();
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(PollBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var poll))
        {
            _working.PollIntervalSeconds = poll;
        }

        if (int.TryParse(DefaultDurationBox.Text, out var duration))
        {
            _working.LastStressDurationMinutes = duration;
        }

        if (int.TryParse(WorkerBox.Text, out var workers))
        {
            _working.StressWorkerCount = workers;
        }

        _working.CloseBehavior = CloseExitRadio.IsChecked == true
            ? CloseBehavior.Exit
            : CloseBehavior.MinimizeToTray;

        _working.Clamp();

        new SettingsStore().Save(_working);
        App.ApplySettings(_working);

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        // Revert any live language change made while the dialog was open.
        LocalizationManager.ApplyAndRemember(App.Settings.Language);
        DialogResult = false;
        Close();
    }
}
