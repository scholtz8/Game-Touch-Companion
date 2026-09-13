using System.Windows;
using Serilog;

namespace GameTouchCompanion.App;

public partial class MainWindow
{
    private readonly GameTouchCompanion.Core.LanguagePreferenceStore languageStore;
    private async void SaveLanguage_Click(object sender, RoutedEventArgs e)
    {
        if (LanguageCombo.SelectedValue is not string language) return;
        SaveLanguageButton.IsEnabled = false;
        LanguageCombo.IsEnabled = false;
        try
        {
            await languageStore.SaveAsync(language);
            Localization.Initialize(language);
            Localization.Text(LanguageStatus, () => Localization.Get("LanguageSaved"));
            ClearSettingsError();
            Log.Information("Language preference saved. Language={Language}", language);
        }
        catch (Exception ex)
        {
            LanguageCombo.SelectedValue = Localization.Language;
            Localization.Text(LanguageStatus, () => Localization.Get("LanguageFailed"));
            ShowSettingsError(() => Localization.Get("LanguageFailed"), "language-save");
            Log.Warning(ex, "Language preference save failed");
        }
        finally { SaveLanguageButton.IsEnabled = true; LanguageCombo.IsEnabled = true; }
    }

    private void LanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (isClosed) return;
        if (!Dispatcher.CheckAccess())
        {
            if (!Dispatcher.HasShutdownStarted) Dispatcher.BeginInvoke(() => LanguageChanged(sender, e));
            return;
        }
        viewModel.RefreshLanguage();
        browserViewModel.RefreshLanguage();
        profilesViewModel.RefreshLanguage();
        UpdateSetupGuide();
        DrawMonitorPreview();
        // Do not capture another diagnostic sample, restart detection, navigate or recreate windows.
    }
}
