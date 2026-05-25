using System.Collections.ObjectModel;
using Aevix.Core.Models;
using Aevix.Data.Dao;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aevix_App.ViewModels;

public sealed partial class SeriesViewModel : ObservableObject
{
    private readonly PlaylistRepository _playlists;
    private readonly ContentRepository _content;
    private readonly SettingsRepository _settings;

    public ObservableCollection<Series> AllSeries { get; } = new();

    [ObservableProperty] private string _statusText = "Loading…";

    public SeriesViewModel(PlaylistRepository playlists, ContentRepository content, SettingsRepository settings)
    {
        _playlists = playlists;
        _content = content;
        _settings = settings;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var active = await _playlists.GetActiveAsync(ct);
        if (active is null) { StatusText = "No active playlist."; return; }
        var settings = await _settings.GetAsync(ct);
        AllSeries.Clear();
        foreach (var s in await _content.GetSeriesAsync(active.Id, settings.AdultContentBlocked, ct))
        {
            AllSeries.Add(s);
        }
        StatusText = AllSeries.Count == 0 ? "This playlist has no series." : $"{AllSeries.Count} series";
    }
}
