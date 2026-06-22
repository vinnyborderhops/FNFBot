using System.Text.Json;

namespace FNFBot.Services;

public static class MetadataLoader
{
    public static double? LoadBpm(string songName, string difficulty, string basePath)
    {
        string? characterVariant = ChartLoader.GetCharacterVariant(difficulty);
        string suffix = difficulty switch
        {
            "erect" or "nightmare" => "-erect",
            _ when characterVariant is not null => $"-{characterVariant}",
            _ => string.Empty
        };

        string metadataPath = Path.Combine(
            basePath,
            songName,
            $"{songName}-metadata{suffix}.json");

        try
        {
            using FileStream stream = File.OpenRead(metadataPath);
            using JsonDocument document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("timeChanges", out JsonElement timeChanges) ||
                timeChanges.ValueKind != JsonValueKind.Array ||
                timeChanges.GetArrayLength() == 0)
            {
                return null;
            }

            JsonElement firstTimeChange = timeChanges[0];
            return firstTimeChange.TryGetProperty("bpm", out JsonElement bpm)
                ? bpm.GetDouble()
                : null;
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
}
