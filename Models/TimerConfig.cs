using System.Text.Json.Serialization;

namespace KeyTimers.Models;

/// <summary>Direction in which a timer counts.</summary>
public enum CountDirection
{
    Down,
    Up
}

/// <summary>Behaviour when the bound key is pressed while the timer is already running.</summary>
public enum ClickBehaviour
{
    /// <summary>Restart the timer on any key press, regardless of state.</summary>
    Restart,

    /// <summary>
    /// While running the key is completely blocked — it is not sent to any other application.
    /// First press (idle) and press after completion start / restart the timer normally.
    /// </summary>
    Block,

    /// <summary>Key is always ignored; the timer simply runs to completion and stays there.</summary>
    Continue,
}

/// <summary>Alert modalities triggered when a timer reaches its target.</summary>
[Flags]
public enum AlertType
{
    None   = 0,
    Visual = 1,
    Sound  = 2,
    Both   = Visual | Sound
}

/// <summary>
/// Mutable configuration for a single key-bound timer.
/// Implements <see cref="INotifyPropertyChanged"/> so the settings UI reacts live.
/// </summary>
public sealed class TimerConfig : ObservableBase
{
    private string _id = Guid.NewGuid().ToString();
    private string _key = "E";
    private string _label = "Timer";
    private double _durationSeconds = 30;
    private CountDirection _direction = CountDirection.Down;
    private ClickBehaviour _clickBehaviour = ClickBehaviour.Restart;
    private AlertType _alertType = AlertType.Visual;
    private string _normalColor = "#FFFFFF";
    private string _alertColor = "#FF4444";
    private string _soundFilePath = "";
    private bool _enabled = true;
    private bool _autoResendOnComplete = false;
    private double _autoTriggerMinDelay = 0;
    private double _autoTriggerMaxDelay = 0;

    public string Id
    {
        get => _id;
        set => Set(ref _id, value);
    }

    /// <summary>Virtual-key string used to trigger this timer (e.g. "E", "F1", "1").</summary>
    public string Key
    {
        get => _key;
        set => Set(ref _key, value);
    }

    /// <summary>Display label shown on the overlay.</summary>
    public string Label
    {
        get => _label;
        set => Set(ref _label, value);
    }

    /// <summary>Target duration in seconds.</summary>
    public double DurationSeconds
    {
        get => _durationSeconds;
        set => Set(ref _durationSeconds, value);
    }

    public CountDirection Direction
    {
        get => _direction;
        set => Set(ref _direction, value);
    }

    public ClickBehaviour ClickBehaviour
    {
        get => _clickBehaviour;
        set => Set(ref _clickBehaviour, value);
    }

    public AlertType AlertType
    {
        get => _alertType;
        set => Set(ref _alertType, value);
    }

    /// <summary>Hex colour for the timer text in its normal state.</summary>
    public string NormalColor
    {
        get => _normalColor;
        set => Set(ref _normalColor, value);
    }

    /// <summary>Hex colour applied when the alert fires.</summary>
    public string AlertColor
    {
        get => _alertColor;
        set => Set(ref _alertColor, value);
    }

    /// <summary>Absolute path to a WAV/MP3 file played on alert. Empty means built-in beep.</summary>
    public string SoundFilePath
    {
        get => _soundFilePath;
        set => Set(ref _soundFilePath, value);
    }

    public bool Enabled
    {
        get => _enabled;
        set => Set(ref _enabled, value);
    }

    /// <summary>
    /// When true, the bound key is automatically simulated (sent to the active window)
    /// when the timer completes, and the timer restarts immediately.
    /// Works independently of <see cref="ClickBehaviour"/>.
    /// </summary>
    public bool AutoResendOnComplete
    {
        get => _autoResendOnComplete;
        set => Set(ref _autoResendOnComplete, value);
    }

    /// <summary>Minimum random extra delay in seconds before the auto-trigger fires.</summary>
    public double AutoTriggerMinDelay
    {
        get => _autoTriggerMinDelay;
        set => Set(ref _autoTriggerMinDelay, Math.Max(0, value));
    }

    /// <summary>Maximum random extra delay in seconds before the auto-trigger fires.</summary>
    public double AutoTriggerMaxDelay
    {
        get => _autoTriggerMaxDelay;
        set => Set(ref _autoTriggerMaxDelay, Math.Max(0, value));
    }

    /// <summary>Returns a deep copy of this instance.</summary>
    public TimerConfig Clone() => new()
    {
        Id                   = Id,
        Key                  = Key,
        Label                = Label,
        DurationSeconds      = DurationSeconds,
        Direction            = Direction,
        ClickBehaviour       = ClickBehaviour,
        AlertType            = AlertType,
        NormalColor          = NormalColor,
        AlertColor           = AlertColor,
        SoundFilePath        = SoundFilePath,
        Enabled              = Enabled,
        AutoResendOnComplete = AutoResendOnComplete,
        AutoTriggerMinDelay  = AutoTriggerMinDelay,
        AutoTriggerMaxDelay  = AutoTriggerMaxDelay,
    };
}
