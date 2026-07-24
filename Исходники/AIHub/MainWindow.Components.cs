using System.ComponentModel;
using System.Windows;
using AIHub.Models;
using AIHub.Services;
using WpfButton = System.Windows.Controls.Button;
using WpfMessageBox = System.Windows.MessageBox;

namespace AIHub;

public partial class MainWindow
{
    private readonly ComponentManager _componentManager = new();
    private readonly ComponentCatalogViewModel _componentCatalogViewModel = new();
    private readonly FileViewerService _fileViewerService = new();
    private CancellationTokenSource? _componentOperationCts;
    private bool _isApplyingViewerSettings;

    private void InitializeComponentCatalogUi()
    {
        ProcessingComponentsItemsControl.ItemsSource = _componentCatalogViewModel.Processing;
        ViewerComponentsItemsControl.ItemsSource = _componentCatalogViewModel.Viewers;
        _isApplyingViewerSettings = true;
        PreferInternalViewersCheckBox.IsChecked = _appSettings.FileViewer.PreferInternalViewers;
        _isApplyingViewerSettings = false;
        RefreshComponentCatalogUi();
    }

    private void RefreshComponentCatalogUi()
    {
        var statuses = _componentManager.GetStatus();
        _componentCatalogViewModel.Processing.Clear();
        _componentCatalogViewModel.Viewers.Clear();
        foreach (var status in statuses)
        {
            var card = new ComponentCardViewModel
            {
                Entry = status.Entry,
                Status = LocalizeComponentStatus(status),
                CanDownload = !status.Entry.IsBuiltIn
                    && !status.Entry.IsPlanned
                    && !status.IsAvailable
                    && status.Record.Status != ComponentInstallStatuses.Downloading,
                CanRemove = !status.Entry.IsBuiltIn
                    && (status.IsInstalled
                        || !string.IsNullOrWhiteSpace(status.Record.DownloadPath)),
                PreferInternal = status.Entry.Extensions.Count == 0
                    || status.Entry.Extensions.All(extension =>
                        !_appSettings.FileViewer.PreferInternalByExtension.TryGetValue(
                            extension,
                            out var preference)
                        ? _appSettings.FileViewer.PreferInternalViewers
                        : preference),
                PreferInternalLabel = L("Components.PreferInternalForFormats"),
                DownloadLabel = L("Components.Download"),
                VerifyLabel = L("Components.Verify"),
                RemoveLabel = L("Components.Remove")
            };
            if (status.Entry.Kind == ComponentKinds.Viewer)
            {
                _componentCatalogViewModel.Viewers.Add(card);
            }
            else
            {
                _componentCatalogViewModel.Processing.Add(card);
            }
        }
    }

    private string LocalizeComponentStatus(ComponentStatusSnapshot status)
    {
        if (status.Entry.IsBuiltIn)
        {
            return L("Components.Status.BuiltIn");
        }
        if (status.Entry.IsPlanned)
        {
            return L("Components.Status.Planned");
        }
        if (status.IsAvailable)
        {
            return L("Components.Status.Installed");
        }
        return status.Record.Status switch
        {
            ComponentInstallStatuses.Downloading => L("Components.Status.Downloading"),
            ComponentInstallStatuses.Downloaded => L("Components.Status.Downloaded"),
            ComponentInstallStatuses.NeedsVerification => L("Components.Status.NeedsVerification"),
            ComponentInstallStatuses.Failed => L("Components.Status.Failed"),
            _ => L("Components.Status.NotInstalled")
        };
    }

    private async void ComponentDownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton { Tag: ComponentCardViewModel card })
        {
            await DownloadComponentsAsync([card]);
        }
    }

    private void ComponentVerifyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not WpfButton { Tag: ComponentCardViewModel card })
        {
            return;
        }

        var status = _componentManager.Verify(card.Entry.Id);
        ComponentOperationStatusText.Text = status.IsAvailable
            ? LF("Components.Verified", card.DisplayName)
            : LF("Components.VerificationFailed", card.DisplayName);
        RefreshComponentCatalogUi();
    }

    private void ComponentRemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not WpfButton { Tag: ComponentCardViewModel card })
        {
            return;
        }

        var confirmation = WpfMessageBox.Show(
            this,
            LF("Components.RemoveConfirm", card.DisplayName),
            L("Components.RemoveTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        _componentManager.Remove(card.Entry.Id);
        ComponentOperationStatusText.Text = LF("Components.Removed", card.DisplayName);
        RefreshComponentCatalogUi();
    }

    private async void DownloadSelectedComponentsButton_Click(object sender, RoutedEventArgs e) =>
        await DownloadComponentsAsync(_componentCatalogViewModel.Processing.Where(card => card.IsSelected));

    private async void DownloadSelectedViewersButton_Click(object sender, RoutedEventArgs e) =>
        await DownloadComponentsAsync(_componentCatalogViewModel.Viewers.Where(card => card.IsSelected));

    private async void DownloadAllComponentsButton_Click(object sender, RoutedEventArgs e) =>
        await DownloadComponentsAsync(_componentCatalogViewModel.Processing);

    private async void DownloadAllViewersButton_Click(object sender, RoutedEventArgs e) =>
        await DownloadComponentsAsync(_componentCatalogViewModel.Viewers);

    private void VerifyInstalledComponentsButton_Click(object sender, RoutedEventArgs e)
    {
        VerifyCatalog(ComponentKinds.Processing);
    }

    private void VerifyInstalledViewersButton_Click(object sender, RoutedEventArgs e)
    {
        VerifyCatalog(ComponentKinds.Viewer);
    }

    private void VerifyCatalog(string kind)
    {
        foreach (var status in _componentManager.GetStatus(kind)
                     .Where(status => status.Entry.IsBuiltIn || status.IsInstalled))
        {
            _componentManager.Verify(status.Entry.Id);
        }
        ComponentOperationStatusText.Text = L("Components.VerificationComplete");
        RefreshComponentCatalogUi();
    }

    private async Task DownloadComponentsAsync(IEnumerable<ComponentCardViewModel> source)
    {
        var requested = source
            .Where(card => card.CanDownload)
            .GroupBy(card => card.Entry.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        if (requested.Count == 0)
        {
            ComponentOperationStatusText.Text = L("Components.NothingSelected");
            return;
        }

        var currentStatuses = _componentManager.GetStatus()
            .ToDictionary(status => status.Entry.Id, StringComparer.OrdinalIgnoreCase);
        var selected = ComponentCatalog.ResolveDependencies(
                requested.Select(card => card.Entry.Id))
            .Where(entry => !entry.IsBuiltIn
                && !entry.IsPlanned
                && (!currentStatuses.TryGetValue(entry.Id, out var status)
                    || !status.IsAvailable))
            .Select(entry => new ComponentCardViewModel
            {
                Entry = entry,
                CanDownload = true
            })
            .ToList();
        var total = selected.Sum(card => card.Entry.DownloadSizeBytes);
        var names = string.Join(Environment.NewLine, selected.Select(card => $"• {card.DisplayName}"));
        var confirmation = WpfMessageBox.Show(
            this,
            LF(
                "Components.DownloadConfirm",
                names,
                ComponentCardViewModel.FormatBytes(total)),
            L("Components.DownloadTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);
        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        _componentOperationCts?.Cancel();
        _componentOperationCts?.Dispose();
        _componentOperationCts = new CancellationTokenSource();
        SetComponentControlsEnabled(false);
        try
        {
            foreach (var card in selected)
            {
                var visibleCard = FindComponentCard(card.Entry.Id);
                SetComponentDownloadProgress(
                    visibleCard,
                    downloadedBytes: 0,
                    totalBytes: card.Entry.DownloadSizeBytes);
                var progress = new Progress<ComponentDownloadProgress>(value =>
                {
                    SetComponentDownloadProgress(
                        FindComponentCard(value.ComponentId) ?? visibleCard,
                        value.DownloadedBytes,
                        value.TotalBytes);
                    ComponentOperationStatusText.Text = LF(
                        "Components.DownloadingProgress",
                        card.DisplayName,
                        ComponentCardViewModel.FormatBytes(value.DownloadedBytes),
                        value.TotalBytes > 0
                            ? ComponentCardViewModel.FormatBytes(value.TotalBytes)
                            : "?");
                });
                var result = await _componentManager.DownloadAndInstallAsync(
                    card.Entry.Id,
                    progress,
                    _componentOperationCts.Token);
                if (visibleCard is not null)
                {
                    visibleCard.ProgressPercent = 100;
                    visibleCard.ProgressText = "100%";
                    visibleCard.IsProgressIndeterminate = false;
                    visibleCard.IsDownloading = false;
                    visibleCard.Status = LocalizeComponentStatus(result);
                }
                if (card.Entry.DeliveryKind == ComponentDeliveryKinds.SystemInstaller)
                {
                    var launch = WpfMessageBox.Show(
                        this,
                        LF("Components.LaunchInstallerConfirm", card.DisplayName, card.Entry.Source),
                        L("Components.LaunchInstallerTitle"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    if (launch == MessageBoxResult.Yes)
                    {
                        _componentManager.LaunchSystemInstaller(card.Entry.Id);
                    }
                }
                else if (!result.IsAvailable)
                {
                    throw new InvalidOperationException(
                        $"Component '{card.DisplayName}' did not pass health-check.");
                }
            }
            ComponentOperationStatusText.Text = L("Components.DownloadComplete");
        }
        catch (OperationCanceledException)
        {
            ComponentOperationStatusText.Text = L("Components.DownloadCancelled");
        }
        catch (Exception ex)
        {
            ComponentOperationStatusText.Text = LF("Components.DownloadFailed", ex.Message);
        }
        finally
        {
            SetComponentControlsEnabled(true);
            RefreshComponentCatalogUi();
        }
    }

    private ComponentCardViewModel? FindComponentCard(string componentId)
    {
        return _componentCatalogViewModel.Processing
            .Concat(_componentCatalogViewModel.Viewers)
            .FirstOrDefault(card => string.Equals(
                card.Entry.Id,
                componentId,
                StringComparison.OrdinalIgnoreCase));
    }

    private void SetComponentDownloadProgress(
        ComponentCardViewModel? card,
        long downloadedBytes,
        long totalBytes)
    {
        if (card is null)
        {
            return;
        }

        var effectiveTotal = totalBytes > 0
            ? totalBytes
            : card.Entry.DownloadSizeBytes;
        card.IsDownloading = true;
        card.IsProgressIndeterminate = effectiveTotal <= 0;
        card.Status = L("Components.Status.Downloading");
        if (effectiveTotal <= 0)
        {
            card.ProgressPercent = 0;
            card.ProgressText = ComponentCardViewModel.FormatBytes(downloadedBytes);
            return;
        }

        var percent = Math.Clamp(downloadedBytes * 100d / effectiveTotal, 0, 100);
        card.ProgressPercent = percent;
        card.ProgressText = $"{percent:0}%";
    }

    private void SetComponentControlsEnabled(bool enabled)
    {
        ProcessingComponentsExpander.IsEnabled = enabled;
        ViewerComponentsExpander.IsEnabled = enabled;
        GlobalFileViewerButton.IsEnabled = enabled
            && ChoiceAiActivityPanel.Visibility != Visibility.Visible;
    }

    private void PreferInternalViewersCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isApplyingViewerSettings)
        {
            return;
        }
        _appSettings.FileViewer.PreferInternalViewers =
            PreferInternalViewersCheckBox.IsChecked == true;
        _appSettingsStore.Save(_appSettings);
    }

    private void ViewerFormatPreferenceCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isApplyingViewerSettings
            || sender is not System.Windows.Controls.CheckBox
            {
                Tag: ComponentCardViewModel card
            } checkBox)
        {
            return;
        }

        foreach (var extension in card.Entry.Extensions)
        {
            _appSettings.FileViewer.PreferInternalByExtension[extension] =
                checkBox.IsChecked == true;
        }
        _appSettingsStore.Save(_appSettings);
    }

    private void GlobalFileViewerButton_Click(object sender, RoutedEventArgs e)
    {
        if (ChoiceAiActivityPanel.Visibility == Visibility.Visible)
        {
            return;
        }
        var path = _fileViewerService.SelectFile(this, _localizationService);
        if (!string.IsNullOrWhiteSpace(path))
        {
            _fileViewerService.Open(
                this,
                path,
                _appSettings.FileViewer,
                _isDarkTheme,
                _localizationService);
        }
    }

    private async Task HandleExecutorCapabilityRequestAsync(ExecutorTurnResult turn)
    {
        var capability = turn.RequestedCapability;
        var providers = ComponentCatalog.FindProviders(capability);
        if (providers.Count == 0)
        {
            await ContinueExecutorAfterCapabilityAsync(
                capability,
                "capability_unavailable",
                "No trusted provider exists in the AI HUB catalog.");
            return;
        }

        if (_componentManager.IsCapabilityAvailable(capability))
        {
            await ContinueExecutorAfterCapabilityAsync(
                capability,
                "capability_available",
                "The requested capability is already installed and passed health-check.");
            return;
        }

        var plan = _componentManager.BuildPlan(
            [capability],
            turn.CapabilityReason,
            turn.CapabilityRequired);
        var pending = plan.Items.Where(item => !item.AlreadyAvailable).ToList();
        var message = string.Join(
            Environment.NewLine,
            LF("Components.ExecutorRequestReason", turn.CapabilityReason),
            string.Empty,
            string.Join(Environment.NewLine, pending.Select(item => $"• {item.Name}")),
            string.Empty,
            LF("Components.ExecutorRequestSize", ComponentCardViewModel.FormatBytes(plan.TotalDownloadBytes)));
        var confirmation = WpfMessageBox.Show(
            this,
            message,
            L("Components.ExecutorRequestTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirmation != MessageBoxResult.Yes)
        {
            await ContinueExecutorAfterCapabilityAsync(
                capability,
                "capability_denied",
                "The user declined the component acquisition plan.");
            return;
        }

        SetExecutorInteractionEnabled(false);
        BackFromChoiceScenarioButton.IsEnabled = false;
        try
        {
            foreach (var item in pending)
            {
                var entry = ComponentCatalog.Find(item.ComponentId)
                    ?? throw new InvalidOperationException(
                        $"Trusted component '{item.ComponentId}' is missing from the catalog.");
                var installResult = await _componentManager.DownloadAndInstallAsync(
                    item.ComponentId,
                    new Progress<ComponentDownloadProgress>(progress =>
                    {
                        StatusText.Text = LF(
                            "Components.DownloadingProgress",
                            item.Name,
                            ComponentCardViewModel.FormatBytes(progress.DownloadedBytes),
                            progress.TotalBytes > 0
                                ? ComponentCardViewModel.FormatBytes(progress.TotalBytes)
                                : "?");
                    }),
                    _executorCts?.Token ?? CancellationToken.None);
                if (entry.DeliveryKind == ComponentDeliveryKinds.SystemInstaller
                    && !installResult.IsAvailable)
                {
                    var launch = WpfMessageBox.Show(
                        this,
                        LF("Components.LaunchInstallerConfirm", item.Name, entry.Source),
                        L("Components.LaunchInstallerTitle"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    if (launch == MessageBoxResult.Yes)
                    {
                        _componentManager.LaunchSystemInstaller(entry.Id);
                    }
                }
            }
            var result = _componentManager.IsCapabilityAvailable(capability)
                ? "capability_available"
                : "capability_needs_manual_install";
            await ContinueExecutorAfterCapabilityAsync(
                capability,
                result,
                result == "capability_available"
                    ? "The trusted component plan completed and passed health-check."
                    : "The package was downloaded, but a system installation or health-check is still required.");
        }
        catch (Exception ex)
        {
            await ContinueExecutorAfterCapabilityAsync(
                capability,
                "capability_install_failed",
                ex.Message);
        }
        finally
        {
            BackFromChoiceScenarioButton.IsEnabled = true;
            SetExecutorInteractionEnabled(true);
        }
    }

    private async Task ContinueExecutorAfterCapabilityAsync(
        string capability,
        string resultCode,
        string details)
    {
        _executorCts?.Dispose();
        _executorCts = new CancellationTokenSource();
        SetExecutorInteractionEnabled(false);
        BackFromChoiceScenarioButton.IsEnabled = false;
        StartChoiceAiActivity();
        try
        {
            var result = await _executorWorkflowService.ContinueAfterCapabilityRequestAsync(
                capability,
                resultCode,
                details,
                CreateMatrixStreamProgress(),
                _executorCts.Token);
            DisplayExecutorResponse(result);
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = L("Status.ExecutorCancelled");
        }
        catch (Exception ex)
        {
            _executorWorkflowService.Write("executor_capability_continuation_failed", new
            {
                Capability = capability,
                Result = resultCode,
                ex.Message,
                ErrorType = ex.GetType().FullName
            });
            StatusText.Text = LF("Status.ExecutorFailed", ex.Message);
        }
        finally
        {
            StopChoiceAiActivity();
            BackFromChoiceScenarioButton.IsEnabled = true;
            SetExecutorInteractionEnabled(true);
        }
    }

    private string BuildExecutorComponentPlanText(ComponentAcquisitionPlan? plan)
    {
        var pending = plan?.Items
            .Where(item => !item.AlreadyAvailable)
            .ToList() ?? [];
        if (pending.Count == 0)
        {
            return string.Empty;
        }

        var names = string.Join(
            Environment.NewLine,
            pending.Select(item => $"• {item.Name}"));
        return Environment.NewLine
            + Environment.NewLine
            + LF(
                "Components.ExecutorKnownPlan",
                names,
                ComponentCardViewModel.FormatBytes(plan!.TotalDownloadBytes));
    }

    private async Task<bool> EnsureExecutorComponentsReadyAsync(
        ComponentAcquisitionPlan? plan,
        bool confirmationAlreadyGranted,
        CancellationToken cancellationToken)
    {
        var pending = plan?.Items
            .Where(item => !item.AlreadyAvailable)
            .ToList() ?? [];
        if (pending.Count == 0)
        {
            return true;
        }

        if (!confirmationAlreadyGranted)
        {
            var confirmation = WpfMessageBox.Show(
                this,
                LF(
                    "Components.ExecutorKnownPlanConfirm",
                    string.Join(Environment.NewLine, pending.Select(item => $"• {item.Name}")),
                    ComponentCardViewModel.FormatBytes(plan!.TotalDownloadBytes)),
                L("Components.DownloadTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            if (confirmation != MessageBoxResult.Yes)
            {
                StatusText.Text = L("Components.ExecutorPlanDeclined");
                return false;
            }
        }

        foreach (var item in pending)
        {
            var entry = ComponentCatalog.Find(item.ComponentId)
                ?? throw new InvalidOperationException(
                    $"Trusted component '{item.ComponentId}' is missing from the catalog.");
            var result = await _componentManager.DownloadAndInstallAsync(
                item.ComponentId,
                new Progress<ComponentDownloadProgress>(progress =>
                {
                    StatusText.Text = LF(
                        "Components.DownloadingProgress",
                        item.Name,
                        ComponentCardViewModel.FormatBytes(progress.DownloadedBytes),
                        progress.TotalBytes > 0
                            ? ComponentCardViewModel.FormatBytes(progress.TotalBytes)
                            : "?");
                }),
                cancellationToken);
            if (entry.DeliveryKind == ComponentDeliveryKinds.SystemInstaller
                && !result.IsAvailable)
            {
                var launch = WpfMessageBox.Show(
                    this,
                    LF("Components.LaunchInstallerConfirm", item.Name, entry.Source),
                    L("Components.LaunchInstallerTitle"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (launch == MessageBoxResult.Yes)
                {
                    _componentManager.LaunchSystemInstaller(entry.Id);
                    StatusText.Text = L("Components.ExecutorInstallerPending");
                }
                else
                {
                    StatusText.Text = L("Components.ExecutorPlanDeclined");
                }
                return false;
            }

            if (!result.IsAvailable)
            {
                throw new InvalidOperationException(
                    $"Component '{item.Name}' did not pass health-check.");
            }
        }

        _pendingExecutorComponentPlan = null;
        return true;
    }
}
