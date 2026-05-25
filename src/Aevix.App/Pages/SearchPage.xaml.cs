using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace Aevix_App.Pages;

public sealed partial class SearchPage : Page
{
    public SearchViewModel Vm { get; }

    public SearchPage()
    {
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        Vm = App.Services.GetRequiredService<SearchViewModel>();
        InitializeComponent();
        ChannelList.ItemsSource = Vm.Channels;
        MovieList.ItemsSource = Vm.Movies;
        SeriesList.ItemsSource = Vm.Series;
        StatusText.Text = Vm.StatusText;
        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Vm.StatusText)) StatusText.Text = Vm.StatusText;
        };
    }

    private void QueryBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // Push the new query into the VM; debounce + search runs there.
        Vm.Query = sender.Text;
    }
}
