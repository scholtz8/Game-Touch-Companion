using System.Windows;
using Serilog;

namespace GameTouchCompanion.App;

public partial class MainWindow
{
    private readonly GameTouchCompanion.Core.LanguagePreferenceStore languageStore;

    private async void SaveGeneralSettings_Click(object sender, RoutedEventArgs e)
    {
        if (LanguageCombo.SelectedValue is not string language || isClosed) return;

        var startHidden = StartInTrayCheck.IsChecked == true;
        var closeHidden = CloseToTrayCheck.IsChecked == true;
        var startWithWindows = WindowsStartupCheck.IsChecked == true;

        SaveGeneralSettingsButton.IsEnabled = false;
        LanguageCombo.IsEnabled = false;
        StartInTrayCheck.IsEnabled = false;
        CloseToTrayCheck.IsEnabled = false;
        WindowsStartupCheck.IsEnabled = false;

        await settingsOperationGate.WaitAsync();
        try
        {
            if (isClosed) return;

            // Windows startup is only changed when the draft differs from the state that
            // was read. An existing registration pointing to another copy is never silently
            // replaced; the explicit "Register this copy" action remains the recovery path.
            if (startupRegistration is not null && startupState is { } state && startWithWindows != state.Exists)
                startupRegistration.SetEnabled(startWithWindows, replaceExisting: false);

            await viewModel.SetTrayPreferencesAsync(startHidden, closeHidden);
            await languageStore.SaveAsync(language);
            Localization.Initialize(language);
            RefreshStartupRegistration();

            Localization.Text(GeneralSettingsStatus, () => Localization.Get("AllSettingsSaved"));
            GeneralSettingsStatus.Visibility = Visibility.Visible;
            ClearSettingsError();
            Log.Information("General settings saved. Language={Language}; StartHidden={StartHidden}; CloseToTray={CloseToTray}; WindowsStartup={WindowsStartup}",
                language, startHidden, closeHidden, startWithWindows);
        }
        catch (Exception ex)
        {
            LanguageCombo.SelectedValue = Localization.Language;
            RefreshStartupRegistration();
            StartInTrayCheck.IsChecked = viewModel.StartMinimizedToTray;
            CloseToTrayCheck.IsChecked = viewModel.CloseToTray;
            Localization.Text(GeneralSettingsStatus, () => Localization.Get("AllSettingsFailed"));
            GeneralSettingsStatus.Visibility = Visibility.Visible;
            ShowSettingsError(() => Localization.Get("AllSettingsFailed"), "general-settings-save");
            Log.Warning(ex, "General settings save failed");
        }
        finally
        {
            settingsOperationGate.Release();
            if (!isClosed)
            {
                SaveGeneralSettingsButton.IsEnabled = true;
                LanguageCombo.IsEnabled = true;
                StartInTrayCheck.IsEnabled = viewModel.SettingsLoaded;
                CloseToTrayCheck.IsEnabled = viewModel.SettingsLoaded;
                WindowsStartupCheck.IsEnabled = startupRegistration is not null;
            }
        }
    }

    private void LanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (isClosed) return;
        if (!Dispatcher.CheckAccess())
        {
            if (!Dispatcher.HasShutdownStarted) Dispatcher.BeginInvoke(() => LanguageChanged(sender, e));
            return;
        }
        LanguageCombo.SelectedValue = Localization.Language;
        viewModel.RefreshLanguage();
        browserViewModel.RefreshLanguage();
        profilesViewModel.RefreshLanguage();
        UpdateSetupGuide();
        DrawMonitorPreview();
        // Do not capture another diagnostic sample, restart detection, navigate or recreate windows.
    }
}
