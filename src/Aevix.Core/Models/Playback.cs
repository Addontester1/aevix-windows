namespace Aevix.Core.Models;

/// <summary>
/// Persisted "continue watching" state. <see cref="Percentage"/> is a 0..1
/// fraction so the UI can paint a progress bar without doing arithmetic.
/// </summary>
public sealed record PlaybackProgress(
    string ItemId,
    long PositionSec,
    long DurationSec,
    float Percentage,
    long LastWatchedTimestamp,
    ContentType ItemType,
    string ItemTitle,
    string? PosterUrl = null,
    string? StreamUri = null);

public enum ContentType { Channel, Vod, Episode }
