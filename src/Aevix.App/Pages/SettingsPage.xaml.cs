using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aevix_App.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel Vm { get; }

    public SettingsPage()
    {
        Vm = App.Services.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            await Vm.LoadAsync();
            AutoPlayToggle.IsOn = Vm.AutoPlay;
            ContinueToggle.IsOn = Vm.ContinueWatchingEnabled;
            EpgRefreshToggle.IsOn = Vm.EpgAutoRefresh;
            AdultBlockToggle.IsOn = Vm.AdultContentBlocked;
            EpgOffsetBox.Value = Vm.EpgOffsetMinutes;
        };
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        Vm.AutoPlay = AutoPlayToggle.IsOn;
        Vm.ContinueWatchingEnabled = ContinueToggle.IsOn;
        Vm.EpgAutoRefresh = EpgRefreshToggle.IsOn;
        Vm.AdultContentBlocked = AdultBlockToggle.IsOn;
        Vm.EpgOffsetMinutes = double.IsNaN(EpgOffsetBox.Value) ? 0 : (int)EpgOffsetBox.Value;
        await Vm.SaveAsync();
    }

    private void Parental_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(ParentalPage));
}
