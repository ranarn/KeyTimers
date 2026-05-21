using System.Drawing;
using System.Windows.Forms;
using KeyTimers.Models;
using KeyTimers.Views;

namespace KeyTimers.Services;

/// <summary>
/// Manages the system-tray icon and its context menu.
/// Uses WinForms <see cref="NotifyIcon"/> — the most reliable tray API on Windows.
/// </summary>
public sealed class TrayService : IDisposable
{
    private readonly AppSettings _settings;
    private readonly TimerEngine _engine;
    private readonly SettingsService _settingsService;
    private readonly NotifyIcon _trayIcon;

    private OverlayWindow? _overlay;

    public TrayService(AppSettings settings, TimerEngine engine, SettingsService settingsService)
    {
        _settings        = settings;
        _engine          = engine;
        _settingsService = settingsService;

        _trayIcon = new NotifyIcon
        {
            Text    = "KeyTimers",
            Icon    = BuildIcon(),
            Visible = true,
        };

        _trayIcon.ContextMenuStrip = BuildMenu();
        _trayIcon.DoubleClick     += (_, _) => OpenSettings();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Creates and shows the overlay window (idempotent).</summary>
    public void ShowOverlay()
    {
        if (_overlay is { IsLoaded: true })
        {
            _overlay.Show();
            _overlay.Activate();
            return;
        }

        _overlay = new OverlayWindow(_settings, _engine);
        _overlay.Show();
    }

    // ── Menu ──────────────────────────────────────────────────────────────────

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        var showOverlay = new ToolStripMenuItem("Show Overlay");
        showOverlay.Click += (_, _) => ShowOverlay();

        var hideOverlay = new ToolStripMenuItem("Hide Overlay");
        hideOverlay.Click += (_, _) => _overlay?.Hide();

        var settings = new ToolStripMenuItem("Settings…");
        settings.Click += (_, _) => OpenSettings();

        var separator = new ToolStripSeparator();

        var exit = new ToolStripMenuItem("Exit");
        exit.Click += (_, _) =>
        {
            _settingsService.Save(_settings);
            System.Windows.Application.Current.Shutdown();
        };

        menu.Items.AddRange([showOverlay, hideOverlay, settings, separator, exit]);
        return menu;
    }

    private void OpenSettings()
    {
        var win = new SettingsWindow(_settings, _engine, _settingsService);
        win.Owner = _overlay;
        win.Show();
    }

    // ── Icon ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a simple programmatic tray icon — avoids a file dependency at startup.
    /// A real .ico file is referenced in the csproj for the taskbar; this is the tray copy.
    /// </summary>
    private static Icon BuildIcon()
    {
        // draw a simple "KT" icon at runtime
        using var bmp = new Bitmap(16, 16);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(30, 30, 46));
        using var brush = new SolidBrush(Color.FromArgb(137, 180, 250));
        using var font  = new Font("Arial", 6f, System.Drawing.FontStyle.Bold, GraphicsUnit.Point);
        g.DrawString("KT", font, brush, 0f, 3f);
        return Icon.FromHandle(bmp.GetHicon());
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
    }
}
