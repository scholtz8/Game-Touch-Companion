using System.ComponentModel;
using System.Diagnostics;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.Native;

public sealed class WindowsGameDetectionSource(IProcessWindowService windows) : IGameDetectionSource
{
    public GameDetectionSnapshot Capture(IReadOnlyCollection<string> executableNames)
    {
        var wanted = new HashSet<string>(executableNames.Where(n => !string.IsNullOrWhiteSpace(n)), StringComparer.OrdinalIgnoreCase);
        var matches = new List<GameProcessInfo>();
        if (wanted.Count == 0) return new(matches, [], windows.GetForegroundWindow());
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    var name = process.ProcessName + ".exe";
                    if (process.Id != Environment.ProcessId && wanted.Contains(name) && !process.HasExited)
                        matches.Add(new GameProcessInfo(process.Id, name, process.StartTime.ToUniversalTime().Ticks));
                }
                catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or NotSupportedException) { }
            }
        }
        var pids = matches.Select(p => p.ProcessId).ToHashSet();
        if (pids.Count == 0) return new(matches, [], windows.GetForegroundWindow());
        return new(matches, windows.GetWindows().Where(w => pids.Contains(w.ProcessId)).ToList(), windows.GetForegroundWindow());
    }

    public bool IsStillForeground(DetectedGame game)
    {
        try
        {
            using var process = Process.GetProcessById(game.Process.ProcessId);
            if (process.HasExited || process.StartTime.ToUniversalTime().Ticks != game.Process.StartTimeUtcTicks ||
                !string.Equals(process.ProcessName + ".exe", game.Process.ProcessName, StringComparison.OrdinalIgnoreCase)) return false;
            return windows.GetForegroundWindow() == game.Window.Handle && windows.GetWindows().Any(w =>
                w.Handle == game.Window.Handle && w.ProcessId == game.Process.ProcessId && w.IsEligible && w.Bounds == game.Window.Bounds);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ArgumentException or NotSupportedException) { return false; }
    }
}
