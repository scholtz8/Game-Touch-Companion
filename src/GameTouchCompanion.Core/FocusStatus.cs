namespace GameTouchCompanion.Core;

public sealed record FocusStatus(nint ExpectedWindow, nint ForegroundWindow, int FocusLossCount)
{
    public bool IsExpectedForeground => ExpectedWindow != nint.Zero && ExpectedWindow == ForegroundWindow;
}
