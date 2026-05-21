using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace KeyTimers.Models;

/// <summary>Runtime status of a single timer instance.</summary>
public enum TimerStatus
{
    Idle,
    Running,
    Completed
}

/// <summary>
/// Observable runtime state bound directly to the overlay UI for one timer.
/// </summary>
public sealed class TimerState : INotifyPropertyChanged
{
    private TimerStatus _status = TimerStatus.Idle;
    private double _elapsed;    // seconds elapsed since start
    private bool _alertActive;
    private bool _isPaused;

    private readonly AppSettings _settings;

    /// <summary>Configuration this state is derived from.</summary>
    public TimerConfig Config { get; }

    public TimerState(TimerConfig config, AppSettings settings)
    {
        Config    = config;
        _settings = settings;

        Config.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(DisplayTime));
            OnPropertyChanged(nameof(DisplayColor));
            OnPropertyChanged(nameof(DisplayLabel));
        };

        _settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppSettings.ShowKeyLabel))
                OnPropertyChanged(nameof(DisplayLabel));
            if (e.PropertyName == nameof(AppSettings.ShowDecimal))
                OnPropertyChanged(nameof(DisplayTime));
        };
    }

    public TimerStatus Status
    {
        get => _status;
        set
        {
            if (Set(ref _status, value))
            {
                OnPropertyChanged(nameof(DisplayTime));
                OnPropertyChanged(nameof(DisplayColor));
            }
        }
    }

    /// <summary>Seconds accumulated since the timer was started.</summary>
    public double Elapsed
    {
        get => _elapsed;
        set
        {
            if (Set(ref _elapsed, value))
                OnPropertyChanged(nameof(DisplayTime));
        }
    }

    /// <summary>True while the alert visual is active (flashing / colour change).</summary>
    public bool AlertActive
    {
        get => _alertActive;
        set
        {
            if (Set(ref _alertActive, value))
                OnPropertyChanged(nameof(DisplayColor));
        }
    }

    /// <summary>Set by <see cref="TimerEngine"/> when the user activates the global pause.</summary>
    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (Set(ref _isPaused, value))
            {
                OnPropertyChanged(nameof(DisplayTime));
                OnPropertyChanged(nameof(DisplayColor));
            }
        }
    }

    // ── Computed display properties ───────────────────────────────────────────

    public string DisplayLabel => _settings.ShowKeyLabel
        ? $"[{Config.Key.ToUpper()}]"
        : Config.Label;

    public string DisplayTime
    {
        get
        {
            if (_isPaused) return "Paused";

            double seconds = Config.Direction == CountDirection.Down
                ? Math.Max(0, Config.DurationSeconds - _elapsed)
                : _elapsed;

            var fmt = _settings.ShowDecimal ? "F1" : "F0";
            return seconds.ToString(fmt, System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    public string DisplayColor
    {
        get
        {
            if (_isPaused)  return _settings.PauseColor;
            if (_alertActive) return Config.AlertColor;
            return Config.NormalColor;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Resets elapsed time and status to idle.</summary>
    public void Reset()
    {
        Elapsed = 0;
        AlertActive = false;
        Status = TimerStatus.Idle;
    }

    // ── INotifyPropertyChanged ────────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
