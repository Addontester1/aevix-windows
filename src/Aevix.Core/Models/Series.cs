namespace Aevix.Core.Models;

/// <summary>A TV series — owns <see cref="Season"/> rows which own <see cref="Episode"/> rows.</summary>
public sealed record Series(
    string Id,
    string Title,
    string PlaylistId,
    string Genre = "",
    int? Year = null,
    string? Description = null,
    string? PosterUrl = null,
    int? TmdbId = null,
    float? Rating = null,
    bool IsAdult = false);

public sealed record Season(
    string SeriesId,
    int SeasonNumber,
    string? Name = null,
    int EpisodeCount = 0,
    string? CoverUrl = null);

public sealed record Episode(
    string Id,
    string SeriesId,
    int SeasonNumber,
    int EpisodeNumber,
    string Title,
    string StreamUri,
    string? Description = null,
    int? Duration = null,
    string? ContainerExtension = null,
    string? AiredDate = null);
