using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Controls;
using System.Windows.Threading;
using GameTouchCompanion.App;
using GameTouchCompanion.Core;
using GameTouchCompanion.Native;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace GameTouchCompanion.IntegrationTests;

/// <summary>
/// Opt-in desktop test with a disposable, isolated browser profile. This is not a physical touch/focus test.
/// </summary>
[Collection("Language state")]
public sealed class BrowserRuntimeTests
{
    [DesktopBrowserFact]
    [Trait("Category", "BrowserRuntime")]
    public async Task LocalNavigationErrorsAndLifecycleWorkWithRealWebView2()
    {
        await RunOnStaAsync(async () =>
        {
            var testDirectory = Path.Combine(AppContext.BaseDirectory, "WebViewSmoke", Guid.NewGuid().ToString("N"));
            var model = new BrowserViewModel(new JsonBrowserSettingsStore(Path.Combine(testDirectory, "browser.json")));
            await model.InitializeAsync();
            // Phase 4: a saved profile queues the first page without changing browser home/favorites.
            var profiles = new ProfilesViewModel(new JsonGameProfileStore(Path.Combine(testDirectory, "profiles.json")));
            await profiles.InitializeAsync();
            profiles.DisplayName = "Runtime smoke";
            profiles.Tabs[0].Url = BrowserUrlPolicy.LocalHomeUrl;
            await profiles.SaveAsync();
            Assert.False(profiles.HasError);
            await profiles.ApplyAsync(profile =>
            {
                Assert.True(model.Navigate(profile.Url));
                return Task.CompletedTask;
            });
            Assert.Equal(BrowserUrlPolicy.LocalHomeUrl, model.HomeUrl);
            Assert.Empty(model.Favorites);
            var monitors = new Win32MonitorService().GetMonitors();
            var monitor = monitors.FirstOrDefault(item => !item.IsPrimary) ?? monitors[0];
            var companion = new CompanionWindow(monitor, model, Path.Combine(testDirectory, "profile"));
            try
            {
                companion.ShowWithoutActivation();
                await WaitUntilAsync(() => model.IsReady && model.CurrentUrl == BrowserUrlPolicy.LocalHomeUrl,
                    () => $"Local page did not initialize. {model.Status}");
                var host = Assert.IsType<Grid>(companion.FindName("BrowserHost"));
                await WaitUntilAsync(() => host.Children.OfType<WebView2>().Any(view => view.CoreWebView2 is not null),
                    () => "No WebView2 tab was initialized.");
                var browser = Assert.Single(host.Children.OfType<WebView2>());
                var core = browser.CoreWebView2;
                Assert.NotNull(core);
                // CurrentUrl now represents the selected tab target immediately, while WebView2 may
                // still be completing its first navigation. Wait for the real document before starting
                // the navigation lifecycle assertions below so the test does not cancel startup work.
                await WaitForDocumentReadyAsync(core, BrowserUrlPolicy.LocalHomeUrl);
                Assert.False(core.Settings.AreHostObjectsAllowed);
                Assert.True(core.Settings.IsWebMessageEnabled);
                Assert.Equal("true", await core.ExecuteScriptAsync("typeof window.__gtcKeyboard === 'object'"));
                Assert.False(core.Settings.AreDefaultScriptDialogsEnabled);
                Assert.False(core.Settings.AreDevToolsEnabled);
                var windowService = new Win32ProcessWindowService();
                var companionHwnd = new System.Windows.Interop.WindowInteropHelper(companion).Handle;
                var metadata = Assert.Single(windowService.GetWindows(), item => item.Handle == companionHwnd);
                Assert.Equal(Environment.ProcessId, metadata.ProcessId);
                Assert.True(metadata.IsVisible);
                Assert.True(metadata.IsToolWindow);
                Assert.False(metadata.IsEligible); // Companion must never be treated as a game's main window.

                const string secondPage = "https://touch-test.local/second.html";
                await NavigateAsync(core, () => Assert.True(model.Navigate(secondPage)));
                Assert.Equal(secondPage, model.CurrentUrl);
                Assert.True(model.CanGoBack);
                await NavigateAsync(core, core.GoBack);
                Assert.Equal(BrowserUrlPolicy.LocalHomeUrl, model.CurrentUrl);
                Assert.True(model.CanGoForward);
                await NavigateAsync(core, core.GoForward);
                Assert.Equal(secondPage, model.CurrentUrl);
                await NavigateAsync(core, core.Reload);
                Assert.Equal(secondPage, model.CurrentUrl);
                await model.AddFavoriteAsync();
                Assert.Contains(model.Favorites, item => item.Url == secondPage);
                await model.SetToolbarVisibleAsync(false);
                Assert.False(model.ShowToolbar);
                await model.SetToolbarVisibleAsync(true);
                Assert.True(model.ShowToolbar);

                Assert.False(model.Navigate("file:///C:/Windows/win.ini"));
                Assert.Equal(secondPage, core.Source);
                await NavigateAsync(core, () => Assert.True(model.GoHome()));

                // Choose an unused local port: deterministic connection failure without an external service.
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                var closedPort = ((IPEndPoint)listener.LocalEndpoint).Port;
                listener.Stop();
                await NavigateAsync(core, () => Assert.True(model.Navigate($"http://127.0.0.1:{closedPort}/")), success: false);
                Assert.True(model.HasError);
                await NavigateAsync(core, () => Assert.True(model.GoHome()));
                Assert.False(model.HasError);
                Assert.Equal(BrowserUrlPolicy.LocalHomeUrl, model.CurrentUrl);
            }
            finally
            {
                companion.Close();
            }

            Assert.False(model.IsReady);
            Assert.False(model.CanGoBack);
            Assert.False(model.CanGoForward);

            // Close immediately while initialization can still be awaiting the runtime.
            var earlyClose = new CompanionWindow(monitor, model, Path.Combine(testDirectory, "early-close-profile"));
            earlyClose.ShowWithoutActivation();
            earlyClose.Close();
            await Task.Delay(250);
            Assert.False(model.IsReady);
        }).WaitAsync(TimeSpan.FromSeconds(90));
    }

    private static async Task WaitForDocumentReadyAsync(CoreWebView2 core, string expectedUrl)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < TimeSpan.FromSeconds(20))
        {
            if (string.Equals(core.Source, expectedUrl, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var state = await core.ExecuteScriptAsync("document.readyState");
                    if (state is "\"interactive\"" or "\"complete\"") return;
                }
                catch (InvalidOperationException)
                {
                    // WebView2 can reject script execution while the initial document is changing.
                }
            }
            await Task.Delay(50);
        }

        Assert.Fail($"Initial WebView2 document did not become ready. Source={core.Source}");
    }

    private static async Task NavigateAsync(CoreWebView2 core, Action action, bool success = true)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        ulong? navigationId = null;
        void Starting(object? sender, CoreWebView2NavigationStartingEventArgs args) => navigationId = args.NavigationId;
        void Completed(object? sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.NavigationId == navigationId) completion.TrySetResult(args.IsSuccess);
        }
        core.NavigationStarting += Starting;
        core.NavigationCompleted += Completed;
        try
        {
            action();
            Assert.Equal(success, await completion.Task.WaitAsync(TimeSpan.FromSeconds(15)));
        }
        finally
        {
            core.NavigationStarting -= Starting;
            core.NavigationCompleted -= Completed;
        }
    }

    internal static async Task WaitUntilAsync(Func<bool> condition, Func<string> failure)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(20), failure());
            await Task.Delay(50);
        }
    }

    internal static Task RunOnStaAsync(Func<Task> body)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            dispatcher.BeginInvoke(new Action(async () =>
            {
                try { await body(); completion.TrySetResult(); }
                catch (Exception exception) { completion.TrySetException(exception); }
                finally { dispatcher.BeginInvokeShutdown(DispatcherPriority.Background); }
            }));
            Dispatcher.Run();
        }) { IsBackground = true, Name = "Browser runtime smoke (STA)" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }
}

public sealed class DesktopBrowserFactAttribute : FactAttribute
{
    public DesktopBrowserFactAttribute()
    {
        if (!OperatingSystem.IsWindows() || Environment.GetEnvironmentVariable("GTC_RUN_WEBVIEW_TESTS") != "1")
            Skip = "OPT-IN DESKTOP TEST: set GTC_RUN_WEBVIEW_TESTS=1 on Windows with WebView2; temporarily shows Companion.";
    }
}
