using Aevix_App.Models;
using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aevix_App.Pages;

public sealed partial class PlaylistsPage : Page
{
    public PlaylistsViewModel Vm { get; }

    public PlaylistsPage()
    {
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        Vm = App.Services.GetRequiredService<PlaylistsViewModel>();
        InitializeComponent();
        PlaylistList.ItemsSource = Vm.Playlists;

        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Vm.SyncStatus)) SyncStatus.Text = Vm.SyncStatus;
            if (e.PropertyName == nameof(Vm.IsSyncing))
            {
                SyncProgress.Visibility = Vm.IsSyncing ? Visibility.Visible : Visibility.Collapsed;
            }
        };

        Vm.Playlists.CollectionChanged += (_, _) => UpdateEmptyState();
        Loaded += (_, _) => UpdateEmptyState();
    }

    /// <summary>Reload list on every nav — the user may have just added or edited a playlist on the form page.</summary>
    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await Vm.LoadAsync();
        UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        var empty = Vm.Playlists.Count == 0;
        EmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        PlaylistList.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Add_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(PlaylistFormPage));

    /// <summary>
    /// Navigate to the form page in edit mode. Passes the playlist id as the
    /// nav parameter — <see cref="PlaylistFormPage.OnNavigatedTo"/> loads it.
    /// </summary>
    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (PlaylistList.SelectedItem is PlaylistView v)
        {
            Frame.Navigate(typeof(PlaylistFormPage), v.Id);
        }
    }

    private async void Sync_Click(object sender, RoutedEventArgs e)
    {
        if (PlaylistList.SelectedItem is PlaylistView v) await Vm.SyncCommand.ExecuteAsync(v);
    }

    private async void Activate_Click(object sender, RoutedEventArgs e)
    {
        if (PlaylistList.SelectedItem is PlaylistView v) await Vm.SetActiveCommand.ExecuteAsync(v);
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (PlaylistList.SelectedItem is PlaylistView v) await Vm.DeleteCommand.ExecuteAsync(v);
    }
}
