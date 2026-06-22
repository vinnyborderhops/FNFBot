using FNFBot.Interop;

namespace FNFBot.Models;

public enum ScheduledKeyEventType
{
    KeyUp,
    KeyDown
}

public sealed record ScheduledKeyEvent(
    double Time,
    VirtualKey Key,
    ScheduledKeyEventType Type,
    int Sequence);
