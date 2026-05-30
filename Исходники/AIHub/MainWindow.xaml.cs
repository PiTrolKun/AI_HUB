using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using AIHub.Models;
using AIHub.Services;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using Media = System.Windows.Media;

namespace AIHub;

public partial class MainWindow : Window
{
    private readonly AppStateStore _appStateStore = new();
    private readonly ComputerPassportService _computerPassportService = new();
    private readonly StorageSettingsStore _storageSettingsStore = new();

    private AppState _appState = new();
    private StorageSettings _storageSettings = new();
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
        if (_appState.HasCompletedSetup)
        {
            StatusText.Text = "Статус: рабочий режим пока не реализован. Для изменения параметров нажмите Перенастроить.";
            return;
        }

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

        RootWindow.Background = (Media.Brush)Resources["WindowBackgroundBrush"];
        ThemeToggleButton.Content = _isDarkTheme ? "☀" : "☾";
        ThemeToggleButton.Foreground = _isDarkTheme
            ? new Media.SolidColorBrush((Media.Color)Media.ColorConverter.ConvertFromString("#FBBF24"))
            : (Media.Brush)Resources["TextPrimaryBrush"];
        ThemeToggleButton.ToolTip = _isDarkTheme
            ? "Переключить на светлую тему"
            : "Переключить на тёмную тему";

        ApplySystemTitleBarTheme();
    }

    private void SetBrush(string resourceKey, string color)
    {
        Resources[resourceKey] = new Media.SolidColorBrush((Media.Color)Media.ColorConverter.ConvertFromString(color));
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
            _storageSettings = _storageSettingsStore.LoadOrCreate();
            var passport = _computerPassportService.RegeneratePassport();
            SavePassportState(passport);
            UpdateComputerPassportStep(passport);
            LoadStorageSettingsIntoControls();
            UpdateStorageSteps();
            UpdateWelcomeStatus();
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
            UpdateComputerPassportStep(passport);
            LoadStorageSettingsIntoControls();
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

    private void BrowseModelsLocationButton_Click(object sender, RoutedEventArgs e)
    {
        BrowseFolderInto(ModelsPathInput);
    }

    private void BrowseResultsLocationButton_Click(object sender, RoutedEventArgs e)
    {
        BrowseFolderInto(ResultsPathInput);
    }

    private void AddModelsLocationButton_Click(object sender, RoutedEventArgs e)
    {
        AddOrUpdateLocation(_storageSettings.Models, ModelsLocationList, ModelsPathInput, ModelsLocationLimitInput);
        RefreshStorageLists();
    }

    private void AddResultsLocationButton_Click(object sender, RoutedEventArgs e)
    {
        AddOrUpdateLocation(_storageSettings.Results, ResultsLocationList, ResultsPathInput, ResultsLocationLimitInput);
        RefreshStorageLists();
    }

    private void RemoveModelsLocationButton_Click(object sender, RoutedEventArgs e)
    {
        RemoveSelectedLocation(_storageSettings.Models, ModelsLocationList, ModelsPathInput, ModelsLocationLimitInput);
        RefreshStorageLists();
    }

    private void RemoveResultsLocationButton_Click(object sender, RoutedEventArgs e)
    {
        RemoveSelectedLocation(_storageSettings.Results, ResultsLocationList, ResultsPathInput, ResultsLocationLimitInput);
        RefreshStorageLists();
    }

    private void MoveModelsLocationUpButton_Click(object sender, RoutedEventArgs e)
    {
        MoveSelectedLocation(_storageSettings.Models, ModelsLocationList, direction: -1);
        RefreshStorageLists();
    }

    private void MoveModelsLocationDownButton_Click(object sender, RoutedEventArgs e)
    {
        MoveSelectedLocation(_storageSettings.Models, ModelsLocationList, direction: 1);
        RefreshStorageLists();
    }

    private void MoveResultsLocationUpButton_Click(object sender, RoutedEventArgs e)
    {
        MoveSelectedLocation(_storageSettings.Results, ResultsLocationList, direction: -1);
        RefreshStorageLists();
    }

    private void MoveResultsLocationDownButton_Click(object sender, RoutedEventArgs e)
    {
        MoveSelectedLocation(_storageSettings.Results, ResultsLocationList, direction: 1);
        RefreshStorageLists();
    }

    private void ModelsLocationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        FillLocationInputs(_storageSettings.Models, ModelsLocationList, ModelsPathInput, ModelsLocationLimitInput);
    }

    private void ResultsLocationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        FillLocationInputs(_storageSettings.Results, ResultsLocationList, ResultsPathInput, ResultsLocationLimitInput);
    }

    private void SaveStorageSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SaveStorageSettingsFromControls();
        _storageSettingsStore.Save(_storageSettings);
        _appState.HasCompletedSetup = HasRequiredStorageSettings();
        _appStateStore.Save(_appState);
        UpdatePrimaryActionButton();
        UpdateStorageSteps();
        StatusText.Text = _appState.HasCompletedSetup
            ? $"Статус: настройка завершена. Настройки хранения сохранены в {AppDataPaths.StorageSettingsPath}."
            : "Статус: настройки сохранены, но для завершения нужно добавить хотя бы один адрес для моделей и один адрес для результатов.";
    }

    private void BackToStartButton_Click(object sender, RoutedEventArgs e)
    {
        SetupPage.Visibility = Visibility.Collapsed;
        WelcomePage.Visibility = Visibility.Visible;
        UpdateWelcomeStatus();
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

    private void UpdateWelcomeStatus()
    {
        StatusText.Text = _appState.HasCompletedSetup
            ? "Статус: настройка завершена. Можно начать работу или изменить параметры через Перенастроить."
            : "Статус: паспорт компьютера готов. Настройка пока не завершена.";
    }

    private bool HasRequiredStorageSettings()
    {
        return _storageSettings.Models.Locations.Count > 0
            && _storageSettings.Results.Locations.Count > 0;
    }

    private void ShowSetupPage(ComputerPassport passport)
    {
        PassportSummaryText.Text = BuildPassportSummary(passport);
        PassportPathText.Text = $"Файл паспорта: {AppDataPaths.ComputerPassportPath}";
        WelcomePage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Visible;
    }

    private void LoadStorageSettingsIntoControls()
    {
        ModelsTotalLimitInput.Text = FormatGb(_storageSettings.Models.TotalLimitGb);
        ModelsAllowOverflowCheckBox.IsChecked = _storageSettings.Models.AllowTemporaryOverflow;
        ModelsTemporaryOverflowInput.Text = FormatGb(_storageSettings.Models.TemporaryOverflowGb);

        ResultsTotalLimitInput.Text = FormatGb(_storageSettings.Results.TotalLimitGb);
        ResultsAllowOverflowCheckBox.IsChecked = _storageSettings.Results.AllowTemporaryOverflow;
        ResultsTemporaryOverflowInput.Text = FormatGb(_storageSettings.Results.TemporaryOverflowGb);

        RefreshStorageLists();
    }

    private void SaveStorageSettingsFromControls()
    {
        _storageSettings.Models.TotalLimitGb = ParseGb(ModelsTotalLimitInput.Text);
        _storageSettings.Models.AllowTemporaryOverflow = ModelsAllowOverflowCheckBox.IsChecked == true;
        _storageSettings.Models.TemporaryOverflowGb = ParseGb(ModelsTemporaryOverflowInput.Text);

        _storageSettings.Results.TotalLimitGb = ParseGb(ResultsTotalLimitInput.Text);
        _storageSettings.Results.AllowTemporaryOverflow = ResultsAllowOverflowCheckBox.IsChecked == true;
        _storageSettings.Results.TemporaryOverflowGb = ParseGb(ResultsTemporaryOverflowInput.Text);
    }

    private void RefreshStorageLists()
    {
        RefreshLocationList(ModelsLocationList, _storageSettings.Models);
        RefreshLocationList(ResultsLocationList, _storageSettings.Results);
    }

    private static void RefreshLocationList(System.Windows.Controls.ListBox listBox, StorageCategorySettings category)
    {
        var selectedIndex = listBox.SelectedIndex;
        listBox.ItemsSource = category.Locations
            .Select((location, index) => $"{index + 1}. {location.Path} — лимит {location.LimitGb:0.##} ГБ")
            .ToList();

        if (selectedIndex >= 0 && selectedIndex < category.Locations.Count)
        {
            listBox.SelectedIndex = selectedIndex;
        }
    }

    private void BrowseFolderInto(System.Windows.Controls.TextBox pathInput)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "Выберите папку хранения AI HUB",
            UseDescriptionForTitle = true
        };

        if (!string.IsNullOrWhiteSpace(pathInput.Text))
        {
            dialog.SelectedPath = pathInput.Text.Trim();
        }

        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            pathInput.Text = dialog.SelectedPath;
        }
    }

    private void AddOrUpdateLocation(
        StorageCategorySettings category,
        System.Windows.Controls.ListBox listBox,
        System.Windows.Controls.TextBox pathInput,
        System.Windows.Controls.TextBox limitInput)
    {
        var path = pathInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            StatusText.Text = "Статус: сначала укажите путь хранения.";
            return;
        }

        var limitGb = ParseGb(limitInput.Text);
        var selectedIndex = listBox.SelectedIndex;
        if (selectedIndex >= 0 && selectedIndex < category.Locations.Count)
        {
            category.Locations[selectedIndex].Path = path;
            category.Locations[selectedIndex].LimitGb = limitGb;
            return;
        }

        var existing = category.Locations.FirstOrDefault(location =>
            string.Equals(location.Path, path, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            existing.LimitGb = limitGb;
            return;
        }

        category.Locations.Add(new StorageLocationSettings
        {
            Path = path,
            LimitGb = limitGb
        });
    }

    private static void RemoveSelectedLocation(
        StorageCategorySettings category,
        System.Windows.Controls.ListBox listBox,
        System.Windows.Controls.TextBox pathInput,
        System.Windows.Controls.TextBox limitInput)
    {
        var selectedIndex = listBox.SelectedIndex;
        if (selectedIndex < 0 || selectedIndex >= category.Locations.Count)
        {
            return;
        }

        category.Locations.RemoveAt(selectedIndex);
        pathInput.Clear();
        limitInput.Clear();
    }

    private static void MoveSelectedLocation(StorageCategorySettings category, System.Windows.Controls.ListBox listBox, int direction)
    {
        var selectedIndex = listBox.SelectedIndex;
        var newIndex = selectedIndex + direction;
        if (selectedIndex < 0 || newIndex < 0 || newIndex >= category.Locations.Count)
        {
            return;
        }

        (category.Locations[selectedIndex], category.Locations[newIndex]) =
            (category.Locations[newIndex], category.Locations[selectedIndex]);
        listBox.SelectedIndex = newIndex;
    }

    private static void FillLocationInputs(
        StorageCategorySettings category,
        System.Windows.Controls.ListBox listBox,
        System.Windows.Controls.TextBox pathInput,
        System.Windows.Controls.TextBox limitInput)
    {
        var selectedIndex = listBox.SelectedIndex;
        if (selectedIndex < 0 || selectedIndex >= category.Locations.Count)
        {
            return;
        }

        var location = category.Locations[selectedIndex];
        pathInput.Text = location.Path;
        limitInput.Text = FormatGb(location.LimitGb);
    }

    private void UpdateStorageSteps()
    {
        ModelsStorageStepText.Text = BuildStorageSummary(_storageSettings.Models, "Выберем диск и лимит для будущих скачиваний.");
        ResultsStorageStepText.Text = BuildStorageSummary(_storageSettings.Results, "Отделим созданные файлы от моделей и кэша.");
    }

    private static string BuildStorageSummary(StorageCategorySettings category, string emptyText)
    {
        if (category.Locations.Count == 0)
        {
            return emptyText;
        }

        var defaultPath = category.Locations[0].Path;
        var additional = category.Locations.Skip(1).Take(2).Select(location => location.Path).ToList();
        var hiddenCount = Math.Max(0, category.Locations.Count - 1 - additional.Count);
        var additionalText = additional.Count == 0
            ? string.Empty
            : $" Дополнительно: {string.Join("; ", additional)}.";

        if (hiddenCount > 0)
        {
            additionalText += $" Ещё: {hiddenCount}.";
        }

        var overflowText = category.AllowTemporaryOverflow
            ? $"+{category.TemporaryOverflowGb:0.##} ГБ"
            : "выключено";

        return $"Настроено: {category.Locations.Count}. По умолчанию: {defaultPath}.{additionalText} Общий лимит: {category.TotalLimitGb:0.##} ГБ. Временное превышение: {overflowText}.";
    }

    private static double ParseGb(string text)
    {
        var normalized = text.Trim().Replace(',', '.');
        return double.TryParse(normalized, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? Math.Max(0, Math.Round(value, 2))
            : 0;
    }

    private static string FormatGb(double value)
    {
        return value <= 0 ? string.Empty : value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
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
            BuildGpuSummary(passport),
            drives);
    }

    private void UpdateComputerPassportStep(ComputerPassport passport)
    {
        ComputerPassportStepText.Text = string.Join(
            Environment.NewLine,
            "Сканирование ПК завершено. Найдено:",
            $"CPU: {passport.CpuName}",
            $"RAM: {passport.RamTotalGb:0.##} ГБ",
            BuildGpuSummary(passport),
            BuildDriveSummary(passport));
    }

    private static string BuildGpuSummary(ComputerPassport passport)
    {
        if (passport.Gpus.Count == 0)
        {
            return "GPU: не найдено; VRAM: unknown";
        }

        var gpuNames = string.Join(", ", passport.Gpus.Select(gpu => gpu.Name));
        var vramTotal = passport.Gpus.Sum(gpu => gpu.VramGb);
        var vramText = vramTotal > 0 ? $"{vramTotal:0.##} ГБ" : "unknown";

        return $"GPU: {gpuNames}; VRAM: {vramText}";
    }

    private static string BuildDriveSummary(ComputerPassport passport)
    {
        if (passport.Drives.Count == 0)
        {
            return "Диски: не найдены";
        }

        var totalFree = passport.Drives.Sum(drive => drive.FreeGb);
        return $"Диски: {passport.Drives.Count}, свободно {totalFree:0.##} ГБ";
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
