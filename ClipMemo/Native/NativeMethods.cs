using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ClipMemo.Native;

internal static class NativeMethods
{
    public const int WM_HOTKEY = 0x0312;
    public const int MOD_ALT = 0x0001;
    public const int MOD_CONTROL = 0x0002;
    public const int MOD_SHIFT = 0x0004;
    public const int MOD_WIN = 0x0008;
    public const int MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);

    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    public static void TryApplyDarkScrollbars(Control control)
    {
        if (!control.IsHandleCreated)
            return;
        try
        {
            // Dark explorer theme for native scrollbars (Win10/11)
            _ = SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
            int useDark = 1;
            _ = DwmSetWindowAttribute(control.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
        }
        catch
        {
            /* older OS / no-op */
        }
    }
}

