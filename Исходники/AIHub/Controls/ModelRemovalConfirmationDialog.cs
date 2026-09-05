using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Orientation = System.Windows.Controls.Orientation;

namespace AIHub.Controls;

public sealed class ModelRemovalConfirmationDialog : Window
{
    public ModelRemovalConfirmationDialog(Window owner, string title, string message,
        string cancelText, string removeText)
    {
        Owner = owner;
        Resources.MergedDictionaries.Add(owner.Resources);
        Title = title;
        Width = 520;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Background = owner.TryFindResource("PanelBrush") as Brush ?? Brushes.White;
        Foreground = owner.TryFindResource("TextPrimaryBrush") as Brush ?? Brushes.Black;
        var content = new StackPanel { Margin = new Thickness(24) };
        content.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, FontSize = 14 });
        var actions = new StackPanel { Orientation = Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new Thickness(0, 24, 0, 0) };
        var cancel = new Button { Content = cancelText, IsCancel = true, IsDefault = true,
            MinWidth = 110, Height = 40, Margin = new Thickness(0, 0, 12, 0) };
        var remove = new Button { Content = removeText, MinWidth = 110, Height = 40 };
        cancel.Style = owner.TryFindResource("SecondaryButtonStyle") as Style;
        remove.Style = owner.TryFindResource("SecondaryButtonStyle") as Style;
        cancel.Click += (_, _) => DialogResult = false;
        remove.Click += (_, _) => DialogResult = true;
        actions.Children.Add(cancel);
        actions.Children.Add(remove);
        content.Children.Add(actions);
        Content = content;
        Loaded += (_, _) => cancel.Focus();
    }
}
