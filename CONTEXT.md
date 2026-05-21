# KeyTimers — Project Context

## What this is

KeyTimers is a Windows desktop utility (WPF + .NET 10, system tray app) that lets users bind keyboard keys to countdown/count-up timers. It targets game players who need to track ability cooldowns or timed events while playing, but it works for any context where key-triggered timers are useful.

## Architecture overview

```
App.xaml.cs          — entry point; wires all services; no main window
Models/
  ObservableBase     — shared INotifyPropertyChanged base class
  AppSettings        — global overlay settings (position, font, pause key, …)
  TimerConfig        — per-timer configuration (key binding, duration, colours, …)
  TimerState         — runtime state for one timer (elapsed, alert, paused)
Services/
  SettingsService    — JSON load/save via %APPDATA%\KeyTimers\settings.json
  TimerEngine        — tick loop (DispatcherTimer at 100 ms), key routing, auto-trigger
  KeyboardHookService— WH_KEYBOARD_LL global hook; blocks/passes keys to TimerEngine
  SoundService       — NAudio playback; falls back to Console.Beep
  TrayService        — NotifyIcon, context menu, overlay and settings window lifecycle
Views/
  OverlayWindow      — always-on-top transparent WPF window; drag to reposition
  SettingsWindow     — editor for AppSettings + TimerConfig list
  HexColorConverter  — hex string → SolidColorBrush (IValueConverter)
  DoubleToThicknessConverter — double → Thickness (IValueConverter)
  KeyCaptureTextBox  — read-only TextBox that captures key presses and maps them to VK names
```

## Key invariants

- **No main window.** `ShutdownMode = OnExplicitShutdown`. App lives entirely in the tray.
- **UI thread only for timer state mutations.** The tick loop runs on the WPF dispatcher thread. `KeyDown` events are marshalled back via `Dispatcher.Invoke`.
- **Settings flow:** live `AppSettings` object is the single source of truth. `SettingsWindow` works on a clone (`AppSettings.Clone()`); `CommitToLive()` + `SettingsService.Save()` + `TimerEngine.ApplyConfigs()` flush changes.
- **Key names are normalised strings** (e.g. `"E"`, `"F1"`, `"NUM0"`, `"SPACE"`). `KeyCaptureTextBox.WpfKeyToName` and `TimerEngine.VkToString` must produce the same strings for a given key — they share a symmetric mapping.
- **`AlertType` is a `[Flags]` enum** with values `None=0`, `Visual=1`, `Sound=2`, `Both=3`. The settings ComboBox exposes all four values individually; users pick `Both` for combined alerts.
- **Auto-trigger** fires via `SendInput` (Win32) to simulate the key press. Injected keys are marked with `LLKHF_INJECTED` and are skipped by the hook so they don't cause infinite loops.
- **`TimerState` is disposable.** Unsubscribes from `TimerConfig.PropertyChanged` and `AppSettings.PropertyChanged` on dispose. `TimerEngine.ApplyConfigs` disposes old states before clearing the collection.

## Non-obvious design decisions

- **`ClickBehaviour.Block`** consumes the key in the hook callback (`ShouldBlockKey` → `return 1`) *before* `HandleKeyDown` changes timer state. This ordering is intentional: the block decision must be made on the pre-change state.
- **`DispatcherTimer` at 100 ms** is used intentionally over `System.Timers.Timer` to keep all state mutations on the UI thread without manual marshalling.
- **`TrayService.BuildIcon()`** uses GDI to draw the "KT" icon at runtime. The raw HICON returned by `Bitmap.GetHicon()` is tracked and freed via `DestroyIcon` in `TrayService.Dispose()`. The `Icon.FromHandle(...).Clone()` pattern is used so the managed Icon owns a copy independent of the HICON lifetime.
- **`SettingsWindow` opens only one at a time.** `TrayService.OpenSettings()` activates the existing window rather than creating a second one.

## Settings persistence

`%APPDATA%\KeyTimers\settings.json` — written via `SettingsService`. Uses a DTO pattern (no direct serialisation of `ObservableCollection`). New fields should be added with default values in both the DTO record and the `FromDto` / `CreateDefaults` methods.

## Build

```powershell
dotnet build                         # debug build
dotnet build -c Release              # release build
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

CI (GitHub Actions) runs on every push to `main` and every PR. Releases are created automatically by `ranarn/release-pilot@v1` when a version tag is pushed.
