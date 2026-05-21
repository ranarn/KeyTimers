using System.Collections.ObjectModel;

namespace KeyTimers.Models;

/// <summary>
/// Global application settings persisted to disk alongside the timer list.
/// </summary>
public sealed class AppSettings : ObservableBase
{
    private double _overlayLeft = 100;
    private double _overlayTop = 100;
    private double _overlayOpacity = 0.92;
    private string _backgroundColor = "#1A1A1A";
    private string _fontFamily = "Consolas";
    private double _fontSize = 14;
    private bool _showKeyLabel = true;
    private string _pauseKey = "";
    private string _pauseColor = "#888888";
    private double _timerBorderThickness = 0;
    private string _timerBorderColor = "#45475A";
    private bool _showDecimal = true;

    /// <summary>Screen X position of the overlay window.</summary>
    public double OverlayLeft
    {
        get => _overlayLeft;
        set => Set(ref _overlayLeft, value);
    }

    /// <summary>Screen Y position of the overlay window.</summary>
    public double OverlayTop
    {
        get => _overlayTop;
        set => Set(ref _overlayTop, value);
    }

    /// <summary>Overall window opacity (0.0–1.0).</summary>
    public double OverlayOpacity
    {
        get => _overlayOpacity;
        set => Set(ref _overlayOpacity, Math.Clamp(value, 0.1, 1.0));
    }

    /// <summary>Hex background colour of the overlay panel.</summary>
    public string BackgroundColor
    {
        get => _backgroundColor;
        set => Set(ref _backgroundColor, value);
    }

    /// <summary>Font family name used for all timer labels.</summary>
    public string FontFamily
    {
        get => _fontFamily;
        set => Set(ref _fontFamily, value);
    }

    /// <summary>Base font size in device-independent pixels.</summary>
    public double FontSize
    {
        get => _fontSize;
        set => Set(ref _fontSize, value);
    }

    /// <summary>Whether to display the bound key letter next to the time.</summary>
    public bool ShowKeyLabel
    {
        get => _showKeyLabel;
        set => Set(ref _showKeyLabel, value);
    }

    /// <summary>Key name that globally pauses/resumes all timers (e.g. "F9"). Empty = disabled.</summary>
    public string PauseKey
    {
        get => _pauseKey;
        set => Set(ref _pauseKey, value);
    }

    /// <summary>Hex colour used on the overlay when timers are paused.</summary>
    public string PauseColor
    {
        get => _pauseColor;
        set => Set(ref _pauseColor, value);
    }

    /// <summary>Uniform border thickness (px) drawn around each timer row on the overlay. 0 = no border.</summary>
    public double TimerBorderThickness
    {
        get => _timerBorderThickness;
        set => Set(ref _timerBorderThickness, value);
    }

    /// <summary>Hex colour of the per-timer border.</summary>
    public string TimerBorderColor
    {
        get => _timerBorderColor;
        set => Set(ref _timerBorderColor, value);
    }

    /// <summary>Whether the overlay shows one decimal digit on the timer (e.g. 90.3 vs 90).</summary>
    public bool ShowDecimal
    {
        get => _showDecimal;
        set => Set(ref _showDecimal, value);
    }

    /// <summary>Ordered list of timer configurations.</summary>
    public ObservableCollection<TimerConfig> Timers { get; set; } = [];

    /// <summary>Returns a deep copy including all timer configurations.</summary>
    public AppSettings Clone()
    {
        var clone = new AppSettings
        {
            OverlayLeft          = OverlayLeft,
            OverlayTop           = OverlayTop,
            OverlayOpacity       = OverlayOpacity,
            BackgroundColor      = BackgroundColor,
            FontFamily           = FontFamily,
            FontSize             = FontSize,
            ShowKeyLabel         = ShowKeyLabel,
            PauseKey             = PauseKey,
            PauseColor           = PauseColor,
            TimerBorderThickness = TimerBorderThickness,
            TimerBorderColor     = TimerBorderColor,
            ShowDecimal          = ShowDecimal,
        };
        foreach (var t in Timers)
            clone.Timers.Add(t.Clone());
        return clone;
    }
}
