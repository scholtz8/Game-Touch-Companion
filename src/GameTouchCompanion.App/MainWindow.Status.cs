using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Serilog;

namespace GameTouchCompanion.App;

public partial class MainWindow
{
    private void ClearSettingsError()
    {
        BindingOperations.ClearBinding(SettingsErrorText, TextBlock.TextProperty);
        SettingsErrorText.Text = string.Empty;
        SettingsErrorPanel.Visibility = Visibility.Collapsed;
    }

    private void ShowSettingsError(Func<string> render, string context)
    {
        Localization.Text(SettingsErrorText, render);
        SettingsErrorPanel.Visibility = Visibility.Visible;
        Log.Warning("Settings UI error. Context={Context}; Message={Message}", context, render());
    }
}
