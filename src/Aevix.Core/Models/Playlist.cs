namespace Aevix.Core.Models;

/// <summary>
/// One playlist configured by the user — M3U URL/file, Xtream Codes credentials,
/// or Stalker portal handshake. Drives every Channel / VodItem / Series row
/// downstream via <see cref="Id"/> as the foreign key.
/// </summary>
public sealed record Playlist(
    string Id,
    string Name,
    PlaylistType Type,
    string Url,
    string? Username = null,
    string? Password = null,
    string? MacAddress = null,
    string? Icon = null,
    long LastSyncTimestamp = 0L,
    long? ExpiryTimestamp = null,
    int ChannelCount = 0,
    int VodCount = 0,
    int SeriesCount = 0,
    bool IsActive = false,
    string? LastError = null);

public enum PlaylistType
{
    Xtream,
    Stalker,
    M3UUrl,
    M3UFile,
}
