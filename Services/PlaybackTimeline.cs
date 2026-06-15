using FnfBot.Interop;
using FnfBot.Models;

namespace FnfBot.Services;

public static class PlaybackTimeline
{
    public const double TapDurationMs = 50;

    public static IReadOnlyList<ScheduledEventGroup> Build(
        IReadOnlyList<Note> notes,
        IReadOnlyDictionary<int, IReadOnlyList<VirtualKey>> keyMap)
    {
        List<ScheduledKeyEvent> events = new(notes.Count * 2);
        Dictionary<int, double[]> keyAvailableAt = [];
        Dictionary<int, int> nextKeyIndex = [];
        int sequence = 0;

        foreach (Note note in notes)
        {
            double pressTime = note.Time;
            double duration = note.Length.HasValue && note.Length.Value != 0
                ? note.Length.Value
                : TapDurationMs;
            VirtualKey key = SelectAvailableKey(
                note.Direction,
                pressTime,
                pressTime + duration,
                keyAvailableAt,
                nextKeyIndex,
                keyMap);

            events.Add(new ScheduledKeyEvent(
                pressTime,
                key,
                ScheduledKeyEventType.KeyDown,
                sequence++));
            events.Add(new ScheduledKeyEvent(
                pressTime + duration,
                key,
                ScheduledKeyEventType.KeyUp,
                sequence++));
        }

        return events
            .OrderBy(static scheduledEvent => scheduledEvent.Time)
            .ThenBy(static scheduledEvent => scheduledEvent.Type)
            .ThenBy(static scheduledEvent => scheduledEvent.Sequence)
            .GroupBy(static scheduledEvent => scheduledEvent.Time)
            .Select(static group => new ScheduledEventGroup(group.Key, group.ToArray()))
            .ToArray();
    }

    private static VirtualKey SelectAvailableKey(
        int direction,
        double pressTime,
        double releaseTime,
        IDictionary<int, double[]> keyAvailableAt,
        IDictionary<int, int> nextKeyIndex,
        IReadOnlyDictionary<int, IReadOnlyList<VirtualKey>> keyMap)
    {
        IReadOnlyList<VirtualKey> keys = keyMap[direction];
        if (!keyAvailableAt.TryGetValue(direction, out double[]? availableAt))
        {
            availableAt = new double[keys.Count];
            keyAvailableAt[direction] = availableAt;
        }

        int preferredIndex = nextKeyIndex.TryGetValue(direction, out int storedIndex)
            ? storedIndex
            : 0;
        int selectedIndex = -1;
        for (int offset = 0; offset < keys.Count; offset++)
        {
            int candidateIndex = (preferredIndex + offset) % keys.Count;
            if (availableAt[candidateIndex] <= pressTime)
            {
                selectedIndex = candidateIndex;
                break;
            }
        }

        if (selectedIndex < 0)
        {
            selectedIndex = Array.IndexOf(availableAt, availableAt.Min());
        }

        availableAt[selectedIndex] = releaseTime;
        nextKeyIndex[direction] = (selectedIndex + 1) % keys.Count;
        return keys[selectedIndex];
    }
}
