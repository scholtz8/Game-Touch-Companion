using GameTouchCompanion.Native;
using Forms = System.Windows.Forms;

namespace GameTouchCompanion.App;

public interface ITrayService : IDisposable
{
    bool IsAvailable { get; }
    void Initialize(Action openConfiguration, Action rearm, Action exit);
}

public sealed class TrayService : ITrayService
{
    private Forms.NotifyIcon? icon;
    private Forms.ContextMenuStrip? menu;
    private System.Drawing.Icon? graphic;
    internal IReadOnlyList<string> MenuLabels => menu?.Items.Cast<Forms.ToolStripItem>().Select(item => item.Text ?? string.Empty).ToArray() ?? [];

    private void LanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (menu is null) return;
        menu.Items[0].Text = Localization.Get("TrayOpen");
        menu.Items[1].Text = Localization.Get("TrayRearm");
        menu.Items[3].Text = Localization.Get("Ui048");
        if (icon is not null) icon.Text = Localization.Get("TrayActiveTip");
    }

    public bool IsAvailable => icon?.Visible == true && WindowsShellAvailability.HasNotificationArea;

    public void Initialize(Action openConfiguration, Action rearm, Action exit)
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
            menu.Items.Add(Localization.Get("TrayRearm"), null, (_, _) => rearm());
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add(Localization.Get("Ui048"), null, (_, _) => exit());
            icon = new Forms.NotifyIcon
            {
                Icon = graphic,
                Text = Localization.Get("TrayActiveTip"),
                ContextMenuStrip = menu,
                Visible = true
            };
            icon.DoubleClick += (_, _) => openConfiguration();
            Localization.State.PropertyChanged += LanguageChanged;
        }
        catch { Dispose(); throw; }
    }

    public void Dispose()
    {
        Localization.State.PropertyChanged -= LanguageChanged;
        if (icon is not null) { icon.Visible = false; icon.Dispose(); icon = null; }
        menu?.Dispose(); menu = null;
        graphic?.Dispose(); graphic = null;
    }
}
