using System.Windows;
using WpfButton = System.Windows.Controls.Button;
using WinOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WinForms = System.Windows.Forms;
using KeyTimers.Models;
using KeyTimers.Services;

namespace KeyTimers.Views;

/// <summary>
/// Settings window opened from the system tray.
/// Works on a deep-cloned copy of the settings so changes can be cancelled cleanly.
/// </summary>
public sealed partial class SettingsWindow : Window
{
    private readonly AppSettings _live;
    private readonly TimerEngine _engine;
    private readonly SettingsService _settingsService;

    /// <summary>Clone of live settings; mutated by the UI and committed on Save.</summary>
    public AppSettings Settings { get; }

    public SettingsWindow(AppSettings live, TimerEngine engine, SettingsService settingsService)
    {
        _live            = live;
        _engine          = engine;
        _settingsService = settingsService;

        // build a working copy so Cancel truly discards changes
        Settings = live.Clone();

        InitializeComponent();
        DataContext = this;
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private void AddTimer_Click(object sender, RoutedEventArgs e)
    {
        Settings.Timers.Add(new TimerConfig
        {
            Key             = "X",
            Label           = "New timer",
            DurationSeconds = 30,
            Direction       = CountDirection.Down,
            AlertType       = AlertType.Visual,
        });
    }

    private void DeleteTimer_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is TimerConfig cfg)
            Settings.Timers.Remove(cfg);
    }

    private void BrowseSoundFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new WinOpenFileDialog
        {
            Title  = "Select alert sound",
            Filter = "Audio files|*.wav;*.mp3;*.ogg|All files|*.*"
        };
        if (dlg.ShowDialog() == true &&
            ((FrameworkElement)sender).Tag is TimerConfig cfg)
        {
            cfg.SoundFilePath = dlg.FileName;
        }
    }

    /// <summary>Opens a colour picker for the global background colour.</summary>
    private void PickBgColor_Click(object sender, RoutedEventArgs e)
    {
        var picked = ShowColorDialog(Settings.BackgroundColor);
        if (picked is not null)
            Settings.BackgroundColor = picked;
    }

    /// <summary>
    /// Opens a colour picker for a per-timer colour.
    /// Tag = <see cref="TimerConfig"/>, CommandParameter = "NormalColor" | "AlertColor".
    /// </summary>
    private void PickTimerColor_Click(object sender, RoutedEventArgs e)
    {
        var btn  = (WpfButton)sender;
        var cfg  = (TimerConfig)btn.Tag;
        var prop = (string)btn.CommandParameter;

        var current = prop == "NormalColor" ? cfg.NormalColor : cfg.AlertColor;
        var picked  = ShowColorDialog(current);
        if (picked is null) return;

        if (prop == "NormalColor") cfg.NormalColor = picked;
        else                       cfg.AlertColor  = picked;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        CommitToLive();
        _settingsService.Save(_live);
        _engine.ApplyConfigs(_live.Timers, _live);
    }

    private void SaveAndExit_Click(object sender, RoutedEventArgs e)
    {
        CommitToLive();
        _settingsService.Save(_live);
        _engine.ApplyConfigs(_live.Timers, _live);
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Shows the Windows color picker and returns a hex string, or null if cancelled.</summary>
    private static string? ShowColorDialog(string currentHex)
    {
        using var dlg = new WinForms.ColorDialog
        {
            FullOpen    = true,
            Color       = HexToDrawingColor(currentHex),
            AnyColor    = true,
        };
        return dlg.ShowDialog() == WinForms.DialogResult.OK
            ? $"#{dlg.Color.R:X2}{dlg.Color.G:X2}{dlg.Color.B:X2}"
            : null;
    }

    private static System.Drawing.Color HexToDrawingColor(string hex)
    {
        try
        {
            var c = (System.Windows.Media.Color)
                System.Windows.Media.ColorConverter.ConvertFromString(hex);
            return System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
        }
        catch { return System.Drawing.Color.White; }
    }

    private void PickPauseColor_Click(object sender, RoutedEventArgs e)
    {
        var picked = ShowColorDialog(Settings.PauseColor);
        if (picked is not null)
            Settings.PauseColor = picked;
    }

    private void PickBorderColor_Click(object sender, RoutedEventArgs e)
    {
        var picked = ShowColorDialog(Settings.TimerBorderColor);
        if (picked is not null)
            Settings.TimerBorderColor = picked;
    }

    private void CommitToLive()
    {
        _live.OverlayOpacity       = Settings.OverlayOpacity;
        _live.BackgroundColor      = Settings.BackgroundColor;
        _live.FontFamily           = Settings.FontFamily;
        _live.FontSize             = Settings.FontSize;
        _live.ShowKeyLabel         = Settings.ShowKeyLabel;
        _live.PauseKey             = Settings.PauseKey;
        _live.PauseColor           = Settings.PauseColor;
        _live.TimerBorderThickness = Settings.TimerBorderThickness;
        _live.TimerBorderColor     = Settings.TimerBorderColor;
        _live.ShowDecimal          = Settings.ShowDecimal;

        _live.Timers.Clear();
        foreach (var t in Settings.Timers)
            _live.Timers.Add(t.Clone());
    }


}
