using System.IO;
using System.Windows;
using Serilog;

namespace FocusProbe;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GameTouchCompanion", "Logs");
        Directory.CreateDirectory(folder);
        Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
            .WriteTo.File(Path.Combine(folder, "focus-probe-.log"), rollingInterval: RollingInterval.Day).CreateLogger();
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e) { Log.CloseAndFlush(); base.OnExit(e); }
}
