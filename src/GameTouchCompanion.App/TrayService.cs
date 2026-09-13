using GameTouchCompanion.Native;
using Forms = System.Windows.Forms;

namespace GameTouchCompanion.App;

public interface ITrayService : IDisposable
{
    bool IsAvailable { get; }
    void Initialize(Action openConfiguration, Action toggleDetection, Action rearm, Action exit);
    void UpdateDetection(bool enabled);
}

public sealed class TrayService : ITrayService
{
    private Forms.NotifyIcon? icon;
    private Forms.ContextMenuStrip? menu;
    private System.Drawing.Icon? graphic;
    private Forms.ToolStripMenuItem? detection;
    private bool detectionEnabled;
    internal IReadOnlyList<string> MenuLabels => menu?.Items.Cast<Forms.ToolStripItem>().Select(item => item.Text ?? string.Empty).ToArray() ?? [];
    private void LanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (menu is null) return;
        menu.Items[0].Text = Localization.Get("TrayOpen");
        menu.Items[2].Text = Localization.Get("TrayRearm");
        menu.Items[4].Text = Localization.Get("Ui048");
        UpdateDetection(detectionEnabled);
    }
    public bool IsAvailable => icon?.Visible == true && WindowsShellAvailability.HasNotificationArea;

    public void Initialize(Action openConfiguration, Action toggleDetection, Action rearm, Action exit)
    {
        if (icon is not null) return;
        try
        {
            using var stream = System.Windows.Application.GetResourceStream(
                new Uri("pack://application:,,,/GameTouchCompanion.App;component/Assets/GameTouchCompanion.ico"))!.Stream;
            using var loaded = new System.Drawing.Icon(stream);
            graphic = (System.Drawing.Icon)loaded.Clone();
            menu = new Forms.ContextMenuStrip();
            menu.Items.Add(Localization.Get("TrayOpen"), null, (_, _) => openConfiguration());
            detection = new Forms.ToolStripMenuItem(Localization.Get("TrayEnable"), null, (_, _) => toggleDetection());
            menu.Items.Add(detection);
            menu.Items.Add(Localization.Get("TrayRearm"), null, (_, _) => rearm());
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add(Localization.Get("Ui048"), null, (_, _) => exit());
            icon = new Forms.NotifyIcon { Icon = graphic, Text = "Game Touch Companion", ContextMenuStrip = menu, Visible = true };
            icon.DoubleClick += (_, _) => openConfiguration();
            Localization.State.PropertyChanged += LanguageChanged;
        }
        catch { Dispose(); throw; }
    }
    public void UpdateDetection(bool enabled)
    {
        detectionEnabled = enabled;
        if (detection is not null) detection.Text = Localization.Get(enabled ? "TrayPause" : "TrayEnable");
        if (icon is not null) icon.Text = Localization.Get(enabled ? "TrayActiveTip" : "TrayPausedTip");
    }
    public void Dispose()
    {
        Localization.State.PropertyChanged -= LanguageChanged;
        if (icon is not null) { icon.Visible = false; icon.Dispose(); icon = null; }
        menu?.Dispose(); menu = null;
        graphic?.Dispose(); graphic = null;
        detection = null;
    }
}
