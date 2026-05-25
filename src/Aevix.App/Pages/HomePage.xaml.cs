using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aevix_App.Pages;

public sealed partial class HomePage : Page
{
    public HomeViewModel Vm { get; }

    public HomePage()
    {
        Vm = App.Services.GetRequiredService<HomeViewModel>();
        InitializeComponent();
        ContinueList.ItemsSource = Vm.ContinueWatching;
        Loaded += async (_, _) =>
        {
            await Vm.LoadAsync();
            StatusText.Text = Vm.HasActivePlaylist
                ? $"Active playlist: {Vm.ActivePlaylist!.Name}"
                : "No playlist configured yet — open Playlists from the sidebar.";
        };
    }

    private void GoPlaylists_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(PlaylistsPage));
    private void GoLiveTv_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(LiveTvPage));
    private void GoMovies_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(MoviesPage));
}
