using System.Text.Json.Serialization;

namespace Aevix.Core.Network.Stalker;

/// <summary>Stalker (MAG-portal) wraps every response in a <c>js</c> envelope.</summary>
public sealed class StalkerTokenResponse
{
    public StalkerToken? Js { get; set; }
}

public sealed class StalkerToken
{
    public string? Token { get; set; }
    public string? Random { get; set; }
}

public sealed class StalkerCategoryResponse
{
    /// <summary>Can be an array of categories or an empty object — always treat as opaque.</summary>
    public System.Text.Json.JsonElement Js { get; set; }
}

public sealed class StalkerCategory
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Alias { get; set; }
    public int? Censored { get; set; }
}

public sealed class StalkerContentResponse
{
    public StalkerContentData? Js { get; set; }
}

public sealed class StalkerContentData
{
    [JsonPropertyName("total_items")] public int? TotalItems { get; set; }
    [JsonPropertyName("max_page_items")] public int? MaxPageItems { get; set; }
    public List<StalkerItem>? Data { get; set; }
}

public sealed class StalkerItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Cmd { get; set; }
    public string? Logo { get; set; }
    [JsonPropertyName("screenshot_uri")] public string? ScreenshotUri { get; set; }
    [JsonPropertyName("tv_genre_id")] public string? TvGenreId { get; set; }
    [JsonPropertyName("genre_id")] public string? GenreId { get; set; }
    public int? Number { get; set; }
    public string? Year { get; set; }
    public string? Description { get; set; }
    public string? Director { get; set; }
    public string? Actors { get; set; }
    public int? Hd { get; set; }
}

public sealed class StalkerEpgResponse
{
    /// <summary>Can be object <c>{data:[...]}</c> or empty array <c>[]</c>.</summary>
    public System.Text.Json.JsonElement Js { get; set; }
}

public sealed class StalkerEpgData
{
    public List<StalkerChannelEpg>? Data { get; set; }
}

public sealed class StalkerChannelEpg
{
    [JsonPropertyName("channel_id")] public string? ChannelId { get; set; }
    public List<StalkerEpgProgram>? Epg { get; set; }
}

public sealed class StalkerEpgProgram
{
    public string? Name { get; set; }
    public string? Descr { get; set; }
    public string? Category { get; set; }
    [JsonPropertyName("start_timestamp")] public long? StartTimestamp { get; set; }
    [JsonPropertyName("stop_timestamp")] public long? StopTimestamp { get; set; }
}

public sealed class StalkerCreateLinkResponse
{
    /// <summary>Either <c>{"cmd":"..."}</c> or <c>[]</c>.</summary>
    public System.Text.Json.JsonElement Js { get; set; }
}

public sealed class StalkerLinkData
{
    public string? Cmd { get; set; }
}
