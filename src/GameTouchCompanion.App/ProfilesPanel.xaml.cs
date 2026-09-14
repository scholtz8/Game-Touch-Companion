using System.Windows;
using System.Windows.Controls;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.App;

public partial class ProfilesPanel : UserControl
{
    public Func<GameProfile, Task>? ApplyProfile { get; set; }
    private ProfilesViewModel? Model => DataContext as ProfilesViewModel;
    public ProfilesPanel() => InitializeComponent();

    public void SetMonitors(IEnumerable<MonitorProfile> monitors) => AvailableMonitorCombo.ItemsSource = monitors;
    private void New_Click(object sender, RoutedEventArgs e) => Model?.NewProfile();
    private async void Save_Click(object sender, RoutedEventArgs e) { if (Model is not null) await Model.SaveAsync(); }
    private async void Apply_Click(object sender, RoutedEventArgs e) { if (Model is not null && ApplyProfile is not null) await Model.ApplyAsync(ApplyProfile); }
    private void Delete_Click(object sender, RoutedEventArgs e) => Model?.RequestDelete();
    private async void ConfirmDelete_Click(object sender, RoutedEventArgs e) { if (Model is not null) await Model.DeleteAsync(); }
    private void CancelDelete_Click(object sender, RoutedEventArgs e) => Model?.CancelDelete();
    private void Discard_Click(object sender, RoutedEventArgs e) => Model?.ConfirmDiscard();
    private void KeepDraft_Click(object sender, RoutedEventArgs e) => Model?.CancelDiscard();
    private void UseMonitor_Click(object sender, RoutedEventArgs e)
    {
        if (Model is not null && AvailableMonitorCombo.SelectedItem is MonitorProfile monitor) Model.CompanionMonitor = monitor.DeviceName;
    }
    private void UseCurrentMonitor_Click(object sender, RoutedEventArgs e)
    {
        if (Model is not null) Model.CompanionMonitor = string.Empty;
    }
    private void AddTab_Click(object sender, RoutedEventArgs e) => Model?.AddTab();
    private void RemoveTab_Click(object sender, RoutedEventArgs e) => Model?.RemoveTab((sender as FrameworkElement)?.Tag as ProfileTabDraft);
    private void PrimaryTab_Click(object sender, RoutedEventArgs e) => Model?.SetPrimary((sender as FrameworkElement)?.Tag as ProfileTabDraft);
    private void MoveTabUp_Click(object sender, RoutedEventArgs e) => Model?.MoveTab((sender as FrameworkElement)?.Tag as ProfileTabDraft, -1);
    private void MoveTabDown_Click(object sender, RoutedEventArgs e) => Model?.MoveTab((sender as FrameworkElement)?.Tag as ProfileTabDraft, 1);
}
