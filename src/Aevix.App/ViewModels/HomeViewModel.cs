using System.Collections.ObjectModel;
using Aevix.Core.Models;
using Aevix.Data.Dao;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aevix_App.ViewModels;

/// <summary>Drives the Home page — active playlist banner + continue-watching strip.</summary>
public sealed partial class HomeViewModel : ObservableObject
{
    private readonly PlaylistRepository _playlists;
    private readonly ProgressRepository _progress;

    [ObservableProperty] private Playlist? _activePlaylist;
    [ObservableProperty] private bool _hasActivePlaylist;

    public ObservableCollection<PlaybackProgress> ContinueWatching { get; } = new();

    public HomeViewModel(PlaylistRepository playlists, ProgressRepository progress)
    {
        _playlists = playlists;
        _progress = progress;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        ActivePlaylist = await _playlists.GetActiveAsync(ct);
        HasActivePlaylist = ActivePlaylist is not null;

        ContinueWatching.Clear();
        foreach (var item in await _progress.GetContinueWatchingAsync(20, ct))
        {
            ContinueWatching.Add(item);
        }
    }
}
