using System.Collections.ObjectModel;
using Aevix.Core.Models;
using Aevix.Data.Dao;
using Aevix_App.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aevix_App.ViewModels;

/// <summary>Live TV page — left sidebar of categories, right list of channels.</summary>
public sealed partial class LiveTvViewModel : ObservableObject
{
    private readonly PlaylistRepository _playlists;
    private readonly ContentRepository _content;
    private readonly SettingsRepository _settings;
    private readonly ParentalGate _gate;

    public ObservableCollection<CategoryCount> Categories { get; } = new();
    public ObservableCollection<Channel> Channels { get; } = new();

    [ObservableProperty] private CategoryCount? _selectedCategory;
    [ObservableProperty] private string _statusText = "Loading…";

    public LiveTvViewModel(PlaylistRepository playlists, ContentRepository content, SettingsRepository settings, ParentalGate gate)
    {
        _playlists = playlists;
        _content = content;
        _settings = settings;
        _gate = gate;
        // If the user unlocks for the session while we're already loaded,
        // refresh categories so adult ones re-appear.
        _gate.SessionStateChanged += (_, _) => _ = LoadAsync();
    }

    /// <summary>True iff adult content should be filtered out of queries.</summary>
    private async Task<bool> HideAdultAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        return _gate.ShouldHideAdultContent(s.AdultContentBlocked);
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var active = await _playlists.GetActiveAsync(ct);
        if (active is null)
        {
            StatusText = "No active playlist — add one in Playlists.";
            return;
        }
        var hide = await HideAdultAsync(ct);
        Categories.Clear();
        foreach (var c in await _content.GetChannelCategoriesAsync(active.Id, hide, ct))
        {
            Categories.Add(c);
        }
        StatusText = Categories.Count == 0 ? "This playlist has no channels yet." : $"{Categories.Count} categories";
    }

    partial void OnSelectedCategoryChanged(CategoryCount? value) => _ = LoadChannelsAsync(value);

    private async Task LoadChannelsAsync(CategoryCount? cat)
    {
        Channels.Clear();
        if (cat is null) return;
        var active = await _playlists.GetActiveAsync();
        if (active is null) return;
        var hide = await HideAdultAsync();
        foreach (var ch in (await _content.GetChannelsAsync(active.Id, hide))
                     .Where(c => string.Equals(c.Group, cat.Group, StringComparison.OrdinalIgnoreCase)))
        {
            Channels.Add(ch);
        }
    }
}
