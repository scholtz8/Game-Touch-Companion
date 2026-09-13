using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;

namespace GameTouchCompanion.App;

public partial class CompanionWindow
{
    private string? appliedPalette;
    private string? appliedDensity;
    private bool? appliedLabels;
    private void InitializeAppearance()
    {
        PropertyChangedEventManager.AddHandler(Appearance.Current, AppearanceChanged, string.Empty);
        ApplyAppearance();
    }
    private void AppearanceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (isClosing) return;
        if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke(ApplyAppearance); return; }
        ApplyAppearance();
    }
    private void ApplyAppearance()
    {
        if (isClosing) return;
        var appearance = Appearance.Current;
        UpdateToolbarVisibilityIcon();
        // ConfigurationTheme belongs solely to MainWindow. Do not recreate Companion toolbar content for it.
        if (appliedPalette == appearance.Settings.Palette && appliedDensity == appearance.Settings.Density &&
            appliedLabels == appearance.Settings.ShowLabels) return;
        appliedPalette = appearance.Settings.Palette;
        appliedDensity = appearance.Settings.Density;
        appliedLabels = appearance.Settings.ShowLabels;
        foreach (var action in ToolbarAppearance.NavigationActions.Concat(ToolbarAppearance.SavedActions))
        {
            var button = (Button)FindName(char.ToUpperInvariant(action[0]) + action[1..] + "Action");
            button.SetBinding(ContentControl.ContentProperty, Localization.ObjectBinding(() => ToolbarAppearance.Content(action, appearance.Settings.ShowLabels, ToolbarAppearance.IconSize(appearance.ButtonSize))));
            button.SetBinding(AutomationProperties.NameProperty, Localization.Binding(() => ToolbarAppearance.Label(action)));
            button.SetBinding(ToolTipProperty, Localization.Binding(() => ToolbarAppearance.Label(action)));
        }
        CloseAction.SetBinding(ContentControl.ContentProperty, Localization.ObjectBinding(() => ToolbarAppearance.Content("close", false, ToolbarAppearance.IconSize(appearance.ButtonSize))));
        CloseAction.SetBinding(ToolTipProperty, Localization.Binding(() => ToolbarAppearance.Label("close")));
    }
    private void UpdateToolbarVisibilityIcon()
    {
        ToggleToolbarAction.SetBinding(ContentControl.ContentProperty, Localization.ObjectBinding(() => ToolbarAppearance.Content(((BrowserViewModel)DataContext).ShowToolbar ? "hide" : "show", false, ToolbarAppearance.IconSize(Appearance.Current.ButtonSize))));
        ToggleToolbarAction.SetBinding(AutomationProperties.NameProperty, Localization.Binding(() => ((BrowserViewModel)DataContext).ShowToolbar ? ToolbarAppearance.Label("hide") : ToolbarAppearance.Label("show")));
        ToggleToolbarAction.SetBinding(ToolTipProperty, Localization.Binding(() => ((BrowserViewModel)DataContext).ShowToolbar ? ToolbarAppearance.Label("hide") : ToolbarAppearance.Label("show")));
    }
}
