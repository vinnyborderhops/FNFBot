using System.Diagnostics;
using FnfBot.Interop;
using FnfBot.Models;
using FnfBot.Services;

namespace FnfBot.Core;

public sealed class RhythmBot
{
    public const double DefaultHitBiasMs = 18;

    private volatile bool _isRunning;
    private double _hitBiasMs = DefaultHitBiasMs;
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

    public event Action<bool>? PlaybackFinished;

    public bool IsRunning
    {
        get => _isRunning;
        set => _isRunning = value;
    }

    public Chart? CurrentChart { get; private set; }

    public string? CurrentSong { get; private set; }

    public string CurrentDifficulty { get; private set; } = "normal";

    public string GameFolder { get; }

    private bool NavigateToRestartConfirmation()
    {
        Console.WriteLine("Navigating menu...");

        InputSimulator.PressKey(VirtualKey.Enter, 80);
        if (!WaitForNavigationDelay(500))
        {
            return false;
        }

        InputSimulator.PressKey(VirtualKey.DownArrow, 80);
        if (!WaitForNavigationDelay(500))
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

        IReadOnlyList<ScheduledEventGroup> timeline = PlaybackTimeline.Build(chart.Notes, InputKeys);

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
            if (!WaitUntil(stopwatch, chartStartMs))
            {
                Console.WriteLine("Playback stopped");
                return false;
            }

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
        string basePath = Path.Combine(GameFolder, "assets", "data", "songs");
        Dictionary<string, string> charts = ChartLoader.FindCharts(songName, basePath);

        if (charts.Count == 0)
        {
            Console.WriteLine($"No charts found for song: {songName}");
            return false;
        }

        if (!charts.TryGetValue(difficulty, out string? chartPath))
        {
            Console.WriteLine($"No '{difficulty}' difficulty found for: {songName}");
            return false;
        }

        Chart? chart = ChartLoader.LoadChart(chartPath, difficulty);
        if (chart is null)
        {
            Console.WriteLine($"Failed to load chart for: {songName}");
            return false;
        }

        CurrentChart = chart;
        CurrentSong = songName;
        CurrentDifficulty = difficulty;
        Console.WriteLine($"Loaded song: {songName} - {difficulty} ({chart.Notes.Count} notes)");
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
        string basePath = Path.Combine(GameFolder, "assets", "data", "songs");
        IReadOnlyList<string> songs = SongFinder.FindAllSongs(basePath);
        if (songs.Count == 0)
        {
            Console.WriteLine("No songs found");
            return [];
        }

        return songs;
    }

    private bool WaitUntil(Stopwatch stopwatch, double targetMilliseconds)
    {
        while (IsRunning)
        {
            double remainingMilliseconds = targetMilliseconds - stopwatch.Elapsed.TotalMilliseconds;
            if (remainingMilliseconds <= 0)
            {
                return true;
            }

            if (remainingMilliseconds > 8)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(remainingMilliseconds - 4));
            }
            else
            {
                Thread.SpinWait(50);
            }
        }

        return false;
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
