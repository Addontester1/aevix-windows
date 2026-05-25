using System.Text.Json.Serialization;

namespace Aevix.Core.Network.Xtream;

/// <summary>Top-level <c>player_api.php</c> auth response. Nullable everywhere — some panels omit fields.</summary>
public sealed class XtreamAuthResponse
{
    [JsonPropertyName("user_info")] public XtreamUserInfo? UserInfo { get; set; }
    [JsonPropertyName("server_info")] public XtreamServerInfo? ServerInfo { get; set; }
}

public sealed class XtreamUserInfo
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Status { get; set; }
    [JsonPropertyName("exp_date")] public string? ExpDate { get; set; }
    [JsonPropertyName("max_connections")] public string? MaxConnections { get; set; }
    [JsonPropertyName("active_cons")] public string? ActiveCons { get; set; }
    [JsonPropertyName("created_at")] public string? CreatedAt { get; set; }
}

public sealed class XtreamServerInfo
{
    public string? Url { get; set; }
    public string? Port { get; set; }
    [JsonPropertyName("https_port")] public string? HttpsPort { get; set; }
    [JsonPropertyName("server_protocol")] public string? ServerProtocol { get; set; }
    public string? Timezone { get; set; }
}

public sealed class XtreamCategory
{
    [JsonPropertyName("category_id")] public string CategoryId { get; set; } = string.Empty;
    [JsonPropertyName("category_name")] public string CategoryName { get; set; } = string.Empty;
    [JsonPropertyName("parent_id")] public int ParentId { get; set; }
}

/// <summary>
/// One stream row — used for live, VOD, and series listings alike. The fields
/// not relevant to a given content type come back null and we just ignore them.
/// </summary>
public sealed class XtreamStream
{
    public int? Num { get; set; }
    public string? Name { get; set; }
    [JsonPropertyName("stream_type")] public string? StreamType { get; set; }
    [JsonPropertyName("stream_id")] public int? StreamId { get; set; }
    [JsonPropertyName("stream_icon")] public string? StreamIcon { get; set; }
    [JsonPropertyName("epg_channel_id")] public string? EpgChannelId { get; set; }
    public string? Added { get; set; }
    [JsonPropertyName("category_id")] public string? CategoryId { get; set; }
    [JsonPropertyName("container_extension")] public string? ContainerExtension { get; set; }
    public string? Rating { get; set; }
    [JsonPropertyName("rating_5based")] public double? Rating5Based { get; set; }
    public string? Year { get; set; }
    public string? Genre { get; set; }
    public string? Plot { get; set; }
    public string? Cast { get; set; }
    public string? Director { get; set; }
    [JsonPropertyName("series_id")] public int? SeriesId { get; set; }
    public string? Cover { get; set; }
}

public sealed class XtreamSeriesInfo
{
    public List<XtreamSeriesSeason>? Seasons { get; set; }
    public XtreamSeriesDetail? Info { get; set; }
    /// <summary>Map of season-number-as-string to its episode list.</summary>
    public Dictionary<string, List<XtreamEpisode>>? Episodes { get; set; }
}

public sealed class XtreamSeriesSeason
{
    [JsonPropertyName("season_number")] public int SeasonNumber { get; set; }
    public string? Name { get; set; }
    public string? Cover { get; set; }
    [JsonPropertyName("episode_count")] public string? EpisodeCount { get; set; }
}

public sealed class XtreamSeriesDetail
{
    public string? Name { get; set; }
    public string? Cover { get; set; }
    public string? Plot { get; set; }
    public string? Cast { get; set; }
    public string? Director { get; set; }
    public string? Genre { get; set; }
    [JsonPropertyName("release_date")] public string? ReleaseDate { get; set; }
    public string? Rating { get; set; }
    [JsonPropertyName("rating_5based")] public double? Rating5Based { get; set; }
    [JsonPropertyName("tmdb_id")] public string? TmdbId { get; set; }
}

public sealed class XtreamEpisode
{
    public string? Id { get; set; }
    [JsonPropertyName("episode_num")] public int? EpisodeNum { get; set; }
    public string? Title { get; set; }
    [JsonPropertyName("container_extension")] public string? ContainerExtension { get; set; }
    public XtreamEpisodeInfo? Info { get; set; }
}

public sealed class XtreamEpisodeInfo
{
    [JsonPropertyName("movie_image")] public string? MovieImage { get; set; }
    public string? Plot { get; set; }
    [JsonPropertyName("duration_secs")] public int? DurationSecs { get; set; }
    public string? Duration { get; set; }
    public double? Rating { get; set; }
    [JsonPropertyName("release_date")] public string? ReleaseDate { get; set; }
}
