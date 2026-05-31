using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using AIHub.Models;
using AIHub.Services;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using Media = System.Windows.Media;

namespace AIHub;

public partial class MainWindow : Window
{
    private const int WmKeyDown = 0x0100;
    private const int VkF12 = 0x7B;

    private readonly AppSettingsStore _appSettingsStore = new();
    private readonly AppStateStore _appStateStore = new();
    private readonly ComputerPassportService _computerPassportService = new();
    private readonly CoreModelManager _coreModelManager = new();
    private readonly UserContextService _userContextService = new(new UserProfileStore(), new IpLocationService());
    private readonly LocalizationService _localizationService = new();
    private readonly StorageSettingsStore _storageSettingsStore = new();

    private AppSettings _appSettings = new();
    private AppState _appState = new();
    private ComputerPassport? _lastPassport;
    private StorageSettings _storageSettings = new();
    private CancellationTokenSource? _coreModelDownloadCts;
    private JsonlSessionLog? _coreSessionLog;
    private CoreModelCheckResult? _lastCoreModelCheck;
    private DebugChatWindow? _debugChatWindow;
    private bool _isApplyingLanguageSelection;
    private bool _isCoreModelPromptPostponed;
    private bool _isDarkTheme;

    public MainWindow()
    {
        InitializeComponent();
        Title = $"AI HUB {GetAppVersion()}";
        _isDarkTheme = IsWindowsAppThemeDark();
        SourceInitialized += (_, _) =>
        {
            ApplySystemTitleBarTheme();
            HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(WndProc);
        };
        PreviewKeyDown += MainWindow_PreviewKeyDown;
        InitializeLocalization();
        ApplyTheme();
        ApplyLocalization();
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
            var coreModelCheck = _coreModelManager.Check(_storageSettings);
            if (coreModelCheck.Availability != CoreModelAvailability.Installed)
            {
                ShowCoreModelPrompt(coreModelCheck);
                return;
            }

            ShowWorkStartPage();
            StatusText.Text = L("Status.WorkStartOpened");
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
            ? L("Theme.SwitchToLight")
            : L("Theme.SwitchToDark");

        ApplySystemTitleBarTheme();
        _debugChatWindow?.ApplyTheme(_isDarkTheme);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowSettingsPage();
        StatusText.Text = L("Status.SettingsOpened");
    }

    private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.F12)
        {
            return;
        }

        e.Handled = true;
        OpenDebugChatWindow();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmKeyDown && wParam.ToInt32() == VkF12)
        {
            handled = true;
            OpenDebugChatWindow();
        }

        return IntPtr.Zero;
    }

    private void OpenDebugChatWindow()
    {
        try
        {
            if (_debugChatWindow is not null)
            {
                _debugChatWindow.Activate();
                return;
            }

            _debugChatWindow = new DebugChatWindow(_localizationService, _storageSettings, _userContextService, _isDarkTheme)
            {
                Owner = this
            };
            _debugChatWindow.Closed += (_, _) => _debugChatWindow = null;
            _debugChatWindow.Show();
            StatusText.Text = L("Status.DebugChatOpened");
        }
        catch (Exception)
        {
            _debugChatWindow = null;
            StatusText.Text = L("Status.DebugChatOpenFailed");
        }
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

    private void InitializeLocalization()
    {
        _appSettings = _appSettingsStore.LoadOrCreate();
        if (!_appSettings.LanguageWasChosen)
        {
            var windowsLanguage = LocalizationService.GetWindowsLanguageCode();
            if (windowsLanguage == "ru")
            {
                _appSettings.LanguageCode = "ru";
            }
            else if (_localizationService.HasLanguage(windowsLanguage))
            {
                _localizationService.Load(windowsLanguage);
                var useWindowsLanguage = System.Windows.MessageBox.Show(
                    L("Dialog.UseWindowsLanguage"),
                    L("Dialog.LanguageTitle"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes;

                _appSettings.LanguageCode = useWindowsLanguage ? windowsLanguage : "ru";
            }
            else
            {
                _appSettings.LanguageCode = "ru";
            }

            _appSettings.LanguageWasChosen = true;
            _appSettingsStore.Save(_appSettings);
        }

        _localizationService.Load(_appSettings.LanguageCode);
        PopulateLanguageComboBox();
    }

    private string L(string key) => _localizationService.T(key);

    private string LF(string key, params object[] args) =>
        string.Format(System.Globalization.CultureInfo.InvariantCulture, L(key), args);

    private void ApplyLocalization()
    {
        HeaderProductNameText.Text = L("App.ProductName");
        HeaderSubtitleText.Text = L("App.Subtitle");
        SettingsButton.ToolTip = L("Header.SettingsTooltip");

        HomeWelcomeTitleText.Text = L("Home.Welcome");
        HomeHeadlineText.Text = L("Home.Headline");
        HomeDescriptionText.Text = L("Home.Description");
        WhatWillBeConfiguredTitleText.Text = L("Home.WhatWillBeConfigured");
        ModelsStorageTitleText.Text = L("Home.ModelsStorage");
        ResultsStorageTitleText.Text = L("Home.ResultsStorage");
        ComputerPassportTitleText.Text = L("Home.ComputerPassport");

        SetupTitleText.Text = L("Setup.Title");
        SetupDescriptionText.Text = L("Setup.Description");
        SetupPriorityHintText.Text = L("Setup.PriorityHint");
        SetupModelsTitleText.Text = L("Home.ModelsStorage");
        SetupModelsHelpText.Text = L("Setup.StorageHelp");
        SetupResultsTitleText.Text = L("Home.ResultsStorage");
        SetupResultsHelpText.Text = L("Setup.StorageHelp");
        SetupPassportTitleText.Text = L("Home.ComputerPassport");

        ModelsAddressLabelText.Text = L("Setup.AddressLabel");
        ResultsAddressLabelText.Text = L("Setup.AddressLabel");
        ModelsLimitLabelText.Text = L("Setup.LocationLimitLabel");
        ResultsLimitLabelText.Text = L("Setup.LocationLimitLabel");
        ModelsPathInput.ToolTip = L("Setup.AddressTooltip");
        ResultsPathInput.ToolTip = L("Setup.AddressTooltip");
        ModelsLocationLimitInput.ToolTip = L("Setup.LocationLimitTooltip");
        ResultsLocationLimitInput.ToolTip = L("Setup.LocationLimitTooltip");

        BrowseModelsLocationButton.Content = L("Setup.Browse");
        BrowseResultsLocationButton.Content = L("Setup.Browse");
        AddModelsLocationButton.Content = L("Setup.Add");
        AddResultsLocationButton.Content = L("Setup.Add");
        MoveModelsLocationUpButton.Content = L("Setup.MoveUp");
        MoveResultsLocationUpButton.Content = L("Setup.MoveUp");
        MoveModelsLocationDownButton.Content = L("Setup.MoveDown");
        MoveResultsLocationDownButton.Content = L("Setup.MoveDown");
        RemoveModelsLocationButton.Content = L("Setup.Delete");
        RemoveResultsLocationButton.Content = L("Setup.Delete");
        ModelsTotalLimitLabelText.Text = L("Setup.TotalLimitGb");
        ResultsTotalLimitLabelText.Text = L("Setup.TotalLimitGb");
        ModelsAllowOverflowCheckBox.Content = L("Setup.AllowTemporaryOverflow");
        ResultsAllowOverflowCheckBox.Content = L("Setup.AllowTemporaryOverflow");
        ModelsPlusGbText.Text = L("Setup.PlusGb");
        ResultsPlusGbText.Text = L("Setup.PlusGb");
        BackToStartButton.Content = L("Setup.Back");
        SaveStorageSettingsButton.Content = L("Setup.Save");

        SettingsTitleText.Text = L("Settings.Title");
        SettingsDescriptionText.Text = L("Settings.Description");
        SettingsLanguageTitleText.Text = L("Settings.LanguageTitle");
        SettingsLanguageHelpText.Text = L("Settings.LanguageHelp");
        SettingsLanguageLabelText.Text = L("Settings.LanguageLabel");
        SettingsLocalizationFolderText.Text = LF("Settings.LocalizationFolder", AppDataPaths.LocalizationDirectory);
        BackFromSettingsButton.Content = L("Settings.Back");

        WorkStartTitleText.Text = L("WorkStart.Title");
        WorkStartDescriptionText.Text = L("WorkStart.Description");
        NewProjectTitleText.Text = L("WorkStart.NewProject");
        ReasoningModeTitleText.Text = L("WorkStart.ReasoningTitle");
        ReasoningModeDescriptionText.Text = L("WorkStart.ReasoningDescription");
        SelectReasoningModeButton.Content = L("WorkStart.SelectMode");
        PreviousWorkHeaderText.Text = L("WorkStart.PreviousWork");
        PreviousWorkExampleTitleText.Text = L("WorkStart.PreviousExampleTitle");
        PreviousWorkExampleNameText.Text = L("WorkStart.PreviousExampleName");
        PreviousWorkExampleDateText.Text = L("WorkStart.PreviousExampleDate");
        ContinuePreviousWorkButton.Content = L("WorkStart.Continue");
        ContinuePreviousWorkButton.ToolTip = L("WorkStart.ContinueTooltip");
        BackFromWorkStartButton.Content = L("Settings.Back");

        DownloadCoreModelButton.Content = L("CoreModel.Download");
        OpenSetupFromCoreModelButton.Content = L("CoreModel.OpenSetup");
        PostponeCoreModelButton.Content = L("CoreModel.Later");
        PauseCoreModelDownloadButton.Content = L("CoreModel.Pause");
        CancelCoreModelDownloadButton.Content = L("CoreModel.Cancel");

        ApplyTheme();
        UpdatePrimaryActionButton();
        UpdateStorageSteps();
        if (_lastPassport is not null)
        {
            UpdateComputerPassportStep(_lastPassport);
            PassportSummaryText.Text = BuildPassportSummary(_lastPassport);
            PassportPathText.Text = LF("Setup.PassportPath", AppDataPaths.ComputerPassportPath);
        }

        if (WelcomePage.Visibility == Visibility.Visible)
        {
            UpdateWelcomeStatus();
        }
    }

    private void PopulateLanguageComboBox()
    {
        _isApplyingLanguageSelection = true;
        var languages = _localizationService.GetAvailableLanguages();
        LanguageComboBox.ItemsSource = languages;
        LanguageComboBox.SelectedItem = languages.FirstOrDefault(language =>
            string.Equals(language.Code, _localizationService.CurrentLanguageCode, StringComparison.OrdinalIgnoreCase));
        _isApplyingLanguageSelection = false;
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isApplyingLanguageSelection || LanguageComboBox.SelectedItem is not LanguageOption language)
        {
            return;
        }

        _appSettings.LanguageCode = language.Code;
        _appSettings.LanguageWasChosen = true;
        _appSettingsStore.Save(_appSettings);
        _localizationService.Load(language.Code);
        PopulateLanguageComboBox();
        ApplyLocalization();
        StatusText.Text = L("Status.LanguageSaved");
    }

    private void InitializeAppData()
    {
        try
        {
            _appState = _appStateStore.LoadOrCreate();
            _storageSettings = _storageSettingsStore.LoadOrCreate();
            StartCoreSessionLog();
            _ = InitializeUserContextAsync();
            var passport = _computerPassportService.RegeneratePassport();
            _lastPassport = passport;
            SavePassportState(passport);
            UpdateComputerPassportStep(passport);
            LoadStorageSettingsIntoControls();
            UpdateStorageSteps();
            UpdateWelcomeStatus();
            EvaluateCoreModelOnStartup();
        }
        catch
        {
            StatusText.Text = L("Status.PassportMissing");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        try
        {
            _coreSessionLog?.Write("session_end");
            _coreSessionLog?.Dispose();
        }
        finally
        {
            base.OnClosed(e);
        }
    }

    private void StartCoreSessionLog()
    {
        try
        {
            _coreSessionLog = JsonlSessionLog.CreateCore(_storageSettings);
            _coreSessionLog.Write("session_start", new
            {
                AppVersion = GetAppVersion(),
                _coreSessionLog.FilePath
            });
            _coreSessionLog.Write("context_snapshot", _userContextService.CreateSnapshot());
        }
        catch
        {
            _coreSessionLog = null;
        }
    }

    private async Task InitializeUserContextAsync()
    {
        try
        {
            await _userContextService.InitializeAsync(CancellationToken.None);
            _coreSessionLog?.Write("context_snapshot", _userContextService.CreateSnapshot());
        }
        catch
        {
            // User context must never block the app startup.
        }
    }

    private void OpenSetupWindow(bool regeneratePassport)
    {
        try
        {
            var passport = regeneratePassport
                ? _computerPassportService.RegeneratePassport()
                : _computerPassportService.EnsurePassport();

            _lastPassport = passport;
            SavePassportState(passport);
            UpdateComputerPassportStep(passport);
            LoadStorageSettingsIntoControls();
            ShowSetupPage(passport);

            StatusText.Text = regeneratePassport
                ? L("Status.PassportRegenerated")
                : L("Status.SetupOpened");
        }
        catch
        {
            StatusText.Text = L("Status.SetupOpenFailed");
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
            ? LF("Status.StorageSavedComplete", AppDataPaths.StorageSettingsPath)
            : L("Status.StorageSavedIncomplete");

        if (_appState.HasCompletedSetup)
        {
            _isCoreModelPromptPostponed = false;
            EvaluateCoreModelAfterStorageSave();
        }
    }

    private void BackToStartButton_Click(object sender, RoutedEventArgs e)
    {
        SetupPage.Visibility = Visibility.Collapsed;
        WelcomePage.Visibility = Visibility.Visible;
        SettingsPage.Visibility = Visibility.Collapsed;
        WorkStartPage.Visibility = Visibility.Collapsed;
        UpdateWelcomeStatus();
    }

    private void BackFromSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsPage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Collapsed;
        WorkStartPage.Visibility = Visibility.Collapsed;
        WelcomePage.Visibility = Visibility.Visible;
        UpdateWelcomeStatus();
    }

    private void BackFromWorkStartButton_Click(object sender, RoutedEventArgs e)
    {
        WorkStartPage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
        WelcomePage.Visibility = Visibility.Visible;
        UpdateWelcomeStatus();
    }

    private void SelectReasoningModeButton_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = L("Status.ReasoningModeNotReady");
    }

    private void PreviousWorkExpander_Expanded(object sender, RoutedEventArgs e)
    {
        StatusText.Text = L("Status.PreviousWorkExpanded");
    }

    private void PreviousWorkExpander_Collapsed(object sender, RoutedEventArgs e)
    {
        StatusText.Text = L("Status.WorkStartOpened");
    }

    private async void DownloadCoreModelButton_Click(object sender, RoutedEventArgs e)
    {
        await StartCoreModelDownloadAsync();
    }

    private void OpenSetupFromCoreModelButton_Click(object sender, RoutedEventArgs e)
    {
        HideCoreModelPrompt();
        OpenSetupWindow(regeneratePassport: false);
    }

    private void PostponeCoreModelButton_Click(object sender, RoutedEventArgs e)
    {
        _isCoreModelPromptPostponed = true;
        HideCoreModelPrompt();
        StatusText.Text = L("Status.CoreModelPostponed");
    }

    private void PauseCoreModelDownloadButton_Click(object sender, RoutedEventArgs e)
    {
        _coreModelDownloadCts?.Cancel();
        StatusText.Text = L("Status.CoreModelPauseRequested");
    }

    private void CancelCoreModelDownloadButton_Click(object sender, RoutedEventArgs e)
    {
        _coreModelDownloadCts?.Cancel();
        StatusText.Text = L("Status.CoreModelCancelRequested");
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
            ? L("Home.StartWork")
            : L("Home.StartSetup");
        ReconfigureButton.Content = L("Home.Reconfigure");
    }

    private void UpdateWelcomeStatus()
    {
        StatusText.Text = _appState.HasCompletedSetup
            ? L("Status.PassportReadySetupComplete")
            : L("Status.PassportReadySetupIncomplete");
    }

    private void EvaluateCoreModelOnStartup()
    {
        var result = _coreModelManager.Check(_storageSettings);
        _lastCoreModelCheck = result;
        if (result.Availability != CoreModelAvailability.Installed && !_isCoreModelPromptPostponed)
        {
            ShowCoreModelPrompt(result);
        }
    }

    private void EvaluateCoreModelAfterStorageSave()
    {
        var result = _coreModelManager.Check(_storageSettings);
        _lastCoreModelCheck = result;
        if (result.Availability == CoreModelAvailability.Installed)
        {
            HideCoreModelPrompt();
            StatusText.Text = L("Status.CoreModelReady");
            return;
        }

        ShowCoreModelPrompt(result);
    }

    private void ShowCoreModelPrompt(CoreModelCheckResult result)
    {
        _lastCoreModelCheck = result;
        CoreModelPromptPanel.Visibility = Visibility.Visible;
        CoreModelDownloadPanel.Visibility = Visibility.Collapsed;
        DownloadCoreModelButton.IsEnabled = CanDownloadCoreModel(result);
        CoreModelPromptText.Text = BuildCoreModelPromptText(result);
        StatusText.Text = result.Availability switch
        {
            CoreModelAvailability.StorageNotConfigured => L("Status.CoreModelStorageNotConfigured"),
            CoreModelAvailability.ModelsFolderUnavailable => L("Status.CoreModelFolderUnavailable"),
            CoreModelAvailability.Partial => L("Status.CoreModelPartial"),
            CoreModelAvailability.Invalid => L("Status.CoreModelInvalid"),
            _ => L("Status.CoreModelMissing")
        };
    }

    private string BuildCoreModelPromptText(CoreModelCheckResult result)
    {
        if (!result.HasEnoughSpace)
        {
            return LF("CoreModel.NotEnoughSpacePrompt", FormatBytes(CoreModelManager.CoreModelTotalBytes), FormatBytes(CoreModelManager.RecommendedFreeBytes));
        }

        return result.Availability switch
        {
            CoreModelAvailability.StorageNotConfigured => L("CoreModel.StorageNotConfiguredPrompt"),
            CoreModelAvailability.ModelsFolderUnavailable => L("CoreModel.FolderUnavailablePrompt"),
            CoreModelAvailability.Partial => LF("CoreModel.PartialPrompt", FormatBytes(result.ExistingBytes), FormatBytes(CoreModelManager.CoreModelTotalBytes)),
            CoreModelAvailability.Invalid => L("CoreModel.InvalidPrompt"),
            _ => L("CoreModel.MissingPrompt")
        };
    }

    private static bool CanDownloadCoreModel(CoreModelCheckResult result)
    {
        return result.HasEnoughSpace
            && result.Availability is CoreModelAvailability.Missing
                or CoreModelAvailability.Partial
                or CoreModelAvailability.Invalid;
    }

    private void HideCoreModelPrompt()
    {
        CoreModelPromptPanel.Visibility = Visibility.Collapsed;
    }

    private async Task StartCoreModelDownloadAsync()
    {
        if (_coreModelDownloadCts is not null)
        {
            return;
        }

        var check = _coreModelManager.Check(_storageSettings);
        if (!CanDownloadCoreModel(check))
        {
            ShowCoreModelPrompt(check);
            return;
        }

        _coreModelDownloadCts = new CancellationTokenSource();
        DownloadCoreModelButton.IsEnabled = false;
        CoreModelPromptPanel.Visibility = Visibility.Collapsed;
        CoreModelDownloadPanel.Visibility = Visibility.Visible;
        CoreModelDownloadProgressBar.Value = 0;
        CoreModelDownloadTitleText.Text = LF(
            "CoreModel.DownloadProgress",
            0,
            FormatBytes(check.ExistingBytes),
            FormatBytes(CoreModelManager.CoreModelTotalBytes),
            FormatBytes(0) + L("Units.PerSecond"));
        StatusText.Text = L("Status.CoreModelDownloadStarted");

        var progress = new Progress<CoreModelDownloadProgress>(UpdateCoreModelDownloadProgress);

        try
        {
            await _coreModelManager.DownloadAsync(_storageSettings, progress, _coreModelDownloadCts.Token);
            CoreModelDownloadPanel.Visibility = Visibility.Collapsed;
            HideCoreModelPrompt();
            StatusText.Text = L("Status.CoreModelInstalled");
        }
        catch (OperationCanceledException)
        {
            CoreModelDownloadPanel.Visibility = Visibility.Collapsed;
            var result = _coreModelManager.Check(_storageSettings);
            ShowCoreModelPrompt(result);
            StatusText.Text = L("Status.CoreModelDownloadPaused");
        }
        catch (Exception)
        {
            CoreModelDownloadPanel.Visibility = Visibility.Collapsed;
            var result = _coreModelManager.Check(_storageSettings);
            ShowCoreModelPrompt(result);
            StatusText.Text = L("Status.CoreModelDownloadFailed");
        }
        finally
        {
            _coreModelDownloadCts?.Dispose();
            _coreModelDownloadCts = null;
            DownloadCoreModelButton.IsEnabled = true;
        }
    }

    private void UpdateCoreModelDownloadProgress(CoreModelDownloadProgress progress)
    {
        if (progress.Stage == "verifying")
        {
            CoreModelDownloadTitleText.Text = L("CoreModel.Verifying");
            CoreModelDownloadProgressBar.Value = 100;
            return;
        }

        if (progress.Stage == "installed")
        {
            CoreModelDownloadTitleText.Text = L("CoreModel.Installed");
            CoreModelDownloadProgressBar.Value = 100;
            return;
        }

        var percent = progress.TotalBytes <= 0
            ? 0
            : Math.Clamp(progress.DownloadedBytes * 100d / progress.TotalBytes, 0, 100);

        CoreModelDownloadProgressBar.Value = percent;
        CoreModelDownloadTitleText.Text = LF(
            "CoreModel.DownloadProgress",
            percent.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
            FormatBytes(progress.DownloadedBytes),
            FormatBytes(progress.TotalBytes),
            FormatBytes(progress.BytesPerSecond) + L("Units.PerSecond"));
    }

    private bool HasRequiredStorageSettings()
    {
        return _storageSettings.Models.Locations.Count > 0
            && _storageSettings.Results.Locations.Count > 0;
    }

    private void ShowSetupPage(ComputerPassport passport)
    {
        PassportSummaryText.Text = BuildPassportSummary(passport);
        PassportPathText.Text = LF("Setup.PassportPath", AppDataPaths.ComputerPassportPath);
        WelcomePage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
        WorkStartPage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Visible;
    }

    private void ShowSettingsPage()
    {
        WelcomePage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Collapsed;
        WorkStartPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Visible;
        PopulateLanguageComboBox();
    }

    private void ShowWorkStartPage()
    {
        PreviousWorkExpander.IsExpanded = false;
        WelcomePage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
        WorkStartPage.Visibility = Visibility.Visible;
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

    private void RefreshLocationList(System.Windows.Controls.ListBox listBox, StorageCategorySettings category)
    {
        var selectedIndex = listBox.SelectedIndex;
        listBox.ItemsSource = category.Locations
            .Select((location, index) => LF("Storage.ListItem", index + 1, location.Path, FormatGbForText(location.LimitGb)))
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
            Description = L("FolderDialog.Description"),
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
            StatusText.Text = L("Status.PathRequired");
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
        ModelsStorageStepText.Text = BuildStorageSummary(_storageSettings.Models, L("Home.ModelsStorageEmpty"));
        ResultsStorageStepText.Text = BuildStorageSummary(_storageSettings.Results, L("Home.ResultsStorageEmpty"));
    }

    private string BuildStorageSummary(StorageCategorySettings category, string emptyText)
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
            : LF("Storage.SummaryAdditional", string.Join("; ", additional));

        if (hiddenCount > 0)
        {
            additionalText += LF("Storage.SummaryHidden", hiddenCount);
        }

        var overflowText = category.AllowTemporaryOverflow
            ? $"+{FormatGbForText(category.TemporaryOverflowGb)} {L("Units.Gb")}"
            : L("Storage.OverflowOff");

        return LF(
            "Storage.SummaryConfigured",
            category.Locations.Count,
            defaultPath,
            additionalText,
            FormatGbForText(category.TotalLimitGb),
            overflowText);
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

    private static string FormatGbForText(double value)
    {
        return Math.Max(0, value).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }

    private string FormatBytes(double bytes)
    {
        string[] units = [L("Units.Bytes"), L("Units.Kb"), L("Units.Mb"), L("Units.Gb")];
        var value = Math.Max(0, bytes);
        var unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }

    private string BuildPassportSummary(ComputerPassport passport)
    {
        var drives = passport.Drives.Count == 0
            ? L("Passport.DrivesUnknown")
            : LF("Passport.DrivesFound", passport.Drives.Count);

        return string.Join(
            Environment.NewLine,
            LF("Passport.Analysis", passport.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss")),
            LF("Passport.Computer", passport.MachineName),
            LF("Passport.Windows", passport.WindowsVersion),
            LF("Passport.Cpu", passport.CpuName),
            LF("Passport.Ram", FormatGbForText(passport.RamTotalGb)),
            BuildGpuSummary(passport),
            drives);
    }

    private void UpdateComputerPassportStep(ComputerPassport passport)
    {
        ComputerPassportStepText.Text = string.Join(
            Environment.NewLine,
            L("Passport.ScanComplete"),
            LF("Passport.Cpu", passport.CpuName),
            LF("Passport.Ram", FormatGbForText(passport.RamTotalGb)),
            BuildGpuSummary(passport),
            BuildDriveSummary(passport));
    }

    private string BuildGpuSummary(ComputerPassport passport)
    {
        if (passport.Gpus.Count == 0)
        {
            return L("Passport.GpuMissing");
        }

        var gpuNames = string.Join(", ", passport.Gpus.Select(gpu => gpu.Name));
        var vramTotal = passport.Gpus.Sum(gpu => gpu.VramGb);
        var vramText = vramTotal > 0 ? $"{FormatGbForText(vramTotal)} {L("Units.Gb")}" : "unknown";

        return LF("Passport.GpuFound", gpuNames, vramText);
    }

    private string BuildDriveSummary(ComputerPassport passport)
    {
        if (passport.Drives.Count == 0)
        {
            return L("Passport.DrivesMissing");
        }

        var totalFree = passport.Drives.Sum(drive => drive.FreeGb);
        return LF("Passport.DrivesFree", passport.Drives.Count, FormatGbForText(totalFree));
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
