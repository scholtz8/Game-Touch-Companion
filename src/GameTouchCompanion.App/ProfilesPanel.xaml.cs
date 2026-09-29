using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.App;

public partial class ProfilesPanel : UserControl
{
    public Func<GameProfile, Task>? ApplyProfile { get; set; }
    private ProfilesViewModel? Model => DataContext as ProfilesViewModel;
    private IReadOnlyList<MonitorProfile> monitors = [];
    private ProfilesViewModel? subscribedModel;

    public ProfilesPanel()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => SubscribeModel();
        PropertyChangedEventManager.AddHandler(Localization.State, LanguageChanged, string.Empty);
        Unloaded += (_, _) =>
        {
            if (subscribedModel is not null) subscribedModel.PropertyChanged -= ModelPropertyChanged;
            PropertyChangedEventManager.RemoveHandler(Localization.State, LanguageChanged, string.Empty);
        };
    }

    public void SetMonitors(IEnumerable<MonitorProfile> availableMonitors)
    {
        monitors = availableMonitors.ToArray();
        AvailableMonitorCombo.ItemsSource = monitors;
        UpdateMonitorSelection();
    }

    private void SubscribeModel()
    {
        if (subscribedModel is not null) subscribedModel.PropertyChanged -= ModelPropertyChanged;
        subscribedModel = Model;
        if (subscribedModel is not null) subscribedModel.PropertyChanged += ModelPropertyChanged;
        UpdateMonitorSelection();
    }

    private void ModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ProfilesViewModel.CompanionMonitor) or nameof(ProfilesViewModel.SelectedProfile))
            UpdateMonitorSelection();
    }

    private void LanguageChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            if (!Dispatcher.HasShutdownStarted) Dispatcher.BeginInvoke(UpdateMonitorSelection);
            return;
        }
        UpdateMonitorSelection();
    }

    private void UpdateMonitorSelection()
    {
        if (ProfileMonitorStatus is null || AvailableMonitorCombo is null) return;
        var reference = Model?.CompanionMonitor;
        if (string.IsNullOrWhiteSpace(reference))
        {
            AvailableMonitorCombo.SelectedItem = null;
            ProfileMonitorStatus.Text = Localization.Get("ProfileMonitorCurrent");
            return;
        }

        var monitor = MonitorSelectionService.FindByReference(monitors, reference);
        AvailableMonitorCombo.SelectedItem = monitor;
        ProfileMonitorStatus.Text = monitor is null
            ? Localization.Get("ProfileMonitorUnavailable")
            : string.Format(Localization.Get("ProfileMonitorConfigured"), Localization.MonitorLabel(monitor));
    }

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
        if (Model is not null && AvailableMonitorCombo.SelectedItem is MonitorProfile monitor)
            Model.CompanionMonitor = monitor.IdentityKey;
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
