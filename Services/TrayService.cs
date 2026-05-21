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
    private readonly IntPtr _hicon;   // raw HICON from GetHicon(); freed in Dispose

    private OverlayWindow? _overlay;
    private SettingsWindow? _settingsWindow;

    public TrayService(AppSettings settings, TimerEngine engine, SettingsService settingsService)
    {
        _settings        = settings;
        _engine          = engine;
        _settingsService = settingsService;

        // Build the icon, track the raw HICON so we can free it later
        (var icon, _hicon) = BuildIcon();

        _trayIcon = new NotifyIcon
        {
            Text    = "KeyTimers",
            Icon    = icon,
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
        // Bring the existing window to focus instead of opening a second one
        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings, _engine, _settingsService);
        _settingsWindow.Owner = _overlay;
        _settingsWindow.Show();
    }

    // ── Icon ──────────────────────────────────────────────────────────────────

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    /// <summary>
    /// Builds a simple programmatic tray icon and returns both the managed <see cref="Icon"/>
    /// and the raw HICON handle (caller must call <see cref="DestroyIcon"/> when done).
    /// </summary>
    private static (Icon icon, IntPtr hicon) BuildIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using var g   = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(30, 30, 46));
        using var brush = new SolidBrush(Color.FromArgb(137, 180, 250));
        using var font  = new Font("Arial", 6f, System.Drawing.FontStyle.Bold, GraphicsUnit.Point);
        g.DrawString("KT", font, brush, 0f, 3f);

        var hicon = bmp.GetHicon();
        // Clone the managed Icon from the HICON so it owns a copy of the image data
        var icon = (Icon)Icon.FromHandle(hicon).Clone();
        return (icon, hicon);
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();   // also disposes the cloned icon set on it
        DestroyIcon(_hicon);   // free the original HICON returned by GetHicon()
    }
}
