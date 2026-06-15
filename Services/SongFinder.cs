namespace FnfBot.Services;

public static class SongFinder
{
    public static IReadOnlyList<string> FindAllSongs(string basePath = "assets/data/songs")
    {
        if (!Directory.Exists(basePath))
        {
            Console.WriteLine($"Songs path not found: {basePath}");
            return [];
        }

        return Directory.EnumerateDirectories(basePath)
            .Where(directory =>
            {
                string name = Path.GetFileName(directory);
                return File.Exists(Path.Combine(directory, $"{name}-chart.json"));
            })
            .Select(Path.GetFileName)
            .Where(static name => name is not null)
            .Select(static name => name!)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
    }
}
