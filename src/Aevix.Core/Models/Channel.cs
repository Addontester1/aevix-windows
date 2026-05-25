namespace Aevix.Core.Models;

/// <summary>
/// A single live-TV channel scoped to a playlist. <see cref="Group"/> is the
/// category/genre label shown in the sidebar. <see cref="TvgId"/> is the
/// canonical EPG matching key when available.
/// </summary>
public sealed record Channel(
    string Id,
    string Name,
    string Group,
    string StreamUri,
    string PlaylistId,
    string? LogoUrl = null,
    string? TvgId = null,
    string? TvgName = null,
    int SortOrder = 0,
    bool IsAdult = false);
