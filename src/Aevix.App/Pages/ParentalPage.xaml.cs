using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aevix_App.Pages;

public sealed partial class ParentalPage : Page
{
    public ParentalViewModel Vm { get; }

    public ParentalPage()
    {
        Vm = App.Services.GetRequiredService<ParentalViewModel>();
        InitializeComponent();
        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Vm.StatusText)) StatusText.Text = Vm.StatusText;
        };
        Loaded += async (_, _) =>
        {
            await Vm.LoadAsync();
            StatusText.Text = Vm.IsPinSet ? "A PIN is already set — entering a new one replaces it." : "No PIN configured yet.";
        };
    }

    private async void Set_Click(object sender, RoutedEventArgs e)
    {
        var pin = PinBox.Password;
        await Vm.SetPinAsync(pin);
        StatusText.Text = Vm.StatusText;
    }
}
