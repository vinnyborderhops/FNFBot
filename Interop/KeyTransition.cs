namespace FnfBot.Interop;

public enum KeyTransitionType
{
    KeyUp,
    KeyDown
}

public readonly record struct KeyTransition(VirtualKey Key, KeyTransitionType Type);
