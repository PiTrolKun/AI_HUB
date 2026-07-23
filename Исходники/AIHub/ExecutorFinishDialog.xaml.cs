using System.Windows;

namespace AIHub;

public enum ExecutorFinishChoice
{
    None,
    FinishWithoutResult,
    ShowInApp,
    ExportDocx
}

public partial class ExecutorFinishDialog : Window
{
    private readonly string _finishHeading;
    private readonly string _finishDescription;
    private readonly string _outputHeading;
    private readonly string _outputDescription;

    public ExecutorFinishDialog(
        string title,
        string finishHeading,
        string finishDescription,
        string generateResult,
        string finishWithoutResult,
        string outputHeading,
        string outputDescription,
        string showInApp,
        string exportDocx,
        string back)
    {
        InitializeComponent();
        Title = title;
        _finishHeading = finishHeading;
        _finishDescription = finishDescription;
        _outputHeading = outputHeading;
        _outputDescription = outputDescription;
        GenerateResultButton.Content = generateResult;
        FinishWithoutResultButton.Content = finishWithoutResult;
        ShowInAppButton.Content = showInApp;
        ExportDocxButton.Content = exportDocx;
        BackButton.Content = back;
        ShowFinishChoice();
    }

    public ExecutorFinishChoice Choice { get; private set; }

    private void GenerateResultButton_Click(object sender, RoutedEventArgs e)
    {
        HeadingText.Text = _outputHeading;
        DescriptionText.Text = _outputDescription;
        FinishChoicePanel.Visibility = Visibility.Collapsed;
        OutputChoicePanel.Visibility = Visibility.Visible;
        GenerateResultButton.IsDefault = false;
        ShowInAppButton.IsDefault = true;
        ShowInAppButton.Focus();
    }

    private void FinishWithoutResultButton_Click(object sender, RoutedEventArgs e) =>
        Complete(ExecutorFinishChoice.FinishWithoutResult);

    private void ShowInAppButton_Click(object sender, RoutedEventArgs e) =>
        Complete(ExecutorFinishChoice.ShowInApp);

    private void ExportDocxButton_Click(object sender, RoutedEventArgs e) =>
        Complete(ExecutorFinishChoice.ExportDocx);

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (OutputChoicePanel.Visibility == Visibility.Visible)
        {
            ShowFinishChoice();
            return;
        }

        DialogResult = false;
    }

    private void ShowFinishChoice()
    {
        HeadingText.Text = _finishHeading;
        DescriptionText.Text = _finishDescription;
        FinishChoicePanel.Visibility = Visibility.Visible;
        OutputChoicePanel.Visibility = Visibility.Collapsed;
        GenerateResultButton.IsDefault = true;
        ShowInAppButton.IsDefault = false;
        GenerateResultButton.Focus();
    }

    private void Complete(ExecutorFinishChoice choice)
    {
        Choice = choice;
        DialogResult = true;
    }
}
