using Aevix.Core.Models;
using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Aevix_App.Pages;

public sealed partial class MoviesPage : Page
{
    public MoviesViewModel Vm { get; }

    public MoviesPage()
    {
        Vm = App.Services.GetRequiredService<MoviesViewModel>();
        InitializeComponent();
        CategoryList.ItemsSource = Vm.Categories;
        MovieGrid.ItemsSource = Vm.Movies;
        StatusText.Text = Vm.StatusText;
        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Vm.StatusText)) StatusText.Text = Vm.StatusText;
        };
        Vm.Movies.CollectionChanged += (_, _) => UpdateEmptyState();
        Loaded += async (_, _) => { await Vm.LoadAsync(); UpdateEmptyState(); };
    }

    private void UpdateEmptyState()
    {
        var empty = Vm.Movies.Count == 0;
        EmptyState.Visibility = empty ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        MovieGrid.Visibility = empty ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
    }

    private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => Vm.SelectedCategory = CategoryList.SelectedItem as CategoryCount;

    private void MovieGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (MovieGrid.SelectedItem is VodItem v)
        {
            Frame.Navigate(typeof(PlayerPage), new PlayRequest(v.Title, v.StreamUri));
        }
    }
}
