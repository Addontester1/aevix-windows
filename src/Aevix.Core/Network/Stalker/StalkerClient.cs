using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aevix.Core.Network.Stalker;

/// <summary>
/// Talks to a Stalker portal (<c>portal.php</c>) using the MAG250
/// emulation handshake. The portal expects very specific User-Agent /
/// X-User-Agent headers — without them most servers return 403 even when
/// credentials are valid.
/// </summary>
public sealed class StalkerClient
{
    private const string UserAgent =
        "Mozilla/5.0 (QtEmbedded; U; Linux; C) AppleWebKit/533.3 (KHTML, like Gecko) MAG200 stbapp ver: 2 rev: 250 Safari/533.3";
    private const string XUserAgent = "Model: MAG250; Link: WiFi";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly ILogger<StalkerClient> _log;

    public StalkerClient(HttpClient http, ILogger<StalkerClient> log)
    {
        _http = http;
        _log = log;
    }

    public Task<StalkerTokenResponse?> HandshakeAsync(string portalUrl, string cookie, CancellationToken ct = default)
    {
        var url = $"{NormalisePortal(portalUrl)}?type=stb&action=handshake&prehash=0";
        return SendAsync<StalkerTokenResponse>(url, cookie, token: null, ct);
    }

    public Task<StalkerCategoryResponse?> GetCategoriesAsync(
        string portalUrl, string cookie, string token, string type, string action = "get_genres", CancellationToken ct = default)
    {
        var url = $"{NormalisePortal(portalUrl)}?type={type}&action={action}&JsHttpRequest=1-xml";
        return SendAsync<StalkerCategoryResponse>(url, cookie, token, ct);
    }

    public Task<StalkerContentResponse?> GetContentAsync(
        string portalUrl, string cookie, string token, string type, string genre, int page, int nums = 500, CancellationToken ct = default)
    {
        var url = $"{NormalisePortal(portalUrl)}?type={type}&action=get_ordered_list&genre={Uri.EscapeDataString(genre)}&p={page}&sortby=added&JsHttpRequest=1-xml&nums={nums}";
        return SendAsync<StalkerContentResponse>(url, cookie, token, ct);
    }

    public Task<StalkerContentResponse?> GetSeriesSeasonsAsync(
        string portalUrl, string cookie, string token, string movieId, string seasonId = "0", CancellationToken ct = default)
    {
        var url = $"{NormalisePortal(portalUrl)}?type=series&action=get_ordered_list&movie_id={Uri.EscapeDataString(movieId)}&season_id={Uri.EscapeDataString(seasonId)}";
        return SendAsync<StalkerContentResponse>(url, cookie, token, ct);
    }

    public Task<StalkerEpgResponse?> GetShortEpgAsync(
        string portalUrl, string cookie, string token, int period = 6, CancellationToken ct = default)
    {
        var url = $"{NormalisePortal(portalUrl)}?type=itv&action=get_short_epg&period={period}&JsHttpRequest=1-xml";
        return SendAsync<StalkerEpgResponse>(url, cookie, token, ct);
    }

    public Task<StalkerCreateLinkResponse?> CreateLinkAsync(
        string portalUrl, string cookie, string token, string cmd, CancellationToken ct = default)
    {
        var url = $"{NormalisePortal(portalUrl)}?type=itv&action=create_link&cmd={Uri.EscapeDataString(cmd)}&JsHttpRequest=1-xml";
        return SendAsync<StalkerCreateLinkResponse>(url, cookie, token, ct);
    }

    private async Task<T?> SendAsync<T>(string url, string cookie, string? token, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("Cookie", cookie);
            req.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            req.Headers.TryAddWithoutValidation("X-User-Agent", XUserAgent);
            if (token is not null) req.Headers.TryAddWithoutValidation("Authorization", token);

            using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                _log.LogWarning("Stalker GET {Url} returned {Status}", url, (int)resp.StatusCode);
                return default;
            }
            return await resp.Content.ReadFromJsonAsync<T>(JsonOpts, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Stalker GET {Url} failed", url);
            return default;
        }
    }

    /// <summary>
    /// Some user-supplied portal URLs already end in <c>portal.php</c>, some
    /// end in <c>/c</c>, some are just the host. Normalise to the
    /// <c>/portal.php</c> form expected by the API.
    /// </summary>
    private static string NormalisePortal(string portalUrl)
    {
        var trimmed = portalUrl.TrimEnd('/');
        if (trimmed.EndsWith("portal.php", StringComparison.OrdinalIgnoreCase)) return trimmed;
        if (trimmed.EndsWith("/c", StringComparison.OrdinalIgnoreCase)) return $"{trimmed}/portal.php";
        return $"{trimmed}/portal.php";
    }
}
