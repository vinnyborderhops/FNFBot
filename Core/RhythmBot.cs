using System.Diagnostics;
using FNFBot.Interop;
using FNFBot.Models;
using FNFBot.Services;

namespace FNFBot.Core;

public sealed class RhythmBot
{
    public const double DefaultHitBiasMs = 22.5;
    public const int DefaultMenuNavigationDelayMs = 250;

    private volatile bool _isRunning;
    private double _hitBiasMs = DefaultHitBiasMs;
    private double _tapDurationMs = PlaybackTimeline.TapDurationMs;
    private double _holdReleaseGuardMs = PlaybackTimeline.HoldReleaseGuardMs;
    private IReadOnlyDictionary<int, IReadOnlyList<VirtualKey>> _inputKeys;

    public RhythmBot(
        double delayBeforePlayMs = 3000,
        string? gameFolder = null,
        IReadOnlyDictionary<int, IReadOnlyList<VirtualKey>>? inputKeys = null)
    {
        DelayBeforePlayMs = delayBeforePlayMs;
        GameFolder = gameFolder ?? Environment.CurrentDirectory;
        _inputKeys = inputKeys ?? BotSettings.CreateDefault(DefaultHitBiasMs).InputKeys;
    }

    public double DelayBeforePlayMs { get; set; }

    public int MenuNavigationDelayMs { get; set; } = DefaultMenuNavigationDelayMs;

    public IReadOnlyDictionary<int, IReadOnlyList<VirtualKey>> InputKeys
    {
        get => Volatile.Read(ref _inputKeys);
        set => Volatile.Write(ref _inputKeys, value);
    }

    public double HitBiasMs
    {
        get => Volatile.Read(ref _hitBiasMs);
        set => Interlocked.Exchange(ref _hitBiasMs, value);
    }

    public double TapDurationMs
    {
        get => Volatile.Read(ref _tapDurationMs);
        set => Interlocked.Exchange(ref _tapDurationMs, value);
    }

    public double HoldReleaseGuardMs
    {
        get => Volatile.Read(ref _holdReleaseGuardMs);
        set => Interlocked.Exchange(ref _holdReleaseGuardMs, value);
    }

    public event Action<bool>? PlaybackFinished;

    public bool IsRunning
    {
        get => _isRunning;
        set => _isRunning = value;
    }

    public Chart? CurrentChart { get; private set; }

    public string? CurrentSong { get; private set; }

    public SongLocation? CurrentSongLocation { get; private set; }

    public string CurrentDifficulty { get; private set; } = "normal";

    public string GameFolder { get; }

    private bool NavigateToRestartConfirmation()
    {
        Console.WriteLine("Navigating menu...");

        InputSimulator.PressKey(VirtualKey.Enter, 80);
        if (!WaitForNavigationDelay(MenuNavigationDelayMs))
        {
            return false;
        }

        InputSimulator.PressKey(VirtualKey.DownArrow, 80);
        if (!WaitForNavigationDelay(MenuNavigationDelayMs))
        {
            return false;
        }

        return true;
    }

    private bool RunBot()
    {
        Chart? chart = CurrentChart;
        if (chart is null)
        {
            Console.WriteLine("No chart loaded");
            return false;
        }

        if (!NavigateToRestartConfirmation())
        {
            Console.WriteLine("Playback stopped");
            return false;
        }

        IReadOnlyList<ScheduledEventGroup> timeline = PlaybackTimeline.Build(
            chart.Notes,
            InputKeys,
            TapDurationMs,
            HoldReleaseGuardMs);

        // The game reacts to the key-down event, so use that exact moment as time zero.
        InputSimulator.KeyDown(VirtualKey.Enter);
        Stopwatch stopwatch = Stopwatch.StartNew();
        Thread.Sleep(80);
        InputSimulator.KeyUp(VirtualKey.Enter);
        Console.WriteLine("Menu navigation complete");

        return PlayChart(chart, timeline, stopwatch);
    }

    private bool PlayChart(
        Chart chart,
        IReadOnlyList<ScheduledEventGroup> timeline,
        Stopwatch stopwatch)
    {
        Console.WriteLine($"Playing chart with {chart.Notes.Count} notes...");
        Console.WriteLine($"Waiting {DelayBeforePlayMs}ms before starting...");
        ActiveKeyState activeKeys = new();
        double chartStartMs = DelayBeforePlayMs;

        try
        {
            foreach (ScheduledEventGroup group in timeline)
            {
                if (!WaitUntilChartEvent(stopwatch, chartStartMs + group.Time))
                {
                    Console.WriteLine("Playback stopped");
                    return false;
                }

                InputSimulator.SendTransitions(activeKeys.Apply(group));
            }

            return true;
        }
        finally
        {
            InputSimulator.SendTransitions(activeKeys.ReleaseAll());
        }
    }

    public bool LoadSong(string songName, string difficulty = "normal")
    {
        SongLocation? songLocation = ListSongLocations()
            .FirstOrDefault(song =>
                string.Equals(song.Name, songName, StringComparison.Ordinal) ||
                string.Equals(song.DisplayName, songName, StringComparison.Ordinal));
        if (songLocation is null)
        {
            Console.WriteLine($"No charts found for song: {songName}");
            return false;
        }

        return LoadSong(songLocation, difficulty);
    }

    public bool LoadSong(SongLocation songLocation, string difficulty = "normal")
    {
        Dictionary<string, string> charts = ChartLoader.FindCharts(songLocation.Name, songLocation.BasePath);

        if (charts.Count == 0)
        {
            Console.WriteLine($"No charts found for song: {songLocation.DisplayName}");
            return false;
        }

        if (!charts.TryGetValue(difficulty, out string? chartPath))
        {
            Console.WriteLine($"No '{difficulty}' difficulty found for: {songLocation.DisplayName}");
            return false;
        }

        Chart? chart = ChartLoader.LoadChart(chartPath, difficulty);
        if (chart is null)
        {
            Console.WriteLine($"Failed to load chart for: {songLocation.DisplayName}");
            return false;
        }

        CurrentChart = chart;
        CurrentSong = songLocation.DisplayName;
        CurrentSongLocation = songLocation;
        CurrentDifficulty = difficulty;
        Console.WriteLine($"Loaded song: {songLocation.DisplayName} - {difficulty} ({chart.Notes.Count} notes)");
        return true;
    }

    public void OnF1Pressed()
    {
        if (IsRunning)
        {
            Console.WriteLine("Bot already running");
            return;
        }

        if (CurrentSong is null)
        {
            Console.WriteLine("No song loaded. Please load a song first.");
            return;
        }

        Console.WriteLine($"Starting bot for: {CurrentSong}");
        IsRunning = true;

        _ = Task.Run(() =>
        {
            bool completed = false;
            try
            {
                completed = RunBot();
                Console.WriteLine(completed ? "Chart playback complete!" : "Chart playback stopped.");
            }
            finally
            {
                IsRunning = false;
                PlaybackFinished?.Invoke(completed);
            }
        });
    }

    public IReadOnlyList<string> ListSongs()
    {
        IReadOnlyList<SongLocation> songLocations = ListSongLocations();
        IReadOnlyList<string> songs = songLocations
            .Select(static song => song.DisplayName)
            .ToArray();
        if (songs.Count == 0)
        {
            Console.WriteLine("No songs found");
            return [];
        }

        return songs;
    }

    public IReadOnlyList<SongLocation> ListSongLocations()
    {
        IReadOnlyList<SongLocation> songs = SongFinder.FindAllSongLocations(GameFolder);
        if (songs.Count == 0)
        {
            Console.WriteLine("No songs found");
        }

        return songs;
    }

    private bool WaitUntilChartEvent(Stopwatch stopwatch, double unbiasedTargetMilliseconds)
    {
        while (IsRunning)
        {
            double targetMilliseconds = unbiasedTargetMilliseconds + HitBiasMs;
            double remainingMilliseconds = targetMilliseconds - stopwatch.Elapsed.TotalMilliseconds;
            if (remainingMilliseconds <= 0)
            {
                return true;
            }

            if (remainingMilliseconds > 8)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(Math.Min(remainingMilliseconds - 4, 10)));
            }
            else
            {
                Thread.SpinWait(50);
            }
        }

        return false;
    }

    private bool WaitForNavigationDelay(int milliseconds)
    {
        const int PollIntervalMs = 10;
        int remaining = milliseconds;

        while (IsRunning && remaining > 0)
        {
            int sleep = Math.Min(PollIntervalMs, remaining);
            Thread.Sleep(sleep);
            remaining -= sleep;
        }

        return IsRunning;
    }
}
