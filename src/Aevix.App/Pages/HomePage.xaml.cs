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
        OtherPlaylistsList.ItemsSource = Vm.OtherPlaylists;
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        await Vm.LoadAsync();

        // Hero
        if (Vm.HasActivePlaylist && Vm.ActivePlaylist is { } p)
        {
            HeroPrimary.Text = p.Name;
            HeroSecondary.Text = Vm.ActivePlaylistSubtitle;
            HeroCounters.Text = Vm.ActivePlaylistCounters;
            HeroActions.Visibility = Visibility.Visible;
            HeroEmptyActions.Visibility = Visibility.Collapsed;
        }
        else
        {
            HeroPrimary.Text = "Welcome to Aevix";
            HeroSecondary.Text = "Native Windows IPTV — fast, focused, free of fluff.";
            HeroCounters.Text = "Add a playlist to start browsing channels, movies, and series.";
            HeroActions.Visibility = Visibility.Collapsed;
            HeroEmptyActions.Visibility = Visibility.Visible;
        }

        // Empty states for the strips.
        ContinueEmpty.Visibility = Vm.ContinueWatching.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ContinueScroller.Visibility = Vm.ContinueWatching.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        OthersEmpty.Visibility = Vm.OtherPlaylists.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void GoPlaylists_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(PlaylistsPage));
    private void GoLiveTv_Click(object sender, RoutedEventArgs e)    => Frame.Navigate(typeof(LiveTvPage));
    private void GoMovies_Click(object sender, RoutedEventArgs e)    => Frame.Navigate(typeof(MoviesPage));
    private void GoSeries_Click(object sender, RoutedEventArgs e)    => Frame.Navigate(typeof(SeriesPage));
    private void GoSearch_Click(object sender, RoutedEventArgs e)    => Frame.Navigate(typeof(SearchPage));
}
