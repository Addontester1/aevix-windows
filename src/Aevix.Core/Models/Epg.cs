namespace Aevix.Core.Models;

/// <summary>
/// One scheduled program for a channel. Timestamps are unix-millis UTC; the
/// EPG grid converts to local time for display via the user-configurable
/// <c>UserSettings.EpgOffsetMinutes</c> offset.
/// </summary>
public sealed record EpgProgram(
    string ChannelId,
    string Title,
    long StartTimestamp,
    long EndTimestamp,
    string? Description = null,
    string? Category = null);

/// <summary>
/// External XMLTV EPG source the user added. <see cref="IsAutoAdded"/> is
/// true when Aevix discovered it from the playlist itself (e.g. Xtream's
/// implicit <c>xmltv.php</c>).
/// </summary>
public sealed record EpgSource(
    string Id,
    string Name,
    string Url,
    bool IsEnabled = true,
    bool IsAutoAdded = false);
