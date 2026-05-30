using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AIHub.Services;

public static class WindowTitleBarThemeService
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;

    public static void Apply(Window window, bool isDarkTheme)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var useDarkMode = isDarkTheme ? 1 : 0;
        var result = DwmSetWindowAttribute(
            handle,
            DwmwaUseImmersiveDarkMode,
            ref useDarkMode,
            Marshal.SizeOf<int>());

        if (result != 0)
        {
            DwmSetWindowAttribute(
                handle,
                DwmwaUseImmersiveDarkModeBefore20H1,
                ref useDarkMode,
                Marshal.SizeOf<int>());
        }
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int attributeValue,
        int attributeSize);
}
