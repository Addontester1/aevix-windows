using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Aevix.Core.Models;

namespace Aevix.Core.Parsers;

/// <summary>
/// Streams one <see cref="M3uItem"/> per playlist entry so the caller can
/// insert into SQLite in batches without holding the whole file in memory.
/// Detects "this is actually a movie" entries by file extension and routes
/// them to <see cref="M3uItem.VodEntry"/> instead of channels.
/// </summary>
public sealed class M3uParser
{
    private static readonly Regex ExtInfRegex = new(@"#EXTINF:-?\d+\s*(.*?),(.*)", RegexOptions.Compiled);
    private static readonly Regex TvgIdRegex = new(@"tvg-id=""([^""]*?)""", RegexOptions.Compiled);
    private static readonly Regex TvgNameRegex = new(@"tvg-name=""([^""]*?)""", RegexOptions.Compiled);
    private static readonly Regex TvgLogoRegex = new(@"tvg-logo=""([^""]*?)""", RegexOptions.Compiled);
    private static readonly Regex GroupTitleRegex = new(@"group-title=""([^""]*?)""", RegexOptions.Compiled);
    public static readonly Regex AdultRegex = new(@"(adult|18\+|xxx|porn|erotic)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] VodExtensions = [".mp4", ".mkv", ".avi", ".mov", ".m4v"];

    public async IAsyncEnumerable<M3uItem> ParseAsync(
        TextReader reader,
        string playlistId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        string attributes = string.Empty;
        string channelName = string.Empty;
        int sortOrder = 0;

        while (await reader.ReadLineAsync(ct).ConfigureAwait(false) is { } line)
        {
            ct.ThrowIfCancellationRequested();
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("#EXTM3U", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (trimmed.StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase))
            {
                var m = ExtInfRegex.Match(trimmed);
                if (m.Success)
                {
                    attributes = m.Groups[1].Value;
                    channelName = m.Groups[2].Value.Trim();
                }
                continue;
            }

            if (trimmed.StartsWith('#'))
            {
                continue;
            }

            // Stream URL line.
            if (channelName.Length == 0)
            {
                continue;
            }

            var tvgId = MatchOrNull(TvgIdRegex, attributes);
            var tvgName = MatchOrNull(TvgNameRegex, attributes);
            var logo = MatchOrNull(TvgLogoRegex, attributes);
            var group = MatchOrNull(GroupTitleRegex, attributes) ?? "Uncategorized";
            var id = $"{playlistId}_{tvgId ?? channelName}_{sortOrder}";
            var isVod = VodExtensions.Any(ext => trimmed.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
            var isAdult = AdultRegex.IsMatch(group) || AdultRegex.IsMatch(channelName);

            yield return isVod
                ? new M3uItem.VodEntry(new VodItem(
                    Id: id,
                    Title: channelName,
                    StreamUri: trimmed,
                    PlaylistId: playlistId,
                    Genre: group,
                    PosterUrl: logo,
                    IsAdult: isAdult))
                : new M3uItem.ChannelItem(new Channel(
                    Id: id,
                    Name: channelName,
                    Group: group,
                    StreamUri: trimmed,
                    PlaylistId: playlistId,
                    LogoUrl: logo,
                    TvgId: tvgId,
                    TvgName: tvgName,
                    SortOrder: sortOrder,
                    IsAdult: isAdult));

            sortOrder++;
            attributes = string.Empty;
            channelName = string.Empty;
        }
    }

    private static string? MatchOrNull(Regex regex, string input)
    {
        var m = regex.Match(input);
        return m.Success ? m.Groups[1].Value : null;
    }
}

/// <summary>One entry from an M3U file — either a live channel or a VOD title.</summary>
public abstract record M3uItem
{
    public sealed record ChannelItem(Channel Channel) : M3uItem;
    public sealed record VodEntry(VodItem Vod) : M3uItem;
}
