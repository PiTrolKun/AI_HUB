using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AIHub.Models;
using UserControl = System.Windows.Controls.UserControl;
using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfEllipse = System.Windows.Shapes.Ellipse;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;

namespace AIHub.Controls;

public partial class ImageAnalysisWorkspaceControl : UserControl
{
    private Func<string, string> _localize = key => key;
    private Func<string, object[], string> _format = (key, _) => key;
    private ImageAnalysisLiterarySession? _session;
    private string _currentStep = ImageAnalysisLiterarySteps.Subscenario;
    private bool _isBusy;
    private bool _suppressVersionSelection;

    public ImageAnalysisWorkspaceControl()
    {
        InitializeComponent();
    }

    public event EventHandler? BackRequested;
    public event EventHandler? SingleSubscenarioRequested;
    public event EventHandler? SelectImageRequested;
    public event EventHandler<ImageAnalysisSettingsRequestedEventArgs>? GenerateRequested;
    public event EventHandler<ImageAnalysisRevisionRequestedEventArgs>? ReviseRequested;
    public event EventHandler? PreviewRequested;
    public event EventHandler? ExportRequested;
    public event EventHandler? CompleteRequested;
    public event EventHandler? CancelRequested;
    public event EventHandler<ImageAnalysisSessionRequestedEventArgs>? ResumeRequested;
    public event EventHandler<ImageAnalysisVersionRequestedEventArgs>? VersionRequested;
    public event EventHandler? HomeRequested;
    public event EventHandler? NewAnalysisRequested;

    public void Configure(Func<string, string> localize, Func<string, object[], string> format)
    {
        _localize = localize;
        _format = format;
        ApplyLocalization();
    }

    public void ShowSubscenarioSelection(IReadOnlyList<ImageAnalysisLiterarySession> sessions)
    {
        _session = null;
        RenderHistory(sessions);
        SetStep(ImageAnalysisLiterarySteps.Subscenario);
        ApplyFile(null);
        RenderEvents(null);
        RenderFindings(null);
        VersionComboBox.ItemsSource = null;
        RevisionPanel.Visibility = Visibility.Collapsed;
        PreviewButton.IsEnabled = false;
        ExportButton.IsEnabled = false;
        CompleteButton.IsEnabled = false;
        CompleteButton.Visibility = Visibility.Visible;
        SetIdle(_localize("ImageAnalysis.Workspace.Activity.Idle"));
        FooterStatusText.Text = _localize("ImageAnalysis.Workspace.Status.ChooseSubscenario");
    }

    public void ShowImageStep(ImageAnalysisLiterarySession session)
    {
        _session = session;
        SetStep(ImageAnalysisLiterarySteps.Image);
        ApplyFile(session.File);
        RenderEvents(session);
        RenderFindings(session);
        SetIdle(_localize("ImageAnalysis.Workspace.Activity.WaitingFile"));
        FooterStatusText.Text = _localize("ImageAnalysis.Workspace.Status.ChooseImage");
    }

    public void ShowFileChecking(string path)
    {
        SelectedImageCard.Visibility = Visibility.Visible;
        SelectedImagePreview.Source = null;
        SelectedFileTitleText.Text = Path.GetFileName(path);
        SelectedFileDetailsText.Text = _localize("ImageAnalysis.Workspace.FileChecking");
        FileValidationPanel.Visibility = Visibility.Visible;
        FileValidationPanel.BorderBrush = new SolidColorBrush(MediaColor.FromRgb(37, 99, 235));
        FileValidationText.Text = _localize("ImageAnalysis.Workspace.FileCheckingDetails");
        ContinueToSettingsButton.IsEnabled = false;
        SelectImageButton.IsEnabled = false;
        SelectImageEmptyButton.IsEnabled = false;
    }

    public void SetValidatedFile(ImageAnalysisLiterarySession session)
    {
        _session = session;
        ApplyFile(session.File);
        RenderEvents(session);
        SelectImageButton.IsEnabled = true;
        SelectImageEmptyButton.IsEnabled = true;
        FooterStatusText.Text = _localize("ImageAnalysis.Workspace.Status.FileReady");
    }

    public void SetFileError(string message)
    {
        SelectImageButton.IsEnabled = true;
        SelectImageEmptyButton.IsEnabled = true;
        ContinueToSettingsButton.IsEnabled = false;
        FileValidationPanel.Visibility = Visibility.Visible;
        FileValidationPanel.BorderBrush = new SolidColorBrush(MediaColor.FromRgb(220, 38, 38));
        FileValidationText.Text = _format("ImageAnalysis.Workspace.FileError", [message]);
        FooterStatusText.Text = _localize("ImageAnalysis.Workspace.Status.FileRejected");
    }

    public void ShowSettings(ImageAnalysisLiterarySession session)
    {
        _session = session;
        SetComboByTag(AccuracyComboBox, session.Settings.Accuracy);
        SetComboByTag(StyleComboBox, session.Settings.Style);
        SetComboByTag(LengthComboBox, session.Settings.Length);
        SetComboByTag(FormComboBox, session.Settings.Form);
        WishesTextBox.Text = session.Settings.Wishes;
        SetStep(ImageAnalysisLiterarySteps.Settings);
        ApplyFile(session.File);
        RenderEvents(session);
        RenderFindings(session);
        SetIdle(_localize("ImageAnalysis.Workspace.Activity.Ready"));
        FooterStatusText.Text = _localize("ImageAnalysis.Workspace.Status.Configure");
    }

    public void ShowSession(ImageAnalysisLiterarySession session)
    {
        _session = session;
        if (session.GetSelectedVersion() is null)
        {
            if (session.CurrentStep == ImageAnalysisLiterarySteps.Result
                || session.Status is ImageAnalysisLiteraryStatuses.AnalysingVision
                    or ImageAnalysisLiteraryStatuses.Writing
                    or ImageAnalysisLiteraryStatuses.Failed)
            {
                SetStep(ImageAnalysisLiterarySteps.Result);
                ApplyFile(session.File);
                RenderEvents(session);
                RenderFindings(session);
                PopulateVersions(session);
                RevisionPanel.Visibility = Visibility.Collapsed;
                PreviewButton.IsEnabled = false;
                ExportButton.IsEnabled = false;
                CompleteButton.IsEnabled = false;
                return;
            }
            if (session.CurrentStep == ImageAnalysisLiterarySteps.Settings
                || !string.IsNullOrWhiteSpace(session.VisualReport))
            {
                ShowSettings(session);
            }
            else
            {
                ShowImageStep(session);
            }
            return;
        }
        SetStep(session.CurrentStep == ImageAnalysisLiterarySteps.Subscenario
            ? ImageAnalysisLiterarySteps.Image
            : session.CurrentStep);
        ApplyFile(session.File);
        RenderEvents(session);
        RenderFindings(session);
        PopulateVersions(session);
        var selected = session.GetSelectedVersion();
        var hasResult = selected is not null;
        PreviewButton.IsEnabled = hasResult;
        ExportButton.IsEnabled = hasResult;
        CompleteButton.IsEnabled = hasResult && session.Status != ImageAnalysisLiteraryStatuses.Completed;
        ReviseButton.IsEnabled = hasResult && session.Status != ImageAnalysisLiteraryStatuses.Completed;
        RevisionTextBox.IsEnabled = ReviseButton.IsEnabled;
        RevisionPanel.Visibility = hasResult && session.Status != ImageAnalysisLiteraryStatuses.Completed
            ? Visibility.Visible
            : Visibility.Collapsed;
        CompletedNavigationPanel.Visibility = session.Status == ImageAnalysisLiteraryStatuses.Completed
            ? Visibility.Visible
            : Visibility.Collapsed;
        CompleteButton.Visibility = session.Status == ImageAnalysisLiteraryStatuses.Completed
            ? Visibility.Collapsed
            : Visibility.Visible;
        BackButton.Visibility = session.Status == ImageAnalysisLiteraryStatuses.Completed
            ? Visibility.Collapsed
            : Visibility.Visible;
        ResultPanelHintText.Text = session.Status == ImageAnalysisLiteraryStatuses.Completed
            ? _localize("ImageAnalysis.Workspace.Result.CompletedHint")
            : _localize("ImageAnalysis.Workspace.Result.Hint");
        SelectImageButton.IsEnabled = session.Status != ImageAnalysisLiteraryStatuses.Completed;
        SetIdle(session.Status == ImageAnalysisLiteraryStatuses.Completed
            ? _localize("ImageAnalysis.Workspace.Activity.Completed")
            : _localize("ImageAnalysis.Workspace.Activity.ResultReady"));
        FooterStatusText.Text = session.Status == ImageAnalysisLiteraryStatuses.Completed
            ? _localize("ImageAnalysis.Workspace.Status.Completed")
            : _localize("ImageAnalysis.Workspace.Status.ResultReady");
    }

    public void SetBusy(string role, string message)
    {
        _isBusy = true;
        FooterStatusText.Text = message;
        SetInteractionEnabled(false);
        CancelOperationButton.Visibility = Visibility.Visible;
        CancelOperationButton.IsEnabled = true;
    }

    public void FeedActivity(string text)
    {
        _ = text;
    }

    public void RefreshActivity(ImageAnalysisLiterarySession session)
    {
        _session = session;
        RenderEvents(session);
        RenderFindings(session);
    }

    public void SetOperationError(string message)
    {
        StopActivity();
        FooterStatusText.Text = _format("ImageAnalysis.Workspace.Status.Error", [message]);
    }

    public void StopActivity()
    {
        _isBusy = false;
        SetInteractionEnabled(true);
        CancelOperationButton.Visibility = Visibility.Collapsed;
    }

    public void ApplyLocalization()
    {
        TitleText.Text = _localize("ImageAnalysis.Workspace.Title");
        DescriptionText.Text = _localize("ImageAnalysis.Workspace.Description");
        SubscenarioStepTitleText.Text = _localize("ImageAnalysis.Workspace.Step.Subscenario");
        SubscenarioStepHintText.Text = _localize("ImageAnalysis.Workspace.Step.SubscenarioHint");
        ImageStepTitleText.Text = _localize("ImageAnalysis.Workspace.Step.Image");
        ImageStepHintText.Text = _localize("ImageAnalysis.Workspace.Step.ImageHint");
        SettingsStepTitleText.Text = _localize("ImageAnalysis.Workspace.Step.Settings");
        SettingsStepHintText.Text = _localize("ImageAnalysis.Workspace.Step.SettingsHint");
        ResultStepTitleText.Text = _localize("ImageAnalysis.Workspace.Step.Result");
        ResultStepHintText.Text = _localize("ImageAnalysis.Workspace.Step.ResultHint");
        ActivityTitleText.Text = _localize("ImageAnalysis.Workspace.Activity.Title");
        EventsEmptyText.Text = _localize("ImageAnalysis.Workspace.Events.Empty");
        CoreLegendText.Text = _localize("ImageAnalysis.Role.Core");
        VisionLegendText.Text = _localize("ImageAnalysis.Role.Vision");
        LocalizerLegendText.Text = _localize("ImageAnalysis.Role.Localizer");
        SubscenarioTitleText.Text = _localize("ImageAnalysis.Workspace.Subscenario.Title");
        SubscenarioDescriptionText.Text = _localize("ImageAnalysis.Workspace.Subscenario.Description");
        SingleScenarioTitleText.Text = _localize("ImageAnalysis.Workspace.Subscenario.Single.Title");
        SingleScenarioDescriptionText.Text = _localize("ImageAnalysis.Workspace.Subscenario.Single.Description");
        SingleScenarioButton.Content = _localize("ImageAnalysis.Workspace.Subscenario.Single.Start");
        MultipleScenarioTitleText.Text = _localize("ImageAnalysis.Workspace.Subscenario.Multiple.Title");
        MultipleScenarioDescriptionText.Text = _localize("ImageAnalysis.Workspace.Subscenario.Multiple.Description");
        MultipleScenarioButton.Content = _localize("ImageAnalysis.Bundle.InDevelopment");
        HistoryTitleText.Text = _localize("ImageAnalysis.Workspace.History.Title");
        HistoryEmptyText.Text = _localize("ImageAnalysis.Workspace.History.Empty");
        ImagePanelTitleText.Text = _localize("ImageAnalysis.Workspace.Image.Title");
        ImagePanelDescriptionText.Text = _localize("ImageAnalysis.Workspace.Image.Description");
        SelectImageButton.Content = _localize("ImageAnalysis.Workspace.SelectImage");
        SelectImageEmptyButton.Content = _localize("ImageAnalysis.Workspace.SelectImage");
        ContinueToSettingsButton.Content = _localize("ImageAnalysis.Workspace.Image.Continue");
        SettingsPanelTitleText.Text = _localize("ImageAnalysis.Workspace.Settings.Title");
        SettingsPanelDescriptionText.Text = _localize("ImageAnalysis.Workspace.Settings.Description");
        AccuracyLabelText.Text = _localize("ImageAnalysis.Workspace.Settings.Accuracy");
        StyleLabelText.Text = _localize("ImageAnalysis.Workspace.Settings.Style");
        LengthLabelText.Text = _localize("ImageAnalysis.Workspace.Settings.Length");
        FormLabelText.Text = _localize("ImageAnalysis.Workspace.Settings.Form");
        WishesLabelText.Text = _localize("ImageAnalysis.Workspace.Settings.Wishes");
        WishesTextBox.ToolTip = _localize("ImageAnalysis.Workspace.Settings.WishesHint");
        GenerateButton.Content = _localize("ImageAnalysis.Workspace.Settings.Generate");
        SetComboText(AccuracyComboBox, "ImageAnalysis.Workspace.Settings.Accuracy.");
        SetComboText(StyleComboBox, "ImageAnalysis.Workspace.Settings.Style.");
        SetComboText(LengthComboBox, "ImageAnalysis.Workspace.Settings.Length.");
        SetComboText(FormComboBox, "ImageAnalysis.Workspace.Settings.Form.");
        ResultEditorTitleText.Text = _localize("ImageAnalysis.Workspace.Editor.Title");
        ResultEditorDescriptionText.Text = _localize("ImageAnalysis.Workspace.Editor.Description");
        FindingsEmptyText.Text = _localize("ImageAnalysis.Workspace.Findings.Empty");
        UncertaintiesTitleText.Text = _localize("ImageAnalysis.Workspace.Review.Uncertainties");
        ReviewSummaryFooterText.Text = _localize("ImageAnalysis.Workspace.Review.Footer");
        RevisionLabelText.Text = _localize("ImageAnalysis.Workspace.Editor.Revision");
        RevisionTextBox.ToolTip = _localize("ImageAnalysis.Workspace.Editor.RevisionHint");
        ReviseButton.Content = _localize("ImageAnalysis.Workspace.Editor.Revise");
        CancelOperationButton.Content = _localize("Common.Cancel");
        ResultPanelTitleText.Text = _localize("ImageAnalysis.Workspace.Result.Title");
        ResultPanelHintText.Text = _localize("ImageAnalysis.Workspace.Result.Hint");
        VersionLabelText.Text = _localize("ImageAnalysis.Workspace.Result.Version");
        PreviewButton.Content = _localize("ImageAnalysis.Workspace.Result.Preview");
        ExportButton.Content = _localize("ImageAnalysis.Workspace.Result.Export");
        CompleteButton.Content = _localize("ImageAnalysis.Workspace.Result.Complete");
        NewAnalysisButton.Content = _localize("ImageAnalysis.Workspace.Completed.NewAnalysis");
        HomeButton.Content = _localize("ImageAnalysis.Workspace.Completed.Home");
        BackButton.Content = _localize("ImageAnalysis.Workspace.Back");
        UpdateStepAppearance();
    }

    private void ApplyFile(ImageAnalysisFilePassport? passport)
    {
        if (passport is null)
        {
            SelectedImageCard.Visibility = Visibility.Collapsed;
            SelectedFileTitleText.Text = _localize("ImageAnalysis.Workspace.Image.NoFile");
            SelectedFileDetailsText.Text = _localize("ImageAnalysis.Workspace.Image.NoFileHint");
            SelectedImagePreview.Source = null;
            SelectImageEmptyButton.Visibility = Visibility.Visible;
            FileValidationPanel.Visibility = Visibility.Collapsed;
            ContinueToSettingsButton.IsEnabled = false;
            return;
        }
        SelectedImageCard.Visibility = Visibility.Visible;
        SelectImageEmptyButton.Visibility = Visibility.Collapsed;
        SelectedFileTitleText.Text = passport.DisplayName;
        SelectedFileDetailsText.Text = _format(
            "ImageAnalysis.Workspace.FileDetailsFull",
            [passport.Format, ComponentCardViewModel.FormatBytes(passport.SizeBytes), passport.PixelWidth, passport.PixelHeight, passport.Sha256[..Math.Min(12, passport.Sha256.Length)]]);
        SelectedImagePreview.Source = LoadPreview(passport.SourcePath);
        FileValidationPanel.Visibility = Visibility.Visible;
        FileValidationPanel.BorderBrush = new SolidColorBrush(MediaColor.FromRgb(34, 197, 94));
        FileValidationText.Text = _localize("ImageAnalysis.Workspace.FileReady");
        ContinueToSettingsButton.IsEnabled = true;
    }

    private void RenderEvents(ImageAnalysisLiterarySession? session)
    {
        EventDetailPopup.IsOpen = false;
        EventItemsPanel.Children.Clear();
        var events = session?.Events ?? [];
        EventsEmptyText.Visibility = events.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var item in events)
        {
            var color = ResolveEventColor(item);
            var bar = new Border
            {
                Width = item.Status == ImageAnalysisEventStatuses.Active ? 52 : 34,
                Height = item.Status == ImageAnalysisEventStatuses.Active ? 4 : 3,
                Background = new SolidColorBrush(color),
                CornerRadius = new CornerRadius(2),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left
            };
            var button = new WpfButton
            {
                Content = bar,
                Background = MediaBrushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6, 8, 6, 8),
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                Tag = item
            };
            AutomationProperties.SetName(button, ResolveEventTitle(item.Code));
            button.MouseEnter += EventButton_MouseEnter;
            button.MouseLeave += EventButton_MouseLeave;
            button.GotKeyboardFocus += EventButton_GotKeyboardFocus;
            button.LostKeyboardFocus += EventButton_LostKeyboardFocus;
            button.Click += EventButton_Click;
            EventItemsPanel.Children.Add(button);
        }
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => EventScrollViewer.ScrollToEnd());
    }

    private void ShowEventDetail(WpfButton button, ImageAnalysisEventEntry item)
    {
        EventDetailTitleText.Text = ResolveEventTitle(item.Code);
        EventDetailText.Text = string.IsNullOrWhiteSpace(item.Detail)
            ? _localize("ImageAnalysis.Workspace.Events.NoDetails")
            : item.Detail;
        EventDetailTimeText.Text = item.CreatedAt.LocalDateTime.ToString("g");
        EventDetailPopup.PlacementTarget = button;
        EventDetailPopup.IsOpen = true;
    }

    private void EventButton_MouseEnter(object sender, WpfMouseEventArgs e)
    {
        if (sender is WpfButton { Tag: ImageAnalysisEventEntry item } button)
        {
            ShowEventDetail(button, item);
        }
    }

    private void EventButton_MouseLeave(object sender, WpfMouseEventArgs e)
    {
        if (sender is WpfButton button && !button.IsKeyboardFocusWithin)
        {
            EventDetailPopup.IsOpen = false;
        }
    }

    private void EventButton_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is WpfButton { Tag: ImageAnalysisEventEntry item } button)
        {
            ShowEventDetail(button, item);
        }
    }

    private void EventButton_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) =>
        EventDetailPopup.IsOpen = false;

    private void EventButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton { Tag: ImageAnalysisEventEntry item } button)
        {
            ShowEventDetail(button, item);
        }
    }

    private string ResolveEventTitle(string code) => _localize("ImageAnalysis.Workspace.Events." + code);

    private static MediaColor ResolveEventColor(ImageAnalysisEventEntry item)
    {
        if (item.Status == ImageAnalysisEventStatuses.Failed)
        {
            return MediaColor.FromRgb(245, 158, 11);
        }
        return item.Role switch
        {
            ManagedModelRoles.Core => MediaColor.FromRgb(42, 210, 108),
            ManagedModelRoles.Vision => MediaColor.FromRgb(59, 130, 246),
            ManagedModelRoles.Localizer => MediaColor.FromRgb(239, 68, 68),
            _ => MediaColor.FromRgb(148, 163, 184)
        };
    }

    private void RenderFindings(ImageAnalysisLiterarySession? session)
    {
        FindingsItemsPanel.Children.Clear();
        UncertaintyItemsPanel.Children.Clear();
        var summary = session?.ReviewSummary ?? new ImageAnalysisReviewSummary();
        var items = summary.Items.Take(6).ToList();
        FindingsEmptyText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var item in items)
        {
            var text = new TextBlock
            {
                Text = item,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            var marker = new WpfEllipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(MediaColor.FromRgb(59, 130, 246)),
                Margin = new Thickness(0, 0, 9, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            var row = new Grid { Margin = new Thickness(0, 0, 0, 9) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(text, 1);
            row.Children.Add(marker);
            row.Children.Add(text);
            FindingsItemsPanel.Children.Add(row);
        }

        var uncertainties = summary.Uncertainties.Take(2).ToList();
        UncertaintiesPanel.Visibility = uncertainties.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        foreach (var uncertainty in uncertainties)
        {
            UncertaintyItemsPanel.Children.Add(new TextBlock
            {
                Text = "• " + uncertainty,
                Foreground = new SolidColorBrush(MediaColor.FromRgb(245, 158, 11)),
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 0, 0, 5)
            });
        }
    }

    private void PopulateVersions(ImageAnalysisLiterarySession session)
    {
        _suppressVersionSelection = true;
        VersionComboBox.ItemsSource = session.Versions.Select(version => new VersionListItem(
            version.VersionId,
            _format("ImageAnalysis.Workspace.Result.VersionName", [version.Number, version.CreatedAt.LocalDateTime.ToString("g")]))).ToList();
        VersionComboBox.SelectedValuePath = nameof(VersionListItem.VersionId);
        VersionComboBox.SelectedValue = session.SelectedVersionId;
        _suppressVersionSelection = false;
    }

    private void RenderHistory(IReadOnlyList<ImageAnalysisLiterarySession> sessions)
    {
        HistoryItemsPanel.Children.Clear();
        var visible = sessions.Take(5).ToList();
        HistoryEmptyText.Visibility = visible.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var session in visible)
        {
            var button = new WpfButton
            {
                Tag = session.SessionId,
                Content = _format(
                    "ImageAnalysis.Workspace.History.Item",
                    [session.File?.DisplayName ?? _localize("ImageAnalysis.Workspace.Image.NoFile"), session.UpdatedAt.LocalDateTime.ToString("g"), session.Versions.Count]),
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 7),
                Padding = new Thickness(10, 8, 10, 8),
                Style = TryFindResource("SecondaryButtonStyle") as Style
            };
            button.Click += HistoryButton_Click;
            HistoryItemsPanel.Children.Add(button);
        }
    }

    private void SetStep(string step)
    {
        _currentStep = step;
        if (_session is not null && step != ImageAnalysisLiterarySteps.Subscenario)
        {
            _session.CurrentStep = step;
        }
        SubscenarioPanel.Visibility = step == ImageAnalysisLiterarySteps.Subscenario ? Visibility.Visible : Visibility.Collapsed;
        ImagePanel.Visibility = step == ImageAnalysisLiterarySteps.Image ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = step == ImageAnalysisLiterarySteps.Settings ? Visibility.Visible : Visibility.Collapsed;
        ResultEditorPanel.Visibility = step == ImageAnalysisLiterarySteps.Result ? Visibility.Visible : Visibility.Collapsed;
        var completed = _session?.Status == ImageAnalysisLiteraryStatuses.Completed;
        CompletedNavigationPanel.Visibility = completed ? Visibility.Visible : Visibility.Collapsed;
        BackButton.Visibility = completed ? Visibility.Collapsed : Visibility.Visible;
        UpdateStepAppearance();
    }

    private void UpdateStepAppearance()
    {
        ApplyStepAppearance(
            SubscenarioStepBorder,
            SubscenarioStepNumberBorder,
            SubscenarioStepNumberText,
            SubscenarioStepTitleText,
            SubscenarioStepHintText,
            _currentStep == ImageAnalysisLiterarySteps.Subscenario);
        ApplyStepAppearance(
            ImageStepBorder,
            ImageStepNumberBorder,
            ImageStepNumberText,
            ImageStepTitleText,
            ImageStepHintText,
            _currentStep == ImageAnalysisLiterarySteps.Image);
        ApplyStepAppearance(
            SettingsStepBorder,
            SettingsStepNumberBorder,
            SettingsStepNumberText,
            SettingsStepTitleText,
            SettingsStepHintText,
            _currentStep == ImageAnalysisLiterarySteps.Settings);
        ApplyStepAppearance(
            ResultStepBorder,
            ResultStepNumberBorder,
            ResultStepNumberText,
            ResultStepTitleText,
            ResultStepHintText,
            _currentStep == ImageAnalysisLiterarySteps.Result);
    }

    private static void ApplyStepAppearance(
        Border container,
        Border number,
        TextBlock numberText,
        TextBlock titleText,
        TextBlock hintText,
        bool active)
    {
        if (active)
        {
            container.SetResourceReference(BackgroundProperty, "AccentDarkBrush");
            number.Background = new SolidColorBrush(MediaColor.FromRgb(96, 165, 250));
            numberText.Foreground = new SolidColorBrush(MediaColor.FromRgb(15, 23, 42));
            titleText.Foreground = MediaBrushes.White;
            hintText.Foreground = new SolidColorBrush(MediaColor.FromRgb(219, 234, 254));
            return;
        }

        container.Background = MediaBrushes.Transparent;
        number.SetResourceReference(BackgroundProperty, "StepBadgeBrush");
        numberText.SetResourceReference(ForegroundProperty, "TextPrimaryBrush");
        titleText.SetResourceReference(ForegroundProperty, "TextPrimaryBrush");
        hintText.SetResourceReference(ForegroundProperty, "TextSecondaryBrush");
    }

    private void SetIdle(string message)
    {
        StopActivity();
        FooterStatusText.Text = message;
    }

    private void SetInteractionEnabled(bool enabled)
    {
        var editable = _session?.Status != ImageAnalysisLiteraryStatuses.Completed;
        BackButton.IsEnabled = enabled;
        SingleScenarioButton.IsEnabled = enabled;
        SelectImageButton.IsEnabled = enabled && editable;
        SelectImageEmptyButton.IsEnabled = enabled && editable;
        ContinueToSettingsButton.IsEnabled = enabled && editable && _session?.File is not null;
        GenerateButton.IsEnabled = enabled && editable;
        ReviseButton.IsEnabled = enabled && _session?.GetSelectedVersion() is not null && _session.Status != ImageAnalysisLiteraryStatuses.Completed;
        PreviewButton.IsEnabled = enabled && _session?.GetSelectedVersion() is not null;
        ExportButton.IsEnabled = enabled && _session?.GetSelectedVersion() is not null;
        CompleteButton.IsEnabled = enabled && _session?.GetSelectedVersion() is not null && _session.Status != ImageAnalysisLiteraryStatuses.Completed;
        NewAnalysisButton.IsEnabled = enabled;
        HomeButton.IsEnabled = enabled;
    }

    private ImageAnalysisLiterarySettings ReadSettings() => new()
    {
        Accuracy = GetSelectedTag(AccuracyComboBox, ImageAnalysisAccuracyModes.Balanced),
        Style = GetSelectedTag(StyleComboBox, ImageAnalysisLiteraryStyles.Atmospheric),
        Length = GetSelectedTag(LengthComboBox, ImageAnalysisTextLengths.Standard),
        Form = GetSelectedTag(FormComboBox, ImageAnalysisTextForms.WithTitle),
        Wishes = WishesTextBox.Text.Trim()
    };

    private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var compact = e.NewSize.Width < 1250;
        RootLayout.Margin = compact ? new Thickness(12, 10, 12, 12) : new Thickness(24, 14, 24, 16);
        ActivityColumn.Width = new GridLength(compact ? 185 : 220);
        CenterColumn.Width = new GridLength(1, GridUnitType.Star);
        ResultColumn.Width = new GridLength(compact ? 275 : 330);
    }

    private void SingleScenarioButton_Click(object sender, RoutedEventArgs e) => SingleSubscenarioRequested?.Invoke(this, EventArgs.Empty);
    private void SelectImageButton_Click(object sender, RoutedEventArgs e) => SelectImageRequested?.Invoke(this, EventArgs.Empty);
    private void ContinueToSettingsButton_Click(object sender, RoutedEventArgs e) { if (_session is not null) ShowSettings(_session); }
    private void GenerateButton_Click(object sender, RoutedEventArgs e) => GenerateRequested?.Invoke(this, new ImageAnalysisSettingsRequestedEventArgs(ReadSettings()));
    private void ReviseButton_Click(object sender, RoutedEventArgs e) { if (!string.IsNullOrWhiteSpace(RevisionTextBox.Text)) ReviseRequested?.Invoke(this, new ImageAnalysisRevisionRequestedEventArgs(RevisionTextBox.Text.Trim())); }
    private void PreviewButton_Click(object sender, RoutedEventArgs e) => PreviewRequested?.Invoke(this, EventArgs.Empty);
    private void ExportButton_Click(object sender, RoutedEventArgs e) => ExportRequested?.Invoke(this, EventArgs.Empty);
    private void CompleteButton_Click(object sender, RoutedEventArgs e) => CompleteRequested?.Invoke(this, EventArgs.Empty);
    private void NewAnalysisButton_Click(object sender, RoutedEventArgs e) => NewAnalysisRequested?.Invoke(this, EventArgs.Empty);
    private void HomeButton_Click(object sender, RoutedEventArgs e) => HomeRequested?.Invoke(this, EventArgs.Empty);
    private void CancelOperationButton_Click(object sender, RoutedEventArgs e) { CancelOperationButton.IsEnabled = false; CancelRequested?.Invoke(this, EventArgs.Empty); }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy) return;
        if (_currentStep == ImageAnalysisLiterarySteps.Subscenario) { BackRequested?.Invoke(this, EventArgs.Empty); return; }
        if (_currentStep == ImageAnalysisLiterarySteps.Image) { ResumeRequested?.Invoke(this, new ImageAnalysisSessionRequestedEventArgs(string.Empty)); return; }
        if (_currentStep == ImageAnalysisLiterarySteps.Settings && _session is not null) { ShowImageStep(_session); return; }
        if (_currentStep == ImageAnalysisLiterarySteps.Result && _session is not null && _session.Status != ImageAnalysisLiteraryStatuses.Completed) { ShowSettings(_session); }
    }

    private void HistoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton { Tag: string sessionId }) ResumeRequested?.Invoke(this, new ImageAnalysisSessionRequestedEventArgs(sessionId));
    }

    private void VersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_suppressVersionSelection && VersionComboBox.SelectedItem is VersionListItem item)
        {
            VersionRequested?.Invoke(this, new ImageAnalysisVersionRequestedEventArgs(item.VersionId));
        }
    }

    private void SetComboText(WpfComboBox comboBox, string keyPrefix)
    {
        foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
        {
            item.Content = _localize(keyPrefix + item.Tag);
        }
    }

    private static void SetComboByTag(WpfComboBox comboBox, string tag)
    {
        comboBox.SelectedItem = comboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => string.Equals(item.Tag?.ToString(), tag, StringComparison.Ordinal));
    }

    private static string GetSelectedTag(WpfComboBox comboBox, string fallback) => (comboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? fallback;

    private static BitmapImage? LoadPreview(string path)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = 320;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch { return null; }
    }

    private sealed record VersionListItem(string VersionId, string DisplayName);
}

public sealed class ImageAnalysisSettingsRequestedEventArgs(ImageAnalysisLiterarySettings settings) : EventArgs
{
    public ImageAnalysisLiterarySettings Settings { get; } = settings;
}

public sealed class ImageAnalysisRevisionRequestedEventArgs(string request) : EventArgs
{
    public string Request { get; } = request;
}

public sealed class ImageAnalysisSessionRequestedEventArgs(string sessionId) : EventArgs
{
    public string SessionId { get; } = sessionId;
}

public sealed class ImageAnalysisVersionRequestedEventArgs(string versionId) : EventArgs
{
    public string VersionId { get; } = versionId;
}
