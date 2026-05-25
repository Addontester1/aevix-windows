using System.Collections.ObjectModel;
using Aevix.Core.Models;
using Aevix.Data.Dao;
using Aevix_App.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aevix_App.ViewModels;

public sealed partial class SeriesViewModel : ObservableObject
{
    private readonly PlaylistRepository _playlists;
    private readonly ContentRepository _content;
    private readonly SettingsRepository _settings;
    private readonly ParentalGate _gate;

    public ObservableCollection<Series> AllSeries { get; } = new();

    [ObservableProperty] private string _statusText = "Loading…";

    public SeriesViewModel(PlaylistRepository playlists, ContentRepository content, SettingsRepository settings, ParentalGate gate)
    {
        _playlists = playlists;
        _content = content;
        _settings = settings;
        _gate = gate;
        _gate.SessionStateChanged += (_, _) => _ = LoadAsync();
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var active = await _playlists.GetActiveAsync(ct);
        if (active is null) { StatusText = "No active playlist."; return; }
        var s = await _settings.GetAsync(ct);
        var hide = _gate.ShouldHideAdultContent(s.AdultContentBlocked);
        AllSeries.Clear();
        foreach (var ser in await _content.GetSeriesAsync(active.Id, hide, ct))
        {
            AllSeries.Add(ser);
        }
        StatusText = AllSeries.Count == 0 ? "This playlist has no series." : $"{AllSeries.Count} series";
    }
}
