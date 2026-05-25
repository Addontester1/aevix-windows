using System.Collections.ObjectModel;
using Aevix.Core.Models;
using Aevix.Core.Services;
using Aevix.Data.Dao;
using Aevix_App.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aevix_App.ViewModels;

/// <summary>Playlist list + sync + delete + activate. Wraps each row in a <see cref="PlaylistView"/> for UI binding.</summary>
public sealed partial class PlaylistsViewModel : ObservableObject
{
    private readonly PlaylistRepository _playlists;
    private readonly SyncService _sync;
    private readonly IContentSink _sink;

    public ObservableCollection<PlaylistView> Playlists { get; } = new();

    [ObservableProperty] private bool _isSyncing;
    [ObservableProperty] private string _syncStatus = string.Empty;

    public PlaylistsViewModel(PlaylistRepository playlists, SyncService sync, IContentSink sink)
    {
        _playlists = playlists;
        _sync = sync;
        _sink = sink;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        Playlists.Clear();
        foreach (var p in await _playlists.GetAllAsync(ct))
        {
            Playlists.Add(new PlaylistView(p));
        }
    }

    [RelayCommand]
    public async Task SetActive(PlaylistView view)
    {
        await _playlists.SetActiveAsync(view.Id);
        await LoadAsync();
    }

    [RelayCommand]
    public async Task Delete(PlaylistView view)
    {
        await _playlists.DeleteAsync(view.Id);
        await LoadAsync();
    }

    [RelayCommand]
    public async Task Sync(PlaylistView view)
    {
        IsSyncing = true;
        SyncStatus = "Starting sync…";
        try
        {
            var progress = new Progress<SyncProgress>(sp =>
                SyncStatus = $"{sp.Stage}: {sp.Channels} channels · {sp.Vod} movies · {sp.Series} series");
            var updated = await _sync.SyncAsync(view.Source, _sink, progress, CancellationToken.None);
            await _playlists.UpsertAsync(updated);
            await LoadAsync();
            SyncStatus = updated.LastError ?? $"Done — {updated.ChannelCount} ch · {updated.VodCount} vod · {updated.SeriesCount} series";
        }
        finally
        {
            IsSyncing = false;
        }
    }
}
