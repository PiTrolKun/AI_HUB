using System.IO;
using System.Windows;
using AIHub.Controls;
using AIHub.Models;
using AIHub.Services;
using WpfMessageBox = System.Windows.MessageBox;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Media = System.Windows.Media;

namespace AIHub;

public partial class MainWindow
{
    private string _imageAnalysisMatrixRole = string.Empty;

    private void ShowImageAnalysisSubscenarioSelection()
    {
        SaveCurrentImageAnalysisSession();
        _imageAnalysisLiterarySession = null;
        ImageAnalysisWorkspacePage.ShowSubscenarioSelection(
            _imageAnalysisSessionStore.LoadAll(_storageSettings));
    }

    private void ImageAnalysisWorkspacePage_SingleSubscenarioRequested(object? sender, EventArgs e)
    {
        _imageAnalysisLiterarySession = new ImageAnalysisLiterarySession
        {
            CurrentStep = ImageAnalysisLiterarySteps.Image,
            Status = ImageAnalysisLiteraryStatuses.Draft
        };
        _imageAnalysisSessionStore.Save(_imageAnalysisLiterarySession, _storageSettings);
        ImageAnalysisWorkspacePage.ShowImageStep(_imageAnalysisLiterarySession);
        StatusText.Text = L("Status.ImageAnalysisChooseFile");
    }

    private async void ImageAnalysisWorkspacePage_SelectImageRequested(object? sender, EventArgs e)
    {
        if (_imageAnalysisLiterarySession is null)
        {
            return;
        }
        var dialog = new WpfOpenFileDialog
        {
            Title = L("ImageAnalysis.Workspace.DialogTitle"),
            Filter = L("ImageAnalysis.Workspace.DialogFilter"),
            Multiselect = false,
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true || !File.Exists(dialog.FileName))
        {
            return;
        }

        CancelImageAnalysisLiteraryOperation();
        _imageAnalysisLiteraryCts = new CancellationTokenSource();
        AddImageAnalysisEvent(
            _imageAnalysisLiterarySession,
            ImageAnalysisEventCodes.FileCheckStarted,
            string.Empty,
            ImageAnalysisEventStatuses.Active,
            Path.GetFileName(dialog.FileName));
        ImageAnalysisWorkspacePage.ShowFileChecking(dialog.FileName);
        ImageAnalysisWorkspacePage.RefreshActivity(_imageAnalysisLiterarySession);
        try
        {
            var passport = await _imageAnalysisFileValidationService.ValidateAsync(
                dialog.FileName,
                _imageAnalysisLiteraryCts.Token);
            _imageAnalysisLiterarySession.File = passport;
            _imageAnalysisLiterarySession.VisualReport = string.Empty;
            _imageAnalysisLiterarySession.Observations.Clear();
            _imageAnalysisLiterarySession.ReviewSummary = new ImageAnalysisReviewSummary();
            _imageAnalysisLiterarySession.Events.Clear();
            _imageAnalysisLiterarySession.Versions.Clear();
            _imageAnalysisLiterarySession.SelectedVersionId = string.Empty;
            _imageAnalysisLiterarySession.CompletedAt = null;
            _imageAnalysisLiterarySession.InternalImageCopyPath = string.Empty;
            _imageAnalysisLiterarySession.InternalDescriptionCopyPath = string.Empty;
            _imageAnalysisLiterarySession.Status = ImageAnalysisLiteraryStatuses.FileReady;
            _imageAnalysisLiterarySession.CurrentStep = ImageAnalysisLiterarySteps.Image;
            _imageAnalysisLiterarySession.LastError = string.Empty;
            AddImageAnalysisEvent(
                _imageAnalysisLiterarySession,
                ImageAnalysisEventCodes.FileCheckStarted,
                string.Empty,
                ImageAnalysisEventStatuses.Completed,
                passport.DisplayName);
            AddImageAnalysisEvent(
                _imageAnalysisLiterarySession,
                ImageAnalysisEventCodes.FileReady,
                string.Empty,
                ImageAnalysisEventStatuses.Completed,
                passport.DisplayName);
            _imageAnalysisSessionStore.Save(_imageAnalysisLiterarySession, _storageSettings);
            ImageAnalysisWorkspacePage.SetValidatedFile(_imageAnalysisLiterarySession);
            StatusText.Text = LF("Status.ImageAnalysisFileSelected", passport.DisplayName);
        }
        catch (OperationCanceledException)
        {
            AddImageAnalysisEvent(
                _imageAnalysisLiterarySession,
                ImageAnalysisEventCodes.FileRejected,
                string.Empty,
                ImageAnalysisEventStatuses.Failed,
                L("ImageAnalysis.Workspace.FileCancelled"));
            _imageAnalysisSessionStore.Save(_imageAnalysisLiterarySession, _storageSettings);
            if (_imageAnalysisLiterarySession.File is null)
            {
                ImageAnalysisWorkspacePage.SetFileError(L("ImageAnalysis.Workspace.FileCancelled"));
                ImageAnalysisWorkspacePage.RefreshActivity(_imageAnalysisLiterarySession);
            }
            else
            {
                ImageAnalysisWorkspacePage.ShowSession(_imageAnalysisLiterarySession);
                ImageAnalysisWorkspacePage.SetOperationError(L("ImageAnalysis.Workspace.FileCancelled"));
            }
        }
        catch (Exception ex)
        {
            _imageAnalysisLiterarySession.LastError = ex.Message;
            AddImageAnalysisEvent(
                _imageAnalysisLiterarySession,
                ImageAnalysisEventCodes.FileRejected,
                string.Empty,
                ImageAnalysisEventStatuses.Failed,
                ex.Message);
            _imageAnalysisSessionStore.Save(_imageAnalysisLiterarySession, _storageSettings);
            if (_imageAnalysisLiterarySession.File is null)
            {
                ImageAnalysisWorkspacePage.SetFileError(ex.Message);
            }
            else
            {
                ImageAnalysisWorkspacePage.ShowSession(_imageAnalysisLiterarySession);
                ImageAnalysisWorkspacePage.SetOperationError(ex.Message);
            }
            StatusText.Text = L("Status.ImageAnalysisFileRejected");
        }
        finally
        {
            _imageAnalysisLiteraryCts?.Dispose();
            _imageAnalysisLiteraryCts = null;
        }
    }

    private async void ImageAnalysisWorkspacePage_GenerateRequested(
        object? sender,
        ImageAnalysisSettingsRequestedEventArgs e)
    {
        if (_imageAnalysisLiterarySession?.File is null)
        {
            return;
        }

        CancelImageAnalysisLiteraryOperation();
        _imageAnalysisLiteraryCts = new CancellationTokenSource();
        var session = _imageAnalysisLiterarySession;
        session.Settings = e.Settings;
        session.CurrentStep = ImageAnalysisLiterarySteps.Result;
        session.Status = ImageAnalysisLiteraryStatuses.AnalysingVision;
        session.LastError = string.Empty;
        AddImageAnalysisEvent(
            session,
            ImageAnalysisEventCodes.VisionStarted,
            ManagedModelRoles.Vision,
            ImageAnalysisEventStatuses.Active,
            session.File.DisplayName);
        _imageAnalysisSessionStore.Save(session, _storageSettings);
        ImageAnalysisWorkspacePage.ShowSession(session);
        ImageAnalysisWorkspacePage.SetBusy(
            ManagedModelRoles.Vision,
            L("ImageAnalysis.Workspace.Activity.VisionActive"));
        StartImageAnalysisMatrix(ManagedModelRoles.Vision);
        StatusText.Text = L("Status.ImageAnalysisVisionRunning");

        var progress = new Progress<ImageAnalysisLiteraryProgress>(value =>
        {
            session.Status = value.Role == ManagedModelRoles.Vision
                ? ImageAnalysisLiteraryStatuses.AnalysingVision
                : ImageAnalysisLiteraryStatuses.Writing;
            var message = value.Role == ManagedModelRoles.Vision
                ? L("ImageAnalysis.Workspace.Activity.VisionActive")
                : L("ImageAnalysis.Workspace.Activity.CoreActive");
            ImageAnalysisWorkspacePage.SetBusy(value.Role, message);
            if (value.Role == ManagedModelRoles.Core)
            {
                AddImageAnalysisEvent(
                    session,
                    ImageAnalysisEventCodes.CoreStarted,
                    ManagedModelRoles.Core,
                    ImageAnalysisEventStatuses.Active,
                    L("ImageAnalysis.Workspace.Activity.CoreActive"));
            }
            StartImageAnalysisMatrix(value.Role);
            ImageAnalysisWorkspacePage.RefreshActivity(session);
            StatusText.Text = value.Role == ManagedModelRoles.Vision
                ? L("Status.ImageAnalysisVisionRunning")
                : L("Status.ImageAnalysisCoreRunning");
        });
        var stream = new Progress<ModelStreamChunk>(chunk =>
        {
            if (!string.IsNullOrWhiteSpace(chunk.Text))
            {
                ChoiceMatrixRain.Feed(chunk.Text);
            }
        });

        try
        {
            var result = await GetImageAnalysisLiteraryService().CreateAsync(
                session.File,
                session.Settings,
                _storageSettings,
                session.VisualReport,
                LogImageAnalysisRuntime,
                progress,
                stream,
                report =>
                {
                    session.VisualReport = report;
                    session.Observations.Clear();
                    session.Status = ImageAnalysisLiteraryStatuses.Writing;
                    AddImageAnalysisEvent(
                        session,
                        ImageAnalysisEventCodes.VisionCompleted,
                        ManagedModelRoles.Vision,
                        ImageAnalysisEventStatuses.Completed,
                        L("ImageAnalysis.Workspace.Activity.VisionReportReady"));
                    _imageAnalysisSessionStore.Save(session, _storageSettings);
                    ImageAnalysisWorkspacePage.RefreshActivity(session);
                },
                _imageAnalysisLiteraryCts.Token);
            session.VisualReport = result.VisualReport;
            session.ReviewSummary = result.ReviewSummary;
            AddImageAnalysisVersion(session, result.Description, string.Empty, "initial");
            session.Status = ImageAnalysisLiteraryStatuses.ResultReady;
            session.CurrentStep = ImageAnalysisLiterarySteps.Result;
            session.LastError = string.Empty;
            AddImageAnalysisEvent(
                session,
                ImageAnalysisEventCodes.DescriptionReady,
                ManagedModelRoles.Core,
                ImageAnalysisEventStatuses.Completed,
                LF("ImageAnalysis.Workspace.Result.VersionName", session.Versions.Count, DateTime.Now.ToString("g")));
            _imageAnalysisSessionStore.Save(session, _storageSettings);
            ImageAnalysisWorkspacePage.ShowSession(session);
            StatusText.Text = L("Status.ImageAnalysisResultReady");
        }
        catch (OperationCanceledException)
        {
            CompleteActiveImageAnalysisEvents(session);
            session.Status = session.Versions.Count > 0
                ? ImageAnalysisLiteraryStatuses.ResultReady
                : ImageAnalysisLiteraryStatuses.FileReady;
            session.LastError = string.Empty;
            _imageAnalysisSessionStore.Save(session, _storageSettings);
            ImageAnalysisWorkspacePage.ShowSession(session);
            StatusText.Text = L("Status.ImageAnalysisCancelled");
        }
        catch (Exception ex)
        {
            session.Status = ImageAnalysisLiteraryStatuses.Failed;
            session.CurrentStep = session.Versions.Count > 0
                ? ImageAnalysisLiterarySteps.Result
                : ImageAnalysisLiterarySteps.Settings;
            session.LastError = ex.Message;
            AddImageAnalysisEvent(
                session,
                ImageAnalysisEventCodes.OperationFailed,
                string.Empty,
                ImageAnalysisEventStatuses.Failed,
                ex.Message);
            _imageAnalysisSessionStore.Save(session, _storageSettings);
            if (session.Versions.Count == 0)
            {
                ImageAnalysisWorkspacePage.ShowSettings(session);
            }
            ImageAnalysisWorkspacePage.SetOperationError(ex.Message);
            StatusText.Text = LF("Status.ImageAnalysisFailed", ex.Message);
        }
        finally
        {
            ImageAnalysisWorkspacePage.StopActivity();
            StopImageAnalysisMatrix();
            _imageAnalysisLiteraryCts?.Dispose();
            _imageAnalysisLiteraryCts = null;
        }
    }

    private async void ImageAnalysisWorkspacePage_ReviseRequested(
        object? sender,
        ImageAnalysisRevisionRequestedEventArgs e)
    {
        if (_imageAnalysisLiterarySession?.GetSelectedVersion() is null)
        {
            return;
        }
        CancelImageAnalysisLiteraryOperation();
        _imageAnalysisLiteraryCts = new CancellationTokenSource();
        var session = _imageAnalysisLiterarySession;
        session.Status = ImageAnalysisLiteraryStatuses.Revising;
        AddImageAnalysisEvent(
            session,
            ImageAnalysisEventCodes.RevisionStarted,
            ManagedModelRoles.Core,
            ImageAnalysisEventStatuses.Active,
            e.Request);
        _imageAnalysisSessionStore.Save(session, _storageSettings);
        ImageAnalysisWorkspacePage.SetBusy(
            ManagedModelRoles.Core,
            L("ImageAnalysis.Workspace.Activity.RevisionActive"));
        StartImageAnalysisMatrix(ManagedModelRoles.Core);
        StatusText.Text = L("Status.ImageAnalysisRevisionRunning");
        var progress = new Progress<ImageAnalysisLiteraryProgress>(_ =>
        {
            ImageAnalysisWorkspacePage.SetBusy(
                ManagedModelRoles.Core,
                L("ImageAnalysis.Workspace.Activity.RevisionActive"));
            StartImageAnalysisMatrix(ManagedModelRoles.Core);
        });
        var stream = new Progress<ModelStreamChunk>(chunk =>
        {
            if (!string.IsNullOrWhiteSpace(chunk.Text))
            {
                ChoiceMatrixRain.Feed(chunk.Text);
            }
        });
        try
        {
            var revised = await GetImageAnalysisLiteraryService().ReviseAsync(
                session,
                e.Request,
                _storageSettings,
                LogImageAnalysisRuntime,
                progress,
                stream,
                _imageAnalysisLiteraryCts.Token);
            AddImageAnalysisVersion(session, revised, e.Request, "revision");
            session.Status = ImageAnalysisLiteraryStatuses.ResultReady;
            session.LastError = string.Empty;
            AddImageAnalysisEvent(
                session,
                ImageAnalysisEventCodes.RevisionReady,
                ManagedModelRoles.Core,
                ImageAnalysisEventStatuses.Completed,
                LF("ImageAnalysis.Workspace.Result.VersionName", session.Versions.Count, DateTime.Now.ToString("g")));
            _imageAnalysisSessionStore.Save(session, _storageSettings);
            ImageAnalysisWorkspacePage.ShowSession(session);
            StatusText.Text = L("Status.ImageAnalysisRevisionReady");
        }
        catch (OperationCanceledException)
        {
            CompleteActiveImageAnalysisEvents(session);
            session.Status = ImageAnalysisLiteraryStatuses.ResultReady;
            _imageAnalysisSessionStore.Save(session, _storageSettings);
            ImageAnalysisWorkspacePage.ShowSession(session);
            StatusText.Text = L("Status.ImageAnalysisCancelled");
        }
        catch (Exception ex)
        {
            session.Status = ImageAnalysisLiteraryStatuses.ResultReady;
            session.LastError = ex.Message;
            AddImageAnalysisEvent(
                session,
                ImageAnalysisEventCodes.OperationFailed,
                string.Empty,
                ImageAnalysisEventStatuses.Failed,
                ex.Message);
            _imageAnalysisSessionStore.Save(session, _storageSettings);
            ImageAnalysisWorkspacePage.SetOperationError(ex.Message);
            StatusText.Text = LF("Status.ImageAnalysisFailed", ex.Message);
        }
        finally
        {
            ImageAnalysisWorkspacePage.StopActivity();
            StopImageAnalysisMatrix();
            _imageAnalysisLiteraryCts?.Dispose();
            _imageAnalysisLiteraryCts = null;
        }
    }

    private void ImageAnalysisWorkspacePage_PreviewRequested(object? sender, EventArgs e)
    {
        var version = _imageAnalysisLiterarySession?.GetSelectedVersion();
        if (version is null)
        {
            return;
        }
        var window = new ImageAnalysisPreviewWindow(
            L("ImageAnalysis.Preview.Title"),
            L("Common.Close"),
            BuildImageAnalysisMarkdown(version));
        window.Owner = this;
        window.ShowDialog();
    }

    private void ImageAnalysisWorkspacePage_ExportRequested(object? sender, EventArgs e)
    {
        var session = _imageAnalysisLiterarySession;
        var version = session?.GetSelectedVersion();
        if (session is null || version is null)
        {
            return;
        }
        var dialog = new WpfSaveFileDialog
        {
            Title = L("ImageAnalysis.Export.Title"),
            Filter = L("ImageAnalysis.Export.Filter"),
            DefaultExt = ".docx",
            AddExtension = true,
            FileName = $"AI_HUB_Описание_{DateTime.Now:yyyyMMdd_HHmm}.docx",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }
        try
        {
            ExecutorDocxExporter.Export(new ExecutorResultSnapshot
            {
                Id = version.VersionId,
                Version = version.Number,
                CreatedAt = version.CreatedAt,
                Title = L("ImageAnalysis.Preview.Title"),
                Markdown = BuildImageAnalysisMarkdown(version),
                IsFinal = session.Status == ImageAnalysisLiteraryStatuses.Completed
            }, dialog.FileName);
            if (!session.ExportedFiles.Contains(dialog.FileName, StringComparer.OrdinalIgnoreCase))
            {
                session.ExportedFiles.Add(dialog.FileName);
            }
            AddImageAnalysisEvent(
                session,
                ImageAnalysisEventCodes.ExportCompleted,
                string.Empty,
                ImageAnalysisEventStatuses.Completed,
                Path.GetFileName(dialog.FileName));
            _imageAnalysisSessionStore.Save(session, _storageSettings);
            ImageAnalysisWorkspacePage.RefreshActivity(session);
            StatusText.Text = LF("Status.ImageAnalysisExported", dialog.FileName);
        }
        catch (Exception ex)
        {
            StatusText.Text = LF("Status.ImageAnalysisExportFailed", ex.Message);
        }
    }

    private async void ImageAnalysisWorkspacePage_CompleteRequested(object? sender, EventArgs e)
    {
        var session = _imageAnalysisLiterarySession;
        if (session?.GetSelectedVersion() is null)
        {
            return;
        }
        var choice = WpfMessageBox.Show(
            this,
            L("ImageAnalysis.Complete.BackupQuestion"),
            L("ImageAnalysis.Complete.Title"),
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);
        if (choice == MessageBoxResult.Cancel)
        {
            return;
        }
        try
        {
            if (choice == MessageBoxResult.Yes)
            {
                await _imageAnalysisSessionStore.CreateInternalBackupAsync(
                    session,
                    _storageSettings,
                    CancellationToken.None);
            }
            session.Status = ImageAnalysisLiteraryStatuses.Completed;
            session.CompletedAt = DateTimeOffset.Now;
            session.CurrentStep = ImageAnalysisLiterarySteps.Result;
            AddImageAnalysisEvent(
                session,
                ImageAnalysisEventCodes.SessionCompleted,
                string.Empty,
                ImageAnalysisEventStatuses.Completed,
                choice == MessageBoxResult.Yes
                    ? L("Status.ImageAnalysisCompletedWithBackup")
                    : L("Status.ImageAnalysisCompleted"));
            _imageAnalysisSessionStore.Save(session, _storageSettings);
            ImageAnalysisWorkspacePage.ShowSession(session);
            StatusText.Text = choice == MessageBoxResult.Yes
                ? L("Status.ImageAnalysisCompletedWithBackup")
                : L("Status.ImageAnalysisCompleted");
        }
        catch (Exception ex)
        {
            StatusText.Text = LF("Status.ImageAnalysisBackupFailed", ex.Message);
        }
    }

    private void ImageAnalysisWorkspacePage_CancelRequested(object? sender, EventArgs e) =>
        _imageAnalysisLiteraryCts?.Cancel();

    private void ImageAnalysisWorkspacePage_NewAnalysisRequested(object? sender, EventArgs e)
    {
        StopImageAnalysisMatrix();
        ShowImageAnalysisSubscenarioSelection();
        StatusText.Text = L("Status.ImageAnalysisNewAnalysis");
    }

    private void ImageAnalysisWorkspacePage_HomeRequested(object? sender, EventArgs e)
    {
        StopImageAnalysisMatrix();
        SaveCurrentImageAnalysisSession();
        ShowWorkStartPage();
        StatusText.Text = L("Status.WorkStartOpened");
    }

    private void ImageAnalysisWorkspacePage_ResumeRequested(
        object? sender,
        ImageAnalysisSessionRequestedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.SessionId))
        {
            ShowImageAnalysisSubscenarioSelection();
            return;
        }
        var session = _imageAnalysisSessionStore.Load(e.SessionId, _storageSettings);
        if (session is null)
        {
            StatusText.Text = L("Status.ImageAnalysisSessionMissing");
            ShowImageAnalysisSubscenarioSelection();
            return;
        }
        _imageAnalysisLiterarySession = session;
        if (session.CurrentStep == ImageAnalysisLiterarySteps.Subscenario)
        {
            session.CurrentStep = ImageAnalysisLiterarySteps.Image;
        }
        ImageAnalysisWorkspacePage.ShowSession(session);
        StatusText.Text = L("Status.ImageAnalysisSessionResumed");
    }

    private void ImageAnalysisWorkspacePage_VersionRequested(
        object? sender,
        ImageAnalysisVersionRequestedEventArgs e)
    {
        var session = _imageAnalysisLiterarySession;
        if (session is null || session.Versions.All(version => version.VersionId != e.VersionId))
        {
            return;
        }
        session.SelectedVersionId = e.VersionId;
        _imageAnalysisSessionStore.Save(session, _storageSettings);
        ImageAnalysisWorkspacePage.ShowSession(session);
    }

    private ImageAnalysisLiteraryService GetImageAnalysisLiteraryService()
    {
        _imageAnalysisLiteraryService ??= new ImageAnalysisLiteraryService(
            new ImageAnalysisKimiRuntimeService(_imageAnalysisBundleInstallationService.LibraryStore),
            new LlamaServerRuntimeService(_userContextService));
        return _imageAnalysisLiteraryService;
    }

    private void AddImageAnalysisVersion(
        ImageAnalysisLiterarySession session,
        string text,
        string changeRequest,
        string source)
    {
        var version = new ImageAnalysisLiteraryVersion
        {
            Number = session.Versions.Count + 1,
            Text = text.Trim(),
            ChangeRequest = changeRequest,
            Source = source
        };
        session.Versions.Add(version);
        session.SelectedVersionId = version.VersionId;
    }

    private void AddImageAnalysisEvent(
        ImageAnalysisLiterarySession session,
        string code,
        string role,
        string status,
        string detail)
    {
        var last = session.Events.LastOrDefault();
        if (last is not null
            && last.Status == ImageAnalysisEventStatuses.Active
            && status == ImageAnalysisEventStatuses.Active
            && string.Equals(last.Code, code, StringComparison.Ordinal))
        {
            last.Detail = detail;
            return;
        }

        CompleteActiveImageAnalysisEvents(session);
        session.Events.Add(new ImageAnalysisEventEntry
        {
            Code = code,
            Role = role,
            Status = status,
            Detail = detail
        });
        if (session.Events.Count > 160)
        {
            session.Events.RemoveRange(0, session.Events.Count - 160);
        }
    }

    private static void CompleteActiveImageAnalysisEvents(ImageAnalysisLiterarySession session)
    {
        foreach (var item in session.Events.Where(item => item.Status == ImageAnalysisEventStatuses.Active))
        {
            item.Status = ImageAnalysisEventStatuses.Completed;
        }
    }

    private void StartImageAnalysisMatrix(string role)
    {
        if (string.Equals(_imageAnalysisMatrixRole, role, StringComparison.Ordinal)
            && ChoiceAiActivityPanel.Visibility == Visibility.Visible)
        {
            return;
        }
        _imageAnalysisMatrixRole = role;
        var color = role switch
        {
            ManagedModelRoles.Vision => Media.Color.FromRgb(59, 130, 246),
            ManagedModelRoles.Localizer => Media.Color.FromRgb(239, 68, 68),
            _ => Media.Color.FromRgb(42, 210, 108)
        };
        StartAiActivityOverlay(color);
    }

    private void StopImageAnalysisMatrix()
    {
        _imageAnalysisMatrixRole = string.Empty;
        StopAiActivityOverlay();
    }

    private static string BuildImageAnalysisMarkdown(ImageAnalysisLiteraryVersion version) =>
        $"# Литературное описание изображения{Environment.NewLine}{Environment.NewLine}{version.Text.Trim()}";

    private void SaveCurrentImageAnalysisSession()
    {
        try
        {
            if (_imageAnalysisLiterarySession is not null)
            {
                _imageAnalysisSessionStore.Save(_imageAnalysisLiterarySession, _storageSettings);
            }
        }
        catch
        {
            // Best-effort save during navigation or application shutdown.
        }
    }

    private void CancelImageAnalysisLiteraryOperation()
    {
        _imageAnalysisLiteraryCts?.Cancel();
        _imageAnalysisLiteraryCts?.Dispose();
        _imageAnalysisLiteraryCts = null;
    }

    private void DisposeImageAnalysisLiteraryRuntime()
    {
        CancelImageAnalysisLiteraryOperation();
        SaveCurrentImageAnalysisSession();
        _imageAnalysisLiteraryService?.Dispose();
        _imageAnalysisLiteraryService = null;
    }

    private void LogImageAnalysisRuntime(string message)
    {
        try
        {
            _coreSessionLog?.Write("image_analysis_runtime", new
            {
                SessionId = _imageAnalysisLiterarySession?.SessionId,
                Message = message
            });
        }
        catch
        {
            // Runtime diagnostics must not break the user scenario.
        }
    }
}
