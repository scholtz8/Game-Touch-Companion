using GameTouchCompanion.Core;

namespace GameTouchCompanion.Core.Tests;

public sealed class GameDetectionTests
{
    private static readonly GameProfile Profile = new() { Id = "game", DisplayName = "Juego", ProcessName = "Game.exe", AutoLaunch = true };
    private static readonly GameProcessInfo Process = new(123, "GAME.exe", 456);
    private static readonly ProcessWindowInfo Window = new(789, 123, new DisplayRect(0, 0, 800, 600), true, false, false, false);
    private static GameDetectionSnapshot Snapshot(nint foreground = 789) => new([Process], [Window], foreground);

    [Fact]
    public void RequiresStableForegroundAndHandlesInstanceOnlyOnce()
    {
        var tracker = new GameDetectionTracker();
        Assert.False(tracker.Observe(Snapshot(), [Profile]).ShouldLaunch);
        var ready = tracker.Observe(Snapshot(), [Profile]);
        Assert.True(ready.ShouldLaunch);
        Assert.True(ready.IsForeground);
        Assert.Equal(123, ready.Game!.Process.ProcessId);
        tracker.MarkAttempted(ready.Game);
        Assert.False(tracker.Observe(Snapshot(), [Profile]).ShouldLaunch);
        tracker.ClearObservation();
        Assert.False(tracker.Observe(Snapshot(), [Profile]).ShouldLaunch);
        Assert.False(tracker.Observe(Snapshot(), [Profile]).ShouldLaunch);
        tracker.Reset();
        Assert.False(tracker.Observe(Snapshot(), [Profile]).ShouldLaunch);
        Assert.True(tracker.Observe(Snapshot(), [Profile]).ShouldLaunch);
    }

    [Fact]
    public void BackgroundOrChangedWindowBreaksStability()
    {
        var tracker = new GameDetectionTracker();
        tracker.Observe(Snapshot(), [Profile]);
        Assert.False(tracker.Observe(Snapshot(999), [Profile]).ShouldLaunch);
        Assert.False(tracker.Observe(Snapshot(), [Profile]).ShouldLaunch);
        var changed = Snapshot() with { Windows = [Window with { Handle = 790 }], ForegroundWindow = 790 };
        Assert.False(tracker.Observe(changed, [Profile]).ShouldLaunch);
        Assert.True(tracker.Observe(changed, [Profile]).ShouldLaunch);
    }

    [Fact]
    public void DuplicateEnabledProfilesBlockButDisabledProfileDoesNotCompete()
    {
        var tracker = new GameDetectionTracker();
        var second = Profile with { Id = "second" };
        tracker.Observe(Snapshot(), [Profile, second]);
        var ambiguous = tracker.Observe(Snapshot(), [Profile, second]);
        Assert.False(ambiguous.ShouldLaunch);
        Assert.Contains("varios", ambiguous.Status);
        var disabled = second with { AutoLaunch = false };
        Assert.False(tracker.Observe(Snapshot(), [Profile, disabled]).ShouldLaunch);
        Assert.True(tracker.Observe(Snapshot(), [Profile, disabled]).ShouldLaunch);
    }

    [Fact]
    public void NoAutoLaunchOrWrongProcessNeverOpens()
    {
        var tracker = new GameDetectionTracker();
        var disabled = Profile with { AutoLaunch = false };
        tracker.Observe(Snapshot(), [disabled]);
        Assert.False(tracker.Observe(Snapshot(), [disabled]).ShouldLaunch);
        Assert.Null(tracker.Observe(Snapshot(), [Profile with { ProcessName = "OtherGame.exe" }]).Game);
        Assert.Null(tracker.Observe(Snapshot(), []).Game);
    }

    [Fact]
    public void FiltersInvalidWindowsAndPrefersForegroundOverLargest()
    {
        var invalid = new[] { Window with { IsVisible = false }, Window with { IsMinimized = true },
            Window with { HasOwner = true }, Window with { IsToolWindow = true }, Window with { Handle = 0 },
            Window with { Bounds = new DisplayRect(0, 0, 0, 100) }, Window with { ProcessId = 456 } };
        foreach (var window in invalid)
            Assert.Null(new GameDetectionTracker().Observe(Snapshot() with { Windows = [window] }, [Profile]).Game);
        var largest = Window with { Handle = 222, Bounds = new DisplayRect(0, 0, 3000, 2000) };
        var result = new GameDetectionTracker().Observe(Snapshot() with { Windows = [largest, Window] }, [Profile]);
        Assert.Equal(Window, result.Game!.Window);
        var background = new GameDetectionTracker().Observe(Snapshot(999) with { Windows = [Window, largest] }, [Profile]);
        Assert.Equal(largest, background.Game!.Window);
        Assert.False(background.ShouldLaunch);
    }

    [Fact]
    public void NewProcessInstanceCanLaunchButHwndReplacementCannotRepeatHandledInstance()
    {
        var tracker = new GameDetectionTracker();
        tracker.Observe(Snapshot(), [Profile]);
        var game = tracker.Observe(Snapshot(), [Profile]).Game!;
        tracker.MarkAttempted(game);
        var replacement = Snapshot() with { Windows = [Window with { Handle = 999 }], ForegroundWindow = 999 };
        tracker.Observe(replacement, [Profile]);
        Assert.False(tracker.Observe(replacement, [Profile]).ShouldLaunch);
        Assert.Null(tracker.Observe(new([], [], 0), [Profile]).Game);
        var restarted = Snapshot() with { Processes = [Process with { StartTimeUtcTicks = 987 }] };
        Assert.False(tracker.Observe(restarted, [Profile]).ShouldLaunch);
        Assert.True(tracker.Observe(restarted, [Profile]).ShouldLaunch);
    }

    [Fact]
    public void ForegroundGameWinsAmongSeveralProcesses()
    {
        var other = new GameProcessInfo(124, "Other.exe", 555);
        var otherWindow = Window with { ProcessId = 124, Handle = 999 };
        var snapshot = new GameDetectionSnapshot([Process, other], [Window, otherWindow], 999);
        var result = new GameDetectionTracker().Observe(snapshot, [Profile, Profile with { Id = "other", ProcessName = "Other.exe" }]);
        Assert.Equal(124, result.Game!.Process.ProcessId);
    }

    [Theory]
    [InlineData(800, 0, false)]
    [InlineData(-800, 0, false)]
    [InlineData(799, 0, true)]
    [InlineData(0, 600, false)]
    [InlineData(0, 599, true)]
    public void ScreenIntersectionRejectsOverlapButNotAdjacentMonitors(int x, int y, bool expected) =>
        Assert.Equal(expected, GameDetectionTracker.Intersects(Window.Bounds, new DisplayRect(x, y, 800, 600)));
}
