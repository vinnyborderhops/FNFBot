using System.Runtime.InteropServices;
using FNFBot.Interop;

namespace FNFBot.Services;

public static class InputSimulator
{
    private const uint InputKeyboard = 1;
    private const uint KeyEventExtendedKey = 0x0001;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventScanCode = 0x0008;

    public static void PressKey(VirtualKey virtualKey, double durationMs = 50)
    {
        KeyDown(virtualKey);
        Thread.Sleep(TimeSpan.FromMilliseconds(durationMs));
        KeyUp(virtualKey);
    }

    public static void KeyDown(VirtualKey virtualKey)
    {
        SendTransitions([new KeyTransition(virtualKey, KeyTransitionType.KeyDown)]);
    }

    public static void KeyUp(VirtualKey virtualKey)
    {
        SendTransitions([new KeyTransition(virtualKey, KeyTransitionType.KeyUp)]);
    }

    public static void SendTransitions(IReadOnlyList<KeyTransition> transitions)
    {
        if (transitions.Count == 0)
        {
            return;
        }

        Input[] inputs = new Input[transitions.Count];
        for (int index = 0; index < transitions.Count; index++)
        {
            KeyTransition transition = transitions[index];
            bool useExtendedScanCode = TryGetArrowScanCode(transition.Key, out ushort scanCode);
            uint flags = transition.Type == KeyTransitionType.KeyUp ? KeyEventKeyUp : 0;
            if (useExtendedScanCode)
            {
                flags |= KeyEventScanCode | KeyEventExtendedKey;
            }

            inputs[index] = new Input
            {
                Type = InputKeyboard,
                Union = new InputUnion
                {
                    Keyboard = new KeyboardInput
                    {
                        VirtualKey = useExtendedScanCode ? (ushort)0 : (ushort)transition.Key,
                        ScanCode = scanCode,
                        Flags = flags,
                        Time = 0,
                        ExtraInfo = UIntPtr.Zero
                    }
                }
            };
        }

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
    }

    private static bool TryGetArrowScanCode(VirtualKey key, out ushort scanCode)
    {
        scanCode = key switch
        {
            VirtualKey.LeftArrow => 0x4B,
            VirtualKey.UpArrow => 0x48,
            VirtualKey.RightArrow => 0x4D,
            VirtualKey.DownArrow => 0x50,
            _ => 0
        };

        return scanCode != 0;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(
        uint inputCount,
        [In] Input[] inputs,
        int inputSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput Mouse;

        [FieldOffset(0)]
        public KeyboardInput Keyboard;

        [FieldOffset(0)]
        public HardwareInput Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HardwareInput
    {
        public uint Message;
        public ushort ParameterLow;
        public ushort ParameterHigh;
    }
}
