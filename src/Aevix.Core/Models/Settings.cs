namespace Aevix.Core.Models;

/// <summary>
/// User-tunable settings persisted in the local SQLite KV table. Defaults
/// here are the same as the Android app so behaviour matches across devices.
/// </summary>
public sealed record UserSettings(
    bool AutoPlay = true,
    bool ContinueWatchingEnabled = true,
    bool EpgAutoRefresh = true,
    bool AdultContentBlocked = false,
    string? ParentalPinHash = null,
    string? ParentalPinSalt = null,
    PlayerEngine DefaultPlayerEngine = PlayerEngine.Vlc,
    int? SleepTimerMinutes = null,
    string? TmdbApiKey = null,
    int EpgOffsetMinutes = 0,
    DecoderMode DecoderMode = DecoderMode.Auto);

public enum PlayerEngine { Auto, ExoPlayer, Vlc }
public enum DecoderMode { Auto, Hardware, Software }
