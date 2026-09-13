using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameTouchCompanion.Core;
using Serilog;

namespace GameTouchCompanion.App;

public partial class AppearancePanel : UserControl
{
    internal AppearanceSettingsStore Store { get; set; } = Appearance.CreateStore();
    private bool ready;
    private string order = "navigation"; // retained only to read older appearance.json files safely.
    private string caption = string.Empty; // retained for existing appearance.json, but no longer displayed in the toolbar.
    public AppearancePanel()
    {
        InitializeComponent();
        LoadDraft(Appearance.Current.Settings);
        if (Appearance.Current.LoadFailed) ShowError(() => Localization.Get("AppearanceLoadFailed"));
        System.ComponentModel.PropertyChangedEventManager.AddHandler(Localization.State, LanguageChanged, string.Empty);
        Loaded += (_, _) => RefreshPreview();
    }
    private void LanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            if (!Dispatcher.HasShutdownStarted) Dispatcher.BeginInvoke(() => LanguageChanged(sender, e));
            return;
        }
        if (IsLoaded) RefreshPreview();
    }
    private void LoadDraft(AppearanceSettings settings)
    {
        ready = false;
        Palette.SelectedValue = settings.Palette; Density.SelectedValue = settings.Density; ConfigurationTheme.SelectedValue = settings.ConfigurationTheme; order = settings.Order; caption = settings.Caption;
        Labels.IsChecked = settings.ShowLabels;
        ready = true; RefreshPreview();
    }
    private AppearanceSettings Draft() => new() { Palette = (string)Palette.SelectedValue, Density = (string)Density.SelectedValue, ConfigurationTheme = (string)ConfigurationTheme.SelectedValue,
        Order = order, ShowLabels = Labels.IsChecked == true, Caption = caption };
    private void DraftChanged(object sender, RoutedEventArgs e) { if (ready) RefreshPreview(); }
    private void RefreshPreview()
    {
        if (!ready) return;
        var draft = Draft();
        var preview = new Appearance(); preview.Apply(draft with { Caption = string.Empty });
        Preview.Background = preview.Background;
        PreviewButtons.Children.Clear(); PreviewSavedActions.Children.Clear();
        foreach (var action in ToolbarAppearance.NavigationActions) PreviewButtons.Children.Add(PreviewButton(action, preview));
        foreach (var action in ToolbarAppearance.SavedActions) PreviewSavedActions.Children.Add(PreviewButton(action, preview));
    }
    private Button PreviewButton(string action, Appearance preview) => new() { Content = ToolbarAppearance.Content(action, preview.Settings.ShowLabels, ToolbarAppearance.IconSize(preview.ButtonSize)),
        Template = (ControlTemplate)FindResource("ToolbarImageButtonTemplate"), Background = Brushes.Transparent, Foreground = Brushes.White,
        MinHeight = preview.ButtonSize, MinWidth = preview.ButtonSize, FontSize = preview.FontSize, Margin = new Thickness(3), Padding = new Thickness(12, 6, 12, 6),
        IsHitTestVisible = false, Focusable = false, IsTabStop = false };
    private void ShowError(Func<string> render)
    {
        Localization.Text(Status, render);
        Status.Visibility = Visibility.Visible;
    }

    private void ClearStatus()
    {
        System.Windows.Data.BindingOperations.ClearBinding(Status, TextBlock.TextProperty);
        Status.Text = string.Empty;
        Status.Visibility = Visibility.Collapsed;
    }
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        IsEnabled = false;
        try
        {
            var draft = Draft();
            await Store.SaveAsync(draft);
            Appearance.Current.Apply(draft);
            ClearStatus();
            Log.Information("Appearance settings saved. Palette={Palette}; Density={Density}; ConfigurationTheme={ConfigurationTheme}; ShowLabels={ShowLabels}",
                draft.Palette, draft.Density, draft.ConfigurationTheme, draft.ShowLabels);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Appearance settings save failed");
            ShowError(() => Localization.Get("AppearanceFailed"));
        }
        finally { IsEnabled = true; }
    }
    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        LoadDraft(new());
        ClearStatus();
        Log.Debug("Appearance draft reset to defaults");
    }
}
