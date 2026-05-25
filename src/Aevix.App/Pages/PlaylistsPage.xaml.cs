using Aevix.Core.Models;
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
        SyncStatus.Text = Vm.SyncStatus;
        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Vm.SyncStatus)) SyncStatus.Text = Vm.SyncStatus;
        };
        Loaded += async (_, _) => await Vm.LoadAsync();
    }

    private void Add_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(PlaylistFormPage));

    private async void Sync_Click(object sender, RoutedEventArgs e)
    {
        if (PlaylistList.SelectedItem is Playlist p) await Vm.SyncCommand.ExecuteAsync(p);
    }

    private async void Activate_Click(object sender, RoutedEventArgs e)
    {
        if (PlaylistList.SelectedItem is Playlist p) await Vm.SetActiveCommand.ExecuteAsync(p);
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (PlaylistList.SelectedItem is Playlist p) await Vm.DeleteCommand.ExecuteAsync(p);
    }
}
