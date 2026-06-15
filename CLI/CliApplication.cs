using FnfBot.Core;

namespace FnfBot.CLI;

public static class CliApplication
{
    public static void Run()
    {
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("Friday Night Funkin' Bot - CLI Mode");
        Console.WriteLine(new string('=', 60));

        RhythmBot bot = new(3000);
        Console.WriteLine("\nCommands:");
        Console.WriteLine("  list           - List all available songs");
        Console.WriteLine("  load <song>    - Load a song");
        Console.WriteLine("  delay <ms>     - Set delay before play (in milliseconds)");
        Console.WriteLine("  f1             - Simulate F1 press (for testing)");
        Console.WriteLine("  quit           - Exit");
        Console.WriteLine("\nNote: Press F1 in-game to start the bot\n");

        PrintSongs(bot.ListSongs());

        while (true)
        {
            try
            {
                Console.Write("bot> ");
                string command = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
                if (command.Length == 0)
                {
                    continue;
                }

                if (command == "quit")
                {
                    Console.WriteLine("Goodbye!");
                    break;
                }

                if (command == "list")
                {
                    PrintSongs(bot.ListSongs());
                }
                else if (command.StartsWith("load ", StringComparison.Ordinal))
                {
                    bot.LoadSong(command[5..].Trim());
                }
                else if (command.StartsWith("delay ", StringComparison.Ordinal))
                {
                    if (double.TryParse(
                            command[6..].Trim(),
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out double delayMs) &&
                        double.IsFinite(delayMs) &&
                        delayMs >= 0)
                    {
                        bot.DelayBeforePlayMs = delayMs;
                        Console.WriteLine($"Delay set to {delayMs}ms");
                    }
                    else
                    {
                        Console.WriteLine("Invalid delay value");
                    }
                }
                else if (command == "f1")
                {
                    bot.OnF1Pressed();
                }
                else
                {
                    Console.WriteLine("Unknown command");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error: {exception.Message}");
            }
        }
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
}
