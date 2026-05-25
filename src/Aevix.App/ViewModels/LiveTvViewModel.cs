using System.Collections.ObjectModel;
using Aevix.Core.Models;
using Aevix.Data.Dao;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aevix_App.ViewModels;

/// <summary>Live TV page — left sidebar of categories, right list of channels.</summary>
public sealed partial class LiveTvViewModel : ObservableObject
{
    private readonly PlaylistRepository _playlists;
    private readonly ContentRepository _content;
    private readonly SettingsRepository _settings;

    public ObservableCollection<CategoryCount> Categories { get; } = new();
    public ObservableCollection<Channel> Channels { get; } = new();

    [ObservableProperty] private CategoryCount? _selectedCategory;
    [ObservableProperty] private string _statusText = "Loading…";

    public LiveTvViewModel(PlaylistRepository playlists, ContentRepository content, SettingsRepository settings)
    {
        _playlists = playlists;
        _content = content;
        _settings = settings;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var active = await _playlists.GetActiveAsync(ct);
        if (active is null)
        {
            StatusText = "No active playlist — add one in Playlists.";
            return;
        }
        var settings = await _settings.GetAsync(ct);
        Categories.Clear();
        foreach (var c in await _content.GetChannelCategoriesAsync(active.Id, settings.AdultContentBlocked, ct))
        {
            Categories.Add(c);
        }
        StatusText = Categories.Count == 0 ? "This playlist has no channels yet." : $"{Categories.Count} categories";
    }

    partial void OnSelectedCategoryChanged(CategoryCount? value)
    {
        _ = LoadChannelsAsync(value);
    }

    private async Task LoadChannelsAsync(CategoryCount? cat)
    {
        Channels.Clear();
        if (cat is null) return;
        var active = await _playlists.GetActiveAsync();
        if (active is null) return;
        var settings = await _settings.GetAsync();
        foreach (var ch in (await _content.GetChannelsAsync(active.Id, settings.AdultContentBlocked))
                     .Where(c => string.Equals(c.Group, cat.Group, StringComparison.OrdinalIgnoreCase)))
        {
            Channels.Add(ch);
        }
    }
}
