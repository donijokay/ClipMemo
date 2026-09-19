using ClipMemo.Native;

namespace ClipMemo.Services;

/// <summary>Global hotkey via RegisterHotKey (default Ctrl+Shift+V).</summary>
public sealed class HotkeyService : IDisposable
{
    public const int HotkeyId = 0xC11F;

    private readonly IntPtr _hwnd;
    private bool _registered;
    private bool _disposed;

    public HotkeyService(IntPtr hwnd)
    {
        _hwnd = hwnd;
    }

    public bool Register(uint modifiers, uint vk)
    {
        Unregister();
        // MOD_NOREPEAT reduces key-repeat spam
        uint mods = modifiers | (uint)NativeMethods.MOD_NOREPEAT;
        _registered = NativeMethods.RegisterHotKey(_hwnd, HotkeyId, mods, vk);
        return _registered;
    }

    public void Unregister()
    {
        if (!_registered)
            return;
        try
        {
            NativeMethods.UnregisterHotKey(_hwnd, HotkeyId);
        }
        catch
        {
            /* ignore */
        }
        _registered = false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Unregister();
    }
}
