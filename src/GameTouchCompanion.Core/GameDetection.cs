namespace GameTouchCompanion.Core;

public sealed record GameProcessInfo(int ProcessId, string ProcessName, long StartTimeUtcTicks);
public sealed record ProcessWindowInfo(nint Handle, int ProcessId, DisplayRect Bounds,
    bool IsVisible, bool IsMinimized, bool HasOwner, bool IsToolWindow)
{
    public bool IsEligible => Handle != 0 && IsVisible && !IsMinimized && !HasOwner && !IsToolWindow &&
                              Bounds.Width > 0 && Bounds.Height > 0;
}
public sealed record GameDetectionSnapshot(IReadOnlyList<GameProcessInfo> Processes,
    IReadOnlyList<ProcessWindowInfo> Windows, nint ForegroundWindow);
public sealed record DetectedGame(GameProfile Profile, GameProcessInfo Process, ProcessWindowInfo Window);
public sealed record DetectionDecision(DetectedGame? Game, bool IsForeground, bool ShouldLaunch, string Status);

public interface IProcessWindowService
{
    IReadOnlyList<ProcessWindowInfo> GetWindows();
    nint GetForegroundWindow();
}
public interface IGameDetectionSource
{
    GameDetectionSnapshot Capture(IReadOnlyCollection<string> executableNames);
    bool IsStillForeground(DetectedGame game);
}

/// <summary>Pure state machine; no polling, native calls or side effects.</summary>
public sealed class GameDetectionTracker
{
    private readonly HashSet<(string Profile, int Pid, long Started)> attempted = [];
    private DetectedGame? previous;
    private int stableSamples;
    public void Reset() { attempted.Clear(); previous = null; stableSamples = 0; }
    public void ClearObservation() { previous = null; stableSamples = 0; }
    public void MarkAttempted(DetectedGame game) => attempted.Add(Key(game));
    private static (string, int, long) Key(DetectedGame game) => (game.Profile.Id, game.Process.ProcessId, game.Process.StartTimeUtcTicks);

    public DetectionDecision Observe(GameDetectionSnapshot snapshot, IReadOnlyList<GameProfile> profiles)
    {
        var candidates = (from process in snapshot.Processes
                          where profiles.Any(p => string.Equals(p.ProcessName, process.ProcessName, StringComparison.OrdinalIgnoreCase))
                          from window in snapshot.Windows
                          where window.ProcessId == process.ProcessId && window.IsEligible
                          orderby window.Handle == snapshot.ForegroundWindow descending,
                              (long)window.Bounds.Width * window.Bounds.Height descending, process.ProcessId, window.Handle
                          select (Process: process, Window: window)).ToList();
        if (candidates.Count == 0)
        {
            previous = null; stableSamples = 0;
            return new(null, false, false, "Sin juego configurado con ventana elegible.");
        }
        var candidate = candidates[0];
        var matches = profiles.Where(p => string.Equals(p.ProcessName, candidate.Process.ProcessName, StringComparison.OrdinalIgnoreCase)).ToList();
        var enabled = matches.Where(p => p.AutoLaunch).ToList();
        var game = new DetectedGame(enabled.Count == 1 ? enabled[0] : matches.OrderBy(p => p.Id, StringComparer.Ordinal).First(), candidate.Process, candidate.Window);
        var foreground = candidate.Window.Handle == snapshot.ForegroundWindow;
        var stable = previous is not null && previous.Profile == game.Profile && previous.Process == game.Process &&
                     previous.Window.Handle == game.Window.Handle;
        stableSamples = foreground ? (stable ? stableSamples + 1 : 1) : 0;
        previous = game;
        if (enabled.Count > 1)
        {
            ClearObservation();
            return new(game, foreground, false, "Hay varios perfiles con Autoarranque para este ejecutable. Deja solo uno habilitado.");
        }
        if (enabled.Count == 0)
        {
            ClearObservation();
            return new(game, foreground, false, "Juego detectado; Autoarranque está desmarcado.");
        }
        if (attempted.Contains(Key(game))) return new(game, foreground, false, "Instancia ya atendida. No se reabrirá hasta Rearmar o reiniciar el juego.");
        if (!foreground) return new(game, false, false, "Juego detectado en segundo plano; esperando foreground.");
        if (stableSamples < 2) return new(game, true, false, "Esperando una segunda muestra estable antes de abrir.");
        return new(game, true, true, "Juego estable; preparado para aplicar perfil y abrir Companion.");
    }

    public static bool Intersects(DisplayRect first, DisplayRect second) =>
        (long)first.X < (long)second.X + second.Width && (long)second.X < (long)first.X + first.Width &&
        (long)first.Y < (long)second.Y + second.Height && (long)second.Y < (long)first.Y + first.Height;
}
