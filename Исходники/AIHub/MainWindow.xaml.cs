using System.Reflection;
using System.Windows;
using System.Windows.Media;
using AIHub.Models;
using AIHub.Services;
using Microsoft.Win32;

namespace AIHub;

public partial class MainWindow : Window
{
    private readonly AppStateStore _appStateStore = new();
    private readonly ComputerPassportService _computerPassportService = new();

    private AppState _appState = new();
    private bool _isDarkTheme;

    public MainWindow()
    {
        InitializeComponent();
        Title = $"AI HUB {GetAppVersion()}";
        _isDarkTheme = IsWindowsAppThemeDark();
        SourceInitialized += (_, _) => ApplySystemTitleBarTheme();
        ApplyTheme();
        InitializeAppData();
        UpdatePrimaryActionButton();
    }

    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _isDarkTheme = !_isDarkTheme;
        ApplyTheme();
    }

    private void PrimaryActionButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSetupWindow(regeneratePassport: false);
    }

    private void ReconfigureButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSetupWindow(regeneratePassport: true);
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

    private void InitializeAppData()
    {
        try
        {
            _appState = _appStateStore.LoadOrCreate();
            var passport = _computerPassportService.EnsurePassport();
            SavePassportState(passport);
            StatusText.Text = "Статус: паспорт компьютера готов. Настройка пока не завершена.";
        }
        catch
        {
            StatusText.Text = "Статус: паспорт компьютера пока не создан. Можно открыть настройку и повторить анализ.";
        }
    }

    private void OpenSetupWindow(bool regeneratePassport)
    {
        try
        {
            var passport = regeneratePassport
                ? _computerPassportService.RegeneratePassport()
                : _computerPassportService.EnsurePassport();

            SavePassportState(passport);
            ShowSetupPage(passport);

            StatusText.Text = regeneratePassport
                ? "Статус: паспорт компьютера пересоздан. Настройка пока в режиме заготовки."
                : "Статус: открыта страница настройки. Реальное изменение параметров пока не выполняется.";
        }
        catch
        {
            StatusText.Text = "Статус: не удалось открыть настройку или обновить паспорт компьютера.";
        }
    }

    private void BackToStartButton_Click(object sender, RoutedEventArgs e)
    {
        SetupPage.Visibility = Visibility.Collapsed;
        WelcomePage.Visibility = Visibility.Visible;
        StatusText.Text = "Статус: паспорт компьютера готов. Настройка пока не завершена.";
    }

    private void SavePassportState(ComputerPassport passport)
    {
        _appState.ComputerPassportLastUpdated = passport.CreatedAt;
        _appStateStore.Save(_appState);
        UpdatePrimaryActionButton();
    }

    private void UpdatePrimaryActionButton()
    {
        PrimaryActionButton.Content = _appState.HasCompletedSetup
            ? "Начать работу"
            : "Начать настройку";
    }

    private void ShowSetupPage(ComputerPassport passport)
    {
        PassportSummaryText.Text = BuildPassportSummary(passport);
        PassportPathText.Text = $"Файл паспорта: {AppDataPaths.ComputerPassportPath}";
        WelcomePage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Visible;
    }

    private static string BuildPassportSummary(ComputerPassport passport)
    {
        var drives = passport.Drives.Count == 0
            ? "Диски: данные пока не получены."
            : $"Диски: найдено {passport.Drives.Count}.";

        return string.Join(
            Environment.NewLine,
            $"Анализ: {passport.CreatedAt:dd.MM.yyyy HH:mm:ss}",
            $"Компьютер: {passport.MachineName}",
            $"Windows: {passport.WindowsVersion}",
            $"CPU: {passport.CpuName}",
            $"RAM: {passport.RamTotalGb:0.##} ГБ",
            drives);
    }

    private void ApplySystemTitleBarTheme()
    {
        WindowTitleBarThemeService.Apply(this, _isDarkTheme);
    }

    private static bool IsWindowsAppThemeDark()
    {
        const string personalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        var appsUseLightTheme = Registry.CurrentUser
            .OpenSubKey(personalizeKey)
            ?.GetValue("AppsUseLightTheme");

        return appsUseLightTheme is int value && value == 0;
    }

}
