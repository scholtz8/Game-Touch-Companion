using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using GameTouchCompanion.App;
using GameTouchCompanion.Core;
using Localization = GameTouchCompanion.App.Localization;

namespace GameTouchCompanion.IntegrationTests;

[CollectionDefinition("Language state", DisableParallelization = true)]
public sealed class LanguageStateCollection;

[Collection("Language state")]
public sealed class LanguageStartupTests
{
    [Fact]
    public async Task CancelBeforeInitializationAndSavedPreferenceSkipsPrompt()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "LanguageSmoke", Guid.NewGuid().ToString("N"));
        var store = new LanguagePreferenceStore(Path.Combine(folder, "language.json"));
        var previous = Localization.Language;
        try
        {
            var prompts = 0;
            Assert.False(await App.App.InitializeLanguageAsync(store, _ => { prompts++; return null; }));
            Assert.Equal(1, prompts);
            Assert.Null(await store.LoadAsync());
            await store.SaveAsync("en");
            Assert.True(await App.App.InitializeLanguageAsync(store, _ => throw new InvalidOperationException("Prompt must not run")));
            Assert.Equal("en", Localization.Language);
        }
        finally { Localization.Initialize(previous); }
    }

    [DesktopBrowserFact]
    [Trait("Category", "BrowserRuntime")]
    public async Task FirstChoicePersistsAndBothLanguagesRenderWithoutReinitializingSession()
    {
        await BrowserRuntimeTests.RunOnStaAsync(async () =>
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "LanguageSmoke", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var store = new LanguagePreferenceStore(Path.Combine(folder, "language.json"));
            var dialog = new LanguageSelectionWindow(store);
            dialog.Loaded += (_, _) =>
            {
                Render(dialog, Path.Combine(folder, "language-dialog.png"));
                dialog.LanguageChoices.SelectedIndex = 1;
                dialog.SaveButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            };
            Assert.True(dialog.ShowDialog());
            Assert.Equal("en", await store.LoadAsync());
            var previous = Localization.Language;
            try
            {
                using var realTray = new TrayService();
                realTray.Initialize(() => { }, () => { }, () => { });
                foreach (var language in new[] { "en", "es" })
                {
                    Localization.Initialize(language);
                    var main = ShellLifecycleTests.CreateForLanguageTest(store);
                    try
                    {
                        await main.StartShellAsync(false, forceShow: true);
                        Assert.Equal(Localization.Get("Ui033"), ((TabItem)main.FindName("SettingsTab")).Header);
                        Assert.Equal(language, ((ComboBox)main.FindName("LanguageCombo")).SelectedValue);
                        Assert.Null(main.FindName("DetectionTab"));
                        Assert.Null(main.FindName("SaveLanguageButton"));
                        Assert.NotNull(main.FindName("SaveGeneralSettingsButton"));
                        Assert.NotNull(main.FindName("DiagnosticsTab"));
                        var tabs = (TabControl)main.FindName("ConfigurationTabs");
                        for (var i = 0; i < tabs.Items.Count; i++)
                        {
                            tabs.SelectedIndex = i;
                            Render(main, Path.Combine(folder, $"{language}-tab-{i}.png"));
                        }
                        Assert.True(main.IsDetectionRunning);
                        var mainHwnd = new WindowInteropHelper(main).Handle;
                        var profileModel = (ProfilesViewModel)((ProfilesPanel)main.FindName("GameProfilesPanel")).DataContext;
                        profileModel.DisplayName = "Siguiente {my draft}";
                        profileModel.NewProfile(); // leave discard confirmation open
                        Assert.True(profileModel.DiscardConfirmation);
                        var diagnostics = ((TextBlock)main.FindName("DiagnosticsSnapshot")).Text;
                        var diagnosticTimestamp = diagnostics.Split('\n')[0].Split(": ", 2)[1];
                        var choice = (ComboBox)main.FindName("LanguageCombo");
                        var save = (Button)main.FindName("SaveGeneralSettingsButton");
                        var nextLanguage = language == "en" ? "es" : "en";
                        choice.SelectedValue = nextLanguage;
                        save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        await BrowserRuntimeTests.WaitUntilAsync(() => save.IsEnabled, () => "Language save did not finish");
                        Assert.Equal(nextLanguage, await store.LoadAsync());
                        Assert.Equal(nextLanguage, Localization.Language); // live text update, no session recreation
                        Assert.NotEmpty(realTray.MenuLabels);
                        Assert.Contains(Localization.Get("TrayOpen"), realTray.MenuLabels);
                        Assert.Contains(Localization.Get("TrayRearm"), realTray.MenuLabels);
                        Assert.Equal(Localization.Get("Ui033"), ((TabItem)main.FindName("SettingsTab")).Header);
                        Assert.True(main.IsDetectionRunning);
                        Assert.Equal(mainHwnd, new WindowInteropHelper(main).Handle);
                        Assert.Null(main.CurrentCompanion);
                        Assert.Equal("Siguiente {my draft}", profileModel.DisplayName);
                        Assert.True(profileModel.DiscardConfirmation);
                        Assert.Contains(diagnosticTimestamp, ((TextBlock)main.FindName("DiagnosticsSnapshot")).Text);
                        Assert.Null(main.FindName("SetupNext"));
                        Assert.NotEmpty(((TextBlock)main.FindName("SetupSummary")).Text);
                        var settingsTab = (TabItem)main.FindName("SettingsTab");
                        tabs.SelectedItem = settingsTab;
                        Render(main, Path.Combine(folder, $"live-{nextLanguage}-settings.png"));
                        main.PrepareForSessionEnd();
                    }
                    finally { main.ExitCompletely(); }
                }
            }
            finally { Localization.Initialize(previous); }
        }).WaitAsync(TimeSpan.FromSeconds(120));
    }

    [Fact]
    public void DynamicMessagesRetainArgumentsAndTranslateErrorsWithoutChangingDraftData()
    {
        var previous = Localization.Language;
        try
        {
            var browser = new BrowserViewModel(new JsonBrowserSettingsStore(Path.Combine(AppContext.BaseDirectory, "unused-language-browser.json")));
            browser.ReportError("Enlace bloqueado: solo se permiten direcciones HTTP y HTTPS sin credenciales.");
            var userData = "Siguiente {my profile}";
            var message = LocalizedMessage.Format($"Selection confirmed. Companion: {userData}.");
            Localization.Initialize("en");
            Assert.Equal(Localization.Get("Dynamic100"), browser.Error);
            Assert.Equal("Selection confirmed. Companion: " + userData + ".", message.Render());
            var guide = new SetupGuide();
            guide.Next(true);
            Assert.StartsWith("2 of 3", guide.Title);
            Localization.Initialize("es");
            Assert.Equal(Localization.Get("Dynamic100"), browser.Error);
            Assert.Contains(userData, message.Render());
            Assert.StartsWith("2 de 3", guide.Title);
            Assert.Equal(1, guide.Step);
        }
        finally { Localization.Initialize(previous); }
    }

    [DesktopBrowserFact]
    [Trait("Category", "BrowserRuntime")]
    public async Task FailedSaveKeepsCurrentLanguageAndDialogsUseTranslatedButtons()
    {
        await BrowserRuntimeTests.RunOnStaAsync(async () =>
        {
            var previous = Localization.Language;
            var folder = Path.Combine(AppContext.BaseDirectory, "LanguageSmoke", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            // Destination is a directory, making atomic replacement fail without touching user data.
            var main = ShellLifecycleTests.CreateForLanguageTest(new LanguagePreferenceStore(folder));
            try
            {
                Localization.Initialize("en");
                await main.StartShellAsync(false, forceShow: true);
                ((ComboBox)main.FindName("LanguageCombo")).SelectedValue = "es";
                var save = (Button)main.FindName("SaveGeneralSettingsButton");
                save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await BrowserRuntimeTests.WaitUntilAsync(() => save.IsEnabled, () => "Save did not finish");
                Assert.Equal("en", Localization.Language);
                Assert.Equal("en", ((ComboBox)main.FindName("LanguageCombo")).SelectedValue);
                Assert.Equal(  Localization.Get("AllSettingsFailed"), ((TextBlock)main.FindName("GeneralSettingsStatus")).Text);
                var result = AppDialog.Show(Localization.T("Game and Companion will use the same monitor. Continue only for testing?"),
                    Localization.T("Same monitor warning"), MessageBoxButton.YesNo, ready: dialog =>
                    {
                        var panel = (StackPanel)dialog.Content;
                        Assert.Contains("same display", ((TextBlock)panel.Children[0]).Text);
                        var actions = (StackPanel)panel.Children[1];
                        Assert.Equal("Yes", ((Button)actions.Children[0]).Content);
                        Assert.Equal("No", ((Button)actions.Children[1]).Content);
                        Render(dialog, Path.Combine(folder, "english-dialog.png"));
                        ((Button)actions.Children[1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    });
                Assert.Equal(MessageBoxResult.No, result);
            }
            finally { main.ExitCompletely(); Localization.Initialize(previous); }
        }).WaitAsync(TimeSpan.FromSeconds(60));
    }

    private static void Render(Window window, string path)
    {
        window.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
