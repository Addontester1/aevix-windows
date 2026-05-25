using System.Collections.ObjectModel;
using Aevix.Core.Models;
using Aevix.Data.Dao;
using Aevix_App.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aevix_App.ViewModels;

/// <summary>
/// Unified search across channels, VOD, and series. Debounces the query so
/// we don't hammer SQLite while the user types.
/// </summary>
public sealed partial class SearchViewModel : ObservableObject
{
    private readonly PlaylistRepository _playlists;
    private readonly ContentRepository _content;
    private readonly SettingsRepository _settings;
    private readonly ParentalGate _gate;

    public ObservableCollection<Channel> Channels { get; } = new();
    public ObservableCollection<VodItem> Movies { get; } = new();
    public ObservableCollection<Series> Series { get; } = new();

    [ObservableProperty] private string _query = string.Empty;
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private string _statusText = "Type to search.";

    private CancellationTokenSource? _cts;

    public SearchViewModel(PlaylistRepository playlists, ContentRepository content, SettingsRepository settings, ParentalGate gate)
    {
        _playlists = playlists;
        _content = content;
        _settings = settings;
        _gate = gate;
    }

    partial void OnQueryChanged(string value) => _ = DebouncedSearchAsync(value);

    private async Task DebouncedSearchAsync(string query)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        try
        {
            await Task.Delay(250, ct);
            await RunAsync(query, ct);
        }
        catch (OperationCanceledException) { }
    }

    public async Task RunAsync(string query, CancellationToken ct = default)
    {
        Channels.Clear(); Movies.Clear(); Series.Clear();
        if (string.IsNullOrWhiteSpace(query)) { StatusText = "Type to search."; return; }
        var active = await _playlists.GetActiveAsync(ct);
        if (active is null) { StatusText = "No active playlist."; return; }

        IsSearching = true;
        try
        {
            var settings = await _settings.GetAsync(ct);
            var hide = _gate.ShouldHideAdultContent(settings.AdultContentBlocked);
            var results = await _content.SearchAsync(active.Id, query, hide, ct);
            foreach (var c in results.Channels) Channels.Add(c);
            foreach (var v in results.Vod) Movies.Add(v);
            foreach (var s in results.Series) Series.Add(s);
            StatusText = results.IsEmpty ? "No matches." : $"{results.TotalCount} results";
        }
        finally
        {
            IsSearching = false;
        }
    }
}
