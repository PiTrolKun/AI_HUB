using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
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
    private readonly UserProfileStore _userProfileStore = new();
    private readonly UserContextService _userContextService;
    private readonly LocalizationService _localizationService = new();
    private readonly StorageSettingsStore _storageSettingsStore = new();
    private readonly ToolModelManager _toolModelManager = new();
    private readonly ChoiceScenarioService _choiceScenarioService = new();
    private readonly ChoiceScenarioOrchestrator _choiceScenarioOrchestrator;
    private readonly DebugModelDiscoveryService _choiceModelDiscoveryService = new();
    private readonly CapabilityInventoryService _choiceInventoryService = new();
    private readonly HuggingFaceCatalogStartupService _catalogStartupService = new();
    private readonly CoreSpeechPresentationCoordinator _coreSpeechCoordinator;
    private readonly DispatcherTimer _profileBlinkTimer = new();
    private readonly ObservableCollection<ChoiceScenarioOption> _choiceScenarioOptions = [];

    private AppSettings _appSettings = new();
    private AppState _appState = new();
    private ComputerPassport? _lastPassport;
    private StorageSettings _storageSettings = new();
    private UserProfile _userProfile = new();
    private CancellationTokenSource? _coreModelDownloadCts;
    private JsonlSessionLog? _coreSessionLog;
    private CoreModelCheckResult? _lastCoreModelCheck;
    private PendingModelDownload _pendingModelDownload = PendingModelDownload.Core;
    private DebugChatWindow? _debugChatWindow;
    private CoreMemoryStatus _coreMemoryStatus = CoreMemoryStatus.Inactive();
    private readonly ChoiceScenarioSessionState _choiceScenarioState = new();
    private ChoiceScenarioStep? _currentChoiceScenarioStep;
    private LlamaServerRuntimeService? _choiceScenarioRuntimeService;
    private CancellationTokenSource? _choiceScenarioCts;
    private CancellationTokenSource? _coreSpeechCts;
    private readonly CancellationTokenSource _catalogStartupCts = new();
    private ISessionEventLog? _choiceScenarioLog;
    private int _choiceScenarioInvalidJsonCount;
    private bool _choiceScenarioRequestInProgress;
    private bool _isApplyingLanguageSelection;
    private bool _isApplyingCoreVoiceSettings = true;
    private bool _coreSpeechPresentationActive;
    private long _coreSpeechPresentationId;
    private bool _isCoreModelPromptPostponed;
    private bool _isDarkTheme;

    private enum PendingModelDownload
    {
        Core,
        Reranker
    }

    public MainWindow()
    {
        _userContextService = new UserContextService(_userProfileStore, new IpLocationService());
        _choiceScenarioOrchestrator = new ChoiceScenarioOrchestrator(_choiceScenarioService);
        InitializeComponent();
        Title = $"AI HUB {GetAppVersion()}";
        _profileBlinkTimer.Interval = TimeSpan.FromMilliseconds(760);
        _profileBlinkTimer.Tick += ProfileBlinkTimer_Tick;
        _isDarkTheme = IsWindowsAppThemeDark();
        SourceInitialized += (_, _) =>
        {
            ApplySystemTitleBarTheme();
            HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(WndProc);
        };
        PreviewKeyDown += MainWindow_PreviewKeyDown;
        InitializeLocalization();
        _coreSpeechCoordinator = new CoreSpeechPresentationCoordinator(
            new CoreVoiceEngineRouter(new EspeakCoreVoiceEngine(), new RhVoiceCoreVoiceEngine()));
        ApplyTheme();
        ApplyLocalization();
        ChoiceOptionsItemsControl.ItemsSource = _choiceScenarioOptions;
        InitializeAppData();
        LoadCoreVoiceSettingsIntoControls();
        UpdateCoreVoiceControls();
        _ = RefreshModelCatalogOnStartupAsync();
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

            if (!_userProfile.IsComplete())
            {
                ShowProfileReminderPage();
                StatusText.Text = L("Status.ProfileReminderOpened");
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
        ProfileButton.Foreground = (Media.Brush)Resources["TextPrimaryBrush"];
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

    private void ProfileButton_Click(object sender, RoutedEventArgs e)
    {
        LoadProfileIntoControls();
        ShowProfilePage();
        StatusText.Text = _userProfile.IsComplete()
            ? L("Status.ProfileOpened")
            : L("Status.ProfileIncomplete");
    }

    private void ProfileBlinkTimer_Tick(object? sender, EventArgs e)
    {
        ProfileButton.Opacity = ProfileButton.Opacity < 0.75 ? 1.0 : 0.48;
    }

    private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape && _coreSpeechPresentationActive)
        {
            e.Handled = true;
            CancelCoreSpeech(revealFullText: true, "keyboard_skip");
            return;
        }

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
            _debugChatWindow.CoreMemoryStatusChanged += DebugChatWindow_CoreMemoryStatusChanged;
            _debugChatWindow.Closed += (_, _) =>
            {
                _debugChatWindow = null;
                UpdateCoreMemoryIndicator(CoreMemoryStatus.Inactive());
            };
            _debugChatWindow.Show();
            StatusText.Text = L("Status.DebugChatOpened");
        }
        catch (Exception)
        {
            _debugChatWindow = null;
            StatusText.Text = L("Status.DebugChatOpenFailed");
        }
    }

    private void DebugChatWindow_CoreMemoryStatusChanged(CoreMemoryStatus status)
    {
        Dispatcher.Invoke(() => UpdateCoreMemoryIndicator(status));
    }

    private void UpdateCoreMemoryIndicator(CoreMemoryStatus status)
    {
        _coreMemoryStatus = status;
        CoreMemoryProgressBar.IsIndeterminate = status.IsCompressing;
        CoreMemoryProgressBar.Value = status.IsCompressing ? 0 : status.FillPercent;
        CoreMemoryIndicatorPanel.Opacity = status.IsActive ? 1.0 : 0.42;
        CoreMemoryIconText.Text = status.IsNearFull ? "🤯" : "🧠";
        CoreMemoryIconText.Foreground = status.IsActive
            ? status.IsNearFull
                ? new Media.SolidColorBrush((Media.Color)Media.ColorConverter.ConvertFromString("#F97316"))
                : new Media.SolidColorBrush((Media.Color)Media.ColorConverter.ConvertFromString("#38BDF8"))
            : (Media.Brush)Resources["TextSecondaryBrush"];

        if (!status.IsActive)
        {
            CoreMemoryIndicatorPanel.ToolTip = L("CoreMemory.TooltipInactive");
            return;
        }

        CoreMemoryIndicatorPanel.ToolTip = status.IsCompressing
            ? L("CoreMemory.TooltipCompressing")
            : status.HasCompressedSummary
                ? L("CoreMemory.TooltipCompressed")
                : L("CoreMemory.TooltipReady");
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
        _appSettings.CoreVoice ??= new CoreVoiceSettings();
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
        ProfileButton.ToolTip = L("Header.ProfileTooltip");
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
        SettingsCoreVoiceTitleText.Text = L("Settings.CoreVoiceTitle");
        SettingsCoreVoiceHelpText.Text = L("Settings.CoreVoiceHelp");
        SettingsCoreVoiceProviderText.Text = L("Settings.CoreVoiceProvider");
        CoreVoiceEnabledCheckBox.Content = L("Settings.CoreVoiceEnabled");
        SettingsCoreVoiceVolumeText.Text = L("Settings.CoreVoiceVolume");
        SettingsCoreVoiceRateText.Text = L("Settings.CoreVoiceRate");
        CoreVoiceTestButton.Content = L("Settings.CoreVoiceTest");
        PopulateCoreVoiceProviderComboBox();
        BackFromSettingsButton.Content = L("Settings.Back");

        ProfileTitleText.Text = L("Profile.Title");
        ProfileDescriptionText.Text = L("Profile.Description");
        ProfileIdentityTitleText.Text = L("Profile.IdentityTitle");
        ProfileIdentityHelpText.Text = L("Profile.IdentityHelp");
        ProfileLocationTitleText.Text = L("Profile.LocationTitle");
        ProfileLocationHelpText.Text = L("Profile.LocationHelp");
        ProfileCityLabelText.Text = L("Profile.City");
        ProfileRegionLabelText.Text = L("Profile.Region");
        ProfileCountryLabelText.Text = L("Profile.Country");
        ProfileTimezoneLabelText.Text = L("Profile.Timezone");
        ProfileAnswerPreferencesTitleText.Text = L("Profile.AnswerPreferencesTitle");
        ProfileAnswerPreferencesHelpText.Text = L("Profile.AnswerPreferencesHelp");
        PreferenceConciseCheckBox.Content = L("Profile.PreferenceConcise");
        PreferenceDetailedCheckBox.Content = L("Profile.PreferenceDetailed");
        PreferenceSimpleCheckBox.Content = L("Profile.PreferenceSimple");
        PreferenceStepsCheckBox.Content = L("Profile.PreferenceSteps");
        PreferenceExamplesCheckBox.Content = L("Profile.PreferenceExamples");
        PreferenceSourcesCheckBox.Content = L("Profile.PreferenceSources");
        PreferenceRisksCheckBox.Content = L("Profile.PreferenceRisks");
        ProfileWorkloadTitleText.Text = L("Profile.WorkloadTitle");
        ProfileWorkloadHelpText.Text = L("Profile.WorkloadHelp");
        WorkloadLightTitleText.Text = L("Profile.WorkloadLightTitle");
        WorkloadLightDescriptionText.Text = L("Profile.WorkloadLightDescription");
        WorkloadBalancedTitleText.Text = L("Profile.WorkloadBalancedTitle");
        WorkloadBalancedDescriptionText.Text = L("Profile.WorkloadBalancedDescription");
        WorkloadExtremeTitleText.Text = L("Profile.WorkloadExtremeTitle");
        WorkloadExtremeDescriptionText.Text = L("Profile.WorkloadExtremeDescription");
        BackFromProfileButton.Content = L("Settings.Back");
        SaveProfileButton.Content = L("Setup.Save");
        ProfileReminderTitleText.Text = L("Profile.ReminderTitle");
        ProfileReminderDescriptionText.Text = L("Profile.ReminderDescription");
        BackFromProfileReminderButton.Content = L("Settings.Back");
        ContinueWithoutProfileButton.Content = L("Profile.ContinueWithoutProfile");
        FillProfileFromReminderButton.Content = L("Profile.FillProfile");

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
        ChoiceScenarioTitleText.Text = L("ChoiceScenario.Title");
        ChoiceScenarioDescriptionText.Text = L("ChoiceScenario.Description");
        ChoiceScenarioCoreThoughtTitleText.Text = L("ChoiceScenario.CoreThoughtTitle");
        UpdateCoreVoiceControls();
        ChoiceCustomOptionButton.Content = L("ChoiceScenario.CustomOption");
        ChoiceCustomInputHelpText.Text = L("ChoiceScenario.CustomInputHelp");
        ChoiceCustomSubmitButton.Content = L("ChoiceScenario.AcceptCustom");
        ChoiceGoFinalButton.Content = L("ChoiceScenario.GoFinal");
        ChoiceScenarioSummaryTitleText.Text = L("ChoiceScenario.TaskCardTitle");
        BackFromChoiceScenarioButton.Content = L("Settings.Back");
        CancelChoiceScenarioButton.Content = L("ChoiceScenario.Cancel");

        DownloadCoreModelButton.Content = L("CoreModel.Download");
        OpenSetupFromCoreModelButton.Content = L("CoreModel.OpenSetup");
        PostponeCoreModelButton.Content = L("CoreModel.Later");
        PauseCoreModelDownloadButton.Content = L("CoreModel.Pause");
        CancelCoreModelDownloadButton.Content = L("CoreModel.Cancel");
        UpdateCoreMemoryIndicator(_coreMemoryStatus);

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

    private void LoadCoreVoiceSettingsIntoControls()
    {
        _isApplyingCoreVoiceSettings = true;
        try
        {
            _appSettings.CoreVoice ??= new CoreVoiceSettings();
            if (_appSettings.CoreVoice.Provider is not CoreVoiceSettings.EspeakProvider
                and not CoreVoiceSettings.RhVoiceProvider)
            {
                _appSettings.CoreVoice.Provider = CoreVoiceSettings.EspeakProvider;
            }

            PopulateCoreVoiceProviderComboBox();
            CoreVoiceEnabledCheckBox.IsChecked = _appSettings.CoreVoice.Enabled;
            CoreVoiceVolumeSlider.Value = Math.Clamp(_appSettings.CoreVoice.Volume, 0, 200);
            CoreVoiceRateSlider.Value = Math.Clamp(_appSettings.CoreVoice.Rate, 80, 240);
        }
        finally
        {
            _isApplyingCoreVoiceSettings = false;
        }
    }

    private void PopulateCoreVoiceProviderComboBox()
    {
        var wasApplying = _isApplyingCoreVoiceSettings;
        _isApplyingCoreVoiceSettings = true;
        try
        {
            var providers = new[]
            {
                new CoreVoiceProviderOption(CoreVoiceSettings.EspeakProvider, L("Settings.CoreVoiceProviderEspeak")),
                new CoreVoiceProviderOption(CoreVoiceSettings.RhVoiceProvider, L("Settings.CoreVoiceProviderRhVoice"))
            };
            CoreVoiceProviderComboBox.ItemsSource = providers;
            CoreVoiceProviderComboBox.SelectedItem = providers.First(provider =>
                string.Equals(provider.Id, _appSettings.CoreVoice.Provider, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _isApplyingCoreVoiceSettings = wasApplying;
        }
    }

    private void CoreVoiceProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isApplyingCoreVoiceSettings
            || CoreVoiceProviderComboBox.SelectedItem is not CoreVoiceProviderOption provider)
        {
            return;
        }

        CancelCoreSpeech(revealFullText: true, "voice_provider_changed");
        _appSettings.CoreVoice.Provider = provider.Id;
        _appSettingsStore.Save(_appSettings);
        UpdateCoreVoiceControls();
        StatusText.Text = LF("Status.CoreVoiceProviderSaved", provider.DisplayName);
    }

    private void CoreVoiceSettingChanged(object sender, RoutedEventArgs e)
    {
        if (_isApplyingCoreVoiceSettings)
        {
            return;
        }

        _appSettings.CoreVoice.Enabled = CoreVoiceEnabledCheckBox.IsChecked == true;
        _appSettingsStore.Save(_appSettings);
        if (!_appSettings.CoreVoice.Enabled)
        {
            CancelCoreSpeech(revealFullText: true, "settings_disabled");
        }

        UpdateCoreVoiceControls();
    }

    private void CoreVoiceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isApplyingCoreVoiceSettings)
        {
            return;
        }

        _appSettings.CoreVoice.Volume = (int)Math.Round(CoreVoiceVolumeSlider.Value);
        _appSettings.CoreVoice.Rate = (int)Math.Round(CoreVoiceRateSlider.Value);
        _appSettingsStore.Save(_appSettings);
        UpdateCoreVoiceControls();
    }

    private async void CoreVoiceTestButton_Click(object sender, RoutedEventArgs e)
    {
        CancelCoreSpeech(revealFullText: true, "settings_test");
        CoreVoiceTestButton.IsEnabled = false;
        try
        {
            var request = new CoreSpeechRequest(
                [new CoreSpeechSegment("test", L("Settings.CoreVoiceTestPhrase"))],
                _appSettings.LanguageCode,
                _appSettings.CoreVoice,
                "settings_test");
            var progress = new Progress<CoreSpeechProgress>(_ => { });
            var result = await _coreSpeechCoordinator.PresentAsync(
                request,
                progress,
                _coreSessionLog,
                CancellationToken.None);
            StatusText.Text = result.Completed
                ? L("Status.CoreVoiceTestCompleted")
                : L("Status.CoreVoiceUnavailable");
        }
        finally
        {
            CoreVoiceTestButton.IsEnabled = true;
        }
    }

    private void UpdateCoreVoiceControls()
    {
        if (!IsInitialized)
        {
            return;
        }

        var enabled = _appSettings.CoreVoice.Enabled;
        CoreVoiceVolumeSlider.IsEnabled = enabled;
        CoreVoiceRateSlider.IsEnabled = enabled;
        CoreVoiceVolumeValueText.Text = _appSettings.CoreVoice.Volume.ToString(System.Globalization.CultureInfo.InvariantCulture);
        CoreVoiceRateValueText.Text = _appSettings.CoreVoice.Rate.ToString(System.Globalization.CultureInfo.InvariantCulture);
        CoreVoiceToggleButton.Content = enabled ? "🔊" : "🔇";
        CoreVoiceToggleButton.ToolTip = enabled
            ? L("ChoiceScenario.CoreVoiceDisable")
            : L("ChoiceScenario.CoreVoiceEnable");
        CoreVoiceProviderComboBox.IsEnabled = enabled;
        CoreVoiceProviderComboBox.ToolTip = string.Equals(
                _appSettings.CoreVoice.Provider,
                CoreVoiceSettings.RhVoiceProvider,
                StringComparison.OrdinalIgnoreCase)
            && !_coreSpeechCoordinator.IsRhVoiceAvailable
                ? L("Settings.CoreVoiceRhVoiceUnavailable")
                : null;
        CoreVoiceTestButton.ToolTip = _coreSpeechCoordinator.IsAvailable
            ? L("Settings.CoreVoiceTestTooltip")
            : L("Settings.CoreVoiceUnavailable");
    }

    private void InitializeAppData()
    {
        try
        {
            _appState = _appStateStore.LoadOrCreate();
            _storageSettings = _storageSettingsStore.LoadOrCreate();
            _userProfile = _userProfileStore.LoadOrCreate();
            LoadProfileIntoControls();
            UpdateProfileButtonState();
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
            CancelCoreSpeech(revealFullText: false, "app_closed");
            _choiceScenarioCts?.Cancel();
            _catalogStartupCts.Cancel();
            _choiceScenarioRuntimeService?.Dispose();
            _choiceScenarioLog?.Write("scenario_session_end", new { Reason = "app_closed" });
            _choiceScenarioLog?.Dispose();
            _coreSessionLog?.Write("session_end");
            _coreSessionLog?.Dispose();
            _coreSpeechCoordinator.Dispose();
            _catalogStartupCts.Dispose();
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

    private async Task RefreshModelCatalogOnStartupAsync()
    {
        await Task.Yield();
        try
        {
            _coreSessionLog?.Write("catalog_startup_sync_started", new
            {
                AppDataPaths.HuggingFaceCatalogPath,
                AppDataPaths.HuggingFaceCatalogSeedPath
            });
            var result = await _catalogStartupService.SynchronizeIfDueAsync(_catalogStartupCts.Token);
            _coreSessionLog?.Write("catalog_startup_sync_finished", result);
        }
        catch (OperationCanceledException)
        {
            _coreSessionLog?.Write("catalog_startup_sync_cancelled");
        }
        catch (Exception ex)
        {
            _coreSessionLog?.Write("catalog_startup_sync_error", new
            {
                ErrorType = ex.GetType().FullName,
                ex.Message
            });
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
        ProfilePage.Visibility = Visibility.Collapsed;
        ProfileReminderPage.Visibility = Visibility.Collapsed;
        WorkStartPage.Visibility = Visibility.Collapsed;
        UpdateWelcomeStatus();
    }

    private void BackFromSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsPage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Collapsed;
        ProfilePage.Visibility = Visibility.Collapsed;
        ProfileReminderPage.Visibility = Visibility.Collapsed;
        WorkStartPage.Visibility = Visibility.Collapsed;
        WelcomePage.Visibility = Visibility.Visible;
        UpdateWelcomeStatus();
    }

    private void BackFromProfileButton_Click(object sender, RoutedEventArgs e)
    {
        ProfilePage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
        ProfileReminderPage.Visibility = Visibility.Collapsed;
        WorkStartPage.Visibility = Visibility.Collapsed;
        WelcomePage.Visibility = Visibility.Visible;
        UpdateWelcomeStatus();
    }

    private void SaveProfileButton_Click(object sender, RoutedEventArgs e)
    {
        SaveProfileFromControls();
        _userContextService.UpdateProfile(_userProfile);
        UpdateProfileButtonState();
        StatusText.Text = _userProfile.IsComplete()
            ? L("Status.ProfileSavedComplete")
            : L("Status.ProfileSavedIncomplete");
    }

    private void FillProfileFromReminderButton_Click(object sender, RoutedEventArgs e)
    {
        LoadProfileIntoControls();
        ShowProfilePage();
        StatusText.Text = L("Status.ProfileIncomplete");
    }

    private void ContinueWithoutProfileButton_Click(object sender, RoutedEventArgs e)
    {
        ShowWorkStartPage();
        StatusText.Text = L("Status.WorkStartOpenedWithoutProfile");
    }

    private void BackFromProfileReminderButton_Click(object sender, RoutedEventArgs e)
    {
        ProfileReminderPage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
        ProfilePage.Visibility = Visibility.Collapsed;
        WorkStartPage.Visibility = Visibility.Collapsed;
        ChoiceScenarioPage.Visibility = Visibility.Collapsed;
        WelcomePage.Visibility = Visibility.Visible;
        UpdateWelcomeStatus();
    }

    private void BackFromWorkStartButton_Click(object sender, RoutedEventArgs e)
    {
        WorkStartPage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
        ProfilePage.Visibility = Visibility.Collapsed;
        ProfileReminderPage.Visibility = Visibility.Collapsed;
        ChoiceScenarioPage.Visibility = Visibility.Collapsed;
        WelcomePage.Visibility = Visibility.Visible;
        UpdateWelcomeStatus();
    }

    private void SelectReasoningModeButton_Click(object sender, RoutedEventArgs e)
    {
        StartChoiceScenario();
        StatusText.Text = L("Status.ChoiceScenarioOpened");
    }

    private void ChoiceOptionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_choiceScenarioRequestInProgress
            || sender is not System.Windows.Controls.Button button
            || button.Tag is not ChoiceScenarioOption option)
        {
            return;
        }

        ChoiceCustomInputPanel.Visibility = Visibility.Collapsed;
        if (option.Id.StartsWith("budget_", StringComparison.Ordinal)
            && !string.Equals(_currentChoiceScenarioStep?.StepType, "budget_setup", StringComparison.Ordinal))
        {
            return;
        }

        if (string.Equals(_currentChoiceScenarioStep?.StepType, "budget_setup", StringComparison.Ordinal)
            && ChoiceScenarioStepBudget.TryCreate(option.Id, out var budget))
        {
            _choiceScenarioState.ConfigureStepBudget(budget);
            _choiceScenarioLog?.Write("scenario_step_budget_selected", new
            {
                budget.Mode,
                budget.MaximumSteps,
                budget.IsAutomatic
            });
            var domainStep = _choiceScenarioService.CreateDomainStartStep(L);
            _choiceScenarioState.AddStep(domainStep, consumedAnswer: false);
            RenderChoiceScenarioStep(domainStep);
            _choiceScenarioLog?.Write("scenario_parsed_step", domainStep);
            StatusText.Text = LF("Status.ChoiceScenarioBudgetSelected", option.Title);
            return;
        }

        if (!_choiceScenarioState.TryAddAnswer(option))
        {
            return;
        }

        _choiceScenarioLog?.Write("scenario_user_choice", _choiceScenarioState.Answers[^1]);
        _choiceScenarioLog?.Write("scenario_capability_profile_updated", new
        {
            Source = "user_choice",
            _choiceScenarioState.Answers[^1].DecisionDimension,
            _choiceScenarioState.Answers[^1].AppliedProfileEffects,
            Profile = _choiceScenarioState.CapabilityProfile
        });
        foreach (var effect in _choiceScenarioState.Answers[^1].AppliedProfileEffects
            .Where(effect => effect.Status is ChoiceDimensionStatuses.Resolved or ChoiceDimensionStatuses.NotApplicable))
        {
            _choiceScenarioLog?.Write("scenario_resolved_dimension", effect);
        }
        StatusText.Text = LF("Status.ChoiceScenarioSelected", option.Title);
        _ = RequestNextChoiceScenarioStepAsync(requestFinal: false);
    }

    private void ChoiceCustomOptionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_choiceScenarioRequestInProgress)
        {
            return;
        }

        ChoiceCustomInputPanel.Visibility = Visibility.Visible;
        ChoiceCustomInput.Focus();
        StatusText.Text = L("Status.ChoiceScenarioCustomInput");
    }

    private void ChoiceCustomSubmitButton_Click(object sender, RoutedEventArgs e)
    {
        if (_choiceScenarioRequestInProgress)
        {
            return;
        }

        var customText = ChoiceCustomInput.Text.Trim();
        if (!IsValidCustomChoice(customText))
        {
            StatusText.Text = L("Status.ChoiceScenarioCustomTooLong");
            return;
        }

        var option = new ChoiceScenarioOption
        {
            Id = "custom",
            Title = customText,
            Description = L("ChoiceScenario.CustomDescription")
        };
        ChoiceCustomInput.Clear();
        ChoiceCustomInputPanel.Visibility = Visibility.Collapsed;
        ChoiceOptionButton_Click(new System.Windows.Controls.Button { Tag = option }, e);
    }

    private void BackFromChoiceScenarioButton_Click(object sender, RoutedEventArgs e)
    {
        if (_choiceScenarioRequestInProgress)
        {
            return;
        }

        if (_choiceScenarioState.TryGoBack(out var previousStep) && previousStep is not null)
        {
            if (string.Equals(previousStep.StepType, "budget_setup", StringComparison.Ordinal))
            {
                _choiceScenarioState.ClearStepBudget();
            }

            RenderChoiceScenarioStep(previousStep);
            _choiceScenarioLog?.Write("scenario_back", new
            {
                RestoredStep = previousStep,
                AnswerCount = _choiceScenarioState.Answers.Count,
                CapabilityProfile = _choiceScenarioState.CapabilityProfile
            });
            StatusText.Text = L("Status.ChoiceScenarioBack");
            return;
        }

        _choiceScenarioLog?.Write("scenario_session_end", new { Reason = "back_to_modes" });
        _choiceScenarioLog?.Dispose();
        _choiceScenarioLog = null;
        ShowWorkStartPage();
        StatusText.Text = L("Status.WorkStartOpened");
    }

    private void CancelChoiceScenarioButton_Click(object sender, RoutedEventArgs e)
    {
        _choiceScenarioLog?.Write("scenario_session_end", new { Reason = "cancelled" });
        _choiceScenarioLog?.Dispose();
        _choiceScenarioLog = null;
        ShowWorkStartPage();
        StatusText.Text = L("Status.ChoiceScenarioCancelled");
    }

    private void ChoiceGoFinalButton_Click(object sender, RoutedEventArgs e)
    {
        if (_choiceScenarioRequestInProgress)
        {
            return;
        }

        _ = RequestNextChoiceScenarioStepAsync(requestFinal: true);
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
        if (_pendingModelDownload == PendingModelDownload.Reranker)
        {
            await StartRerankerDownloadAsync();
            return;
        }

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
        StatusText.Text = _pendingModelDownload == PendingModelDownload.Reranker
            ? L("Status.RerankerModelPostponed")
            : L("Status.CoreModelPostponed");
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
            return;
        }

        if (result.Availability == CoreModelAvailability.Installed
            && !_toolModelManager.IsRerankerInstalled(_storageSettings)
            && !_isCoreModelPromptPostponed)
        {
            ShowRerankerModelPrompt();
        }
    }

    private void EvaluateCoreModelAfterStorageSave()
    {
        var result = _coreModelManager.Check(_storageSettings);
        _lastCoreModelCheck = result;
        if (result.Availability == CoreModelAvailability.Installed)
        {
            HideCoreModelPrompt();
            if (_toolModelManager.IsRerankerInstalled(_storageSettings))
            {
                StatusText.Text = L("Status.CoreModelReady");
            }
            else
            {
                ShowRerankerModelPrompt();
            }
            return;
        }

        ShowCoreModelPrompt(result);
    }

    private void ShowCoreModelPrompt(CoreModelCheckResult result)
    {
        _pendingModelDownload = PendingModelDownload.Core;
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

    private void ShowRerankerModelPrompt()
    {
        _pendingModelDownload = PendingModelDownload.Reranker;
        CoreModelPromptPanel.Visibility = Visibility.Visible;
        CoreModelDownloadPanel.Visibility = Visibility.Collapsed;
        DownloadCoreModelButton.IsEnabled = HasModelsStorage();
        CoreModelPromptText.Text = LF(
            "ToolModel.RerankerMissingPrompt",
            ToolModelManager.RerankerDisplayName,
            FormatBytes(ToolModelManager.RerankerTotalBytes));
        StatusText.Text = L("Status.RerankerModelMissing");
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

    private bool HasModelsStorage()
    {
        return _storageSettings.Models.Locations.Any(location => !string.IsNullOrWhiteSpace(location.Path));
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

        var coreInstalled = false;
        try
        {
            await _coreModelManager.DownloadAsync(_storageSettings, progress, _coreModelDownloadCts.Token);
            coreInstalled = true;
            await _toolModelManager.EnsureRerankerDownloadedAsync(_storageSettings, progress, _coreModelDownloadCts.Token);
            CoreModelDownloadPanel.Visibility = Visibility.Collapsed;
            HideCoreModelPrompt();
            StatusText.Text = L("Status.CoreAndRerankerInstalled");
        }
        catch (OperationCanceledException)
        {
            CoreModelDownloadPanel.Visibility = Visibility.Collapsed;
            if (coreInstalled)
            {
                ShowRerankerModelPrompt();
            }
            else
            {
                var result = _coreModelManager.Check(_storageSettings);
                ShowCoreModelPrompt(result);
            }

            StatusText.Text = L("Status.CoreModelDownloadPaused");
        }
        catch (Exception)
        {
            CoreModelDownloadPanel.Visibility = Visibility.Collapsed;
            if (coreInstalled)
            {
                ShowRerankerModelPrompt();
                StatusText.Text = L("Status.RerankerModelDownloadFailed");
            }
            else
            {
                var result = _coreModelManager.Check(_storageSettings);
                ShowCoreModelPrompt(result);
                StatusText.Text = L("Status.CoreModelDownloadFailed");
            }
        }
        finally
        {
            _coreModelDownloadCts?.Dispose();
            _coreModelDownloadCts = null;
            DownloadCoreModelButton.IsEnabled = true;
        }
    }

    private async Task StartRerankerDownloadAsync()
    {
        if (_coreModelDownloadCts is not null)
        {
            return;
        }

        if (!HasModelsStorage())
        {
            ShowRerankerModelPrompt();
            return;
        }

        _coreModelDownloadCts = new CancellationTokenSource();
        DownloadCoreModelButton.IsEnabled = false;
        CoreModelPromptPanel.Visibility = Visibility.Collapsed;
        CoreModelDownloadPanel.Visibility = Visibility.Visible;
        CoreModelDownloadProgressBar.Value = 0;
        CoreModelDownloadTitleText.Text = LF(
            "ToolModel.RerankerDownloadProgress",
            0,
            FormatBytes(0),
            FormatBytes(ToolModelManager.RerankerTotalBytes),
            FormatBytes(0) + L("Units.PerSecond"));
        StatusText.Text = L("Status.RerankerModelDownloadStarted");

        var progress = new Progress<CoreModelDownloadProgress>(UpdateCoreModelDownloadProgress);

        try
        {
            await _toolModelManager.EnsureRerankerDownloadedAsync(_storageSettings, progress, _coreModelDownloadCts.Token);
            CoreModelDownloadPanel.Visibility = Visibility.Collapsed;
            HideCoreModelPrompt();
            StatusText.Text = L("Status.RerankerModelInstalled");
        }
        catch (OperationCanceledException)
        {
            CoreModelDownloadPanel.Visibility = Visibility.Collapsed;
            ShowRerankerModelPrompt();
            StatusText.Text = L("Status.CoreModelDownloadPaused");
        }
        catch (Exception)
        {
            CoreModelDownloadPanel.Visibility = Visibility.Collapsed;
            ShowRerankerModelPrompt();
            StatusText.Text = L("Status.RerankerModelDownloadFailed");
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

        if (progress.Stage == "verifying-reranker")
        {
            CoreModelDownloadTitleText.Text = L("ToolModel.RerankerVerifying");
            CoreModelDownloadProgressBar.Value = 100;
            return;
        }

        if (progress.Stage == "installed-reranker")
        {
            CoreModelDownloadTitleText.Text = L("ToolModel.RerankerInstalled");
            CoreModelDownloadProgressBar.Value = 100;
            return;
        }

        var percent = progress.TotalBytes <= 0
            ? 0
            : Math.Clamp(progress.DownloadedBytes * 100d / progress.TotalBytes, 0, 100);

        CoreModelDownloadProgressBar.Value = percent;
        var progressKey = progress.Stage == "downloading-reranker"
            ? "ToolModel.RerankerDownloadProgress"
            : "CoreModel.DownloadProgress";
        CoreModelDownloadTitleText.Text = LF(
            progressKey,
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
        ProfilePage.Visibility = Visibility.Collapsed;
        ProfileReminderPage.Visibility = Visibility.Collapsed;
        WorkStartPage.Visibility = Visibility.Collapsed;
        ChoiceScenarioPage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Visible;
    }

    private void ShowSettingsPage()
    {
        CancelCoreSpeech(revealFullText: false, "open_settings");
        WelcomePage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Collapsed;
        ProfilePage.Visibility = Visibility.Collapsed;
        ProfileReminderPage.Visibility = Visibility.Collapsed;
        WorkStartPage.Visibility = Visibility.Collapsed;
        ChoiceScenarioPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Visible;
        PopulateLanguageComboBox();
    }

    private void ShowProfilePage()
    {
        CancelCoreSpeech(revealFullText: false, "open_profile");
        WelcomePage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
        ProfileReminderPage.Visibility = Visibility.Collapsed;
        WorkStartPage.Visibility = Visibility.Collapsed;
        ChoiceScenarioPage.Visibility = Visibility.Collapsed;
        ProfilePage.Visibility = Visibility.Visible;
    }

    private void ShowProfileReminderPage()
    {
        CancelCoreSpeech(revealFullText: false, "open_profile_reminder");
        WelcomePage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
        ProfilePage.Visibility = Visibility.Collapsed;
        WorkStartPage.Visibility = Visibility.Collapsed;
        ChoiceScenarioPage.Visibility = Visibility.Collapsed;
        ProfileReminderPage.Visibility = Visibility.Visible;
    }

    private void ShowWorkStartPage()
    {
        CancelCoreSpeech(revealFullText: false, "open_work_start");
        PreviousWorkExpander.IsExpanded = false;
        WelcomePage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
        ProfilePage.Visibility = Visibility.Collapsed;
        ProfileReminderPage.Visibility = Visibility.Collapsed;
        ChoiceScenarioPage.Visibility = Visibility.Collapsed;
        WorkStartPage.Visibility = Visibility.Visible;
    }

    private void StartChoiceScenario()
    {
        _choiceScenarioLog?.Write("scenario_session_end", new { Reason = "restart" });
        _choiceScenarioLog?.Dispose();
        try
        {
            _choiceScenarioLog = ScenarioSessionLog.CreateUncertainty(_storageSettings);
        }
        catch (Exception ex)
        {
            _choiceScenarioLog = new NullSessionEventLog();
            _choiceScenarioLog.Write("scenario_log_unavailable", new
            {
                ErrorType = ex.GetType().FullName,
                ex.Message
            });
        }

        _choiceScenarioLog.Write("scenario_session_start", new
        {
            AppVersion = GetAppVersion(),
            _choiceScenarioLog.FilePath
        });
        _choiceScenarioLog.Write("scenario_context_snapshot", _userContextService.CreateSnapshot());
        _choiceScenarioInvalidJsonCount = 0;
        _choiceScenarioRequestInProgress = false;
        ChoiceCustomInput.Clear();
        ChoiceCustomInputPanel.Visibility = Visibility.Collapsed;
        var startStep = _choiceScenarioService.CreateBudgetStep(L);
        _choiceScenarioState.Reset(startStep);
        RenderChoiceScenarioStep(startStep);
        _choiceScenarioLog.Write("scenario_parsed_step", _currentChoiceScenarioStep);
        WelcomePage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
        ProfilePage.Visibility = Visibility.Collapsed;
        ProfileReminderPage.Visibility = Visibility.Collapsed;
        WorkStartPage.Visibility = Visibility.Collapsed;
        ChoiceScenarioPage.Visibility = Visibility.Visible;
    }

    private async Task RequestNextChoiceScenarioStepAsync(bool requestFinal)
    {
        if (_choiceScenarioRequestInProgress || _choiceScenarioCts is not null)
        {
            return;
        }

        var stepBudget = _choiceScenarioState.StepBudget;
        if (stepBudget is null)
        {
            return;
        }

        var mustReturnFinal = _choiceScenarioState.IsStepBudgetExhausted;
        var effectiveRequestFinal = requestFinal || mustReturnFinal;
        _choiceScenarioRequestInProgress = true;
        _choiceScenarioCts = new CancellationTokenSource();
        SetChoiceScenarioInteractionEnabled(false);
        ChoiceScenarioStatusText.Text = L("ChoiceScenario.CoreWorking");
        StatusText.Text = effectiveRequestFinal
            ? L("Status.ChoiceScenarioRequestFinal")
            : L("Status.ChoiceScenarioCoreThinking");

        try
        {
            var model = _choiceModelDiscoveryService
                .Discover(_storageSettings)
                .FirstOrDefault(item => item.IsCoreModel && item.IsRunnable)
                ?? _choiceModelDiscoveryService.Discover(_storageSettings).FirstOrDefault(item => item.IsRunnable);

            if (model is null)
            {
                _choiceScenarioLog?.Write("scenario_core_unavailable", new { Reason = "No runnable model" });
                var fallbackStep = _choiceScenarioService.CreateFallbackStep(_choiceScenarioState.Answers, L);
                if (_choiceScenarioState.GetFingerprintCount(fallbackStep) > 0)
                {
                    if (!requestFinal)
                    {
                        _choiceScenarioState.RemoveLastAnswer();
                    }
                    ChoiceScenarioStatusText.Text = L("ChoiceScenario.CoreUnavailableStop");
                    StatusText.Text = L("Status.ChoiceScenarioCoreUnavailable");
                    return;
                }

                _choiceScenarioState.AddStep(fallbackStep, consumedAnswer: !requestFinal);
                RenderChoiceScenarioStep(fallbackStep);
                StatusText.Text = L("Status.ChoiceScenarioCoreUnavailable");
                return;
            }

            _choiceScenarioRuntimeService ??= new LlamaServerRuntimeService(_userContextService);
            var inventory = _choiceInventoryService.Create(_storageSettings);
            var inventorySummary = string.Join(
                Environment.NewLine,
                inventory.Items.Select(item => $"{item.Role}: installed={item.IsInstalled}; runnable={item.IsRunnable}; name={item.Name}"));
            var systemPrompt = _choiceScenarioService.BuildSystemPrompt();
            var userPrompt = _choiceScenarioService.BuildUserPrompt(
                _choiceScenarioState.Answers,
                requestFinal,
                mustReturnFinal,
                _userContextService.CreateSnapshot(),
                inventorySummary,
                _appSettings.LanguageCode,
                stepBudget,
                _choiceScenarioState.SubstantiveStepsUsed,
                _choiceScenarioState.StepsRemaining,
                _choiceScenarioState.CapabilityProfile);
            _choiceScenarioLog?.Write("scenario_core_prompt", new
            {
                Model = model.Name,
                model.Path,
                RequestFinal = effectiveRequestFinal,
                ManualRequestFinal = requestFinal,
                MustReturnFinal = mustReturnFinal,
                CapabilityProfile = _choiceScenarioState.CapabilityProfile,
                StepBudget = stepBudget,
                StepsUsed = _choiceScenarioState.SubstantiveStepsUsed,
                StepsRemaining = _choiceScenarioState.StepsRemaining,
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt
            });

            var generation = await _choiceScenarioOrchestrator.GenerateAsync(
                _choiceScenarioRuntimeService,
                model,
                systemPrompt,
                userPrompt,
                _storageSettings,
                _choiceScenarioLog ?? new NullSessionEventLog(),
                _choiceScenarioCts.Token,
                effectiveRequestFinal,
                mustReturnFinal,
                _userProfile.WorkloadMode,
                _choiceScenarioState.CapabilityProfile);
            _choiceScenarioLog?.Write("scenario_core_raw_response", new
            {
                Model = model.Name,
                Text = generation.RawResponse,
                generation.RepairAttempts
            });

            if (generation.Step is { } step)
            {
                var repeatedStep = !step.IsFinal
                    && (_choiceScenarioState.GetFingerprintCount(step) > 0
                        || _choiceScenarioState.IsSemanticLoop(step));
                var subjectMatterOverreach = !step.IsFinal
                    && _choiceScenarioState.IsSubjectMatterOverreach(step);
                if (repeatedStep || subjectMatterOverreach)
                {
                    _choiceScenarioLog?.Write(
                        subjectMatterOverreach
                            ? "scenario_subject_matter_overreach"
                            : "scenario_repeated_step_blocked",
                        new
                        {
                            step.Question,
                            step.DecisionDimension,
                            step.SelectionImpact,
                            Fingerprint = ChoiceScenarioSessionState.CreateFingerprint(step)
                        });
                    var correctionPrompt = userPrompt + Environment.NewLine + Environment.NewLine
                        + (subjectMatterOverreach
                            ? "Предыдущий вопрос отклонён как повторное предметное углубление. Не уточняй содержание темы. Выбери другое неизвестное измерение профиля исполнителя: операцию, данные, контекст, инструменты, точность, скорость или приватность. Если профиль уже достаточен, сформируй final_task_card."
                            : "Запрещено повторять уже заданный вопрос, измерение или тот же набор вариантов. Выбери другое неизвестное измерение профиля исполнителя. Если данных достаточно, сформируй final_task_card.");
                    generation = await _choiceScenarioOrchestrator.GenerateAsync(
                        _choiceScenarioRuntimeService,
                        model,
                        systemPrompt,
                        correctionPrompt,
                        _storageSettings,
                        _choiceScenarioLog ?? new NullSessionEventLog(),
                        _choiceScenarioCts.Token,
                        effectiveRequestFinal,
                        mustReturnFinal,
                        _userProfile.WorkloadMode,
                        _choiceScenarioState.CapabilityProfile);
                    step = generation.Step ?? step;
                    repeatedStep = !step.IsFinal
                        && (_choiceScenarioState.GetFingerprintCount(step) > 0
                            || _choiceScenarioState.IsSemanticLoop(step));
                    subjectMatterOverreach = !step.IsFinal
                        && _choiceScenarioState.IsSubjectMatterOverreach(step);
                    if (repeatedStep || subjectMatterOverreach)
                    {
                        _choiceScenarioLog?.Write("scenario_question_rejected_as_non_productive", new
                        {
                            step.Question,
                            step.DecisionDimension,
                            Reason = subjectMatterOverreach ? "subject_matter_overreach" : "repeated_step"
                        });
                        var forcedFinalPrompt = userPrompt + Environment.NewLine + Environment.NewLine
                            + "Два последовательных вопроса отклонены как непродуктивные. Новый question_step запрещён. Сформируй final_task_card по текущему capability profile, честно обозначь пробелы и поручай рабочей модели задать недостающие предметные вопросы.";
                        generation = await _choiceScenarioOrchestrator.GenerateAsync(
                            _choiceScenarioRuntimeService,
                            model,
                            systemPrompt,
                            forcedFinalPrompt,
                            _storageSettings,
                            _choiceScenarioLog ?? new NullSessionEventLog(),
                            _choiceScenarioCts.Token,
                            requestFinal: true,
                            mustReturnFinal: true,
                            workloadMode: _userProfile.WorkloadMode,
                            capabilityProfile: _choiceScenarioState.CapabilityProfile);
                        step = generation.Step ?? step;
                        if (!step.IsFinal)
                        {
                            _choiceScenarioInvalidJsonCount++;
                            if (!requestFinal)
                            {
                                _choiceScenarioState.RemoveLastAnswer();
                            }
                            ChoiceScenarioStatusText.Text = L("ChoiceScenario.RepeatedStepError");
                            StatusText.Text = L("Status.ChoiceScenarioRepeatedStep");
                            return;
                        }
                    }
                }

                _choiceScenarioInvalidJsonCount = 0;
                _choiceScenarioState.AddStep(step, consumedAnswer: !requestFinal);
                RenderChoiceScenarioStep(step);
                _choiceScenarioLog?.Write("scenario_capability_profile_updated", _choiceScenarioState.CapabilityProfile);
                if (!step.IsFinal)
                {
                    _choiceScenarioLog?.Write("scenario_decision_dimension", new
                    {
                        step.DecisionDimension,
                        step.SelectionImpact,
                        ResolvedDimensions = _choiceScenarioState.CapabilityProfile.ResolvedDimensions
                    });
                }
                _choiceScenarioLog?.Write(step.IsFinal ? "scenario_final_task_card" : "scenario_parsed_step", step);
                StatusText.Text = step.IsFinal
                    ? L("Status.ChoiceScenarioTaskCardReady")
                    : L("Status.ChoiceScenarioStepReady");
                return;
            }

            _choiceScenarioInvalidJsonCount++;
            if (!requestFinal)
            {
                _choiceScenarioState.RemoveLastAnswer();
            }
            _choiceScenarioLog?.Write("scenario_structure_error", new
            {
                Error = generation.Error,
                Raw = generation.RawResponse,
                Count = _choiceScenarioInvalidJsonCount
            });
            ChoiceScenarioStatusText.Text = L("ChoiceScenario.StructureError");
            StatusText.Text = L("Status.ChoiceScenarioStructureError");
        }
        catch (OperationCanceledException)
        {
            _choiceScenarioLog?.Write("scenario_request_cancelled");
        }
        catch (Exception ex)
        {
            _choiceScenarioLog?.Write("scenario_core_unavailable", new
            {
                Reason = "Exception while requesting core step",
                ErrorType = ex.GetType().FullName,
                ex.Message
            });
            var fallbackStep = _choiceScenarioService.CreateFallbackStep(_choiceScenarioState.Answers, L);
            if (_choiceScenarioState.GetFingerprintCount(fallbackStep) > 0)
            {
                if (!requestFinal)
                {
                    _choiceScenarioState.RemoveLastAnswer();
                }
                ChoiceScenarioStatusText.Text = L("ChoiceScenario.CoreUnavailableStop");
                StatusText.Text = L("Status.ChoiceScenarioCoreUnavailable");
                return;
            }

            _choiceScenarioState.AddStep(fallbackStep, consumedAnswer: !requestFinal);
            RenderChoiceScenarioStep(fallbackStep);
            StatusText.Text = L("Status.ChoiceScenarioCoreUnavailable");
        }
        finally
        {
            _choiceScenarioCts?.Dispose();
            _choiceScenarioCts = null;
            _choiceScenarioRequestInProgress = false;
            SetChoiceScenarioInteractionEnabled(true);
        }
    }

    private void RenderChoiceScenarioStep(ChoiceScenarioStep step)
    {
        CancelCoreSpeech(revealFullText: false, "step_replaced");
        _currentChoiceScenarioStep = step;
        ChoiceScenarioQuestionMeasureText.Text = step.Question;
        ChoiceScenarioCoreThoughtMeasureText.Text = step.CoreThought;
        AutomationProperties.SetName(ChoiceScenarioQuestionText, step.Question);
        AutomationProperties.SetName(ChoiceScenarioCoreThoughtText, step.CoreThought);
        ChoiceScenarioQuestionText.Text = step.Question;
        ChoiceScenarioCoreThoughtText.Text = step.CoreThought;
        var stepStatus = step.IsFinal
            ? L("ChoiceScenario.FinalStatus")
            : L("ChoiceScenario.StepStatus");
        if (!step.IsFinal
            && !string.Equals(step.StepType, "budget_setup", StringComparison.Ordinal)
            && _choiceScenarioState.StepBudget is { } budget)
        {
            var currentQuestion = Math.Min(budget.MaximumSteps, _choiceScenarioState.SubstantiveStepsUsed + 1);
            var progress = budget.IsAutomatic
                ? LF("ChoiceScenario.BudgetProgressAutomatic", currentQuestion, budget.MaximumSteps)
                : LF("ChoiceScenario.BudgetProgress", currentQuestion, budget.MaximumSteps);
            stepStatus += Environment.NewLine + progress;
        }

        ChoiceScenarioStatusText.Text = stepStatus;

        _choiceScenarioOptions.Clear();
        foreach (var option in step.Options.Take(6))
        {
            _choiceScenarioOptions.Add(option);
        }

        ChoiceCustomOptionButton.Visibility = step.AllowCustom ? Visibility.Visible : Visibility.Collapsed;
        ChoiceGoFinalButton.Visibility = !step.IsFinal && _choiceScenarioState.Answers.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        ChoiceCustomInputPanel.Visibility = Visibility.Collapsed;
        RenderChoiceScenarioSummary(step);
        if (!step.IsFinal
            && _appSettings.CoreVoice.Enabled
            && _coreSpeechCoordinator.IsAvailable
            && (!string.IsNullOrWhiteSpace(step.Question) || !string.IsNullOrWhiteSpace(step.CoreThought)))
        {
            StartChoiceScenarioSpeech(step);
        }
    }

    private void SetChoiceScenarioInteractionEnabled(bool enabled)
    {
        var answersEnabled = enabled && !_coreSpeechPresentationActive;
        ChoiceOptionsItemsControl.IsEnabled = answersEnabled;
        ChoiceOptionsItemsControl.Opacity = answersEnabled ? 1.0 : 0.58;
        ChoiceCustomOptionButton.IsEnabled = answersEnabled;
        ChoiceCustomSubmitButton.IsEnabled = answersEnabled;
        ChoiceGoFinalButton.IsEnabled = answersEnabled;
        BackFromChoiceScenarioButton.IsEnabled = enabled;
        CancelChoiceScenarioButton.IsEnabled = enabled;
    }

    private void StartChoiceScenarioSpeech(ChoiceScenarioStep step)
    {
        _coreSpeechCts = new CancellationTokenSource();
        var presentationId = Interlocked.Increment(ref _coreSpeechPresentationId);
        _coreSpeechPresentationActive = true;
        ChoiceScenarioQuestionText.Text = string.Empty;
        ChoiceScenarioCoreThoughtText.Text = string.Empty;
        SetChoiceScenarioInteractionEnabled(!_choiceScenarioRequestInProgress);

        var request = new CoreSpeechRequest(
            [
                new CoreSpeechSegment("coreThought", step.CoreThought),
                new CoreSpeechSegment("question", step.Question)
            ],
            _appSettings.LanguageCode,
            _appSettings.CoreVoice,
            $"uncertainty:{step.StepType}");
        var progress = new Progress<CoreSpeechProgress>(value =>
        {
            if (presentationId != _coreSpeechPresentationId)
            {
                return;
            }

            ChoiceScenarioCoreThoughtText.Text = VisibleText(
                step.CoreThought,
                value.VisibleCharacters.GetValueOrDefault("coreThought"));
            ChoiceScenarioQuestionText.Text = VisibleText(
                step.Question,
                value.VisibleCharacters.GetValueOrDefault("question"));
        });

        _ = PresentChoiceScenarioSpeechAsync(step, request, progress, presentationId, _coreSpeechCts);
    }

    private async Task PresentChoiceScenarioSpeechAsync(
        ChoiceScenarioStep step,
        CoreSpeechRequest request,
        IProgress<CoreSpeechProgress> progress,
        long presentationId,
        CancellationTokenSource cancellationSource)
    {
        try
        {
            var result = await _coreSpeechCoordinator.PresentAsync(
                request,
                progress,
                _choiceScenarioLog,
                cancellationSource.Token);
            if (presentationId != _coreSpeechPresentationId)
            {
                return;
            }

            if (!result.Completed && !result.Skipped)
            {
                _choiceScenarioLog?.Write("core_voice_disabled_for_session", new
                {
                    request.Source,
                    result.ErrorCode
                });
            }
        }
        finally
        {
            if (presentationId == _coreSpeechPresentationId)
            {
                ChoiceScenarioCoreThoughtText.Text = step.CoreThought;
                ChoiceScenarioQuestionText.Text = step.Question;
                _coreSpeechPresentationActive = false;
                _coreSpeechCts = null;
                SetChoiceScenarioInteractionEnabled(!_choiceScenarioRequestInProgress);
            }

            cancellationSource.Dispose();
        }
    }

    private void CancelCoreSpeech(bool revealFullText, string reason)
    {
        if (_coreSpeechCts is null && !_coreSpeechPresentationActive)
        {
            return;
        }

        Interlocked.Increment(ref _coreSpeechPresentationId);
        var cancellationSource = _coreSpeechCts;
        _coreSpeechCts = null;
        cancellationSource?.Cancel();
        _coreSpeechCoordinator.Cancel();
        _coreSpeechPresentationActive = false;
        if (revealFullText && _currentChoiceScenarioStep is { } step)
        {
            ChoiceScenarioCoreThoughtText.Text = step.CoreThought;
            ChoiceScenarioQuestionText.Text = step.Question;
        }

        _choiceScenarioLog?.Write("core_voice_cancelled", new { Reason = reason });
        SetChoiceScenarioInteractionEnabled(!_choiceScenarioRequestInProgress);
    }

    private static string VisibleText(string text, int visibleCharacters) =>
        string.IsNullOrEmpty(text)
            ? string.Empty
            : text[..Math.Clamp(visibleCharacters, 0, text.Length)];

    private void CoreVoiceToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _appSettings.CoreVoice.Enabled = !_appSettings.CoreVoice.Enabled;
        _appSettingsStore.Save(_appSettings);
        _isApplyingCoreVoiceSettings = true;
        CoreVoiceEnabledCheckBox.IsChecked = _appSettings.CoreVoice.Enabled;
        _isApplyingCoreVoiceSettings = false;

        if (!_appSettings.CoreVoice.Enabled)
        {
            CancelCoreSpeech(revealFullText: true, "scenario_muted");
        }
        else if (_currentChoiceScenarioStep is { IsFinal: false } step)
        {
            StartChoiceScenarioSpeech(step);
        }

        UpdateCoreVoiceControls();
    }

    private void RenderChoiceScenarioSummary(ChoiceScenarioStep step)
    {
        if (!step.SummaryLines.Any() && step.TaskCard is null)
        {
            ChoiceScenarioSummaryPanel.Visibility = Visibility.Collapsed;
            ChoiceScenarioSummaryText.Text = string.Empty;
            return;
        }

        var builder = new StringBuilder();
        foreach (var line in step.SummaryLines)
        {
            builder.AppendLine(line);
        }

        if (step.TaskCard is not null)
        {
            var card = step.TaskCard;
            builder.AppendLine();
            builder.AppendLine(LF("ChoiceScenario.Card.Goal", card.Goal));
            builder.AppendLine(LF("ChoiceScenario.Card.Executor", card.RecommendedExecutor));
            builder.AppendLine(LF("ChoiceScenario.Card.ExecutorStatus", card.ExecutorStatus));
            builder.AppendLine(LF("ChoiceScenario.Card.Reason", card.ExecutorReason));
            builder.AppendLine(LF("ChoiceScenario.Card.Internet", card.NeedsWeb ? L("Common.Yes") : L("Common.No")));
            if (card.RequiredTools.Count > 0)
            {
                builder.AppendLine(LF("ChoiceScenario.Card.RequiredTools", string.Join(", ", card.RequiredTools)));
            }
            if (card.CapabilityProfile.Dimensions.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine(L("ChoiceScenario.Card.CapabilityProfile"));
                foreach (var dimension in card.CapabilityProfile.Dimensions)
                {
                    var label = L($"ChoiceScenario.Dimension.{dimension.Dimension}");
                    var values = dimension.Values.Count == 0
                        ? dimension.Status
                        : string.Join(", ", dimension.Values);
                    builder.AppendLine($"{label}: {values}");
                }
            }
            builder.AppendLine();
            builder.AppendLine(L("ChoiceScenario.Card.Prompt"));
            builder.AppendLine(card.PromptForExecutor);
        }

        ChoiceScenarioSummaryPanel.Visibility = Visibility.Visible;
        ChoiceScenarioSummaryText.Text = builder.ToString().Trim();
    }

    private static bool IsValidCustomChoice(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var words = value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return words.Length is >= 1 and <= 3;
    }

    private static string FormatInvariant(string format, params object[] args)
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args);
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

    private void LoadProfileIntoControls()
    {
        ProfileDisplayNameInput.Text = _userProfile.DisplayName;
        ProfileCityInput.Text = _userProfile.Location.City;
        ProfileRegionInput.Text = _userProfile.Location.Region;
        ProfileCountryInput.Text = _userProfile.Location.Country;
        ProfileTimezoneInput.Text = string.IsNullOrWhiteSpace(_userProfile.Location.Timezone)
            ? TimeZoneInfo.Local.Id
            : _userProfile.Location.Timezone;

        PreferenceConciseCheckBox.IsChecked = _userProfile.AnswerPreferences.Concise;
        PreferenceDetailedCheckBox.IsChecked = _userProfile.AnswerPreferences.Detailed;
        PreferenceSimpleCheckBox.IsChecked = _userProfile.AnswerPreferences.SimpleLanguage;
        PreferenceStepsCheckBox.IsChecked = _userProfile.AnswerPreferences.StepByStep;
        PreferenceExamplesCheckBox.IsChecked = _userProfile.AnswerPreferences.Examples;
        PreferenceSourcesCheckBox.IsChecked = _userProfile.AnswerPreferences.SourcesWhenSearching;
        PreferenceRisksCheckBox.IsChecked = _userProfile.AnswerPreferences.WarnAboutRisks;
        SetSelectedWorkloadMode(_userProfile.WorkloadMode);
    }

    private void SaveProfileFromControls()
    {
        _userProfile.ProfileVersion = 1;
        _userProfile.DisplayName = ProfileDisplayNameInput.Text.Trim();
        _userProfile.Location.Mode = "manual";
        _userProfile.Location.Source = "manual";
        _userProfile.Location.City = ProfileCityInput.Text.Trim();
        _userProfile.Location.Region = ProfileRegionInput.Text.Trim();
        _userProfile.Location.Country = ProfileCountryInput.Text.Trim();
        _userProfile.Location.Timezone = ProfileTimezoneInput.Text.Trim();
        _userProfile.Location.UpdatedAt = DateTimeOffset.Now;
        _userProfile.AnswerPreferences.Concise = PreferenceConciseCheckBox.IsChecked == true;
        _userProfile.AnswerPreferences.Detailed = PreferenceDetailedCheckBox.IsChecked == true;
        _userProfile.AnswerPreferences.SimpleLanguage = PreferenceSimpleCheckBox.IsChecked == true;
        _userProfile.AnswerPreferences.StepByStep = PreferenceStepsCheckBox.IsChecked == true;
        _userProfile.AnswerPreferences.Examples = PreferenceExamplesCheckBox.IsChecked == true;
        _userProfile.AnswerPreferences.SourcesWhenSearching = PreferenceSourcesCheckBox.IsChecked == true;
        _userProfile.AnswerPreferences.WarnAboutRisks = PreferenceRisksCheckBox.IsChecked == true;
        _userProfile.WorkloadMode = GetSelectedWorkloadMode();
        _userProfileStore.Save(_userProfile);
    }

    private void UpdateProfileButtonState()
    {
        if (_userProfile.IsComplete())
        {
            _profileBlinkTimer.Stop();
            ProfileButton.Opacity = 1.0;
            return;
        }

        if (!_profileBlinkTimer.IsEnabled)
        {
            ProfileButton.Opacity = 1.0;
            _profileBlinkTimer.Start();
        }
    }

    private string GetSelectedWorkloadMode()
    {
        if (WorkloadLightRadio.IsChecked == true)
        {
            return UserWorkloadModes.Light;
        }

        if (WorkloadExtremeRadio.IsChecked == true)
        {
            return UserWorkloadModes.Extreme;
        }

        return UserWorkloadModes.Balanced;
    }

    private void SetSelectedWorkloadMode(string mode)
    {
        WorkloadLightRadio.IsChecked = string.Equals(mode, UserWorkloadModes.Light, StringComparison.OrdinalIgnoreCase);
        WorkloadExtremeRadio.IsChecked = string.Equals(mode, UserWorkloadModes.Extreme, StringComparison.OrdinalIgnoreCase);
        WorkloadBalancedRadio.IsChecked = WorkloadLightRadio.IsChecked != true && WorkloadExtremeRadio.IsChecked != true;
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
