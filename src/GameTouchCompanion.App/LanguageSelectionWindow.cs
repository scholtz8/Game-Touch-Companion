using System.Windows;
using System.Windows.Controls;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.App;

public sealed class LanguageSelectionWindow : Window
{
    private readonly ComboBox languages = new() { MinHeight = 36, Margin = new Thickness(0, 14, 0, 14) };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Button save = new() { Content = "Guardar / Save", Padding = new Thickness(18, 8, 18, 8), IsDefault = true };
    private bool saving;
    internal ComboBox LanguageChoices => languages;
    internal Button SaveButton => save;
    public string? SelectedLanguage { get; private set; }

    public LanguageSelectionWindow(LanguagePreferenceStore store)
    {
        Title = "Game Touch Companion · Idioma / Language";
        Width = 490; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = "Elige el idioma / Choose your language", FontSize = 22, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new TextBlock { Text = "Podrás cambiarlo en Ajustes. / You can change it in Settings.", Margin = new Thickness(0, 12, 0, 0), TextWrapping = TextWrapping.Wrap });
        languages.Items.Add(new ComboBoxItem { Content = "Español", Tag = "es" });
        languages.Items.Add(new ComboBoxItem { Content = "English", Tag = "en" });
        languages.SelectedIndex = 0;
        panel.Children.Add(languages); panel.Children.Add(save); panel.Children.Add(status);
        Content = panel;
        Closing += (_, e) => { if (saving) e.Cancel = true; };
        save.Click += async (_, _) =>
        {
            if (saving) return;
            saving = true; save.IsEnabled = false; languages.IsEnabled = false;
            try
            {
                var language = (string)((ComboBoxItem)languages.SelectedItem).Tag;
                await store.SaveAsync(language);
                SelectedLanguage = language;
                saving = false;
                DialogResult = true;
            }
            catch (Exception)
            {
                status.Text = "No se pudo guardar. Reintenta o cierra para cancelar. / Could not save. Retry or close to cancel.";
            }
            finally { saving = false; save.IsEnabled = true; languages.IsEnabled = true; }
        };
    }
}
