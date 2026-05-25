using System.Collections.ObjectModel;
using Aevix.Core.Models;
using Aevix.Data.Dao;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aevix_App.ViewModels;

/// <summary>
/// Drives the Home page:
///   - Hero card for the active playlist (name, type, last sync, counters)
///   - Continue-watching strip
///   - "Other playlists" strip so the user can hop between them quickly
/// </summary>
public sealed partial class HomeViewModel : ObservableObject
{
    private readonly PlaylistRepository _playlists;
    private readonly ProgressRepository _progress;

    [ObservableProperty] private Playlist? _activePlaylist;
    [ObservableProperty] private bool _hasActivePlaylist;
    [ObservableProperty] private string _activePlaylistSubtitle = string.Empty;
    [ObservableProperty] private string _activePlaylistCounters = string.Empty;

    public ObservableCollection<PlaybackProgress> ContinueWatching { get; } = new();
    public ObservableCollection<Playlist> OtherPlaylists { get; } = new();

    public HomeViewModel(PlaylistRepository playlists, ProgressRepository progress)
    {
        _playlists = playlists;
        _progress = progress;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        ActivePlaylist = await _playlists.GetActiveAsync(ct);
        HasActivePlaylist = ActivePlaylist is not null;

        if (ActivePlaylist is { } p)
        {
            ActivePlaylistSubtitle = p.LastSyncTimestamp == 0
                ? $"{p.Type} • Never synced"
                : $"{p.Type} • Synced {RelativeTime(p.LastSyncTimestamp)}";
            ActivePlaylistCounters = $"{p.ChannelCount} channels · {p.VodCount} movies · {p.SeriesCount} series";
        }

        ContinueWatching.Clear();
        foreach (var item in await _progress.GetContinueWatchingAsync(20, ct))
        {
            ContinueWatching.Add(item);
        }

        OtherPlaylists.Clear();
        foreach (var pl in (await _playlists.GetAllAsync(ct)).Where(x => !x.IsActive))
        {
            OtherPlaylists.Add(pl);
        }
    }

    /// <summary>Human-friendly "n minutes ago" / "yesterday" formatting.</summary>
    private static string RelativeTime(long unixMs)
    {
        var when = DateTimeOffset.FromUnixTimeMilliseconds(unixMs).LocalDateTime;
        var delta = DateTime.Now - when;
        if (delta < TimeSpan.FromMinutes(1)) return "just now";
        if (delta < TimeSpan.FromHours(1))   return $"{(int)delta.TotalMinutes}m ago";
        if (delta < TimeSpan.FromDays(1))    return $"{(int)delta.TotalHours}h ago";
        if (delta < TimeSpan.FromDays(7))    return $"{(int)delta.TotalDays}d ago";
        return when.ToString("MMM d");
    }
}
