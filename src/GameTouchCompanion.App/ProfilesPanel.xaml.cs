using System.Windows;
using System.Windows.Controls;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.App;

public partial class ProfilesPanel : UserControl
{
    public ProfilesPanel() => InitializeComponent();
    public Func<GameProfile, Task>? ApplyProfile { get; set; }
    public void SetMonitors(IEnumerable<MonitorProfile> monitors) => AvailableMonitorCombo.ItemsSource = monitors;
    private void UseMonitor_Click(object sender, RoutedEventArgs e)
    {
        if (Model is { CanEdit: true } && AvailableMonitorCombo.SelectedItem is MonitorProfile monitor)
            Model.CompanionMonitor = monitor.DeviceName;
    }
    private void UseCurrentMonitor_Click(object sender, RoutedEventArgs e) { if (Model is { CanEdit: true }) Model.CompanionMonitor = string.Empty; }
    private void Discard_Click(object sender, RoutedEventArgs e) => Model?.ConfirmDiscard();
    private void KeepDraft_Click(object sender, RoutedEventArgs e) => Model?.CancelDiscard();
    private ProfilesViewModel? Model => DataContext as ProfilesViewModel;
    private void New_Click(object sender, RoutedEventArgs e) => Model?.NewProfile();
    private async void Save_Click(object sender, RoutedEventArgs e) { if (Model is not null) await Model.SaveAsync(); }
    private async void Apply_Click(object sender, RoutedEventArgs e) { if (Model is not null && ApplyProfile is not null) await Model.ApplyAsync(ApplyProfile); }
    private void Delete_Click(object sender, RoutedEventArgs e) => Model?.RequestDelete();
    private void CancelDelete_Click(object sender, RoutedEventArgs e) => Model?.CancelDelete();
    private async void ConfirmDelete_Click(object sender, RoutedEventArgs e) { if (Model is not null) await Model.DeleteAsync(); }
}
