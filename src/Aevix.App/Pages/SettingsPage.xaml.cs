using Aevix.Core.Models;
using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aevix_App.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel Vm { get; }

    /// <summary>True while we're programmatically hydrating controls so the change handlers don't fight back.</summary>
    private bool _hydrating;

    public SettingsPage()
    {
        Vm = App.Services.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Vm.ParentalStatus)) ParentalStatusText.Text = Vm.ParentalStatus;
            if (e.PropertyName == nameof(Vm.SaveStatus)) SaveStatusText.Text = Vm.SaveStatus;
            if (e.PropertyName == nameof(Vm.AdultContentBlocked)) AdultBlockToggle.IsOn = Vm.AdultContentBlocked;
            if (e.PropertyName == nameof(Vm.IsPinSet)) ClearPinButton.IsEnabled = Vm.IsPinSet;
        };

        Loaded += async (_, _) => await HydrateAsync();
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
    /// When the PIN is set, the user can only turn adult content back ON
    /// freely — turning it OFF requires the PIN. We snap the toggle back
    /// and ask for verification.
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
            // Allowed to block without PIN — turning it on only tightens.
            Vm.AdultContentBlocked = true;
            return;
        }
        // User is trying to unblock — require PIN.
        var pin = PinBox.Password;
        if (string.IsNullOrEmpty(pin))
        {
            AdultBlockToggle.IsOn = true;
            Vm.ParentalStatus = "Enter your PIN in the box below to unblock adult content.";
            return;
        }
        var ok = await Vm.VerifyAndClearPinAsync(pin);
        if (!ok)
        {
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
    }

    private async void ClearPin_Click(object sender, RoutedEventArgs e)
    {
        var pin = PinBox.Password;
        if (await Vm.VerifyAndClearPinAsync(pin))
        {
            PinBox.Password = string.Empty;
        }
    }
}
