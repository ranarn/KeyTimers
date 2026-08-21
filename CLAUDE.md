# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

KeyTimers is a Windows desktop utility (WPF + .NET 10, C# 13, system tray app) that binds keyboard keys to countdown/count-up timers — built for tracking game ability cooldowns or other timed events. There is no main window; the app lives entirely in the system tray and ships as a self-contained single `.exe` (Windows 10+ x64 only).

## Commands

```powershell
dotnet build                        # debug build
dotnet build -c Release             # release build
dotnet run                          # run the tray app
dotnet publish KeyTimers.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

No test project exists in this repo — verification is manual (run the app) or via `dotnet build` output. `TreatWarningsAsErrors` is enabled project-wide, so any warning (including deprecation warnings from dependencies) fails the build, not just `dotnet build`'s own diagnostics.

CI (`.github/workflows/pull-request.yml`, `main.yml`) restores and builds on `windows-latest` runners for every PR and push to `main`. `main.yml` additionally runs `ranarn/release-pilot@v1` and, on a release, publishes + zips + uploads the single-file build.

## Architecture

```
App.xaml.cs           entry point; manually wires all services (no DI container); no main window
Models/
  ObservableBase       shared INotifyPropertyChanged base (Set(ref field, value))
  AppSettings          global overlay settings (position, font, pause key, …) — Clone()-able
  TimerConfig          per-timer config (key binding, duration, colours, …) — Clone()-able
  TimerState           runtime state for one timer (elapsed, alert, paused) — IDisposable
Services/
  SettingsService      JSON load/save via %APPDATA%\KeyTimers\settings.json, DTO pattern
  TimerEngine          DispatcherTimer tick loop (100 ms), key routing, SendInput auto-trigger
  KeyboardHookService  WH_KEYBOARD_LL global hook; blocks/passes keys to TimerEngine
  SoundService         NAudio playback; falls back to Console.Beep
  TrayService          NotifyIcon, context menu, overlay/settings window lifecycle
Views/
  OverlayWindow        always-on-top transparent WPF window; drag to reposition
  SettingsWindow        editor for AppSettings + TimerConfig list; singleton, edits a clone
  HexColorConverter / DoubleToThicknessConverter   IValueConverters
  KeyCaptureTextBox    captures key presses, maps them to normalised VK name strings
```

### Key invariants

- No main window: `ShutdownMode = OnExplicitShutdown`; the app exits only via tray → Exit.
- All timer-state mutations happen on the WPF dispatcher thread; `KeyDown` events from the low-level hook are marshalled back via `Dispatcher.Invoke`.
- Settings flow: the live `AppSettings` instance is the single source of truth. `SettingsWindow` edits a clone (`AppSettings.Clone()`); `CommitToLive()` + `SettingsService.Save()` + `TimerEngine.ApplyConfigs()` flush changes back.
- Adding a new `AppSettings`/`TimerConfig` property requires updates in four places: the model class (field + property + `Clone()`), `CommitToLive()`, and the `SettingsService` DTO record's `ToDto()`/`FromDto()`.
- Key names are normalised uppercase strings (`"E"`, `"F1"`, `"NUM0"`, `"SPACE"`, …). `KeyCaptureTextBox.WpfKeyToName` and `TimerEngine.VkToString`/`BuildKeyMap` must stay symmetric for a given key.
- `AlertType` is a `[Flags]` enum (`None=0, Visual=1, Sound=2, Both=3`); the settings ComboBox exposes all four values individually so users can pick `Both`.
- Auto-trigger simulates key presses via Win32 `SendInput`; injected keys carry `LLKHF_INJECTED` and are skipped by the hook to avoid feedback loops.
- `TimerState` is `IDisposable` — unsubscribes from `TimerConfig`/`AppSettings` `PropertyChanged`. `TimerEngine.ApplyConfigs` disposes old states before replacing the collection.
- `ClickBehaviour.Block` consumes the key in the hook callback *before* `HandleKeyDown` changes timer state — the block decision is deliberately made on pre-change state.
- `TrayService.BuildIcon()` draws the tray icon via GDI at runtime; the raw HICON from `Bitmap.GetHicon()` is freed via `DestroyIcon` in `Dispose()`.
- `SettingsWindow` is a singleton — `TrayService.OpenSettings()` activates the existing window instead of creating a second one.
- All P/Invoke (`SetWindowsHookEx`, `SendInput`, `DestroyIcon`, …) uses safe managed interop; `AllowUnsafeBlocks` is not set.
- `TreatWarningsAsErrors` is project-wide — a dependency's API deprecation becomes a build failure, not just a warning (e.g. NAudio 3.x renamed `WaveOutEvent` to `WaveOut` and marked the old name `[Obsolete]`).

## Agent skills

### Issue tracker

Issues live in GitHub Issues for this repo. See `.claude/project/issue-tracker.md`.

### Triage labels

Default label vocabulary (needs-triage, needs-info, ready-for-agent, ready-for-human, wontfix). See `.claude/project/triage-labels.md`.

### Domain docs

Single-context layout — one `CONTEXT.md` + `docs/adr/` at the repo root. See `.claude/project/domain.md`.
