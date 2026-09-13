using System.Windows;
using System.Windows.Controls;

namespace GameTouchCompanion.App;

internal static class AppDialog
{
    internal static MessageBoxResult Show(string message, string title, MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage image = MessageBoxImage.None, Action<Window>? ready = null)
    {
        var appearance = Appearance.Current;
        var window = new Window { Title = title, Width = 510, SizeToContent = SizeToContent.Height,
            ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Background = appearance.ConfigurationBackground, Foreground = appearance.ConfigurationForeground };
        if (Application.Current?.MainWindow is { IsVisible: true } owner)
        {
            window.Owner = owner;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 20),
            Foreground = appearance.ConfigurationForeground });
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var result = buttons == MessageBoxButton.YesNo ? MessageBoxResult.No : MessageBoxResult.Cancel;
        foreach (var choice in buttons == MessageBoxButton.YesNo ? new[] { MessageBoxResult.Yes, MessageBoxResult.No } : new[] { MessageBoxResult.OK })
        {
            var button = new Button { Content = Localization.T(choice.ToString()), MinWidth = 88, Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(4),
                Background = appearance.ConfigurationControl, Foreground = appearance.ConfigurationForeground, BorderBrush = appearance.ConfigurationBorder,
                IsDefault = choice is MessageBoxResult.No or MessageBoxResult.OK, IsCancel = choice == MessageBoxResult.No };
            button.Click += (_, _) => { result = choice; window.DialogResult = true; };
            actions.Children.Add(button);
        }
        panel.Children.Add(actions); window.Content = panel;
        if (ready is not null) window.Loaded += (_, _) => ready(window);
        window.ShowDialog();
        return result;
    }
}
