using System.Diagnostics;
using FNFBot.Core;

namespace FNFBot.CLI;

public static class CliApplication
{
    private static readonly DateTime StartedAt = DateTime.Now;
    private static bool _debugEnabled = IsDebugEnabledByDefault();

    public static void Run()
    {
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("Friday Night Funkin' Bot - CLI Mode");
        Console.WriteLine(new string('=', 60));

        RhythmBot bot = new(3000);
        LogDebug("CLI application started");
        LogDebug($"Process ID: {Environment.ProcessId}");
        LogDebug($"Current directory: {Environment.CurrentDirectory}");
        LogDebug($".NET version: {Environment.Version}");

        Console.WriteLine("\nCommands:");
        Console.WriteLine("  list           - List all available songs");
        Console.WriteLine("  load <song>    - Load a song");
        Console.WriteLine("  delay <ms>     - Set delay before play (in milliseconds)");
        Console.WriteLine("  status         - Show current bot state");
        Console.WriteLine("  debug [on|off] - Toggle debug diagnostics");
        Console.WriteLine("  f1             - Simulate F1 press (for testing)");
        Console.WriteLine("  quit           - Exit");
        Console.WriteLine("\nSet FNFBOT_CLI_DEBUG=1 to enable debug diagnostics on startup.");
        Console.WriteLine("\nNote: Press F1 in-game to start the bot\n");

        PrintSongs(bot.ListSongs());

        while (true)
        {
            try
            {
                Console.Write("bot> ");
                string input = (Console.ReadLine() ?? string.Empty).Trim();
                if (input.Length == 0)
                {
                    continue;
                }

                Stopwatch commandTimer = Stopwatch.StartNew();
                LogDebug($"Command received: {input}");
                if (ExecuteCommand(bot, input))
                {
                    break;
                }

                commandTimer.Stop();
                LogDebug($"Command completed in {commandTimer.Elapsed.TotalMilliseconds:0.###}ms");
            }
            catch (Exception exception)
            {
                WriteException(exception);
            }
        }
    }

    private static bool ExecuteCommand(RhythmBot bot, string input)
    {
        string command = input;
        string argument = string.Empty;
        int separatorIndex = input.IndexOf(' ', StringComparison.Ordinal);
        if (separatorIndex >= 0)
        {
            command = input[..separatorIndex];
            argument = input[(separatorIndex + 1)..].Trim();
        }

        switch (command.ToLowerInvariant())
        {
            case "quit":
                Console.WriteLine("Goodbye!");
                return true;

            case "list":
                PrintSongs(bot.ListSongs());
                return false;

            case "load":
                if (argument.Length == 0)
                {
                    Console.WriteLine("Usage: load <song>");
                    return false;
                }

                LogDebug($"Loading song '{argument}'");
                bool loaded = bot.LoadSong(argument);
                LogDebug($"Load result: {loaded}");
                PrintStatus(bot);
                return false;

            case "delay":
                SetDelay(bot, argument);
                return false;

            case "status":
                PrintStatus(bot);
                return false;

            case "debug":
                SetDebug(argument);
                return false;

            case "f1":
                LogDebug("Simulating F1 press");
                bot.OnF1Pressed();
                PrintStatus(bot);
                return false;

            default:
                Console.WriteLine("Unknown command");
                LogDebug($"Unknown command '{command}' with argument '{argument}'");
                return false;
        }
    }

    private static void SetDelay(RhythmBot bot, string argument)
    {
        if (double.TryParse(
                argument,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double delayMs) &&
            double.IsFinite(delayMs) &&
            delayMs >= 0)
        {
            bot.DelayBeforePlayMs = delayMs;
            Console.WriteLine($"Delay set to {delayMs}ms");
            LogDebug($"DelayBeforePlayMs changed to {delayMs}");
        }
        else
        {
            Console.WriteLine("Invalid delay value");
            LogDebug($"Rejected delay value '{argument}'");
        }
    }

    private static void SetDebug(string argument)
    {
        if (argument.Length == 0 || argument.Equals("toggle", StringComparison.OrdinalIgnoreCase))
        {
            _debugEnabled = !_debugEnabled;
        }
        else if (argument.Equals("on", StringComparison.OrdinalIgnoreCase))
        {
            _debugEnabled = true;
        }
        else if (argument.Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            _debugEnabled = false;
        }
        else
        {
            Console.WriteLine("Usage: debug [on|off]");
            return;
        }

        Console.WriteLine($"Debug diagnostics {(_debugEnabled ? "enabled" : "disabled")}");
        LogDebug("Debug diagnostics are enabled");
    }

    private static void PrintStatus(RhythmBot bot)
    {
        Console.WriteLine("Status:");
        Console.WriteLine($"  Running: {bot.IsRunning}");
        Console.WriteLine($"  Current song: {bot.CurrentSong ?? "(none)"}");
        Console.WriteLine($"  Difficulty: {bot.CurrentDifficulty}");
        Console.WriteLine($"  Notes loaded: {bot.CurrentChart?.Notes.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "0"}");
        Console.WriteLine($"  Delay before play: {bot.DelayBeforePlayMs}ms");
        Console.WriteLine($"  Hit bias: {bot.HitBiasMs}ms");
        Console.WriteLine($"  Game folder: {bot.GameFolder}");
        Console.WriteLine($"  Uptime: {DateTime.Now - StartedAt:g}");
        Console.WriteLine($"  Debug: {(_debugEnabled ? "on" : "off")}");
    }

    private static void PrintSongs(IReadOnlyList<string> songs)
    {
        if (songs.Count == 0)
        {
            Console.WriteLine("No songs found\n");
            return;
        }

        Console.WriteLine("Available songs:");
        for (int index = 0; index < songs.Count; index++)
        {
            Console.WriteLine($"  {index + 1}. {songs[index]}");
        }

        Console.WriteLine();
    }

    private static void LogDebug(string message)
    {
        if (!_debugEnabled)
        {
            return;
        }

        Console.Error.WriteLine($"[debug {DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    private static void WriteException(Exception exception)
    {
        if (_debugEnabled)
        {
            Console.Error.WriteLine($"[debug {DateTime.Now:HH:mm:ss.fff}] Unhandled command error:");
            Console.Error.WriteLine(exception);
            return;
        }

        Console.WriteLine($"Error: {exception.Message}");
        Console.WriteLine("Run 'debug on' for full exception details.");
    }

    private static bool IsDebugEnabledByDefault()
    {
        string? value = Environment.GetEnvironmentVariable("FNFBOT_CLI_DEBUG");
        return value is not null &&
            (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
             value.Equals("on", StringComparison.OrdinalIgnoreCase));
    }
}
