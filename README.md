# KeyTimers

Keyboard-triggered countdown timers with a transparent overlay — bind any key, set a duration, get alerted when time's up.

![KeyTimers overlay](docs/screenshot.png)

> Screenshot coming soon.

## Features

- **Bind any key** — assign a keyboard key (E, F1, 1, …) to each timer
- **Transparent overlay** — always-on-top panel shows all timers at a glance; drag it anywhere on screen
- **Count down or up** — configure each timer independently
- **Visual & sound alerts** — change color and/or play a sound when the timer fires
- **Auto-resend** — optionally re-press the bound key automatically when the timer completes, with configurable random delay jitter
- **Global pause** — assign a key to pause and resume all timers at once
- **Per-timer customization** — label, colors, font, border, opacity
- **System tray** — runs quietly in the background; right-click the tray icon to open settings
- **No installation** — self-contained single `.exe`, no .NET runtime required

## Download

Grab the latest release from the [Releases](https://github.com/ranarn/KeyTimers/releases) page and run `KeyTimers.exe` — no installer needed.

**Requirements:** Windows 10 or later (x64)

## Quick Start

1. Download and run `KeyTimers.exe`
2. Right-click the tray icon → **Settings**
3. Add a timer, pick a key (e.g. `E`), set a duration
4. Press that key in any application — the overlay counts down
5. Drag the overlay to wherever you want it on screen

## Configuration

All settings are saved automatically. Open **Settings** from the tray icon to configure:

| Setting | Description |
|---------|-------------|
| Key | The key that starts/resets this timer |
| Label | Name shown on the overlay |
| Duration | Timer length in seconds |
| Direction | Count down or count up |
| On click | Restart, pause, or stop when the key is pressed while running |
| Alert type | Visual (color change), sound, or both |
| Alert color | Hex color applied when the timer fires |
| Auto-resend | Re-press the key automatically on completion |
| Random delay | Add jitter (min–max seconds) to the auto-resend |

Global settings (overlay font, opacity, background color, pause key) are at the top of the Settings window.

## Building from Source

```powershell
git clone https://github.com/ranarn/KeyTimers.git
cd KeyTimers
dotnet publish KeyTimers.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

## License

MIT — see [LICENSE](LICENSE).
