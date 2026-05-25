using Aevix.Core.Models;
using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Aevix_App.Pages;

public sealed partial class MoviesPage : Page
{
    public MoviesViewModel Vm { get; }

    private bool _initialLoadDone;

    public MoviesPage()
    {
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
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
        Loaded += (_, _) => UpdateEmptyState();
    }

    /// <summary>Refresh on every nav so a newly-synced playlist's categories appear, preserving the prior selection by name.</summary>
    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var previousGroup = Vm.SelectedCategory?.Group;
        await Vm.LoadAsync();
        _initialLoadDone = true;
        if (previousGroup is not null)
        {
            var match = Vm.Categories.FirstOrDefault(c =>
                string.Equals(c.Group, previousGroup, StringComparison.OrdinalIgnoreCase));
            if (match is not null && !ReferenceEquals(match, Vm.SelectedCategory))
            {
                Vm.SelectedCategory = match;
                CategoryList.SelectedItem = match;
            }
            else if (match is null)
            {
                Vm.SelectedCategory = null;
                CategoryList.SelectedItem = null;
                Vm.Movies.Clear();
            }
        }
        UpdateEmptyState();
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
