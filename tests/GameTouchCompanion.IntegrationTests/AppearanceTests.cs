using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GameTouchCompanion.App;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.IntegrationTests;

[Collection("Language state")]
public sealed class AppearanceTests
{
    [DesktopBrowserFact]
    [Trait("Category", "BrowserRuntime")]
    public async Task ToolbarLabelsRemainWhiteIndependentlyOfConfigurationTheme()
    {
        await BrowserRuntimeTests.RunOnStaAsync(() =>
        {
            foreach (var theme in new[] { "light", "dark" })
            {
                var preview = new Appearance();
                preview.Apply(new AppearanceSettings { ConfigurationTheme = theme, ShowLabels = true });
                var content = Assert.IsType<StackPanel>(ToolbarAppearance.Content("back", true, ToolbarAppearance.IconSize(preview.ButtonSize)));
                var label = Assert.IsType<TextBlock>(content.Children[1]);
                Assert.Equal(Colors.White, Assert.IsType<SolidColorBrush>(label.Foreground).Color);
            }
            return Task.CompletedTask;
        }).WaitAsync(TimeSpan.FromSeconds(20));
    }

    [DesktopBrowserFact]
    [Trait("Category", "BrowserRuntime")]
    public async Task PreviewSaveResetAndFailurePreserveLiveCompanion()
    {
        await BrowserRuntimeTests.RunOnStaAsync(async () =>
        {
            var previous = Appearance.Current.Settings;
            var folder = Path.Combine(AppContext.BaseDirectory, "AppearanceSmoke", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var store = new AppearanceSettingsStore(Path.Combine(folder, "appearance.json"));
            var main = ShellLifecycleTests.CreateForLanguageTest(new LanguagePreferenceStore(Path.Combine(folder, "language.json")));
            try
            {
                await main.StartShellAsync(false, forceShow: true);
                var panel = (AppearancePanel)main.FindName("CustomizationPanel");
                panel.Store = store;
                ((TabControl)main.FindName("ConfigurationTabs")).SelectedItem = main.FindName("AppearanceTab");
                ((Button)main.FindName("OpenCompanionButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await BrowserRuntimeTests.WaitUntilAsync(() => main.CurrentCompanion is not null && ((BrowserViewModel)main.CurrentCompanion.DataContext).CurrentUrl.Length > 0, () => "Companion not ready");
                var companion = main.CurrentCompanion!;
                var hwnd = new WindowInteropHelper(companion).Handle;
                var browser = (BrowserViewModel)companion.DataContext;
                var url = browser.CurrentUrl;
                var backContent = ((Button)companion.FindName("BackAction")).Content;
                ((ComboBox)panel.FindName("ConfigurationTheme")).SelectedValue = "dark";
                ((Button)panel.FindName("SaveButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await BrowserRuntimeTests.WaitUntilAsync(() => panel.IsEnabled, () => "Theme save did not finish");
                Assert.Same(backContent, ((Button)companion.FindName("BackAction")).Content);
                Assert.Equal(hwnd, new WindowInteropHelper(companion).Handle);
                Assert.Equal(url, browser.CurrentUrl);
                foreach (var palette in new[] { "slate", "ocean", "forest", "plum" })
                foreach (var density in new[] { "compact", "comfortable", "touch" })
                {
                    var active = Appearance.Current.Settings;
                    ((ComboBox)panel.FindName("Palette")).SelectedValue = palette;
                    ((ComboBox)panel.FindName("Density")).SelectedValue = density;
                    ((ComboBox)panel.FindName("ConfigurationTheme")).SelectedValue = "dark";
                    ((CheckBox)panel.FindName("Labels")).IsChecked = false;
                    Assert.Equal(active, Appearance.Current.Settings); // draft only
                    Assert.All(((WrapPanel)panel.FindName("PreviewButtons")).Children.Cast<Button>(), b => { Assert.False(b.Focusable); Assert.False(b.IsTabStop); });
                    ((Button)panel.FindName("SaveButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    await BrowserRuntimeTests.WaitUntilAsync(() => panel.IsEnabled, () => "Appearance save did not finish");
                    Assert.Equal(Appearance.Current.Settings, await store.LoadAsync());
                    Assert.Equal(palette, Appearance.Current.Settings.Palette);
                    Assert.Equal("dark", Appearance.Current.Settings.ConfigurationTheme);
                    Assert.Equal(System.Windows.Media.Color.FromRgb(0x11, 0x18, 0x27), ((SolidColorBrush)main.Background).Color);
                    companion.UpdateLayout();
                    Assert.Same(companion, main.CurrentCompanion);
                    Assert.Equal(hwnd, new WindowInteropHelper(companion).Handle);
                    Assert.Equal(url, browser.CurrentUrl);
                    Assert.True(((CheckBox)main.FindName("DetectionEnabledCheck")).IsChecked);
                    var actions = (WrapPanel)companion.FindName("NavigationActions");
                    var savedActions = (WrapPanel)companion.FindName("SavedActions");
                    Assert.Equal(["BackAction", "ForwardAction", "ReloadAction"], actions.Children.Cast<Button>().Where(b => b.Visibility == Visibility.Visible).Select(b => b.Name));
                    Assert.Equal(Visibility.Collapsed, ((Button)companion.FindName("HomeAction")).Visibility);
                    Assert.Equal(["SaveAction", "FavoritesAction"], savedActions.Children.Cast<Button>().Select(b => b.Name));
                    Assert.All(actions.Children.Cast<Button>().Where(b => b.Visibility == Visibility.Visible).Concat(savedActions.Children.Cast<Button>()), b => Assert.IsType<Image>(b.Content));
                    Assert.All(actions.Children.Cast<Button>(), b => { Assert.False(b.Focusable); Assert.False(b.IsTabStop); Assert.True(b.MinHeight >= 44); });
                    Assert.Null(companion.FindName("BarCaption"));
                }
                Render(main, Path.Combine(folder, "personalization.png"));
                Render(companion, Path.Combine(folder, "companion-plum-touch.png"));
                foreach (var theme in new[] { "light", "dark" })
                {
                    Appearance.Current.Apply(Appearance.Current.Settings with { ConfigurationTheme = theme });
                    var tabs = (TabControl)main.FindName("ConfigurationTabs");
                    foreach (TabItem tab in tabs.Items)
                    {
                        tabs.SelectedItem = tab;
                        main.UpdateLayout();
                        await main.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
                        Render(main, Path.Combine(folder, $"{theme}-{tab.Name}.png"));
                    }
                    tabs.SelectedItem = main.FindName("SettingsTab");
                    var selector = (ComboBox)main.FindName("LanguageCombo");
                    selector.IsDropDownOpen = true;
                    main.UpdateLayout();
                    await main.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
                    var popup = (System.Windows.Controls.Primitives.Popup)selector.Template.FindName("PART_Popup", selector);
                    Assert.True(popup.IsOpen);
                    RenderElement(popup.Child, Path.Combine(folder, $"{theme}-dropdown.png"));
                    selector.IsDropDownOpen = false;
                }
                ((TabControl)main.FindName("ConfigurationTabs")).SelectedItem = main.FindName("AppearanceTab");
                var current = Appearance.Current.Settings;
                panel.Store = new AppearanceSettingsStore(folder); // occupied directory: failure
                ((ComboBox)panel.FindName("Palette")).SelectedValue = "ocean";
                ((Button)panel.FindName("SaveButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await BrowserRuntimeTests.WaitUntilAsync(() => panel.IsEnabled, () => "Failed save did not finish");
                Assert.Equal(current, Appearance.Current.Settings);
                panel.Store = store;
                ((Button)panel.FindName("ResetButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal(current, Appearance.Current.Settings);
                ((Button)panel.FindName("SaveButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await BrowserRuntimeTests.WaitUntilAsync(() => panel.IsEnabled, () => "Reset save did not finish");
                Assert.Equal(new AppearanceSettings(), await store.LoadAsync());
                Assert.Equal(hwnd, new WindowInteropHelper(companion).Handle);
            }
            finally { main.ExitCompletely(); Appearance.Current.Apply(previous); }
        }).WaitAsync(TimeSpan.FromSeconds(60));
    }
    private static void Render(Window window, string path)
    {
        window.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path); encoder.Save(stream);
    }
    private static void RenderElement(UIElement element, string path)
    {
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.RenderSize.Width), (int)Math.Ceiling(element.RenderSize.Height), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path); encoder.Save(stream);
    }
}
