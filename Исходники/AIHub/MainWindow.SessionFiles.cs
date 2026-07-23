using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using AIHub.Models;
using AIHub.Services;
using FilePickerDialog = Microsoft.Win32.OpenFileDialog;
using WpfButton = System.Windows.Controls.Button;
using WpfContextMenu = System.Windows.Controls.ContextMenu;
using WpfMenuItem = System.Windows.Controls.MenuItem;

namespace AIHub;

public partial class MainWindow
{
    private void ShowCustomChoiceMenu(WpfButton anchor, bool executorContext)
    {
        var menu = new WpfContextMenu
        {
            PlacementTarget = anchor,
            Placement = PlacementMode.Bottom,
            HorizontalOffset = 0,
            VerticalOffset = 2
        };
        var textItem = new WpfMenuItem
        {
            Header = L("ChoiceScenario.CustomAction.Text")
        };
        textItem.Click += (_, _) =>
        {
            if (executorContext)
            {
                ExecutorCustomInputPanel.Visibility = Visibility.Visible;
                ExecutorCustomInput.Clear();
                ExecutorCustomInput.Focus();
                StatusText.Text = L("Status.ExecutorCustomInput");
            }
            else
            {
                ShowCoreCustomTextInput();
            }
        };
        var filesItem = new WpfMenuItem
        {
            Header = L("ChoiceScenario.CustomAction.Files")
        };
        filesItem.Click += async (_, _) => await AddSessionFilesFromCustomActionAsync(executorContext);
        menu.Items.Add(textItem);
        menu.Items.Add(filesItem);
        anchor.ContextMenu = menu;
        menu.IsOpen = true;
    }

    private int AddSessionFilesFromPicker()
    {
        if (_activeResumableSession is null)
        {
            return -1;
        }

        var dialog = new FilePickerDialog
        {
            Title = L("ChoiceScenario.Files.PickerTitle"),
            Filter = L("ChoiceScenario.Files.PickerFilter"),
            Multiselect = true,
            CheckFileExists = true,
            CheckPathExists = true,
            DereferenceLinks = true
        };
        if (dialog.ShowDialog(this) != true)
        {
            return -1;
        }

        var added = _sessionFileManifestService.AddFiles(
            _activeResumableSession.FileManifest,
            dialog.FileNames);
        RefreshSessionFileCards();
        return added;
    }

    private async Task AddSessionFilesFromCustomActionAsync(bool executorContext)
    {
        var added = AddSessionFilesFromPicker();
        if (added < 0)
        {
            StatusText.Text = L("Status.ChoiceScenarioFileSelectionCancelled");
            return;
        }

        if (added == 0)
        {
            StatusText.Text = L("Status.ChoiceScenarioFilesAlreadyAdded");
            return;
        }

        ApplyFileManifestToCoreProfile();
        WriteFileManifestEvent("scenario_files_added");
        SaveActiveSessionCheckpoint();
        StatusText.Text = LF("Status.ChoiceScenarioFilesAdded", added);
        await NotifyActiveModelAboutFileManifestAsync(executorContext);
    }

    private async void SessionFileRemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not WpfButton { Tag: string id }
            || _activeResumableSession is null
            || !_sessionFileManifestService.RemoveFile(_activeResumableSession.FileManifest, id))
        {
            return;
        }

        ApplyFileManifestToCoreProfile();
        RefreshSessionFileCards();
        WriteFileManifestEvent("scenario_file_removed");
        SaveActiveSessionCheckpoint();
        StatusText.Text = L("Status.ChoiceScenarioFileRemoved");
        var executorContext = ExecutorResultPanel.Visibility == Visibility.Visible
            && _executorWorkflowService.CreateCheckpoint() is not null;
        await NotifyActiveModelAboutFileManifestAsync(executorContext);
    }

    private async Task NotifyActiveModelAboutFileManifestAsync(bool executorContext)
    {
        if (_activeResumableSession is null)
        {
            return;
        }

        if (executorContext)
        {
            await UpdateExecutorFileManifestAsync();
            return;
        }

        if (_currentChoiceScenarioStep is null
            || _choiceScenarioState.StepBudget is null
            || string.Equals(_currentChoiceScenarioStep.StepType, "budget_setup", StringComparison.Ordinal)
            || string.Equals(
                _currentChoiceScenarioStep.StepType,
                ChoiceScenarioService.FileSetupStepType,
                StringComparison.Ordinal))
        {
            return;
        }

        await RequestNextChoiceScenarioStepAsync(
            requestFinal: _currentChoiceScenarioStep.IsFinal,
            consumesAnswer: false,
            requestTrigger: "file_manifest_updated");
    }

    private SessionFileManifest GetActiveFileManifest() =>
        _activeResumableSession?.FileManifest
        ?? throw new InvalidOperationException("The active scenario has no file manifest.");

    private void ApplyFileManifestToCoreProfile()
    {
        if (_activeResumableSession is null)
        {
            return;
        }

        _choiceScenarioState.ApplyTrustedProfileUpdate(
            _sessionFileManifestService.CreateCapabilityUpdate(
                _activeResumableSession.FileManifest));
    }

    private void RefreshSessionFileCards()
    {
        _sessionFileCards.Clear();
        if (_activeResumableSession is null)
        {
            ChoiceSessionFilesPanel.Visibility = Visibility.Collapsed;
            ExecutorSessionFilesPanel.Visibility = Visibility.Collapsed;
            return;
        }

        _sessionFileManifestService.RefreshAvailability(_activeResumableSession.FileManifest);
        foreach (var file in _activeResumableSession.FileManifest.Files)
        {
            var extension = string.IsNullOrWhiteSpace(file.Extension)
                ? L("ChoiceScenario.Files.NoExtension")
                : file.Extension.TrimStart('.').ToUpperInvariant();
            var category = L($"ChoiceScenario.Files.Category.{file.Category}");
            _sessionFileCards.Add(new SessionFileCardViewModel
            {
                Id = file.Id,
                DisplayName = file.DisplayName,
                Details = LF(
                    "ChoiceScenario.Files.CardDetails",
                    extension,
                    category,
                    FormatBytes(file.SizeBytes)),
                AvailabilityText = file.IsAvailable
                    ? L("ChoiceScenario.Files.Available")
                    : L("ChoiceScenario.Files.Missing"),
                IsAvailable = file.IsAvailable,
                RemoveTooltip = L("ChoiceScenario.Files.Remove")
            });
        }

        var visibility = _sessionFileCards.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        ChoiceSessionFilesPanel.Visibility = visibility;
        ExecutorSessionFilesPanel.Visibility = visibility;
    }

    private void WriteFileManifestEvent(string eventType)
    {
        if (_activeResumableSession is null)
        {
            return;
        }

        var safeManifest = _sessionFileManifestService.CreatePromptManifest(
            _activeResumableSession.FileManifest);
        _choiceScenarioLog?.Write(eventType, safeManifest);
        _executorWorkflowService.Write(eventType, safeManifest);
    }
}
