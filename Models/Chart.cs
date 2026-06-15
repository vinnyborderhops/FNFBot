namespace FnfBot.Models;

public sealed class Chart
{
    public Chart(string difficulty, IReadOnlyList<Note> notes, IReadOnlyDictionary<string, double> scrollSpeed)
    {
        Difficulty = difficulty;
        Notes = notes;
        ScrollSpeed = scrollSpeed;
    }

    public string Difficulty { get; }

    public IReadOnlyList<Note> Notes { get; }

    public IReadOnlyDictionary<string, double> ScrollSpeed { get; }
}
