using Aevix.Core.Models;
using Aevix_App.Services;
using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aevix_App.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel Vm { get; }
    private readonly ParentalGate _gate;

    /// <summary>True while we're programmatically hydrating controls so the change handlers don't fight back.</summary>
    private bool _hydrating;

    public SettingsPage()
    {
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        Vm = App.Services.GetRequiredService<SettingsViewModel>();
        _gate = App.Services.GetRequiredService<ParentalGate>();
        InitializeComponent();
        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Vm.ParentalStatus)) ParentalStatusText.Text = Vm.ParentalStatus;
            if (e.PropertyName == nameof(Vm.SaveStatus)) SaveStatusText.Text = Vm.SaveStatus;
            if (e.PropertyName == nameof(Vm.AdultContentBlocked)) AdultBlockToggle.IsOn = Vm.AdultContentBlocked;
            if (e.PropertyName == nameof(Vm.IsPinSet)) UpdateButtonState();
        };
        _gate.SessionStateChanged += (_, _) => UpdateSessionStatus();

        Loaded += async (_, _) => { await HydrateAsync(); UpdateSessionStatus(); UpdateButtonState(); };
    }

    private void UpdateButtonState()
    {
        ClearPinButton.IsEnabled = Vm.IsPinSet;
        UnlockSessionBtn.IsEnabled = Vm.IsPinSet && Vm.AdultContentBlocked && !_gate.IsSessionUnlocked;
        LockSessionBtn.IsEnabled = _gate.IsSessionUnlocked;
    }

    private void UpdateSessionStatus()
    {
        SessionStatusText.Text = _gate.IsSessionUnlocked
            ? "Session unlocked — adult content is visible until you close Aevix or click ‘Lock now’."
            : (Vm.IsPinSet && Vm.AdultContentBlocked
                ? "Session locked — adult content is hidden."
                : string.Empty);
        UpdateButtonState();
    }

    private async Task HydrateAsync()
    {
        _hydrating = true;
        try
        {
            await Vm.LoadAsync();
            AutoPlayToggle.IsOn = Vm.AutoPlay;
            ContinueToggle.IsOn = Vm.ContinueWatchingEnabled;
            EpgRefreshToggle.IsOn = Vm.EpgAutoRefresh;
            AdultBlockToggle.IsOn = Vm.AdultContentBlocked;
            EpgOffsetBox.Value = Vm.EpgOffsetMinutes;
            TmdbKeyBox.Password = Vm.TmdbApiKey ?? string.Empty;
            PlayerEngineBox.SelectedIndex = Vm.DefaultPlayerEngine switch
            {
                PlayerEngine.Auto => 0,
                _ => 1, // VLC (or any non-Auto)
            };
            DecoderModeBox.SelectedIndex = Vm.DecoderMode switch
            {
                DecoderMode.Hardware => 1,
                DecoderMode.Software => 2,
                _ => 0,
            };
            SleepTimerBox.SelectedIndex = (Vm.SleepTimerMinutes ?? 0) switch
            {
                15 => 1, 30 => 2, 60 => 3, 90 => 4, 120 => 5, _ => 0,
            };
            ParentalStatusText.Text = Vm.ParentalStatus;
            ClearPinButton.IsEnabled = Vm.IsPinSet;
        }
        finally
        {
            _hydrating = false;
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        Vm.AutoPlay = AutoPlayToggle.IsOn;
        Vm.ContinueWatchingEnabled = ContinueToggle.IsOn;
        Vm.EpgAutoRefresh = EpgRefreshToggle.IsOn;
        Vm.EpgOffsetMinutes = double.IsNaN(EpgOffsetBox.Value) ? 0 : (int)EpgOffsetBox.Value;
        Vm.TmdbApiKey = TmdbKeyBox.Password;
        Vm.DefaultPlayerEngine = (PlayerEngineBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() switch
        {
            "Auto" => PlayerEngine.Auto,
            _ => PlayerEngine.Vlc,
        };
        Vm.DecoderMode = (DecoderModeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() switch
        {
            "Hardware" => DecoderMode.Hardware,
            "Software" => DecoderMode.Software,
            _ => DecoderMode.Auto,
        };
        var sleepTag = (SleepTimerBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        Vm.SleepTimerMinutes = int.TryParse(sleepTag, out var m) && m > 0 ? m : null;
        await Vm.SaveAsync();
    }

    /// <summary>
    /// Adult-content toggle behaviour:
    ///   - No PIN set → toggle freely.
    ///   - PIN set, turning ON → allowed (only tightens security).
    ///   - PIN set, turning OFF → verify PIN, but **keep the PIN in place**
    ///     so the user can re-block any time without recreating it. To
    ///     actually remove the PIN, use the dedicated "Remove PIN" button.
    /// </summary>
    private async void AdultBlockToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_hydrating) return;
        if (!Vm.IsPinSet)
        {
            Vm.AdultContentBlocked = AdultBlockToggle.IsOn;
            return;
        }
        if (AdultBlockToggle.IsOn)
        {
            // Re-block is free — no PIN required to TIGHTEN security.
            Vm.AdultContentBlocked = true;
            Vm.ParentalStatus = "Adult content is blocked.";
            return;
        }
        // User wants to unblock — require PIN, but leave the PIN itself set.
        var pin = PinBox.Password;
        if (string.IsNullOrEmpty(pin))
        {
            AdultBlockToggle.IsOn = true;
            Vm.ParentalStatus = "Enter your PIN in the box below, then toggle this switch off again to unblock.";
            return;
        }
        var ok = await Vm.VerifyAndUnblockAdultAsync(pin);
        if (!ok)
        {
            // Wrong PIN — snap the toggle back so it stays in sync with state.
            AdultBlockToggle.IsOn = true;
        }
        else
        {
            PinBox.Password = string.Empty;
        }
    }

    private async void SetPin_Click(object sender, RoutedEventArgs e)
    {
        var pin = PinBox.Password;
        if (await Vm.SetPinAsync(pin))
        {
            PinBox.Password = string.Empty;
        }
        UpdateSessionStatus();
    }

    private async void ClearPin_Click(object sender, RoutedEventArgs e)
    {
        var pin = PinBox.Password;
        if (await Vm.VerifyAndClearPinAsync(pin))
        {
            PinBox.Password = string.Empty;
        }
        UpdateSessionStatus();
    }

    /// <summary>
    /// TV-style: verify the PIN and unlock adult content for the current
    /// session only. Persistent block setting stays ON, so a process
    /// restart returns to the locked state.
    /// </summary>
    private async void UnlockSession_Click(object sender, RoutedEventArgs e)
    {
        var pin = PinBox.Password;
        if (string.IsNullOrWhiteSpace(pin))
        {
            Vm.ParentalStatus = "Enter your PIN in the box, then click Unlock.";
            return;
        }
        var ok = await _gate.TryUnlockSessionAsync(pin);
        if (ok)
        {
            PinBox.Password = string.Empty;
            Vm.ParentalStatus = "Session unlocked.";
        }
        else
        {
            Vm.ParentalStatus = "Wrong PIN.";
        }
        UpdateSessionStatus();
    }

    private void LockSession_Click(object sender, RoutedEventArgs e)
    {
        _gate.LockSession();
        Vm.ParentalStatus = "Session locked — adult content hidden again.";
        UpdateSessionStatus();
    }
}
