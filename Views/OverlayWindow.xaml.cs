using System.Windows;
using System.Windows.Input;
using KeyTimers.Models;
using KeyTimers.Services;

namespace KeyTimers.Views;

/// <summary>
/// Transparent, always-on-top overlay window that displays all active timers.
/// No buttons — interaction is purely via the global keyboard hook.
/// </summary>
public sealed partial class OverlayWindow : Window
{
    public AppSettings Settings { get; }
    public IEnumerable<TimerState> TimerStates { get; }

    public OverlayWindow(AppSettings settings, TimerEngine engine)
    {
        InitializeComponent();
        Settings    = settings;
        TimerStates = engine.States;
        DataContext = this;
    }

    // ── Drag to move ──────────────────────────────────────────────────────────

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
        // persist new position immediately
        Settings.OverlayLeft = Left;
        Settings.OverlayTop  = Top;
    }
}
