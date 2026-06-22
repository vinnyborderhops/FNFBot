namespace FNFBot.Services;

public static class SongFinder
{
    public static IReadOnlyList<string> FindAllSongs(string basePath = "assets/data/songs")
    {
        return FindSongsInPath(basePath, true)
            .Select(static song => song.Name)
            .ToArray();
    }

    public static IReadOnlyList<SongLocation> FindAllSongLocations(string gameFolder)
    {
        List<SongLocation> songs = [];

        string basePath = Path.Combine(gameFolder, "assets", "data", "songs");
        songs.AddRange(FindSongsInPath(basePath, true));

        string modsPath = Path.Combine(gameFolder, "mods");
        if (Directory.Exists(modsPath))
        {
            foreach (string modDirectory in Directory.EnumerateDirectories(modsPath).OrderBy(Path.GetFileName, StringComparer.Ordinal))
            {
                string modName = Path.GetFileName(modDirectory);
                string modSongsPath = Path.Combine(modDirectory, "data", "songs");
                songs.AddRange(FindSongsInPath(modSongsPath, false, modName));
            }
        }

        return songs;
    }

    private static IReadOnlyList<SongLocation> FindSongsInPath(
        string basePath,
        bool logMissingPath,
        string? modName = null)
    {
        if (!Directory.Exists(basePath))
        {
            if (logMissingPath)
            {
                Console.WriteLine($"Songs path not found: {basePath}");
            }

            return [];
        }

        return Directory.EnumerateDirectories(basePath)
            .Where(directory =>
            {
                string name = Path.GetFileName(directory);
                return File.Exists(Path.Combine(directory, $"{name}-chart.json"));
            })
            .Select(directory => new SongLocation(Path.GetFileName(directory), basePath, modName))
            .OrderBy(static song => song.Name, StringComparer.Ordinal)
            .ToArray();
    }
}
