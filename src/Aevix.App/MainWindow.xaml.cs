using Aevix.Data.Dao;
using Aevix_App.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aevix_App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("Assets/AppIcon.ico");
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));

        // First-launch routing: if there are no playlists yet, drop the user
        // on the onboarding card instead of an empty Home page.
        _ = RouteInitialAsync();
    }

    private async Task RouteInitialAsync()
    {
        try
        {
            await using var scope = App.Services.CreateAsyncScope();
            var playlists = scope.ServiceProvider.GetRequiredService<PlaylistRepository>();
            var all = await playlists.GetAllAsync();
            NavFrame.Navigate(all.Count == 0 ? typeof(OnboardingPage) : typeof(HomePage));
        }
        catch
        {
            // If routing fails for any reason just fall back to Home.
            NavFrame.Navigate(typeof(HomePage));
        }
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
        => NavView.IsPaneOpen = !NavView.IsPaneOpen;

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        if (NavFrame.CanGoBack) NavFrame.GoBack();
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItem is not NavigationViewItem item) return;
        var target = item.Tag switch
        {
            "home"        => typeof(HomePage),
            "livetv"      => typeof(LiveTvPage),
            "movies"      => typeof(MoviesPage),
            "series"      => typeof(SeriesPage),
            "search"      => typeof(SearchPage),
            "favorites"   => typeof(FavoritesPage),
            "playlists"   => typeof(PlaylistsPage),
            "multiscreen" => typeof(MultiScreenPage),
            _             => typeof(HomePage),
        };
        if (NavFrame.CurrentSourcePageType != target)
        {
            NavFrame.Navigate(target);
        }
    }
}
