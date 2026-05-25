namespace Aevix.Core.Models;

/// <summary>One category/group with its current item count — used by sidebars.</summary>
public sealed record CategoryCount(string Group, int Count);

/// <summary>
/// Result of a unified search across channels, VOD, and series. Convenience
/// properties save the UI from doing collection arithmetic in XAML bindings.
/// </summary>
public sealed record SearchResults
{
    public IReadOnlyList<Channel> Channels { get; init; } = Array.Empty<Channel>();
    public IReadOnlyList<VodItem> Vod { get; init; } = Array.Empty<VodItem>();
    public IReadOnlyList<Series> Series { get; init; } = Array.Empty<Series>();

    public bool IsEmpty => Channels.Count == 0 && Vod.Count == 0 && Series.Count == 0;
    public int TotalCount => Channels.Count + Vod.Count + Series.Count;
}

/// <summary>Top-level destinations for the navigation shell, in display order.</summary>
public enum AppRoute
{
    Home,
    LiveTv,
    Movies,
    Series,
    Search,
    Favorites,
    Playlists,
    MultiScreen,
    Settings,
}
