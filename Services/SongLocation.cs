namespace FNFBot.Services;

public sealed record SongLocation(string Name, string BasePath, string? ModName = null)
{
    public bool IsMod => ModName is not null;

    public string DisplayName => ModName is null ? Name : $"{ModName}/{Name}";
}
