using System.Text.Json;
using Aevix.Core.Models;
using Aevix.Core.Network.Stalker;
using Aevix.Core.Network.Xtream;
using Aevix.Core.Parsers;
using Microsoft.Extensions.Logging;

namespace Aevix.Core.Services;

/// <summary>
/// Reports incremental progress while syncing — the UI binds to this so the
/// playlist manager can show a spinner + counters.
/// </summary>
public sealed record SyncProgress(string Stage, int Channels, int Vod, int Series);

/// <summary>
/// Coordinates one full playlist sync — fetches via the right client
/// (Xtream / Stalker / M3U) and hands batches to the
/// <see cref="IContentSink"/> for persistence.
///
/// The Core layer never knows about EF — the App composition root injects a
/// sink that writes via the data-layer repositories.
/// </summary>
public sealed class SyncService
{
    private readonly XtreamClient _xtream;
    private readonly StalkerClient _stalker;
    private readonly M3uParser _m3u;
    private readonly HttpClient _http;
    private readonly ILogger<SyncService> _log;

    public SyncService(
        XtreamClient xtream,
        StalkerClient stalker,
        M3uParser m3u,
        HttpClient http,
        ILogger<SyncService> log)
    {
        _xtream = xtream;
        _stalker = stalker;
        _m3u = m3u;
        _http = http;
        _log = log;
    }

    /// <summary>
    /// Runs a full sync for the given playlist and reports progress.
    /// Returns the playlist with updated counts so callers can persist it.
    /// </summary>
    public async Task<Playlist> SyncAsync(
        Playlist playlist,
        IContentSink sink,
        IProgress<SyncProgress>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            return playlist.Type switch
            {
                PlaylistType.Xtream => await SyncXtreamAsync(playlist, sink, progress, ct),
                PlaylistType.Stalker => await SyncStalkerAsync(playlist, sink, progress, ct),
                PlaylistType.M3UUrl => await SyncM3uUrlAsync(playlist, sink, progress, ct),
                PlaylistType.M3UFile => await SyncM3uFileAsync(playlist, sink, progress, ct),
                _ => playlist,
            };
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Sync failed for playlist {Name}", playlist.Name);
            return playlist with { LastError = ex.Message };
        }
    }

    // -------- Xtream -----------------------------------------------------

    private async Task<Playlist> SyncXtreamAsync(Playlist p, IContentSink sink, IProgress<SyncProgress>? progress, CancellationToken ct)
    {
        progress?.Report(new SyncProgress("Authenticating", 0, 0, 0));
        var auth = await _xtream.AuthenticateAsync(p.Url, p.Username!, p.Password!, ct);
        if (auth?.UserInfo?.Status != "Active")
        {
            return p with { LastError = $"Xtream auth status: {auth?.UserInfo?.Status ?? "unknown"}" };
        }

        progress?.Report(new SyncProgress("Fetching live", 0, 0, 0));
        var liveCats = await _xtream.GetLiveCategoriesAsync(p.Url, p.Username!, p.Password!, ct) ?? new();
        var liveStreams = await _xtream.GetLiveStreamsAsync(p.Url, p.Username!, p.Password!, null, ct) ?? new();
        var channels = liveStreams.Select(s => MapXtreamChannel(p, s, liveCats)).ToList();

        progress?.Report(new SyncProgress("Fetching movies", channels.Count, 0, 0));
        var vodCats = await _xtream.GetVodCategoriesAsync(p.Url, p.Username!, p.Password!, ct) ?? new();
        var vodStreams = await _xtream.GetVodStreamsAsync(p.Url, p.Username!, p.Password!, null, ct) ?? new();
        var vods = vodStreams.Select(s => MapXtreamVod(p, s, vodCats)).ToList();

        progress?.Report(new SyncProgress("Fetching series", channels.Count, vods.Count, 0));
        var seriesCats = await _xtream.GetSeriesCategoriesAsync(p.Url, p.Username!, p.Password!, ct) ?? new();
        var seriesStreams = await _xtream.GetSeriesAsync(p.Url, p.Username!, p.Password!, null, ct) ?? new();
        var series = seriesStreams.Select(s => MapXtreamSeries(p, s, seriesCats)).ToList();

        await sink.WriteChannelsAsync(p.Id, channels, ct);
        await sink.WriteVodAsync(p.Id, vods, ct);
        await sink.WriteSeriesAsync(p.Id, series, ct);

        progress?.Report(new SyncProgress("Done", channels.Count, vods.Count, series.Count));
        return p with
        {
            LastSyncTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ChannelCount = channels.Count,
            VodCount = vods.Count,
            SeriesCount = series.Count,
            LastError = null,
        };
    }

    private static Channel MapXtreamChannel(Playlist p, XtreamStream s, List<XtreamCategory> cats)
    {
        var group = cats.FirstOrDefault(c => c.CategoryId == s.CategoryId)?.CategoryName ?? "Uncategorized";
        var streamId = s.StreamId?.ToString() ?? Guid.NewGuid().ToString();
        var baseUrl = p.Url.TrimEnd('/').Replace("/player_api.php", "", StringComparison.OrdinalIgnoreCase);
        var uri = $"{baseUrl}/live/{p.Username}/{p.Password}/{streamId}.ts";
        return new Channel(
            Id: $"{p.Id}_live_{streamId}",
            Name: s.Name ?? "Unnamed",
            Group: group,
            StreamUri: uri,
            PlaylistId: p.Id,
            LogoUrl: s.StreamIcon,
            TvgId: s.EpgChannelId,
            SortOrder: s.Num ?? 0,
            IsAdult: M3uParser.AdultRegex.IsMatch(group) || M3uParser.AdultRegex.IsMatch(s.Name ?? string.Empty));
    }

    private static VodItem MapXtreamVod(Playlist p, XtreamStream s, List<XtreamCategory> cats)
    {
        var group = cats.FirstOrDefault(c => c.CategoryId == s.CategoryId)?.CategoryName ?? "Uncategorized";
        var streamId = s.StreamId?.ToString() ?? Guid.NewGuid().ToString();
        var ext = s.ContainerExtension ?? "mp4";
        var baseUrl = p.Url.TrimEnd('/').Replace("/player_api.php", "", StringComparison.OrdinalIgnoreCase);
        var uri = $"{baseUrl}/movie/{p.Username}/{p.Password}/{streamId}.{ext}";
        return new VodItem(
            Id: $"{p.Id}_vod_{streamId}",
            Title: s.Name ?? "Untitled",
            StreamUri: uri,
            PlaylistId: p.Id,
            Genre: group,
            Year: int.TryParse(s.Year, out var y) ? y : null,
            Description: s.Plot,
            PosterUrl: s.StreamIcon ?? s.Cover,
            Rating: float.TryParse(s.Rating, out var r) ? r : null,
            ContainerExtension: ext,
            IsAdult: M3uParser.AdultRegex.IsMatch(group) || M3uParser.AdultRegex.IsMatch(s.Name ?? string.Empty));
    }

    private static Series MapXtreamSeries(Playlist p, XtreamStream s, List<XtreamCategory> cats)
    {
        var group = cats.FirstOrDefault(c => c.CategoryId == s.CategoryId)?.CategoryName ?? "Uncategorized";
        var seriesId = s.SeriesId?.ToString() ?? Guid.NewGuid().ToString();
        return new Series(
            Id: $"{p.Id}_series_{seriesId}",
            Title: s.Name ?? "Untitled",
            PlaylistId: p.Id,
            Genre: group,
            Year: int.TryParse(s.Year, out var y) ? y : null,
            Description: s.Plot,
            PosterUrl: s.Cover ?? s.StreamIcon,
            Rating: float.TryParse(s.Rating, out var r) ? r : null,
            IsAdult: M3uParser.AdultRegex.IsMatch(group) || M3uParser.AdultRegex.IsMatch(s.Name ?? string.Empty));
    }

    // -------- Stalker (categories-only, lazy load content per category) --

    private async Task<Playlist> SyncStalkerAsync(Playlist p, IContentSink sink, IProgress<SyncProgress>? progress, CancellationToken ct)
    {
        progress?.Report(new SyncProgress("Handshaking with portal", 0, 0, 0));
        var cookie = $"mac={Uri.EscapeDataString(p.MacAddress ?? string.Empty)}; stb_lang=en; timezone=UTC";
        var hs = await _stalker.HandshakeAsync(p.Url, cookie, ct);
        var token = hs?.Js?.Token;
        if (string.IsNullOrEmpty(token))
        {
            return p with { LastError = "Stalker handshake returned no token" };
        }
        var auth = $"Bearer {token}";

        progress?.Report(new SyncProgress("Fetching live categories", 0, 0, 0));
        var liveCats = await ParseStalkerCategoriesAsync(p, auth, cookie, "itv", ct);
        progress?.Report(new SyncProgress("Fetching VOD categories", 0, 0, 0));
        var vodCats = await ParseStalkerCategoriesAsync(p, auth, cookie, "vod", ct);
        progress?.Report(new SyncProgress("Fetching series categories", 0, 0, 0));
        var seriesCats = await ParseStalkerCategoriesAsync(p, auth, cookie, "series", ct);

        // For Stalker we materialise category placeholders so the UI's
        // category sidebar has something to render; per-category content is
        // lazy-loaded when the user opens it (matches the Android behaviour).
        var channels = liveCats.Select((c, i) => new Channel(
            Id: $"{p.Id}_cat_itv_{c.Id}",
            Name: $"[{c.Title}]",
            Group: c.Title ?? "Uncategorized",
            StreamUri: $"stalker://category/itv/{c.Id}",
            PlaylistId: p.Id,
            SortOrder: i)).ToList();

        var vods = vodCats.Select((c, i) => new VodItem(
            Id: $"{p.Id}_cat_vod_{c.Id}",
            Title: $"[{c.Title}]",
            StreamUri: $"stalker://category/vod/{c.Id}",
            PlaylistId: p.Id,
            Genre: c.Title ?? "Uncategorized")).ToList();

        var series = seriesCats.Select((c, i) => new Series(
            Id: $"{p.Id}_cat_series_{c.Id}",
            Title: $"[{c.Title}]",
            PlaylistId: p.Id,
            Genre: c.Title ?? "Uncategorized")).ToList();

        await sink.WriteChannelsAsync(p.Id, channels, ct);
        await sink.WriteVodAsync(p.Id, vods, ct);
        await sink.WriteSeriesAsync(p.Id, series, ct);

        progress?.Report(new SyncProgress("Done", channels.Count, vods.Count, series.Count));
        return p with
        {
            LastSyncTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ChannelCount = channels.Count,
            VodCount = vods.Count,
            SeriesCount = series.Count,
            LastError = null,
        };
    }

    private async Task<List<StalkerCategory>> ParseStalkerCategoriesAsync(Playlist p, string token, string cookie, string type, CancellationToken ct)
    {
        // Try get_categories first (newer portals); fall back to get_genres.
        foreach (var action in new[] { "get_categories", "get_genres" })
        {
            var resp = await _stalker.GetCategoriesAsync(p.Url, cookie, token, type, action, ct);
            if (resp is null) continue;
            try
            {
                if (resp.Js.ValueKind == JsonValueKind.Array)
                {
                    var list = resp.Js.Deserialize<List<StalkerCategory>>();
                    if (list is { Count: > 0 }) return list;
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Stalker {Type} categories ({Action}) parse failed", type, action);
            }
        }
        return new();
    }

    // -------- M3U --------------------------------------------------------

    private async Task<Playlist> SyncM3uUrlAsync(Playlist p, IContentSink sink, IProgress<SyncProgress>? progress, CancellationToken ct)
    {
        progress?.Report(new SyncProgress("Downloading M3U", 0, 0, 0));
        await using var stream = await _http.GetStreamAsync(p.Url, ct);
        using var reader = new StreamReader(stream);
        return await ConsumeM3uAsync(p, reader, sink, progress, ct);
    }

    private async Task<Playlist> SyncM3uFileAsync(Playlist p, IContentSink sink, IProgress<SyncProgress>? progress, CancellationToken ct)
    {
        progress?.Report(new SyncProgress("Reading M3U file", 0, 0, 0));
        using var reader = new StreamReader(p.Url);
        return await ConsumeM3uAsync(p, reader, sink, progress, ct);
    }

    private async Task<Playlist> ConsumeM3uAsync(Playlist p, TextReader reader, IContentSink sink, IProgress<SyncProgress>? progress, CancellationToken ct)
    {
        var channels = new List<Channel>();
        var vods = new List<VodItem>();
        await foreach (var item in _m3u.ParseAsync(reader, p.Id, ct))
        {
            switch (item)
            {
                case M3uItem.ChannelItem c: channels.Add(c.Channel); break;
                case M3uItem.VodEntry v: vods.Add(v.Vod); break;
            }
            if ((channels.Count + vods.Count) % 250 == 0)
            {
                progress?.Report(new SyncProgress("Parsing", channels.Count, vods.Count, 0));
            }
        }

        await sink.WriteChannelsAsync(p.Id, channels, ct);
        await sink.WriteVodAsync(p.Id, vods, ct);
        progress?.Report(new SyncProgress("Done", channels.Count, vods.Count, 0));

        return p with
        {
            LastSyncTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ChannelCount = channels.Count,
            VodCount = vods.Count,
            SeriesCount = 0,
            LastError = null,
        };
    }
}

/// <summary>
/// The persistence interface the SyncService writes to. Implemented in the
/// App layer using the EF repositories — kept here so Core stays free of
/// EF Core.
/// </summary>
public interface IContentSink
{
    Task WriteChannelsAsync(string playlistId, IReadOnlyList<Channel> channels, CancellationToken ct);
    Task WriteVodAsync(string playlistId, IReadOnlyList<VodItem> vod, CancellationToken ct);
    Task WriteSeriesAsync(string playlistId, IReadOnlyList<Series> series, CancellationToken ct);
}
