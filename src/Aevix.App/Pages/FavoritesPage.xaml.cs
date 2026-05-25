using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace Aevix_App.Pages;

public sealed partial class FavoritesPage : Page
{
    public FavoritesViewModel Vm { get; }

    public FavoritesPage()
    {
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        Vm = App.Services.GetRequiredService<FavoritesViewModel>();
        InitializeComponent();
        FavList.ItemsSource = Vm.RawFavorites;
        StatusText.Text = Vm.StatusText;
        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Vm.StatusText)) StatusText.Text = Vm.StatusText;
        };
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await Vm.LoadAsync();
    }
}
