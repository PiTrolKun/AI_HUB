using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using AIHub.Models;
using AIHub.Services;
using Media = System.Windows.Media;

namespace AIHub;

public partial class DebugChatWindow : Window
{
    private const string CoreToolTestPrefix = "core_tool_test:";
    private const int MaxDebugToolRequests = 10;
    private const int MaxDebugAgentSteps = 12;

    private readonly DebugModelDiscoveryService _modelDiscoveryService = new();
    private readonly ToolGateway _toolGateway = new();
    private readonly LlamaServerRuntimeService _serverRuntimeService;
    private readonly LlamaCliRuntimeService _cliRuntimeService;
    private readonly LocalizationService _localizationService;
    private readonly StorageSettings _storageSettings;
    private readonly UserContextService _userContextService;
    private readonly JsonlSessionLog _debugSessionLog;
    private readonly ObservableCollection<string> _chatItems = [];
    private readonly ObservableCollection<string> _logItems = [];
    private readonly List<DebugChatMessage> _history = [];
    private CancellationTokenSource? _generationCts;

    public DebugChatWindow(
        LocalizationService localizationService,
        StorageSettings storageSettings,
        UserContextService userContextService,
        bool isDarkTheme)
    {
        _localizationService = localizationService;
        _storageSettings = storageSettings;
        _userContextService = userContextService;
        _serverRuntimeService = new LlamaServerRuntimeService(_userContextService);
        _cliRuntimeService = new LlamaCliRuntimeService(_userContextService);
        _debugSessionLog = JsonlSessionLog.CreateDebugModelTester(_storageSettings);

        InitializeComponent();
        ApplyTheme(isDarkTheme);
        ApplyLocalization();
        ChatListBox.ItemsSource = _chatItems;
        LogListBox.ItemsSource = _logItems;
        _debugSessionLog.Write("debug_session_start", new
        {
            AppVersion = GetAppVersion(),
            _debugSessionLog.FilePath
        });
        _debugSessionLog.Write("context_snapshot", _userContextService.CreateSnapshot());
        RefreshModels();
    }

    protected override void OnClosed(EventArgs e)
    {
        _generationCts?.Cancel();
        _generationCts?.Dispose();
        _serverRuntimeService.Dispose();
        _debugSessionLog.Write("debug_session_end");
        _debugSessionLog.Dispose();
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
            if (!model.IsRunnable)
            {
                StatusText.Text = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    L("DebugChat.ToolModelSelected"),
                    model.Path,
                    string.IsNullOrWhiteSpace(model.Role) ? L("DebugChat.Unknown") : model.Role,
                    string.IsNullOrWhiteSpace(model.Status) ? L("DebugChat.Unknown") : model.Status);
                AddLog(string.Format(System.Globalization.CultureInfo.InvariantCulture, L("DebugChat.LogModelSelected"), model.Name));
                return;
            }

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
        _serverRuntimeService.Stop();
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
        ModelsComboBox.SelectedItem = models.FirstOrDefault(model => model.IsCoreModel && model.IsRunnable)
            ?? models.FirstOrDefault(model => model.IsRunnable)
            ?? models.FirstOrDefault();

        AddLog(string.Format(System.Globalization.CultureInfo.InvariantCulture, L("DebugChat.LogModelsFound"), models.Count));
        AddLog(_serverRuntimeService.IsAvailable
            ? string.Format(System.Globalization.CultureInfo.InvariantCulture, L("DebugChat.LogServerBackendFound"), _serverRuntimeService.ExpectedExecutablePath)
            : string.Format(System.Globalization.CultureInfo.InvariantCulture, L("DebugChat.LogServerBackendMissing"), _serverRuntimeService.ExpectedExecutablePath));
        AddLog(_cliRuntimeService.IsAvailable
            ? string.Format(System.Globalization.CultureInfo.InvariantCulture, L("DebugChat.LogCliBackendFound"), _cliRuntimeService.ExpectedExecutablePath)
            : string.Format(System.Globalization.CultureInfo.InvariantCulture, L("DebugChat.LogCliBackendMissing"), _cliRuntimeService.ExpectedExecutablePath));

        if (models.Count == 0)
        {
            StatusText.Text = L("DebugChat.NoModels");
        }
        else if (!_serverRuntimeService.IsAvailable && !_cliRuntimeService.IsAvailable)
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

        if (!model.IsRunnable)
        {
            StatusText.Text = L("DebugChat.ToolModelNotRunnable");
            AddLog(string.Format(System.Globalization.CultureInfo.InvariantCulture, L("DebugChat.LogToolModelNotRunnable"), model.Name));
            return;
        }

        var prompt = PromptTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            StatusText.Text = L("DebugChat.EmptyPrompt");
            return;
        }

        _generationCts = new CancellationTokenSource();
        var requestHistory = _history.ToList();
        SetBusy(true);
        PromptTextBox.Clear();
        _history.Add(new DebugChatMessage { Role = L("DebugChat.UserRole"), Text = prompt });
        _chatItems.Add($"{L("DebugChat.UserRole")}: {prompt}");
        _debugSessionLog.Write("debug_user_message", new
        {
            Model = model.Name,
            ModelPath = model.Path,
            Text = prompt
        });
        AddLog(L("DebugChat.LogPromptSent"));
        StatusText.Text = L("DebugChat.Generating");

        try
        {
            var response = _toolGateway.IsToolCommand(prompt)
                ? await ExecuteToolCommandAsync(prompt, _generationCts.Token)
                : IsCoreToolTestCommand(prompt)
                    ? await ExecuteCoreToolTestAsync(model, prompt, _generationCts.Token)
                    : await ExecuteDebugToolAgentAsync(model, requestHistory, prompt, _generationCts.Token);
            _history.Add(new DebugChatMessage { Role = L("DebugChat.ModelRole"), Text = response });
            _chatItems.Add($"{L("DebugChat.ModelRole")}: {response}");
            _debugSessionLog.Write("debug_assistant_message", new
            {
                Model = model.Name,
                ModelPath = model.Path,
                Text = response
            });
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

    private async Task<string> ExecuteToolCommandAsync(string prompt, CancellationToken cancellationToken)
    {
        AddLog(L("DebugChat.LogUsingToolGateway"));
        var progress = CreateDownloadProgress();
        var result = await _toolGateway.ExecuteAsync(prompt, _storageSettings, _debugSessionLog, cancellationToken, progress);
        AddLog(L("DebugChat.LogToolCommandDone"));
        return result;
    }

    private async Task<string> ExecuteCoreToolTestAsync(
        DebugModelInfo model,
        string prompt,
        CancellationToken cancellationToken)
    {
        AddLog(L("DebugChat.LogCoreToolTestStarted"));
        _debugSessionLog.Write("core_tool_test_start", new { Prompt = prompt });

        var task = prompt[CoreToolTestPrefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(task))
        {
            task = "Найди и скачай самую маленькую полезную Qwen GGUF-модель для будущих тестов AI HUB.";
        }

        return await ExecuteToolAgentLoopAsync(
            model,
            [],
            BuildCoreToolTestPrompt(task),
            "core_tool_test",
            task,
            cancellationToken);
    }

    private async Task<string> ExecuteDebugToolAgentAsync(
        DebugModelInfo model,
        IReadOnlyList<DebugChatMessage> requestHistory,
        string prompt,
        CancellationToken cancellationToken)
    {
        AddLog(L("DebugChat.LogToolAgentStarted"));
        _debugSessionLog.Write("debug_tool_agent_start", new { Prompt = prompt });

        var result = await ExecuteToolAgentLoopAsync(
            model,
            requestHistory,
            BuildDebugToolAgentPrompt(prompt, requestHistory),
            "debug_tool_agent",
            prompt,
            cancellationToken);

        return result;
    }

    private async Task<string> ExecuteToolAgentLoopAsync(
        DebugModelInfo model,
        IReadOnlyList<DebugChatMessage> conversationHistory,
        string initialPrompt,
        string eventPrefix,
        string fallbackSearchQuery,
        CancellationToken cancellationToken)
    {
        var agentHistory = conversationHistory.ToList();
        var nextPrompt = initialPrompt;
        var usedTool = false;
        var downloadRequested = IsDownloadRequested(fallbackSearchQuery);
        var successfulDownload = false;
        var lastDownloadResult = string.Empty;
        var toolRequestCount = 0;

        for (var step = 1; step <= MaxDebugAgentSteps; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddLog(string.Format(System.Globalization.CultureInfo.InvariantCulture, L("DebugChat.LogCoreToolStep"), step));

            var modelResponse = await GenerateWithPreferredRuntimeAsync(model, agentHistory, nextPrompt, cancellationToken);
            _debugSessionLog.Write($"{eventPrefix}_model_response", new { Step = step, Text = modelResponse });

            var command = ExtractToolCommand(modelResponse);
            if (command is null)
            {
                if (!usedTool && IsToolAccessRefusal(modelResponse))
                {
                    command = "web_search: " + fallbackSearchQuery;
                    AddLog(string.Format(System.Globalization.CultureInfo.InvariantCulture, L("DebugChat.LogCoreToolCommand"), command));
                    _debugSessionLog.Write($"{eventPrefix}_refusal_recovered", new { Step = step, Command = command, Refusal = modelResponse });
                }
                else
                {
                    var final = ExtractFinalAnswer(modelResponse);
                    if (downloadRequested && !successfulDownload)
                    {
                        if (toolRequestCount >= MaxDebugToolRequests)
                        {
                            _debugSessionLog.Write($"{eventPrefix}_finish_blocked_without_download", new { Step = step, ToolRequests = toolRequestCount, Final = final });
                            return "Файл не скачан: ядро не получило подтверждения `Web download complete` от инструмента скачивания. Нужна прямая ссылка на файл или другой источник.";
                        }

                        nextPrompt = BuildDownloadRequiredPrompt(final, toolRequestCount);
                        _debugSessionLog.Write($"{eventPrefix}_premature_final_blocked", new { Step = step, ToolRequests = toolRequestCount, Final = final });
                        continue;
                    }

                    AddLog(eventPrefix.Equals("core_tool_test", StringComparison.OrdinalIgnoreCase)
                        ? L("DebugChat.LogCoreToolTestFinished")
                        : L("DebugChat.LogToolAgentFinished"));
                    _debugSessionLog.Write($"{eventPrefix}_finish", new { Step = step, ToolRequests = toolRequestCount, Final = final });
                    return final;
                }
            }

            var originalCommand = command;
            command = NormalizeToolCommand(command, downloadRequested);
            if (!string.Equals(originalCommand, command, StringComparison.Ordinal))
            {
                _debugSessionLog.Write($"{eventPrefix}_tool_command_normalized", new { Step = step, Original = originalCommand, Normalized = command });
            }

            if (toolRequestCount >= MaxDebugToolRequests)
            {
                _debugSessionLog.Write($"{eventPrefix}_tool_limit_reached", new { Step = step, ToolRequests = toolRequestCount, RequestedCommand = command });
                if (downloadRequested && !successfulDownload)
                {
                    return "Файл не скачан: достигнут лимит инструментов, но ни один `web_download` не подтвердил успешное сохранение файла.";
                }

                var finalPrompt = BuildToolLimitPrompt(command);
                var finalResponse = await GenerateWithPreferredRuntimeAsync(model, agentHistory, finalPrompt, cancellationToken);
                var final = ExtractFinalAnswer(finalResponse);
                AddLog(eventPrefix.Equals("core_tool_test", StringComparison.OrdinalIgnoreCase)
                    ? L("DebugChat.LogCoreToolTestFinished")
                    : L("DebugChat.LogToolAgentFinished"));
                _debugSessionLog.Write($"{eventPrefix}_finish", new { Step = step, ToolRequests = toolRequestCount, LimitReached = true, Final = final });
                return final;
            }

            toolRequestCount++;
            AddLog(string.Format(System.Globalization.CultureInfo.InvariantCulture, L("DebugChat.LogCoreToolCommand"), command));
            var progress = CreateDownloadProgress();
            var toolResult = await _toolGateway.ExecuteAsync(command, _storageSettings, _debugSessionLog, cancellationToken, progress);
            usedTool = true;
            if (IsSuccessfulDownloadResult(toolResult))
            {
                successfulDownload = true;
                lastDownloadResult = toolResult;
            }

            agentHistory.Add(new DebugChatMessage { Role = L("DebugChat.UserRole"), Text = nextPrompt });
            agentHistory.Add(new DebugChatMessage { Role = L("DebugChat.ModelRole"), Text = modelResponse });

            nextPrompt = BuildToolResultPrompt(toolResult, toolRequestCount, downloadRequested, successfulDownload, lastDownloadResult);
        }

        throw new InvalidOperationException(L("DebugChat.CoreToolTestStepLimit"));
    }

    private static bool IsCoreToolTestCommand(string prompt)
    {
        return prompt.TrimStart().StartsWith(CoreToolTestPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildCoreToolTestPrompt(string task)
    {
        return string.Join(
            Environment.NewLine,
            "Ты ядро AI HUB и проверяешь, что можешь пользоваться интернет-инструментами программы.",
            "Выполни задачу через инструменты, не придумывай результат без проверки.",
            "Доступные инструменты. Если нужен инструмент, ответь только одной строкой:",
            "web_search: поисковый запрос",
            "web_read: https://адрес-страницы",
            "web_download: https://прямая-ссылка-на-файл",
            "Если работа закончена, ответь строкой, начинающейся с FINAL:",
            $"Можно сделать до {MaxDebugToolRequests} tool-запросов. Не ограничивайся первым сайтом: проверяй несколько источников и выбирай лучший подходящий результат.",
            "Для Hugging Face прямую ссылку на файл обычно можно собрать как https://huggingface.co/<repo>/resolve/main/<filename>.",
            "Если задача требует скачать файл, запрещено отвечать FINAL: до результата `Web download complete` от инструмента `web_download`.",
            "Не запускай скачанный файл. Для теста предпочитай маленькую Qwen GGUF-модель, например Qwen3 0.6B Q4_K_M.",
            string.Empty,
            "Задача:",
            task);
    }

    private static string BuildDebugToolAgentPrompt(
        string task,
        IReadOnlyList<DebugChatMessage> requestHistory)
    {
        var history = requestHistory
            .TakeLast(6)
            .Select(message => $"{message.Role}: {LimitForPrompt(message.Text, 700)}")
            .ToList();

        return string.Join(
            Environment.NewLine,
            "Ты debug-ядро AI HUB. В этом окне тебе доступны все подключённые сейчас возможности программы.",
            "Отвечай пользователю по делу. Если можешь ответить без инструмента, ответь строкой, начинающейся с FINAL:",
            "Если нужен интернет или скачивание, запроси ровно один инструмент одной строкой:",
            "web_search: поисковый запрос",
            "web_read: https://адрес-страницы",
            "web_download: https://прямая-ссылка-на-файл",
            "Инструменты выполняет AI HUB. Ты не запускаешь скачанные файлы и не утверждаешь, что файл скачан, пока не получил результат инструмента.",
            "Запрещено отвечать, что у тебя нет доступа к интернету или файлам, пока ты не попробовал доступные инструменты AI HUB.",
            $"Можно сделать до {MaxDebugToolRequests} tool-запросов на один пользовательский запрос. Для поиска и скачивания не ограничивайся первым сайтом: проверяй несколько источников и выбирай лучший подходящий результат.",
            "Если пользователь просит найти или скачать публичный файл/материал и прямой ссылки нет, первым шагом используй web_search.",
            "Если пользователь просит скачать файл по прямой ссылке, сразу используй web_download.",
            "Если пользователь просит скачать, запрещено отвечать FINAL: до результата `Web download complete` от инструмента `web_download`.",
            "Для просьбы скачать картинку сначала используй поиск, потом чтение подходящей страницы, затем скачивай только прямую ссылку на файл, если она найдена.",
            "Не используй web_download для страниц поиска вроде /search, /images/search или URL без признаков файла, если пользователь просит именно картинку.",
            "Если web_download вернул Content-Kind: html или Warning, это не картинка; продолжай искать прямую ссылку на изображение.",
            string.Empty,
            "Краткая история диалога:",
            history.Count == 0 ? "(пусто)" : string.Join(Environment.NewLine, history),
            string.Empty,
            "Текущий запрос пользователя:",
            task);
    }

    private static string BuildToolResultPrompt(
        string toolResult,
        int toolRequestCount,
        bool downloadRequested,
        bool successfulDownload,
        string lastDownloadResult)
    {
        var prompt = "Результат инструмента AI HUB:" + Environment.NewLine
            + LimitForPrompt(toolResult)
            + Environment.NewLine
            + $"Это tool-запрос {toolRequestCount} из {MaxDebugToolRequests}."
            + Environment.NewLine
            + "Если результат полностью соответствует задаче пользователя, ответь FINAL: с коротким итогом."
            + Environment.NewLine
            + "Если это ошибка, страница поиска, HTML вместо нужного файла, неподходящий формат, слишком низкое качество или сомнительный источник, продолжай через следующий инструмент."
            + Environment.NewLine
            + "Сравнивай несколько источников и выбирай наиболее подходящий результат, а не первый попавшийся сайт.";

        if (downloadRequested && !successfulDownload)
        {
            prompt += Environment.NewLine
                + "ВАЖНО: пользователь просил скачать файл. Пока нет успешного результата `Web download complete` от `web_download`, нельзя отвечать FINAL: и нельзя писать, что файл сохранён. Найди прямую ссылку и вызови `web_download:`.";
        }
        else if (downloadRequested)
        {
            prompt += Environment.NewLine
                + "Файл уже скачан инструментом. В FINAL: обязательно укажи путь файла из результата скачивания."
                + Environment.NewLine
                + LimitForPrompt(lastDownloadResult, 1200);
        }

        return prompt;
    }

    private static string BuildDownloadRequiredPrompt(string prematureFinal, int toolRequestCount)
    {
        return "Предыдущий ответ нельзя принять как финальный." + Environment.NewLine
            + $"Ты хотел ответить: {LimitForPrompt(prematureFinal, 1000)}"
            + Environment.NewLine
            + $"Сделано tool-запросов: {toolRequestCount} из {MaxDebugToolRequests}."
            + Environment.NewLine
            + "Пользователь просил скачать файл, но `web_download` ещё не вернул `Web download complete`."
            + Environment.NewLine
            + "Продолжай: найди прямую ссылку на файл и вызови `web_download: https://...`. Если прямую ссылку найти невозможно, продолжай проверять другие источники до лимита.";
    }

    private static string BuildToolLimitPrompt(string requestedCommand)
    {
        return $"Лимит инструментов AI HUB на этот запрос достигнут: {MaxDebugToolRequests} tool-запросов."
            + Environment.NewLine
            + $"Следующий запрошенный инструмент не выполнен: {requestedCommand}"
            + Environment.NewLine
            + "Выбери лучший результат из уже найденных данных и ответь строкой, начинающейся с FINAL:. Если подходящий результат не найден, честно объясни это коротко.";
    }

    private static string? ExtractToolCommand(string text)
    {
        foreach (var rawLine in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.Trim().Trim('`', '"', '\'');
            if (line.StartsWith("web_search:", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("web_read:", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("web_download:", StringComparison.OrdinalIgnoreCase))
            {
                return line;
            }
        }

        return null;
    }

    private static string NormalizeToolCommand(string command, bool downloadRequested)
    {
        if (!downloadRequested || !command.StartsWith("web_read:", StringComparison.OrdinalIgnoreCase))
        {
            return command;
        }

        var url = command["web_read:".Length..].Trim();
        return IsDirectFileUrl(url) ? "web_download: " + url : command;
    }

    private static string ExtractFinalAnswer(string text)
    {
        foreach (var rawLine in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith("FINAL:", StringComparison.OrdinalIgnoreCase))
            {
                return line["FINAL:".Length..].Trim();
            }
        }

        return text.Trim();
    }

    private static bool IsDownloadRequested(string task)
    {
        var text = task.ToLowerInvariant();
        return text.Contains("скач", StringComparison.Ordinal)
            || text.Contains("загруз", StringComparison.Ordinal)
            || text.Contains("download", StringComparison.Ordinal)
            || text.Contains("save file", StringComparison.Ordinal);
    }

    private static bool IsSuccessfulDownloadResult(string toolResult)
    {
        return toolResult.Contains("Web download complete.", StringComparison.OrdinalIgnoreCase)
            && toolResult.Contains("File:", StringComparison.OrdinalIgnoreCase)
            && !toolResult.Contains("Content-Kind: html", StringComparison.OrdinalIgnoreCase)
            && !toolResult.Contains("Warning:", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDirectFileUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var extension = System.IO.Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".bmp" or ".svg"
            or ".mp3" or ".wav" or ".ogg" or ".flac" or ".m4a"
            or ".mp4" or ".webm" or ".avi" or ".mov" or ".mkv"
            or ".pdf" or ".txt" or ".json" or ".csv" or ".zip" or ".7z" or ".rar" or ".gz"
            or ".gguf" or ".bin";
    }

    private static bool IsToolAccessRefusal(string text)
    {
        return text.Contains("нет доступа", StringComparison.OrdinalIgnoreCase)
            || text.Contains("не могу получить доступ", StringComparison.OrdinalIgnoreCase)
            || text.Contains("невозможно выполнить", StringComparison.OrdinalIgnoreCase)
            || text.Contains("no access", StringComparison.OrdinalIgnoreCase)
            || text.Contains("cannot access", StringComparison.OrdinalIgnoreCase);
    }

    private static string LimitForPrompt(string text)
    {
        return LimitForPrompt(text, 5000);
    }

    private static string LimitForPrompt(string text, int maxLength)
    {
        return text.Length <= maxLength ? text : text[..maxLength] + Environment.NewLine + "...";
    }

    private void SetBusy(bool isBusy)
    {
        SendButton.IsEnabled = !isBusy;
        RefreshModelsButton.IsEnabled = !isBusy;
        ModelsComboBox.IsEnabled = !isBusy;
        StopButton.IsEnabled = isBusy;
        if (!isBusy)
        {
            DownloadProgressBar.Visibility = Visibility.Collapsed;
            DownloadProgressText.Visibility = Visibility.Collapsed;
            DownloadProgressBar.IsIndeterminate = false;
            DownloadProgressBar.Value = 0;
        }
    }

    private IProgress<WebDownloadProgress> CreateDownloadProgress()
    {
        return new Progress<WebDownloadProgress>(UpdateDownloadProgress);
    }

    private void UpdateDownloadProgress(WebDownloadProgress progress)
    {
        Dispatcher.Invoke(() =>
        {
            DownloadProgressBar.Visibility = Visibility.Visible;
            DownloadProgressText.Visibility = Visibility.Visible;
            DownloadProgressBar.IsIndeterminate = progress.TotalBytes is null or <= 0;

            var downloaded = FormatBytes(progress.DownloadedBytes);
            var speed = FormatBytes((long)Math.Max(0, progress.BytesPerSecond)) + L("Units.PerSecond");
            string message;

            if (progress.TotalBytes is > 0)
            {
                var percent = Math.Clamp(progress.DownloadedBytes * 100d / progress.TotalBytes.Value, 0, 100);
                DownloadProgressBar.Value = percent;
                message = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    L(progress.IsComplete ? "DebugChat.DownloadComplete" : "DebugChat.DownloadProgress"),
                    percent,
                    downloaded,
                    FormatBytes(progress.TotalBytes.Value),
                    speed);
            }
            else
            {
                message = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    L(progress.IsComplete ? "DebugChat.DownloadCompleteUnknownTotal" : "DebugChat.DownloadProgressUnknownTotal"),
                    downloaded,
                    speed);
            }

            StatusText.Text = message;
            DownloadProgressText.Text = message;
        });
    }

    private async Task<string> GenerateWithPreferredRuntimeAsync(
        DebugModelInfo model,
        IReadOnlyList<DebugChatMessage> requestHistory,
        string prompt,
        CancellationToken cancellationToken)
    {
        if (_serverRuntimeService.IsAvailable)
        {
            try
            {
                AddLog(L("DebugChat.LogUsingServerBackend"));
                return await _serverRuntimeService.GenerateAsync(model, requestHistory, prompt, AddLog, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (_cliRuntimeService.IsAvailable)
            {
                AddLog(string.Format(System.Globalization.CultureInfo.InvariantCulture, L("DebugChat.LogServerFallback"), ex.Message));
            }
        }

        if (!_cliRuntimeService.IsAvailable)
        {
            throw new InvalidOperationException(L("DebugChat.BackendMissing"));
        }

        AddLog(L("DebugChat.LogUsingCliBackend"));
        return await _cliRuntimeService.GenerateAsync(model, requestHistory, prompt, AddLog, cancellationToken);
    }

    private void AddLog(string message)
    {
        _debugSessionLog.Write("debug_log", new { Message = message });
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

    private string FormatBytes(long bytes)
    {
        const double scale = 1024;
        var value = (double)bytes;
        var unit = L("Units.Bytes");

        if (value >= scale)
        {
            value /= scale;
            unit = L("Units.Kb");
        }

        if (value >= scale)
        {
            value /= scale;
            unit = L("Units.Mb");
        }

        if (value >= scale)
        {
            value /= scale;
            unit = L("Units.Gb");
        }

        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.##} {1}", value, unit);
    }

    private static string GetAppVersion()
    {
        return typeof(DebugChatWindow).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), inherit: false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()
            ?.InformationalVersion ?? "unknown";
    }
}
