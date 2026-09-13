using GameTouchCompanion.Core;
using GameTouchCompanion.Native;

namespace GameTouchCompanion.IntegrationTests;

public sealed class ProcessWindowServiceTests
{
    [DesktopBrowserFact]
    [Trait("Category", "BrowserRuntime")]
    public void EnumeratesDesktopWindowMetadataWithoutTitlesOrActivation()
    {
        var service = new Win32ProcessWindowService();
        var windows = service.GetWindows();
        Assert.All(windows, window =>
        {
            Assert.NotEqual(0, window.Handle);
            Assert.True(window.ProcessId >= 0);
            Assert.True(window.Bounds.Width >= 0 && window.Bounds.Height >= 0);
        });
        Assert.Equal(windows.Count, windows.Select(w => w.Handle).Distinct().Count());
    }

    [Fact]
    public void EmptyConfigurationDoesNotEnumerateProcessesOrWindowsAndStaleProcessFailsClosed()
    {
        var windows = new CountingWindowService();
        var source = new WindowsGameDetectionSource(windows);
        var snapshot = source.Capture([]);
        Assert.Empty(snapshot.Processes);
        Assert.Empty(snapshot.Windows);
        Assert.Equal(0, windows.Enumerations);
        var missing = new DetectedGame(new GameProfile(), new GameProcessInfo(int.MaxValue, "Missing.exe", 1),
            new ProcessWindowInfo(123, int.MaxValue, new DisplayRect(0, 0, 1, 1), true, false, false, false));
        Assert.False(source.IsStillForeground(missing));
    }

    private sealed class CountingWindowService : IProcessWindowService
    {
        public int Enumerations { get; private set; }
        public nint GetForegroundWindow() => 0;
        public IReadOnlyList<ProcessWindowInfo> GetWindows() { Enumerations++; return []; }
    }
}
