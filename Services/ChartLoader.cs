using System.Globalization;
using System.Text.Json;
using FnfBot.Models;

namespace FnfBot.Services;

public static class ChartLoader
{
    public static readonly IReadOnlyDictionary<int, string> DirectionMap =
        new Dictionary<int, string>
        {
            [0] = "left",
            [1] = "down",
            [2] = "up",
            [3] = "right"
        };

    public static readonly IReadOnlyList<string> DifficultyOrder =
    [
        "easy", "normal", "hard", "erect", "nightmare",
        "bf-easy", "bf-normal", "bf-hard",
        "pico-easy", "pico-normal", "pico-hard"
    ];

    public static Chart? LoadChart(string chartPath, string difficulty = "normal")
    {
        try
        {
            using FileStream stream = File.OpenRead(chartPath);
            using JsonDocument document = JsonDocument.Parse(stream);
            JsonElement root = document.RootElement;

            string noteDifficulty = GetNoteDifficulty(difficulty);
            if (!root.TryGetProperty("notes", out JsonElement notesByDifficulty) ||
                !notesByDifficulty.TryGetProperty(noteDifficulty, out JsonElement notesData))
            {
                Console.WriteLine($"No '{noteDifficulty}' difficulty found in {chartPath}");
                return null;
            }

            Dictionary<string, double> scrollSpeed = ReadScrollSpeed(root);
            List<Note> notes = [];

            foreach (JsonElement noteData in notesData.EnumerateArray())
            {
                int direction = noteData.GetProperty("d").GetInt32();
                if (direction > 3)
                {
                    continue;
                }

                double time = noteData.GetProperty("t").GetDouble();
                double? length = noteData.TryGetProperty("l", out JsonElement lengthElement)
                    ? lengthElement.GetDouble()
                    : null;

                notes.Add(new Note(time, direction, length));
            }

            List<Note> sortedNotes = notes
                .OrderBy(static note => note.Time)
                .ToList();
            return new Chart(difficulty, sortedNotes, scrollSpeed);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error loading chart from {chartPath}: {exception.Message}");
            return null;
        }
    }

    public static Dictionary<string, string> FindCharts(
        string songName,
        string basePath = "assets/data/songs")
    {
        Dictionary<string, string> charts = new(StringComparer.Ordinal);
        string songPath = Path.Combine(basePath, songName);

        if (!Directory.Exists(songPath))
        {
            return charts;
        }

        string chartPrefix = songName + "-chart";
        foreach (string file in Directory.EnumerateFiles(songPath, "*-chart*.json", SearchOption.TopDirectoryOnly))
        {
            string stem = Path.GetFileNameWithoutExtension(file);
            if (!stem.StartsWith(chartPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            string suffix = stem[chartPrefix.Length..];
            if (suffix.StartsWith("-", StringComparison.Ordinal))
            {
                suffix = suffix[1..];
            }

            HashSet<string>? noteDifficulties = ReadNoteDifficulties(file);
            if (noteDifficulties is null)
            {
                continue;
            }

            if (suffix.Length == 0)
            {
                AddIfPresent(charts, noteDifficulties, "easy", file);
                AddIfPresent(charts, noteDifficulties, "normal", file);
                AddIfPresent(charts, noteDifficulties, "hard", file);
            }
            else if (suffix == "erect")
            {
                AddIfPresent(charts, noteDifficulties, "erect", file);
                AddIfPresent(charts, noteDifficulties, "nightmare", file);
            }
            else if (suffix is "bf" or "pico")
            {
                AddCharacterDifficulties(charts, noteDifficulties, suffix, file);
            }
        }

        return charts;
    }

    public static IReadOnlyList<string> AvailableDifficulties(
        string songName,
        string basePath = "assets/data/songs")
    {
        Dictionary<string, string> charts = FindCharts(songName, basePath);
        return DifficultyOrder.Where(charts.ContainsKey).ToArray();
    }

    public static double? GetScrollSpeed(Chart chart, string difficulty)
    {
        string noteDifficulty = GetNoteDifficulty(difficulty);
        if (chart.ScrollSpeed.TryGetValue(noteDifficulty, out double speed))
        {
            return speed;
        }

        return chart.ScrollSpeed.TryGetValue("default", out speed) ? speed : null;
    }

    public static string GetDisplayName(string difficulty)
    {
        (string? character, string noteDifficulty) = SplitDifficulty(difficulty);
        string difficultyName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(noteDifficulty);
        return character is null
            ? difficultyName
            : $"{character.ToUpperInvariant()} {difficultyName}";
    }

    public static string? GetCharacterVariant(string difficulty)
    {
        return SplitDifficulty(difficulty).Character;
    }

    private static Dictionary<string, double> ReadScrollSpeed(JsonElement root)
    {
        if (!root.TryGetProperty("scrollSpeed", out JsonElement scrollSpeedElement) ||
            scrollSpeedElement.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["normal"] = 1
            };
        }

        Dictionary<string, double> result = new(StringComparer.Ordinal);
        foreach (JsonProperty property in scrollSpeedElement.EnumerateObject())
        {
            result[property.Name] = property.Value.GetDouble();
        }

        return result;
    }

    private static HashSet<string>? ReadNoteDifficulties(string file)
    {
        try
        {
            using FileStream stream = File.OpenRead(file);
            using JsonDocument document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("notes", out JsonElement notes) ||
                notes.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            return notes.EnumerateObject()
                .Select(static property => property.Name)
                .ToHashSet(StringComparer.Ordinal);
        }
        catch (IOException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void AddIfPresent(
        IDictionary<string, string> charts,
        ISet<string> noteDifficulties,
        string difficulty,
        string file)
    {
        if (noteDifficulties.Contains(difficulty))
        {
            charts[difficulty] = file;
        }
    }

    private static void AddCharacterDifficulties(
        IDictionary<string, string> charts,
        ISet<string> noteDifficulties,
        string character,
        string file)
    {
        foreach (string difficulty in new[] { "easy", "normal", "hard" })
        {
            if (noteDifficulties.Contains(difficulty))
            {
                charts[$"{character}-{difficulty}"] = file;
            }
        }
    }

    private static string GetNoteDifficulty(string difficulty)
    {
        return SplitDifficulty(difficulty).NoteDifficulty;
    }

    private static (string? Character, string NoteDifficulty) SplitDifficulty(string difficulty)
    {
        foreach (string character in new[] { "bf", "pico" })
        {
            string prefix = character + "-";
            if (difficulty.StartsWith(prefix, StringComparison.Ordinal))
            {
                return (character, difficulty[prefix.Length..]);
            }
        }

        return (null, difficulty);
    }
}
