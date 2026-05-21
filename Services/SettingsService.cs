using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using KeyTimers.Models;

namespace KeyTimers.Services;

/// <summary>
/// Persists and loads <see cref="AppSettings"/> as JSON in the user's AppData folder.
/// </summary>
public sealed class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KeyTimers",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Loads settings from disk, or returns defaults if no file exists.</summary>
    public AppSettings Load()
    {
        if (!File.Exists(SettingsPath))
            return CreateDefaults();

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var dto  = JsonSerializer.Deserialize<SettingsDto>(json, JsonOptions);
            return dto is null ? CreateDefaults() : FromDto(dto);
        }
        catch
        {
            return CreateDefaults();
        }
    }

    /// <summary>Persists the current settings to disk.</summary>
    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        var dto  = ToDto(settings);
        var json = JsonSerializer.Serialize(dto, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }

    // ── Serialisation DTOs (avoids serialising ObservableCollection directly) ─

    private sealed record SettingsDto(
        double OverlayLeft,
        double OverlayTop,
        double OverlayOpacity,
        string BackgroundColor,
        string FontFamily,
        double FontSize,
        bool   ShowKeyLabel,
        List<TimerConfigDto> Timers,
        string PauseKey             = "",
        string PauseColor           = "#888888",
        double TimerBorderThickness = 0,
        string TimerBorderColor     = "#45475A",
        bool   ShowDecimal          = true);

    private sealed record TimerConfigDto(
        string        Id,
        string        Key,
        string        Label,
        double        DurationSeconds,
        CountDirection Direction,
        ClickBehaviour ClickBehaviour,
        AlertType     AlertType,
        string        NormalColor,
        string        AlertColor,
        string        SoundFilePath,
        bool          Enabled,
        bool          AutoResendOnComplete = false,
        double        AutoTriggerMinDelay  = 0,
        double        AutoTriggerMaxDelay  = 0);

    private static SettingsDto ToDto(AppSettings s) => new(
        s.OverlayLeft,
        s.OverlayTop,
        s.OverlayOpacity,
        s.BackgroundColor,
        s.FontFamily,
        s.FontSize,
        s.ShowKeyLabel,
        s.Timers.Select(t => new TimerConfigDto(
            t.Id, t.Key, t.Label, t.DurationSeconds,
            t.Direction, t.ClickBehaviour, t.AlertType,
            t.NormalColor, t.AlertColor, t.SoundFilePath, t.Enabled,
            t.AutoResendOnComplete, t.AutoTriggerMinDelay, t.AutoTriggerMaxDelay)).ToList(),
        s.PauseKey,
        s.PauseColor,
        s.TimerBorderThickness,
        s.TimerBorderColor,
        s.ShowDecimal);

    private static AppSettings FromDto(SettingsDto dto)
    {
        var s = new AppSettings
        {
            OverlayLeft          = dto.OverlayLeft,
            OverlayTop           = dto.OverlayTop,
            OverlayOpacity       = dto.OverlayOpacity,
            BackgroundColor      = dto.BackgroundColor,
            FontFamily           = dto.FontFamily,
            FontSize             = dto.FontSize,
            ShowKeyLabel         = dto.ShowKeyLabel,
            PauseKey             = dto.PauseKey,
            PauseColor           = dto.PauseColor,
            TimerBorderThickness = dto.TimerBorderThickness,
            TimerBorderColor     = dto.TimerBorderColor,
            ShowDecimal          = dto.ShowDecimal,
        };
        foreach (var t in dto.Timers)
            s.Timers.Add(new TimerConfig
            {
                Id             = t.Id,
                Key            = t.Key,
                Label          = t.Label,
                DurationSeconds = t.DurationSeconds,
                Direction      = t.Direction,
                ClickBehaviour = t.ClickBehaviour,
                AlertType      = t.AlertType,
                NormalColor    = t.NormalColor,
                AlertColor     = t.AlertColor,
                SoundFilePath  = t.SoundFilePath,
                Enabled              = t.Enabled,
                AutoResendOnComplete = t.AutoResendOnComplete,
                AutoTriggerMinDelay  = t.AutoTriggerMinDelay,
                AutoTriggerMaxDelay  = t.AutoTriggerMaxDelay,
            });
        return s;
    }

    private static AppSettings CreateDefaults()
    {
        var s = new AppSettings();
        s.Timers.Add(new TimerConfig
        {
            Key   = "E",
            Label = "Ability",
            DurationSeconds = 30,
            Direction = CountDirection.Down,
            AlertType = AlertType.Visual,
        });
        return s;
    }
}
