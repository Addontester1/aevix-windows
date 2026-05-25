using Aevix.Core.Models;
using Aevix.Data.Dao;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aevix_App.ViewModels;

/// <summary>EPG grid — populated later, this is a placeholder shell.</summary>
public sealed partial class EpgViewModel : ObservableObject
{
    [ObservableProperty] private string _statusText = "EPG coming soon.";
}

/// <summary>Multi-screen — placeholder shell.</summary>
public sealed partial class MultiScreenViewModel : ObservableObject
{
    [ObservableProperty] private string _statusText = "Multi-screen coming soon.";
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

    private static string HashPin(string pin, string salt)
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

/// <summary>Settings page — backs the editable form on the Settings view.</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsRepository _settings;

    [ObservableProperty] private bool _autoPlay;
    [ObservableProperty] private bool _continueWatchingEnabled;
    [ObservableProperty] private bool _epgAutoRefresh;
    [ObservableProperty] private bool _adultContentBlocked;
    [ObservableProperty] private int _epgOffsetMinutes;

    public SettingsViewModel(SettingsRepository settings) => _settings = settings;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        AutoPlay = s.AutoPlay;
        ContinueWatchingEnabled = s.ContinueWatchingEnabled;
        EpgAutoRefresh = s.EpgAutoRefresh;
        AdultContentBlocked = s.AdultContentBlocked;
        EpgOffsetMinutes = s.EpgOffsetMinutes;
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        await _settings.SaveAsync(s with
        {
            AutoPlay = AutoPlay,
            ContinueWatchingEnabled = ContinueWatchingEnabled,
            EpgAutoRefresh = EpgAutoRefresh,
            AdultContentBlocked = AdultContentBlocked,
            EpgOffsetMinutes = EpgOffsetMinutes,
        }, ct);
    }
}
