using KeyTimers.Services;
using KeyTimers.Views;
using System.Windows;

namespace KeyTimers;

/// <summary>
/// Application entry point.
/// Wires together all services and starts the overlay + tray icon.
/// No main window — the app lives entirely in the system tray.
/// </summary>
public partial class App : System.Windows.Application
{
    private SettingsService?   _settingsService;
    private SoundService?      _soundService;
    private TimerEngine?       _timerEngine;
    private KeyboardHookService? _keyboardHook;
    private TrayService?       _trayService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // prevent shutdown when settings window closes
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // services
        _settingsService = new SettingsService();
        var settings     = _settingsService.Load();

        _soundService    = new SoundService();
        _timerEngine     = new TimerEngine(_soundService);
        _timerEngine.ApplyConfigs(settings.Timers, settings);

        // keyboard hook — must be installed on the UI thread (has a message loop)
        _keyboardHook = new KeyboardHookService();
        _keyboardHook.ShouldBlockKey = vk => _timerEngine.ShouldBlockKey(vk);
        _keyboardHook.KeyDown += vk => Dispatcher.Invoke(() => _timerEngine.HandleKeyDown(vk));
        _keyboardHook.Install();

        // tray + overlay
        _trayService = new TrayService(settings, _timerEngine, _settingsService);
        _trayService.ShowOverlay();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _keyboardHook?.Dispose();
        _timerEngine?.Dispose();
        _trayService?.Dispose();
        base.OnExit(e);
    }
}
