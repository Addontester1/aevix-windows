using Aevix.Data.Dao;
using Microsoft.Extensions.DependencyInjection;

namespace Aevix_App.Services;

/// <summary>
/// Session-scoped parental control gate.
///
/// The persistent <c>UserSettings.AdultContentBlocked</c> setting says
/// whether adult content is gated *at all*; this singleton tracks whether
/// the user has unlocked the current session with the PIN. Once unlocked,
/// adult content is visible until the process exits — next launch starts
/// locked again (matching the TV app's behaviour).
///
/// Repositories read <see cref="ShouldHideAdultContent"/> when building
/// queries — it returns true when content should be filtered out.
/// </summary>
public sealed class ParentalGate
{
    private readonly IServiceProvider _root;
    private bool _sessionUnlocked;

    public ParentalGate(IServiceProvider root) => _root = root;

    /// <summary>True for this process lifetime once a valid PIN is entered.</summary>
    public bool IsSessionUnlocked => _sessionUnlocked;

    /// <summary>Fires when <see cref="IsSessionUnlocked"/> flips.</summary>
    public event EventHandler? SessionStateChanged;

    /// <summary>
    /// The flag repositories should pass as <c>blockAdult</c>: true when
    /// content should be hidden. Returns
    /// <c>AdultContentBlocked &amp;&amp; !IsSessionUnlocked</c>.
    /// </summary>
    public async Task<bool> ShouldHideAdultContentAsync(CancellationToken ct = default)
    {
        if (_sessionUnlocked) return false;
        await using var scope = _root.CreateAsyncScope();
        var settings = await scope.ServiceProvider.GetRequiredService<SettingsRepository>().GetAsync(ct);
        return settings.AdultContentBlocked;
    }

    /// <summary>Sync flavour for places that already have the settings in hand.</summary>
    public bool ShouldHideAdultContent(bool adultContentBlockedSetting)
        => adultContentBlockedSetting && !_sessionUnlocked;

    /// <summary>
    /// Verify <paramref name="pin"/> against the stored hash. On success
    /// flips the session into "unlocked" so adult content shows for the
    /// rest of the run. The stored PIN is untouched.
    /// </summary>
    public async Task<bool> TryUnlockSessionAsync(string pin, CancellationToken ct = default)
    {
        await using var scope = _root.CreateAsyncScope();
        var settings = await scope.ServiceProvider.GetRequiredService<SettingsRepository>().GetAsync(ct);
        if (string.IsNullOrWhiteSpace(settings.ParentalPinHash) || string.IsNullOrWhiteSpace(settings.ParentalPinSalt))
        {
            // No PIN configured — nothing to gate against, treat as unlocked.
            _sessionUnlocked = true;
            SessionStateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        var match = string.Equals(
            ViewModels.ParentalViewModel.HashPin(pin, settings.ParentalPinSalt),
            settings.ParentalPinHash,
            StringComparison.Ordinal);
        if (match)
        {
            _sessionUnlocked = true;
            SessionStateChanged?.Invoke(this, EventArgs.Empty);
        }
        return match;
    }

    /// <summary>Force the session back into the locked state (e.g. user clicked "Lock now").</summary>
    public void LockSession()
    {
        if (!_sessionUnlocked) return;
        _sessionUnlocked = false;
        SessionStateChanged?.Invoke(this, EventArgs.Empty);
    }
}
