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

    private async void AddFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { } model) await model.AddFavoriteAsync(model.Address);
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
