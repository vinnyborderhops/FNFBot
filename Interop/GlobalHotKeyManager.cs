using System.ComponentModel;
using System.Runtime.InteropServices;

namespace FNFBot.Interop;

public sealed class GlobalHotKeyManager : IDisposable
{
    private const uint NoModifier = 0;
    private readonly IntPtr _windowHandle;
    private readonly HashSet<int> _registeredIds = [];
    private bool _disposed;

    public GlobalHotKeyManager(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    public void Register(int id, Keys key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!RegisterHotKey(_windowHandle, id, NoModifier, (uint)key))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        _registeredIds.Add(id);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (int id in _registeredIds)
        {
            UnregisterHotKey(_windowHandle, id);
        }

        _registeredIds.Clear();
        _disposed = true;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(
        IntPtr windowHandle,
        int id,
        uint modifiers,
        uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr windowHandle, int id);
}
