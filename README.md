# KeyTimers

Keyboard-triggered countdown timers with a transparent overlay — bind any key, set a duration, get alerted when time's up.

## Features

- **Bind any key** — assign a keyboard key (E, F1, 1, …) to each timer
- **Transparent overlay** — always-on-top panel shows all timers at a glance; drag it anywhere on screen
- **Count down or up** — configure each timer independently
- **Visual & sound alerts** — change colour and/or play a sound when the timer fires
- **Auto-trigger** — optionally re-press the bound key automatically when the timer completes, with configurable random-delay jitter
- **Global pause** — assign a key to pause and resume all timers at once
- **Per-timer customisation** — label, colours, font, border, opacity
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

All settings are saved automatically to `%APPDATA%\KeyTimers\settings.json`. Open **Settings** from the tray icon to configure:

### Global overlay settings

| Setting | Description |
|---------|-------------|
| Background colour | Hex colour of the overlay panel |
| Opacity | Panel transparency (0.1–1.0) |
| Font family | Font used for all labels and timers |
| Font size | Text size in device-independent pixels |
| Show key label | Show `[E]`-style key name instead of the timer label |
| Pause key | Key that globally pauses/resumes all timers (e.g. `F9`). Leave empty to disable. |
| Pause colour | Overlay text colour while paused |
| Timer border px | Border thickness around each timer row (0 = no border) |
| Timer border colour | Colour of the per-timer border |
| Show decimal | Show one decimal digit (e.g. `4.2` vs `4`) |

### Per-timer settings

| Setting | Description |
|---------|-------------|
| Key | The key that triggers this timer |
| Label | Name shown on the overlay |
| Seconds | Timer duration in seconds |
| Enabled | Toggle the timer on/off without deleting it |
| Direction | `Down` (counts to zero) or `Up` (counts from zero) |
| On re-press | What happens when the key is pressed while the timer is running: `Restart` restarts immediately; `Block` suppresses the key so it isn't sent to other apps; `Continue` ignores the key entirely |
| Alert type | `Visual` (colour change), `Sound` (audio file or beep), or `Both` |
| Normal colour | Text colour while the timer is running |
| Alert colour | Text colour when the alert fires |
| Sound file | Path to a `.wav` or `.mp3` file. Leave empty to use the built-in beep. |
| Auto-trigger | Re-press the key automatically when the timer completes and restart it |
| Trigger min/max delay | Random jitter range (seconds) added before the auto-trigger fires |

## Building from Source

```powershell
git clone https://github.com/ranarn/KeyTimers.git
cd KeyTimers
dotnet build
```

To produce a self-contained single-file release:

```powershell
dotnet publish KeyTimers.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

## License

MIT — see [LICENSE](LICENSE).
