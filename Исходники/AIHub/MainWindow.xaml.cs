using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace AIHub;

public partial class MainWindow : Window
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;

    private bool _isDarkTheme;

    public MainWindow()
    {
        InitializeComponent();
        Title = $"AI HUB {GetAppVersion()}";
        _isDarkTheme = IsWindowsAppThemeDark();
        SourceInitialized += (_, _) => ApplySystemTitleBarTheme();
        ApplyTheme();
    }

    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _isDarkTheme = !_isDarkTheme;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        SetBrush("WindowBackgroundBrush", _isDarkTheme ? "#111827" : "#F3F3F3");
        SetBrush("HeaderBackgroundBrush", _isDarkTheme ? "#0B1220" : "#FFFFFF");
        SetBrush("PanelBrush", _isDarkTheme ? "#172033" : "#FFFFFF");
        SetBrush("LineBrush", _isDarkTheme ? "#2D374B" : "#DADDE3");
        SetBrush("TextPrimaryBrush", _isDarkTheme ? "#F8FAFC" : "#1F1F1F");
        SetBrush("TextSecondaryBrush", _isDarkTheme ? "#AAB4C4" : "#5D6470");
        SetBrush("StepBadgeBrush", _isDarkTheme ? "#1E3A5F" : "#EAF1FF");
        SetBrush("SecondaryButtonBackgroundBrush", _isDarkTheme ? "#111827" : "#F8F8F8");

        RootWindow.Background = (Brush)Resources["WindowBackgroundBrush"];
        ThemeToggleButton.Content = _isDarkTheme ? "☀" : "☾";
        ThemeToggleButton.Foreground = _isDarkTheme
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBBF24"))
            : (Brush)Resources["TextPrimaryBrush"];
        ThemeToggleButton.ToolTip = _isDarkTheme
            ? "Переключить на светлую тему"
            : "Переключить на тёмную тему";

        ApplySystemTitleBarTheme();
    }

    private void SetBrush(string resourceKey, string color)
    {
        Resources[resourceKey] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }

    private static string GetAppVersion()
    {
        return typeof(MainWindow).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";
    }

    private void ApplySystemTitleBarTheme()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var useDarkMode = _isDarkTheme ? 1 : 0;
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

    private static bool IsWindowsAppThemeDark()
    {
        const string personalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        var appsUseLightTheme = Registry.CurrentUser
            .OpenSubKey(personalizeKey)
            ?.GetValue("AppsUseLightTheme");

        return appsUseLightTheme is int value && value == 0;
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int attributeValue,
        int attributeSize);
}
