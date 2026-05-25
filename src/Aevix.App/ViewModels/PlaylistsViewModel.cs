using System.Collections.ObjectModel;
using Aevix.Core.Models;
using Aevix.Core.Services;
using Aevix.Data.Dao;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aevix_App.ViewModels;

/// <summary>Playlist list + sync + delete + activate.</summary>
public sealed partial class PlaylistsViewModel : ObservableObject
{
    private readonly PlaylistRepository _playlists;
    private readonly SyncService _sync;
    private readonly IContentSink _sink;

    public ObservableCollection<Playlist> Playlists { get; } = new();

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
        foreach (var p in await _playlists.GetAllAsync(ct)) Playlists.Add(p);
    }

    [RelayCommand]
    public async Task SetActive(Playlist p)
    {
        await _playlists.SetActiveAsync(p.Id);
        await LoadAsync();
    }

    [RelayCommand]
    public async Task Delete(Playlist p)
    {
        await _playlists.DeleteAsync(p.Id);
        await LoadAsync();
    }

    [RelayCommand]
    public async Task Sync(Playlist p)
    {
        IsSyncing = true;
        SyncStatus = "Starting sync…";
        try
        {
            var progress = new Progress<SyncProgress>(sp => SyncStatus = $"{sp.Stage}: {sp.Channels} channels, {sp.Vod} VOD, {sp.Series} series");
            var updated = await _sync.SyncAsync(p, _sink, progress, CancellationToken.None);
            await _playlists.UpsertAsync(updated);
            await LoadAsync();
            SyncStatus = updated.LastError ?? $"Done — {updated.ChannelCount}/{updated.VodCount}/{updated.SeriesCount}";
        }
        finally
        {
            IsSyncing = false;
        }
    }
}
