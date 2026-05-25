using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Aevix_App.Pages;

public sealed partial class SeriesPage : Page
{
    public SeriesViewModel Vm { get; }

    public SeriesPage()
    {
        Vm = App.Services.GetRequiredService<SeriesViewModel>();
        InitializeComponent();
        SeriesGrid.ItemsSource = Vm.AllSeries;
        StatusText.Text = Vm.StatusText;
        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Vm.StatusText)) StatusText.Text = Vm.StatusText;
        };
        Vm.AllSeries.CollectionChanged += (_, _) => UpdateEmptyState();
        Loaded += async (_, _) => { await Vm.LoadAsync(); UpdateEmptyState(); };
    }

    private void UpdateEmptyState()
    {
        var empty = Vm.AllSeries.Count == 0;
        EmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        SeriesGrid.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }
}
