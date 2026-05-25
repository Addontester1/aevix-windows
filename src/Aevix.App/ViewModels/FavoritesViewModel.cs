using System.Collections.ObjectModel;
using Aevix.Data.Dao;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aevix_App.ViewModels;

public sealed partial class FavoritesViewModel : ObservableObject
{
    private readonly FavoriteRepository _favorites;

    public ObservableCollection<string> RawFavorites { get; } = new();
    [ObservableProperty] private string _statusText = "Loading…";

    public FavoritesViewModel(FavoriteRepository favorites) => _favorites = favorites;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        RawFavorites.Clear();
        var rows = await _favorites.GetAllAsync(ct);
        foreach (var (type, id, addedAt) in rows)
        {
            RawFavorites.Add($"{type}: {id}");
        }
        StatusText = rows.Count == 0 ? "No favorites yet." : $"{rows.Count} favorites";
    }
}
