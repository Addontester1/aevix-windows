namespace Aevix.Core.Models;

/// <summary>
/// A single video-on-demand title (a movie). The Stalker portal calls these
/// <c>vod</c>; Xtream calls them <c>movie</c>; we normalise to one shape.
/// </summary>
public sealed record VodItem(
    string Id,
    string Title,
    string StreamUri,
    string PlaylistId,
    string Genre = "",
    int? Year = null,
    string? Description = null,
    string? PosterUrl = null,
    int? Duration = null,
    float? Rating = null,
    int? TmdbId = null,
    string? ContainerExtension = null,
    bool IsAdult = false);
