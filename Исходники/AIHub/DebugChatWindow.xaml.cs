using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using AIHub.Models;
using AIHub.Services;
using Media = System.Windows.Media;

namespace AIHub;

public partial class DebugChatWindow : Window
{
    private readonly DebugModelDiscoveryService _modelDiscoveryService = new();
    private readonly LlamaCliRuntimeService _runtimeService = new();
    private readonly LocalizationService _localizationService;
    private readonly StorageSettings _storageSettings;
    private readonly ObservableCollection<string> _chatItems = [];
    private readonly ObservableCollection<string> _logItems = [];
    private readonly List<DebugChatMessage> _history = [];
    private CancellationTokenSource? _generationCts;

    public DebugChatWindow(LocalizationService localizationService, StorageSettings storageSettings, bool isDarkTheme)
    {
        _localizationService = localizationService;
        _storageSettings = storageSettings;

        InitializeComponent();
        ApplyTheme(isDarkTheme);
        ApplyLocalization();
        ChatListBox.ItemsSource = _chatItems;
        LogListBox.ItemsSource = _logItems;
        RefreshModels();
    }

    protected override void OnClosed(EventArgs e)
    {
        _generationCts?.Cancel();
        _generationCts?.Dispose();
        base.OnClosed(e);
    }

    private string L(string key) => _localizationService.T(key);

    public void ApplyTheme(bool isDarkTheme)
    {
        SetBrush("WindowBackgroundBrush", isDarkTheme ? "#111827" : "#F3F3F3");
        SetBrush("HeaderBackgroundBrush", isDarkTheme ? "#0B1220" : "#FFFFFF");
        SetBrush("PanelBrush", isDarkTheme ? "#172033" : "#FFFFFF");
        SetBrush("LineBrush", isDarkTheme ? "#2D374B" : "#DADDE3");
        SetBrush("TextPrimaryBrush", isDarkTheme ? "#F8FAFC" : "#1F1F1F");
        SetBrush("TextSecondaryBrush", isDarkTheme ? "#AAB4C4" : "#5D6470");
        SetBrush("NativeComboTextBrush", "#1F1F1F");
        SetBrush("InputBackgroundBrush", isDarkTheme ? "#0B1220" : "#FFFFFF");
        SetBrush("InputSelectedBrush", isDarkTheme ? "#244A85" : "#DCEBFF");
    }

    private void SetBrush(string resourceKey, string color)
    {
        Resources[resourceKey] = new Media.SolidColorBrush((Media.Color)Media.ColorConverter.ConvertFromString(color));
    }

    private void ApplyLocalization()
    {
        Title = L("DebugChat.WindowTitle");
        TitleText.Text = L("DebugChat.Title");
        DescriptionText.Text = L("DebugChat.Description");
        RefreshModelsButton.Content = L("DebugChat.RefreshModels");
        ModelLabelText.Text = L("DebugChat.Model");
        StatusLabelText.Text = L("DebugChat.Status");
        ChatLabelText.Text = L("DebugChat.Chat");
        LogLabelText.Text = L("DebugChat.Logs");
        SendButton.Content = L("DebugChat.Send");
        StopButton.Content = L("DebugChat.Stop");
        ClearChatButton.Content = L("DebugChat.ClearChat");
        ClearLogButton.Content = L("DebugChat.ClearLog");
        FooterText.Text = L("DebugChat.Footer");
    }

    private void RefreshModelsButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshModels();
    }

    private void ModelsComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ModelsComboBox.SelectedItem is DebugModelInfo model)
        {
            StatusText.Text = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                L("DebugChat.ModelSelected"),
                model.Path,
                string.IsNullOrWhiteSpace(model.Role) ? L("DebugChat.NoManifest") : model.Role,
                string.IsNullOrWhiteSpace(model.Status) ? L("DebugChat.Unknown") : model.Status);
            AddLog(string.Format(System.Globalization.CultureInfo.InvariantCulture, L("DebugChat.LogModelSelected"), model.Name));
        }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await SendPromptAsync();
    }

    private async void PromptTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            await SendPromptAsync();
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _generationCts?.Cancel();
        AddLog(L("DebugChat.LogStopRequested"));
    }

    private void ClearChatButton_Click(object sender, RoutedEventArgs e)
    {
        _history.Clear();
        _chatItems.Clear();
        AddLog(L("DebugChat.LogChatCleared"));
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        _logItems.Clear();
        AddLog(L("DebugChat.LogCleared"));
    }

    private void RefreshModels()
    {
        var models = _modelDiscoveryService.Discover(_storageSettings);
        ModelsComboBox.ItemsSource = models;
        ModelsComboBox.SelectedItem = models.FirstOrDefault(model => model.IsCoreModel) ?? models.FirstOrDefault();

        AddLog(string.Format(System.Globalization.CultureInfo.InvariantCulture, L("DebugChat.LogModelsFound"), models.Count));
        AddLog(_runtimeService.IsAvailable
            ? string.Format(System.Globalization.CultureInfo.InvariantCulture, L("DebugChat.LogBackendFound"), _runtimeService.ExpectedExecutablePath)
            : string.Format(System.Globalization.CultureInfo.InvariantCulture, L("DebugChat.LogBackendMissing"), _runtimeService.ExpectedExecutablePath));

        if (models.Count == 0)
        {
            StatusText.Text = L("DebugChat.NoModels");
        }
        else if (!_runtimeService.IsAvailable)
        {
            StatusText.Text = L("DebugChat.BackendMissing");
        }
    }

    private async Task SendPromptAsync()
    {
        if (_generationCts is not null)
        {
            return;
        }

        if (ModelsComboBox.SelectedItem is not DebugModelInfo model)
        {
            StatusText.Text = L("DebugChat.NoModelSelected");
            AddLog(L("DebugChat.LogNoModelSelected"));
            return;
        }

        var prompt = PromptTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            StatusText.Text = L("DebugChat.EmptyPrompt");
            return;
        }

        _generationCts = new CancellationTokenSource();
        SetBusy(true);
        PromptTextBox.Clear();
        _history.Add(new DebugChatMessage { Role = L("DebugChat.UserRole"), Text = prompt });
        _chatItems.Add($"{L("DebugChat.UserRole")}: {prompt}");
        AddLog(L("DebugChat.LogPromptSent"));
        StatusText.Text = L("DebugChat.Generating");

        try
        {
            var response = await _runtimeService.GenerateAsync(model, _history, prompt, AddLog, _generationCts.Token);
            _history.Add(new DebugChatMessage { Role = L("DebugChat.ModelRole"), Text = response });
            _chatItems.Add($"{L("DebugChat.ModelRole")}: {response}");
            StatusText.Text = L("DebugChat.Done");
            AddLog(L("DebugChat.LogResponseReceived"));
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = L("DebugChat.Stopped");
            AddLog(L("DebugChat.LogStopped"));
        }
        catch (Exception ex)
        {
            StatusText.Text = L("DebugChat.Error");
            AddLog(string.Format(System.Globalization.CultureInfo.InvariantCulture, L("DebugChat.LogError"), ex.Message));
        }
        finally
        {
            SetBusy(false);
            _generationCts?.Dispose();
            _generationCts = null;
        }
    }

    private void SetBusy(bool isBusy)
    {
        SendButton.IsEnabled = !isBusy;
        RefreshModelsButton.IsEnabled = !isBusy;
        ModelsComboBox.IsEnabled = !isBusy;
        StopButton.IsEnabled = isBusy;
    }

    private void AddLog(string message)
    {
        Dispatcher.Invoke(() =>
        {
            _logItems.Add($"{DateTime.Now:HH:mm:ss}  {message}");
            if (_logItems.Count > 500)
            {
                _logItems.RemoveAt(0);
            }

            LogListBox.ScrollIntoView(_logItems.LastOrDefault());
        });
    }
}
