using FNFBot.Interop;
using FNFBot.Models;

namespace FNFBot.Services;

public sealed class ActiveKeyState
{
    private readonly Dictionary<VirtualKey, int> _activeCounts = [];

    public IReadOnlyList<KeyTransition> Apply(ScheduledEventGroup group)
    {
        List<KeyTransition> transitions = [];

        foreach (IGrouping<VirtualKey, ScheduledKeyEvent> keyEvents in group.Events
                     .Where(static scheduledEvent => scheduledEvent.Type == ScheduledKeyEventType.KeyUp)
                     .GroupBy(static scheduledEvent => scheduledEvent.Key))
        {
            int activeCount = GetActiveCount(keyEvents.Key);
            int updatedCount = Math.Max(0, activeCount - keyEvents.Count());
            SetActiveCount(keyEvents.Key, updatedCount);

            if (activeCount > 0 && updatedCount == 0)
            {
                transitions.Add(new KeyTransition(keyEvents.Key, KeyTransitionType.KeyUp));
            }
        }

        foreach (IGrouping<VirtualKey, ScheduledKeyEvent> keyEvents in group.Events
                     .Where(static scheduledEvent => scheduledEvent.Type == ScheduledKeyEventType.KeyDown)
                     .GroupBy(static scheduledEvent => scheduledEvent.Key))
        {
            int activeCount = GetActiveCount(keyEvents.Key);
            SetActiveCount(keyEvents.Key, activeCount + keyEvents.Count());

            if (activeCount == 0)
            {
                transitions.Add(new KeyTransition(keyEvents.Key, KeyTransitionType.KeyDown));
            }
        }

        return transitions;
    }

    public IReadOnlyList<KeyTransition> ReleaseAll()
    {
        KeyTransition[] releases = _activeCounts
            .Where(static pair => pair.Value > 0)
            .Select(static pair => new KeyTransition(pair.Key, KeyTransitionType.KeyUp))
            .ToArray();
        _activeCounts.Clear();
        return releases;
    }

    private int GetActiveCount(VirtualKey key)
    {
        return _activeCounts.GetValueOrDefault(key);
    }

    private void SetActiveCount(VirtualKey key, int count)
    {
        if (count == 0)
        {
            _activeCounts.Remove(key);
        }
        else
        {
            _activeCounts[key] = count;
        }
    }
}
