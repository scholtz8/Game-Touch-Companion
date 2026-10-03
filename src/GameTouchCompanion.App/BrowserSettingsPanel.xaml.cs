using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GameTouchCompanion.App;

public partial class BrowserSettingsPanel : UserControl
{
    public BrowserSettingsPanel() => InitializeComponent();
    private BrowserViewModel? ViewModel => DataContext as BrowserViewModel;

    private void Navigate_Click(object sender, RoutedEventArgs e) => ViewModel?.NavigateAddress();
    private void Address_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        ViewModel?.NavigateAddress();
        e.Handled = true;
    }

    private async void SetHome_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { } model) await model.SetHomeFromAddressAsync();
    }

    private async void FavoriteAction_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } model) return;

        if (model.IsEditingFavorite)
        {
            await model.UpdateEditingFavoriteAsync(model.Address, model.FavoriteTitle);
            return;
        }

        var count = model.Favorites.Count;
        await model.AddFavoriteAsync(model.Address, model.FavoriteTitle);
        if (model.Favorites.Count > count && !model.HasSettingsError) model.FavoriteTitle = string.Empty;
    }

    private void EditFavorite_Click(object sender, RoutedEventArgs e) => ViewModel?.BeginEditSelectedFavorite();

    private void CancelFavoriteEdit_Click(object sender, RoutedEventArgs e) => ViewModel?.CancelFavoriteEdit();

    private async void OpenNewWindowsInTabs_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { } model && sender is CheckBox checkBox)
            await model.SetOpenNewWindowsInTabsAsync(checkBox.IsChecked == true);
    }

    private async void ActivateNewWindowTabs_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { } model && sender is CheckBox checkBox)
            await model.SetActivateNewWindowTabsAsync(checkBox.IsChecked == true);
    }

    private void OpenFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { SelectedFavorite: { } favorite } model) model.Navigate(favorite.Url);
    }

    private async void RemoveFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { SelectedFavorite: { } favorite } model) await model.RemoveFavoriteAsync(favorite);
    }
}
