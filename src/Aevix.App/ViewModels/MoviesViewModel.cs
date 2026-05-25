using System.Collections.ObjectModel;
using Aevix.Core.Models;
using Aevix.Data.Dao;
using Aevix_App.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aevix_App.ViewModels;

public sealed partial class MoviesViewModel : ObservableObject
{
    private readonly PlaylistRepository _playlists;
    private readonly ContentRepository _content;
    private readonly SettingsRepository _settings;
    private readonly ParentalGate _gate;

    public ObservableCollection<CategoryCount> Categories { get; } = new();
    public ObservableCollection<VodItem> Movies { get; } = new();

    [ObservableProperty] private CategoryCount? _selectedCategory;
    [ObservableProperty] private string _statusText = "Loading…";

    public MoviesViewModel(PlaylistRepository playlists, ContentRepository content, SettingsRepository settings, ParentalGate gate)
    {
        _playlists = playlists;
        _content = content;
        _settings = settings;
        _gate = gate;
        _gate.SessionStateChanged += (_, _) => _ = LoadAsync();
    }

    private async Task<bool> HideAdultAsync(CancellationToken ct = default)
        => _gate.ShouldHideAdultContent((await _settings.GetAsync(ct)).AdultContentBlocked);

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var active = await _playlists.GetActiveAsync(ct);
        if (active is null) { StatusText = "No active playlist."; return; }
        var hide = await HideAdultAsync(ct);
        Categories.Clear();
        foreach (var c in await _content.GetVodCategoriesAsync(active.Id, hide, ct))
        {
            Categories.Add(c);
        }
        StatusText = Categories.Count == 0 ? "This playlist has no movies." : $"{Categories.Count} categories";
    }

    partial void OnSelectedCategoryChanged(CategoryCount? value) => _ = LoadMoviesAsync(value);

    private async Task LoadMoviesAsync(CategoryCount? cat)
    {
        Movies.Clear();
        if (cat is null) return;
        var active = await _playlists.GetActiveAsync();
        if (active is null) return;
        var hide = await HideAdultAsync();
        foreach (var v in (await _content.GetVodAsync(active.Id, hide))
                     .Where(v => string.Equals(v.Genre, cat.Group, StringComparison.OrdinalIgnoreCase)))
        {
            Movies.Add(v);
        }
    }
}
