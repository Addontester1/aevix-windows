using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aevix.Data.Entities;

[Table("playlists")]
public sealed class PlaylistEntity
{
    [Key] public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>String for portability — maps to <c>Core.Models.PlaylistType</c>.</summary>
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? MacAddress { get; set; }
    public string? Icon { get; set; }
    public long LastSyncTimestamp { get; set; }
    public long? ExpiryTimestamp { get; set; }
    public int ChannelCount { get; set; }
    public int VodCount { get; set; }
    public int SeriesCount { get; set; }
    public bool IsActive { get; set; }
    public string? LastError { get; set; }
}

[Table("channels")]
public sealed class ChannelEntity
{
    [Key] public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string StreamUri { get; set; } = string.Empty;
    public string PlaylistId { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? TvgId { get; set; }
    public string? TvgName { get; set; }
    public int SortOrder { get; set; }
    public bool IsAdult { get; set; }
}

[Table("vod_items")]
public sealed class VodItemEntity
{
    [Key] public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string StreamUri { get; set; } = string.Empty;
    public string PlaylistId { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public int? Duration { get; set; }
    public float? Rating { get; set; }
    public int? TmdbId { get; set; }
    public string? ContainerExtension { get; set; }
    public bool IsAdult { get; set; }
}

[Table("series")]
public sealed class SeriesEntity
{
    [Key] public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string PlaylistId { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public int? TmdbId { get; set; }
    public float? Rating { get; set; }
    public bool IsAdult { get; set; }
}

[Table("seasons")]
public sealed class SeasonEntity
{
    [Key] public int Id { get; set; }
    public string SeriesId { get; set; } = string.Empty;
    public int SeasonNumber { get; set; }
    public string? Name { get; set; }
    public int EpisodeCount { get; set; }
    public string? CoverUrl { get; set; }
}

[Table("episodes")]
public sealed class EpisodeEntity
{
    [Key] public string Id { get; set; } = string.Empty;
    public string SeriesId { get; set; } = string.Empty;
    public int SeasonNumber { get; set; }
    public int EpisodeNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string StreamUri { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Duration { get; set; }
    public string? ContainerExtension { get; set; }
    public string? AiredDate { get; set; }
}

[Table("epg_programs")]
public sealed class EpgProgramEntity
{
    [Key] public int Id { get; set; }
    public string ChannelId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public long StartTimestamp { get; set; }
    public long EndTimestamp { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
}

[Table("epg_sources")]
public sealed class EpgSourceEntity
{
    [Key] public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool IsAutoAdded { get; set; }
}

[Table("favorites")]
public sealed class FavoriteEntity
{
    [Key] public int Id { get; set; }
    /// <summary>Maps to <c>Core.Models.ContentType</c> as a string.</summary>
    public string ItemType { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;
    public long AddedTimestamp { get; set; }
}

[Table("playback_progress")]
public sealed class PlaybackProgressEntity
{
    [Key] public string ItemId { get; set; } = string.Empty;
    public long PositionSec { get; set; }
    public long DurationSec { get; set; }
    public float Percentage { get; set; }
    public long LastWatchedTimestamp { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string ItemTitle { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public string? StreamUri { get; set; }
}

/// <summary>
/// Single-row blob of user-configurable settings. Stored as JSON in the
/// <c>Json</c> column so we can add new settings without migrations.
/// </summary>
[Table("user_settings")]
public sealed class UserSettingsEntity
{
    [Key] public int Id { get; set; }
    public string Json { get; set; } = "{}";
}
