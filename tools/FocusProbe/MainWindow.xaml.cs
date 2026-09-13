using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using GameTouchCompanion.Native;
using Serilog;

namespace FocusProbe;

public partial class MainWindow : Window
{
    private readonly IForegroundWindowService foregroundService = new Win32ForegroundWindowService();
    private readonly ForegroundWindowWatcher watcher;
    private nint hwnd;
    private int losses;
    private int spaceCounter;
    private bool wasForeground;

    public MainWindow()
    {
        InitializeComponent();
        watcher = new ForegroundWindowWatcher(foregroundService);
        watcher.ForegroundChanged += OnForegroundChanged;
        SourceInitialized += OnSourceInitialized;
        Closed += (_, _) => watcher.Dispose();
        PidText.Text = Environment.ProcessId.ToString();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        hwnd = new WindowInteropHelper(this).Handle;
        HwndText.Text = Format(hwnd);
        HwndSource.FromHwnd(hwnd)?.AddHook(WindowProc);
        watcher.Start(TimeSpan.FromMilliseconds(40));
    }

    private void OnForegroundChanged(object? sender, nint foreground) => Dispatcher.Invoke(() =>
    {
        var isForeground = foreground == hwnd;
        if (wasForeground && !isForeground) losses++;
        wasForeground = isForeground;
        ForegroundText.Text = Format(foreground);
        StateText.Text = isForeground ? "FOREGROUND" : "BACKGROUND";
        LossText.Text = losses.ToString();
        var line = $"{DateTimeOffset.Now:HH:mm:ss.fff} {Format(foreground)} {(isForeground ? "FOCUS PROBE" : "OTHER")}";
        Timeline.Items.Insert(0, line);
        while (Timeline.Items.Count > 100) Timeline.Items.RemoveAt(Timeline.Items.Count - 1);
        Log.Information("Foreground={Foreground}; IsProbe={IsProbe}; Losses={Losses}", Format(foreground), isForeground, losses);
    });

    private nint WindowProc(nint window, int message, nint wParam, nint lParam, ref bool handled)
    {
        var name = message switch
        {
            NativeConstants.WmActivate => "WM_ACTIVATE",
            NativeConstants.WmActivateApp => "WM_ACTIVATEAPP",
            NativeConstants.WmSetFocus => "WM_SETFOCUS",
            NativeConstants.WmKillFocus => "WM_KILLFOCUS",
            NativeConstants.WmMouseActivate => "WM_MOUSEACTIVATE",
            _ => null
        };
        if (name is not null)
        {
            EventText.Text = name;
            Log.Information("{Message}; wParam={WParam}; lParam={LParam}", name, wParam, lParam);
        }
        return nint.Zero;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Space) return;
        CounterText.Text = (++spaceCounter).ToString();
        e.Handled = true;
    }

    private static string Format(nint value) => $"0x{value.ToInt64():X16}";
}
