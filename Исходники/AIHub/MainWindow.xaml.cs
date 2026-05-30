using System.Windows;
using System.Windows.Media;

namespace AIHub;

public partial class MainWindow : Window
{
    private bool _isDarkTheme;

    public MainWindow()
    {
        InitializeComponent();
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
        ThemeToggleButton.Content = _isDarkTheme ? "Светлая тема" : "Тёмная тема";
    }

    private void SetBrush(string resourceKey, string color)
    {
        Resources[resourceKey] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }
}
