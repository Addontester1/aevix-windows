using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aevix.Core.Network.Xtream;

/// <summary>
/// Thin wrapper over the Xtream Codes <c>player_api.php</c> endpoint. Every
/// public method maps 1:1 to one server action so the SyncService stays
/// readable. The <see cref="HttpClient"/> is injected so we can share a
/// connection pool and so tests can swap in a mock handler.
/// </summary>
public sealed class XtreamClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly ILogger<XtreamClient> _log;

    public XtreamClient(HttpClient http, ILogger<XtreamClient> log)
    {
        _http = http;
        _log = log;
    }

    public Task<XtreamAuthResponse?> AuthenticateAsync(string baseUrl, string username, string password, CancellationToken ct = default)
        => GetAsync<XtreamAuthResponse>(BuildUrl(baseUrl, username, password, action: null), ct);

    public Task<List<XtreamCategory>?> GetLiveCategoriesAsync(string baseUrl, string username, string password, CancellationToken ct = default)
        => GetAsync<List<XtreamCategory>>(BuildUrl(baseUrl, username, password, "get_live_categories"), ct);

    public Task<List<XtreamStream>?> GetLiveStreamsAsync(string baseUrl, string username, string password, string? categoryId = null, CancellationToken ct = default)
        => GetAsync<List<XtreamStream>>(BuildUrl(baseUrl, username, password, "get_live_streams", categoryId), ct);

    public Task<List<XtreamCategory>?> GetVodCategoriesAsync(string baseUrl, string username, string password, CancellationToken ct = default)
        => GetAsync<List<XtreamCategory>>(BuildUrl(baseUrl, username, password, "get_vod_categories"), ct);

    public Task<List<XtreamStream>?> GetVodStreamsAsync(string baseUrl, string username, string password, string? categoryId = null, CancellationToken ct = default)
        => GetAsync<List<XtreamStream>>(BuildUrl(baseUrl, username, password, "get_vod_streams", categoryId), ct);

    public Task<List<XtreamCategory>?> GetSeriesCategoriesAsync(string baseUrl, string username, string password, CancellationToken ct = default)
        => GetAsync<List<XtreamCategory>>(BuildUrl(baseUrl, username, password, "get_series_categories"), ct);

    public Task<List<XtreamStream>?> GetSeriesAsync(string baseUrl, string username, string password, string? categoryId = null, CancellationToken ct = default)
        => GetAsync<List<XtreamStream>>(BuildUrl(baseUrl, username, password, "get_series", categoryId), ct);

    public Task<XtreamSeriesInfo?> GetSeriesInfoAsync(string baseUrl, string username, string password, string seriesId, CancellationToken ct = default)
    {
        var url = $"{BuildUrl(baseUrl, username, password, "get_series_info")}&series_id={Uri.EscapeDataString(seriesId)}";
        return GetAsync<XtreamSeriesInfo>(url, ct);
    }

    /// <summary>
    /// Constructs the <c>player_api.php</c> URL. We accept both forms of
    /// "base URL" — bare host (<c>http://example.com:8080</c>) and one that
    /// already includes the script — by normalising here.
    /// </summary>
    private static string BuildUrl(string baseUrl, string username, string password, string? action, string? categoryId = null)
    {
        var root = baseUrl.TrimEnd('/');
        if (!root.EndsWith("player_api.php", StringComparison.OrdinalIgnoreCase))
        {
            root = $"{root}/player_api.php";
        }
        var url = $"{root}?username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";
        if (action is not null) url += $"&action={action}";
        if (categoryId is not null) url += $"&category_id={Uri.EscapeDataString(categoryId)}";
        return url;
    }

    private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            using var resp = await _http.GetAsync(url, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                _log.LogWarning("Xtream GET {Url} returned {Status}", url, (int)resp.StatusCode);
                return default;
            }
            return await resp.Content.ReadFromJsonAsync<T>(JsonOpts, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Xtream GET {Url} failed", url);
            return default;
        }
    }
}
