using System.Text.Json;
using FNFBot.Interop;

namespace FNFBot.Services;

public static class UserSettings
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FNFBot");

    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static BotSettings Load(double defaultHitBiasMs)
    {
        BotSettings defaults = BotSettings.CreateDefault(defaultHitBiasMs);

        try
        {
            if (!File.Exists(SettingsPath))
            {
                Save(defaults);
                return defaults;
            }

            using FileStream stream = File.OpenRead(SettingsPath);
            SettingsData? settings = JsonSerializer.Deserialize<SettingsData>(stream, SerializerOptions);
            if (settings is null)
            {
                return defaults;
            }

            return new BotSettings(
                settings.HitBiasMs.HasValue && double.IsFinite(settings.HitBiasMs.Value)
                    ? settings.HitBiasMs.Value
                    : defaultHitBiasMs,
                ParseFiniteSetting(settings.TapDurationMs, defaults.TapDurationMs),
                ParseFiniteSetting(settings.HoldReleaseGuardMs, defaults.HoldReleaseGuardMs),
                ParseNavigationDelay(settings.MenuNavigationDelayMs, defaults.MenuNavigationDelayMs),
                ParseHotKey(settings.Hotkeys?.Start, defaults.StartHotKey),
                ParseHotKey(settings.Hotkeys?.Stop, defaults.StopHotKey),
                ParseHotKey(
                    settings.Hotkeys?.DecreaseHitBias ?? settings.Hotkeys?.DecreaseDelay,
                    defaults.DecreaseHitBiasHotKey),
                ParseHotKey(
                    settings.Hotkeys?.IncreaseHitBias ?? settings.Hotkeys?.IncreaseDelay,
                    defaults.IncreaseHitBiasHotKey),
                new Dictionary<int, IReadOnlyList<VirtualKey>>
                {
                    [0] = ParseInputKeys(settings.Inputs?.Left, defaults.InputKeys[0]),
                    [1] = ParseInputKeys(settings.Inputs?.Down, defaults.InputKeys[1]),
                    [2] = ParseInputKeys(settings.Inputs?.Up, defaults.InputKeys[2]),
                    [3] = ParseInputKeys(settings.Inputs?.Right, defaults.InputKeys[3])
                },
                ParseColorScheme(settings.ColorScheme));
        }
        catch (IOException exception)
        {
            Console.WriteLine($"Could not load settings: {exception.Message}");
            return defaults;
        }
        catch (UnauthorizedAccessException exception)
        {
            Console.WriteLine($"Could not load settings: {exception.Message}");
            return defaults;
        }
        catch (JsonException exception)
        {
            Console.WriteLine($"Could not parse settings: {exception.Message}");
            return defaults;
        }
    }

    public static void Save(BotSettings settings)
    {
        if (!double.IsFinite(settings.HitBiasMs) ||
            !double.IsFinite(settings.TapDurationMs) ||
            !double.IsFinite(settings.HoldReleaseGuardMs))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            using FileStream stream = File.Create(SettingsPath);
            JsonSerializer.Serialize(
                stream,
                new SettingsData
                {
                    HitBiasMs = settings.HitBiasMs,
                    TapDurationMs = settings.TapDurationMs,
                    HoldReleaseGuardMs = settings.HoldReleaseGuardMs,
                    MenuNavigationDelayMs = settings.MenuNavigationDelayMs,
                    Hotkeys = new HotkeyData
                    {
                        Start = KeyNames.ToSettingsName(settings.StartHotKey),
                        Stop = KeyNames.ToSettingsName(settings.StopHotKey),
                        DecreaseHitBias = KeyNames.ToSettingsName(settings.DecreaseHitBiasHotKey),
                        IncreaseHitBias = KeyNames.ToSettingsName(settings.IncreaseHitBiasHotKey)
                    },
                    Inputs = new InputData
                    {
                        Left = FormatInputKeys(settings.InputKeys[0]),
                        Down = FormatInputKeys(settings.InputKeys[1]),
                        Up = FormatInputKeys(settings.InputKeys[2]),
                        Right = FormatInputKeys(settings.InputKeys[3])
                    },
                    ColorScheme = settings.ColorScheme.ToString()
                },
                SerializerOptions);
        }
        catch (IOException exception)
        {
            Console.WriteLine($"Could not save settings: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            Console.WriteLine($"Could not save settings: {exception.Message}");
        }
    }

    private static Keys ParseHotKey(string? value, Keys fallback)
    {
        return TryParseKey(value, out Keys key) ? key : fallback;
    }

    private static double ParseFiniteSetting(double? value, double fallback)
    {
        return value.HasValue && double.IsFinite(value.Value)
            ? value.Value
            : fallback;
    }

    private static int ParseNavigationDelay(int? value, int fallback)
    {
        return value.HasValue
            ? Math.Clamp(value.Value, 50, 1000)
            : fallback;
    }

    private static IReadOnlyList<VirtualKey> ParseInputKeys(
        IReadOnlyList<string>? values,
        IReadOnlyList<VirtualKey> fallback)
    {
        if (values is null || values.Count == 0)
        {
            return fallback;
        }

        List<VirtualKey> keys = [];
        foreach (string value in values)
        {
            if (TryParseKey(value, out Keys key))
            {
                VirtualKey virtualKey = (VirtualKey)(ushort)key;
                if (!keys.Contains(virtualKey))
                {
                    keys.Add(virtualKey);
                }
            }
        }

        return keys.Count > 0 ? keys : fallback;
    }

    private static bool TryParseKey(string? value, out Keys key)
    {
        if (!KeyNames.TryParse(value, out key))
        {
            Console.WriteLine($"Unknown key name in settings: {value}");
            return false;
        }

        return true;
    }

    private static string[] FormatInputKeys(IReadOnlyList<VirtualKey> keys)
    {
        return keys
            .Select(static key => KeyNames.ToSettingsName((Keys)(ushort)key))
            .ToArray();
    }

    private static ColorScheme ParseColorScheme(string? value)
    {
        return Enum.TryParse(value, true, out ColorScheme colorScheme)
            ? colorScheme
            : ColorScheme.System;
    }

    private sealed class SettingsData
    {
        public double? HitBiasMs { get; init; }

        public double? TapDurationMs { get; init; }

        public double? HoldReleaseGuardMs { get; init; }

        public int? MenuNavigationDelayMs { get; init; }

        public HotkeyData? Hotkeys { get; init; }

        public InputData? Inputs { get; init; }

        public string? ColorScheme { get; init; }
    }

    private sealed class HotkeyData
    {
        public string? Start { get; init; }

        public string? Stop { get; init; }

        public string? DecreaseHitBias { get; init; }

        public string? IncreaseHitBias { get; init; }

        // Retained so existing settings files migrate without losing custom keys.
        public string? DecreaseDelay { get; init; }

        public string? IncreaseDelay { get; init; }
    }

    private sealed class InputData
    {
        public IReadOnlyList<string>? Left { get; init; }

        public IReadOnlyList<string>? Down { get; init; }

        public IReadOnlyList<string>? Up { get; init; }

        public IReadOnlyList<string>? Right { get; init; }
    }
}

public sealed record BotSettings(
    double HitBiasMs,
    double TapDurationMs,
    double HoldReleaseGuardMs,
    int MenuNavigationDelayMs,
    Keys StartHotKey,
    Keys StopHotKey,
    Keys DecreaseHitBiasHotKey,
    Keys IncreaseHitBiasHotKey,
    IReadOnlyDictionary<int, IReadOnlyList<VirtualKey>> InputKeys,
    ColorScheme ColorScheme)
{
    public static BotSettings CreateDefault(double hitBiasMs)
    {
        return new BotSettings(
            hitBiasMs,
            PlaybackTimeline.TapDurationMs,
            PlaybackTimeline.HoldReleaseGuardMs,
            FNFBot.Core.RhythmBot.DefaultMenuNavigationDelayMs,
            Keys.F1,
            Keys.F4,
            Keys.F2,
            Keys.F3,
            new Dictionary<int, IReadOnlyList<VirtualKey>>
            {
                [0] = [VirtualKey.A, VirtualKey.LeftArrow],
                [1] = [VirtualKey.S, VirtualKey.DownArrow],
                [2] = [VirtualKey.W, VirtualKey.UpArrow],
                [3] = [VirtualKey.D, VirtualKey.RightArrow]
            },
            ColorScheme.System);
    }
}

public enum ColorScheme
{
    System,
    Light,
    Dark
}
