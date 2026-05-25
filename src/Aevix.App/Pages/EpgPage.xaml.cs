using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace Aevix_App.Pages;

public sealed partial class EpgPage : Page
{
    public EpgViewModel Vm { get; }

    public EpgPage()
    {
        Vm = App.Services.GetRequiredService<EpgViewModel>();
        InitializeComponent();
        StatusText.Text = Vm.StatusText;
    }
}
