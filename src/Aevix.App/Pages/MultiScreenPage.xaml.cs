using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace Aevix_App.Pages;

public sealed partial class MultiScreenPage : Page
{
    public MultiScreenViewModel Vm { get; }

    public MultiScreenPage()
    {
        Vm = App.Services.GetRequiredService<MultiScreenViewModel>();
        InitializeComponent();
        StatusText.Text = Vm.StatusText;
    }
}
