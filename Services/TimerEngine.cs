using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using KeyTimers.Models;

namespace KeyTimers.Services;

/// <summary>
/// Manages the runtime state for all configured timers.
/// Responds to key-down events from <see cref="KeyboardHookService"/> and advances
/// each active timer on a 100 ms <see cref="DispatcherTimer"/>.
/// </summary>
public sealed class TimerEngine : IDisposable
{
    private readonly SoundService _sound;
    private readonly DispatcherTimer _ticker;
    private DateTime _lastTick;
    private AppSettings? _settings;
    private bool _isPaused;

    /// <summary>Live timer states; one entry per enabled <see cref="TimerConfig"/>.</summary>
    public ObservableCollection<TimerState> States { get; } = [];

    /// <summary>When true, all timers are frozen and blocking / auto-trigger are suppressed.</summary>
    public bool IsPaused
    {
        get => _isPaused;
        private set
        {
            if (_isPaused == value) return;
            _isPaused = value;
            foreach (var s in States) s.IsPaused = value;
        }
    }

    // ── Construction ──────────────────────────────────────────────────────────

    public TimerEngine(SoundService sound)
    {
        _sound = sound;

        _ticker = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _ticker.Tick += OnTick;
        _ticker.Start();
        _lastTick = DateTime.UtcNow;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Rebuilds the state list from a new set of configurations.</summary>
    public void ApplyConfigs(IEnumerable<TimerConfig> configs, AppSettings settings)
    {
        _settings = settings;
        IsPaused  = false;
        States.Clear();
        foreach (var cfg in configs.Where(c => c.Enabled))
            States.Add(new TimerState(cfg, settings));
    }

    /// <summary>
    /// Returns <c>true</c> when the key mapped to <paramref name="vkCode"/> should be
    /// consumed (not forwarded to other apps). Called by <see cref="KeyboardHookService"/>
    /// before <see cref="HandleKeyDown"/>, so state has not yet changed.
    /// </summary>
    public bool ShouldBlockKey(uint vkCode)
    {
        var keyName = VkToString(vkCode);

        // always consume the pause key so the game never sees it
        if (_settings is not null &&
            !string.IsNullOrEmpty(_settings.PauseKey) &&
            string.Equals(_settings.PauseKey, keyName, StringComparison.OrdinalIgnoreCase))
            return true;

        if (IsPaused) return false;

        foreach (var state in States)
        {
            if (!string.Equals(state.Config.Key, keyName, StringComparison.OrdinalIgnoreCase))
                continue;

            // only block while the timer is actively running with Block behaviour
            if (state.Config.ClickBehaviour == ClickBehaviour.Block &&
                state.Status == TimerStatus.Running)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Called by <see cref="KeyboardHookService"/> with a virtual-key code.
    /// Finds the matching timer and applies the configured behaviour.
    /// </summary>
    public void HandleKeyDown(uint vkCode)
    {
        var keyName = VkToString(vkCode);

        // toggle global pause
        if (_settings is not null &&
            !string.IsNullOrEmpty(_settings.PauseKey) &&
            string.Equals(_settings.PauseKey, keyName, StringComparison.OrdinalIgnoreCase))
        {
            IsPaused = !IsPaused;
            return;
        }

        if (IsPaused) return;

        foreach (var state in States)
        {
            if (!string.Equals(state.Config.Key, keyName, StringComparison.OrdinalIgnoreCase))
                continue;

            switch (state.Status)
            {
                case TimerStatus.Idle:
                    // all behaviours start the timer on first press
                    StartTimer(state);
                    break;

                case TimerStatus.Running:
                    if (state.Config.ClickBehaviour == ClickBehaviour.Restart)
                        StartTimer(state);
                    // Block:             key already consumed in hook; do nothing here
                    // RestartOnComplete: wait for completion
                    // Continue:          ignore entirely
                    break;

                case TimerStatus.Completed:
                    // Continue is true "fire and forget" — never auto-restarts
                    if (state.Config.ClickBehaviour != ClickBehaviour.Continue)
                        StartTimer(state);
                    break;
            }
        }
    }

    // ── Tick ──────────────────────────────────────────────────────────────────

    private void OnTick(object? sender, EventArgs e)
    {
        var now   = DateTime.UtcNow;
        var delta = (now - _lastTick).TotalSeconds;
        _lastTick = now;

        if (IsPaused) return;

        foreach (var state in States)
        {
            if (state.Status != TimerStatus.Running) continue;

            state.Elapsed += delta;

            // check completion
            if (state.Elapsed >= state.Config.DurationSeconds && !state.AlertActive)
                TriggerAlert(state);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void StartTimer(TimerState state)
    {
        state.AlertActive = false;
        state.Elapsed     = 0;
        state.Status      = TimerStatus.Running;
    }

    private void TriggerAlert(TimerState state)
    {
        state.Status      = TimerStatus.Completed;
        state.AlertActive = true;

        var alert = state.Config.AlertType;

        if (alert.HasFlag(AlertType.Sound))
            _sound.Play(state.Config.SoundFilePath);

        // Visual alert auto-clears after 3 s to avoid permanent red display
        if (alert.HasFlag(AlertType.Visual))
        {
            Task.Delay(3000).ContinueWith(_ =>
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    state.AlertActive = false);
            });
        }

        // send the bound key to the active window and restart the timer
        if (state.Config.AutoResendOnComplete)
        {
            var minMs = state.Config.AutoTriggerMinDelay * 1000;
            var maxMs = Math.Max(minMs, state.Config.AutoTriggerMaxDelay * 1000);
            var delayMs = (int)(minMs + Random.Shared.NextDouble() * (maxMs - minMs));

            if (delayMs <= 0)
            {
                SendKeyPress(state.Config.Key);
                StartTimer(state);
            }
            else
            {
                Task.Delay(delayMs).ContinueWith(_ =>
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        // only fire if the user hasn't manually restarted in the meantime
                        if (state.Status == TimerStatus.Completed && !IsPaused)
                        {
                            SendKeyPress(state.Config.Key);
                            StartTimer(state);
                        }
                    }));
            }
        }
    }

    /// <summary>Maps a Win32 virtual-key code to a displayable key name.</summary>
    private static string VkToString(uint vk) => vk switch
    {
        // Letters A-Z
        >= 0x41 and <= 0x5A => ((char)vk).ToString(),

        // Digits 0-9 (top row)
        >= 0x30 and <= 0x39 => ((char)vk).ToString(),

        // Numpad 0-9
        >= 0x60 and <= 0x69 => $"NUM{vk - 0x60}",

        // Function keys F1-F12
        >= 0x70 and <= 0x7B => $"F{vk - 0x6F}",

        0x13 => "PAUSE",
        0x20 => "SPACE",
        0x08 => "BACK",
        0x09 => "TAB",
        0x0D => "ENTER",
        0x1B => "ESC",
        0x2E => "DELETE",
        0x2D => "INSERT",
        0x24 => "HOME",
        0x23 => "END",
        0x21 => "PGUP",
        0x22 => "PGDN",
        0x25 => "LEFT",
        0x26 => "UP",
        0x27 => "RIGHT",
        0x28 => "DOWN",
        0xBA => ";",
        0xBB => "=",
        0xBC => ",",
        0xBD => "-",
        0xBE => ".",
        0xBF => "/",
        0xC0 => "`",
        0xDB => "[",
        0xDC => "\\",
        0xDD => "]",
        0xDE => "'",
        _    => $"VK{vk:X2}"
    };

    // ── SendInput (auto-resend) ───────────────────────────────────────────────

    private const int  INPUT_KEYBOARD   = 1;
    private const uint KEYEVENTF_KEYUP  = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint   dwFlags;
        public uint   time;
        public IntPtr dwExtraInfo;
    }

    // On x64: sizeof(INPUT) = 40. type(4) + padding(4) + union(32).
    // The union size is driven by MOUSEINPUT (the largest member at 32 bytes).
    // Size must be set explicitly so cbSize passed to SendInput is correct.
    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct INPUT
    {
        [FieldOffset(0)] public int        type;
        [FieldOffset(8)] public KEYBDINPUT ki;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    // reverse map built once from the same table used by VkToString
    private static readonly Dictionary<string, ushort> _keyToVk = BuildKeyMap();

    private static Dictionary<string, ushort> BuildKeyMap()
    {
        var m = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase);

        for (uint vk = 0x41; vk <= 0x5A; vk++) m[((char)vk).ToString()] = (ushort)vk;
        for (uint vk = 0x30; vk <= 0x39; vk++) m[((char)vk).ToString()] = (ushort)vk;
        for (uint vk = 0x60; vk <= 0x69; vk++) m[$"NUM{vk - 0x60}"]     = (ushort)vk;
        for (uint vk = 0x70; vk <= 0x7B; vk++) m[$"F{vk - 0x6F}"]       = (ushort)vk;

        m["PAUSE"]  = 0x13;
        m["SPACE"]  = 0x20; m["BACK"]   = 0x08; m["TAB"]  = 0x09;
        m["ENTER"]  = 0x0D; m["ESC"]    = 0x1B; m["DELETE"] = 0x2E;
        m["INSERT"] = 0x2D; m["HOME"]   = 0x24; m["END"]  = 0x23;
        m["PGUP"]   = 0x21; m["PGDN"]   = 0x22;
        m["LEFT"]   = 0x25; m["UP"]     = 0x26;
        m["RIGHT"]  = 0x27; m["DOWN"]   = 0x28;
        m[";"] = 0xBA; m["="] = 0xBB; m[","] = 0xBC; m["-"] = 0xBD;
        m["."] = 0xBE; m["/"] = 0xBF; m["`"] = 0xC0;
        m["["] = 0xDB; m["\\"] = 0xDC; m["]"] = 0xDD; m["'"] = 0xDE;

        return m;
    }

    /// <summary>Sends a virtual key-down + key-up pair via SendInput.</summary>
    private static void SendKeyPress(string key)
    {
        if (!_keyToVk.TryGetValue(key, out var vk)) return;

        var inputs = new INPUT[2];

        // key-down
        inputs[0] = new INPUT { type = INPUT_KEYBOARD,
            ki = new KEYBDINPUT { wVk = vk } };

        // key-up
        inputs[1] = new INPUT { type = INPUT_KEYBOARD,
            ki = new KEYBDINPUT { wVk = vk, dwFlags = KEYEVENTF_KEYUP } };

        SendInput(2, inputs, Marshal.SizeOf<INPUT>());
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        _ticker.Stop();
        _sound.Dispose();
    }
}
