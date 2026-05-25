using Aevix.Core.Models;
using Aevix.Data.Dao;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aevix_App.ViewModels;

/// <summary>EPG grid — populated later, this is a placeholder shell.</summary>
public sealed partial class EpgViewModel : ObservableObject
{
    [ObservableProperty] private string _statusText = "EPG coming soon.";
}

/// <summary>
/// Parental-PIN gate. We hash the PIN with PBKDF2 + a per-user salt so the
/// stored value isn't directly invertible — same approach as the Android app.
/// </summary>
public sealed partial class ParentalViewModel : ObservableObject
{
    private readonly SettingsRepository _settings;

    [ObservableProperty] private bool _isPinSet;
    [ObservableProperty] private string _pinEntry = string.Empty;
    [ObservableProperty] private string _statusText = string.Empty;

    public ParentalViewModel(SettingsRepository settings) => _settings = settings;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        IsPinSet = !string.IsNullOrWhiteSpace(s.ParentalPinHash);
    }

    public async Task<bool> SetPinAsync(string newPin, CancellationToken ct = default)
    {
        if (newPin.Length < 4) { StatusText = "PIN must be at least 4 digits."; return false; }
        var salt = Guid.NewGuid().ToString("n");
        var hash = HashPin(newPin, salt);
        var s = await _settings.GetAsync(ct);
        await _settings.SaveAsync(s with { ParentalPinHash = hash, ParentalPinSalt = salt, AdultContentBlocked = true }, ct);
        IsPinSet = true;
        StatusText = "PIN saved.";
        return true;
    }

    public async Task<bool> VerifyPinAsync(string pin, CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        if (string.IsNullOrWhiteSpace(s.ParentalPinHash) || string.IsNullOrWhiteSpace(s.ParentalPinSalt)) return true;
        return string.Equals(HashPin(pin, s.ParentalPinSalt), s.ParentalPinHash, StringComparison.Ordinal);
    }

    public async Task ClearPinAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        await _settings.SaveAsync(s with { ParentalPinHash = null, ParentalPinSalt = null, AdultContentBlocked = false }, ct);
        IsPinSet = false;
        StatusText = "PIN removed.";
    }

    internal static string HashPin(string pin, string salt)
    {
        var bytes = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            System.Text.Encoding.UTF8.GetBytes(pin),
            System.Text.Encoding.UTF8.GetBytes(salt),
            iterations: 100_000,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            outputLength: 32);
        return Convert.ToHexString(bytes);
    }
}

/// <summary>
/// Full-parity Settings view-model — every toggle the Android app exposes,
/// inline parental PIN, and a sleep-timer picker. Persists via
/// <see cref="SettingsRepository"/>; reads + writes the whole
/// <see cref="UserSettings"/> blob.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsRepository _settings;

    // -- General ---------------------------------------------------------
    [ObservableProperty] private bool _autoPlay;
    [ObservableProperty] private bool _continueWatchingEnabled;

    // -- Player ----------------------------------------------------------
    [ObservableProperty] private PlayerEngine _defaultPlayerEngine = PlayerEngine.Vlc;
    [ObservableProperty] private DecoderMode _decoderMode = DecoderMode.Auto;

    // -- EPG -------------------------------------------------------------
    [ObservableProperty] private bool _epgAutoRefresh;
    [ObservableProperty] private int _epgOffsetMinutes;

    // -- TMDB ------------------------------------------------------------
    [ObservableProperty] private string? _tmdbApiKey;

    // -- Sleep timer (minutes; null/0 means off) ------------------------
    [ObservableProperty] private int? _sleepTimerMinutes;

    // -- Parental --------------------------------------------------------
    [ObservableProperty] private bool _adultContentBlocked;
    [ObservableProperty] private bool _isPinSet;
    [ObservableProperty] private string _parentalStatus = string.Empty;
    [ObservableProperty] private string _saveStatus = string.Empty;

    public SettingsViewModel(SettingsRepository settings) => _settings = settings;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        AutoPlay = s.AutoPlay;
        ContinueWatchingEnabled = s.ContinueWatchingEnabled;
        EpgAutoRefresh = s.EpgAutoRefresh;
        EpgOffsetMinutes = s.EpgOffsetMinutes;
        AdultContentBlocked = s.AdultContentBlocked;
        DefaultPlayerEngine = s.DefaultPlayerEngine;
        DecoderMode = s.DecoderMode;
        SleepTimerMinutes = s.SleepTimerMinutes;
        TmdbApiKey = s.TmdbApiKey;
        IsPinSet = !string.IsNullOrWhiteSpace(s.ParentalPinHash);
        ParentalStatus = IsPinSet ? "A parental PIN is set." : "No PIN configured.";
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        // If the user turns adult content back on and a PIN is set, the
        // page asks them for it before we get here — but defence in depth.
        var adult = AdultContentBlocked;
        if (!adult && IsPinSet)
        {
            // Adult content can only be unblocked via the explicit verify path.
            adult = s.AdultContentBlocked;
        }
        await _settings.SaveAsync(s with
        {
            AutoPlay = AutoPlay,
            ContinueWatchingEnabled = ContinueWatchingEnabled,
            EpgAutoRefresh = EpgAutoRefresh,
            EpgOffsetMinutes = EpgOffsetMinutes,
            AdultContentBlocked = adult,
            DefaultPlayerEngine = DefaultPlayerEngine,
            DecoderMode = DecoderMode,
            SleepTimerMinutes = SleepTimerMinutes,
            TmdbApiKey = string.IsNullOrWhiteSpace(TmdbApiKey) ? null : TmdbApiKey,
        }, ct);
        SaveStatus = $"Saved at {DateTime.Now:HH:mm:ss}.";
    }

    /// <summary>Hash + persist a new PIN. Returns false if the PIN is too short.</summary>
    public async Task<bool> SetPinAsync(string newPin, CancellationToken ct = default)
    {
        if (newPin.Length < 4) { ParentalStatus = "PIN must be at least 4 digits."; return false; }
        var salt = Guid.NewGuid().ToString("n");
        var hash = ParentalViewModel.HashPin(newPin, salt);
        var s = await _settings.GetAsync(ct);
        await _settings.SaveAsync(s with
        {
            ParentalPinHash = hash,
            ParentalPinSalt = salt,
            AdultContentBlocked = true,
        }, ct);
        IsPinSet = true;
        AdultContentBlocked = true;
        ParentalStatus = "PIN set. Adult content is now blocked.";
        return true;
    }

    /// <summary>
    /// Verify the entered PIN and *unblock* adult content. Leaves the PIN
    /// itself in place — the user can re-block any time without re-entering
    /// a fresh PIN. Use <see cref="VerifyAndClearPinAsync"/> to actually
    /// remove the PIN.
    /// </summary>
    public async Task<bool> VerifyAndUnblockAdultAsync(string pin, CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        if (string.IsNullOrWhiteSpace(s.ParentalPinHash) || string.IsNullOrWhiteSpace(s.ParentalPinSalt))
        {
            // No PIN configured — toggling is free.
            AdultContentBlocked = false;
            return true;
        }
        var match = string.Equals(ParentalViewModel.HashPin(pin, s.ParentalPinSalt), s.ParentalPinHash, StringComparison.Ordinal);
        if (!match) { ParentalStatus = "Wrong PIN."; return false; }
        await _settings.SaveAsync(s with { AdultContentBlocked = false }, ct);
        AdultContentBlocked = false;
        ParentalStatus = "PIN verified — adult content unblocked. Toggle on again any time to re-block.";
        return true;
    }

    /// <summary>
    /// Verify the entered PIN and *remove* it entirely. Also unblocks adult
    /// content (since there's no PIN to gate it anymore).
    /// </summary>
    public async Task<bool> VerifyAndClearPinAsync(string pin, CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        if (string.IsNullOrWhiteSpace(s.ParentalPinHash) || string.IsNullOrWhiteSpace(s.ParentalPinSalt))
        {
            IsPinSet = false;
            return true;
        }
        var match = string.Equals(ParentalViewModel.HashPin(pin, s.ParentalPinSalt), s.ParentalPinHash, StringComparison.Ordinal);
        if (!match) { ParentalStatus = "Wrong PIN."; return false; }
        await _settings.SaveAsync(s with { ParentalPinHash = null, ParentalPinSalt = null, AdultContentBlocked = false }, ct);
        IsPinSet = false;
        AdultContentBlocked = false;
        ParentalStatus = "PIN removed.";
        return true;
    }
}
