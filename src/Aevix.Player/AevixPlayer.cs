using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;

namespace Aevix.Player;

/// <summary>
/// One-per-app wrapper around libVLC. Owns the <see cref="LibVLC"/> instance
/// (expensive to construct, cheap to reuse) and a <see cref="MediaPlayer"/>
/// that the UI binds a surface to.
///
/// The actual SwapChainPanel / HWND attachment lives in the UI layer's
/// <c>VideoSurface</c> control — this class deliberately stays UI-agnostic
/// so unit tests can drive it without WinUI loaded.
/// </summary>
public sealed class AevixPlayer : IDisposable
{
    private readonly ILogger<AevixPlayer> _log;
    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private bool _initialized;

    public AevixPlayer(ILogger<AevixPlayer> log) => _log = log;

    public LibVLC LibVlc => _libVlc ?? throw new InvalidOperationException("Call InitializeAsync first.");
    public MediaPlayer MediaPlayer => _mediaPlayer ?? throw new InvalidOperationException("Call InitializeAsync first.");

    /// <summary>
    /// True when <see cref="InitializeAsync"/> has completed at least once.
    /// Callers that may run before Initialize finishes (e.g. fast
    /// navigate-then-back) should check this before touching
    /// <see cref="MediaPlayer"/>.
    /// </summary>
    public bool IsInitialized => _initialized && _mediaPlayer is not null;

    public bool IsPlaying => _mediaPlayer?.IsPlaying ?? false;

    public long PositionMs
    {
        get => _mediaPlayer?.Time ?? 0L;
        set { if (_mediaPlayer is not null) _mediaPlayer.Time = value; }
    }

    public long DurationMs => _mediaPlayer?.Length ?? 0L;

    public int Volume
    {
        get => _mediaPlayer?.Volume ?? 100;
        set { if (_mediaPlayer is not null) _mediaPlayer.Volume = Math.Clamp(value, 0, 200); }
    }

    public bool Mute
    {
        get => _mediaPlayer?.Mute ?? false;
        set { if (_mediaPlayer is not null) _mediaPlayer.Mute = value; }
    }

    /// <summary>
    /// One-time libVLC init. Must run on a background thread — Core::new()
    /// blocks while the native lib enumerates audio/video plugins.
    /// </summary>
    public Task InitializeAsync()
    {
        if (_initialized) return Task.CompletedTask;
        return Task.Run(() =>
        {
            try
            {
                LibVLCSharp.Shared.Core.Initialize();
                _libVlc = new LibVLC(enableDebugLogs: false,
                    // Same tuning we picked for the Android app — works well
                    // for IPTV / TS / HLS streams on Windows.
                    "--clock-jitter=0",
                    "--clock-synchro=0",
                    "--rtsp-tcp",
                    "--no-drop-late-frames",
                    "--no-skip-frames",
                    "--avcodec-skiploopfilter=4",
                    "--avcodec-fast",
                    "--http-reconnect",
                    "--http-continuous",
                    "--network-caching=1500");
                _mediaPlayer = new MediaPlayer(_libVlc);
                _initialized = true;
                _log.LogInformation("LibVLC initialised: {Version}", _libVlc.Changeset);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "LibVLC initialisation failed");
                throw;
            }
        });
    }

    /// <summary>Plays the given URL. Replaces any current media.</summary>
    public void Play(string url, IEnumerable<string>? mediaOptions = null)
    {
        if (_libVlc is null || _mediaPlayer is null) throw new InvalidOperationException("Initialize first.");
        var media = new Media(_libVlc, new Uri(url));
        if (mediaOptions is not null)
        {
            foreach (var opt in mediaOptions) media.AddOption(opt);
        }
        _mediaPlayer.Play(media);
        media.Dispose();
    }

    public void Pause() => _mediaPlayer?.Pause();
    public void Resume() { if (_mediaPlayer is { IsPlaying: false }) _mediaPlayer.Play(); }
    public void Stop() => _mediaPlayer?.Stop();

    /// <summary>
    /// Mints an additional <see cref="MediaPlayer"/> on the shared libVLC
    /// instance. Multi-screen creates one of these per cell so each video
    /// renders to its own HWND independently of the main player.
    ///
    /// The caller owns the returned object — it must <see cref="MediaPlayer.Dispose"/>
    /// it when done. libVLC handles concurrent MediaPlayer instances natively.
    /// </summary>
    public MediaPlayer CreateAdditionalMediaPlayer()
    {
        if (_libVlc is null) throw new InvalidOperationException("Call InitializeAsync first.");
        return new MediaPlayer(_libVlc);
    }

    /// <summary>Play a URL on a caller-owned MediaPlayer (used by multi-screen cells).</summary>
    public void PlayOn(MediaPlayer mp, string url)
    {
        if (_libVlc is null) throw new InvalidOperationException("Call InitializeAsync first.");
        using var media = new Media(_libVlc, new Uri(url));
        mp.Play(media);
    }

    public void Dispose()
    {
        _mediaPlayer?.Dispose();
        _libVlc?.Dispose();
        _mediaPlayer = null;
        _libVlc = null;
        _initialized = false;
    }
}
