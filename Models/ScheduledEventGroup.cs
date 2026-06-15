namespace FnfBot.Models;

public sealed class ScheduledEventGroup
{
    public ScheduledEventGroup(double time, IReadOnlyList<ScheduledKeyEvent> events)
    {
        Time = time;
        Events = events;
    }

    public double Time { get; }

    public IReadOnlyList<ScheduledKeyEvent> Events { get; }
}
