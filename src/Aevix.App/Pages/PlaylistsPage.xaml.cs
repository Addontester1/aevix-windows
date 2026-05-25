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
        Loaded += async (_, _) => { await Vm.LoadAsync(); UpdateEmptyState(); };
    }

    private void UpdateEmptyState()
    {
        var empty = Vm.Playlists.Count == 0;
        EmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        PlaylistList.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Add_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(PlaylistFormPage));

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
