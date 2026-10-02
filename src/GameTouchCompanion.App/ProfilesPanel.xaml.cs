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
    private IReadOnlyList<MonitorChoice> monitorChoices = [];
    private bool updatingMonitorSelection;

    private sealed record MonitorChoice(string Label, MonitorProfile? Monitor)
    {
        public override string ToString() => Label;
    }

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
        RebuildMonitorChoices();
        UpdateMonitorSelection();
    }

    private void RebuildMonitorChoices()
    {
        if (AvailableMonitorCombo is null) return;

        var choices = new List<MonitorChoice>
        {
            new(Localization.Get("ProfileMonitorDefaultOption"), null)
        };

        choices.AddRange(
            monitors.Select(monitor =>
                new MonitorChoice(Localization.MonitorLabel(monitor), monitor)));

        monitorChoices = choices;

        updatingMonitorSelection = true;
        try
        {
            AvailableMonitorCombo.ItemsSource = monitorChoices;
        }
        finally
        {
            updatingMonitorSelection = false;
        }
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
            if (!Dispatcher.HasShutdownStarted)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    RebuildMonitorChoices();
                    UpdateMonitorSelection();
                });
            }

            return;
        }

        RebuildMonitorChoices();
        UpdateMonitorSelection();
    }

    private void UpdateMonitorSelection()
    {
        if (ProfileMonitorStatus is null || AvailableMonitorCombo is null) return;

        updatingMonitorSelection = true;

        try
        {
            var reference = Model?.CompanionMonitor;

            if (string.IsNullOrWhiteSpace(reference))
            {
                AvailableMonitorCombo.SelectedItem =
                    monitorChoices.FirstOrDefault(choice => choice.Monitor is null);

                ProfileMonitorStatus.Text = Localization.Get("ProfileMonitorCurrent");
                return;
            }

            var monitor = MonitorSelectionService.FindByReference(monitors, reference);

            AvailableMonitorCombo.SelectedItem = monitor is null
                ? null
                : monitorChoices.FirstOrDefault(choice =>
                    choice.Monitor is not null &&
                    string.Equals(
                        choice.Monitor.IdentityKey,
                        monitor.IdentityKey,
                        StringComparison.OrdinalIgnoreCase));

            ProfileMonitorStatus.Text = monitor is null
                ? Localization.Get("ProfileMonitorUnavailable")
                : string.Format(
                    Localization.Get("ProfileMonitorConfigured"),
                    Localization.MonitorLabel(monitor));
        }
        finally
        {
            updatingMonitorSelection = false;
        }
    }

    private void New_Click(object sender, RoutedEventArgs e) => Model?.NewProfile();
    private async void Save_Click(object sender, RoutedEventArgs e) { if (Model is not null) await Model.SaveAsync(); }
    private async void Apply_Click(object sender, RoutedEventArgs e) { if (Model is not null && ApplyProfile is not null) await Model.ApplyAsync(ApplyProfile); }
    private void Delete_Click(object sender, RoutedEventArgs e) => Model?.RequestDelete();
    private async void ConfirmDelete_Click(object sender, RoutedEventArgs e) { if (Model is not null) await Model.DeleteAsync(); }
    private void CancelDelete_Click(object sender, RoutedEventArgs e) => Model?.CancelDelete();
    private void Discard_Click(object sender, RoutedEventArgs e) => Model?.ConfirmDiscard();
    private void KeepDraft_Click(object sender, RoutedEventArgs e) => Model?.CancelDiscard();
    private void AvailableMonitorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (updatingMonitorSelection ||
            Model is null ||
            AvailableMonitorCombo.SelectedItem is not MonitorChoice choice)
            return;

        Model.CompanionMonitor = choice.Monitor?.IdentityKey ?? string.Empty;
        UpdateMonitorSelection();
    }
    private void AddTab_Click(object sender, RoutedEventArgs e) => Model?.AddTab();
    private void RemoveTab_Click(object sender, RoutedEventArgs e) => Model?.RemoveTab((sender as FrameworkElement)?.Tag as ProfileTabDraft);
    private void PrimaryTab_Click(object sender, RoutedEventArgs e) => Model?.SetPrimary((sender as FrameworkElement)?.Tag as ProfileTabDraft);
    private void MoveTabUp_Click(object sender, RoutedEventArgs e) => Model?.MoveTab((sender as FrameworkElement)?.Tag as ProfileTabDraft, -1);
    private void MoveTabDown_Click(object sender, RoutedEventArgs e) => Model?.MoveTab((sender as FrameworkElement)?.Tag as ProfileTabDraft, 1);
}
