using Aevix.Core.Models;
using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Aevix_App.Pages;

public sealed partial class LiveTvPage : Page
{
    public LiveTvViewModel Vm { get; }

    private bool _initialLoadDone;

    public LiveTvPage()
    {
        // Cache the page instance across navigations so category selection
        // + scroll position survive a round-trip through Player.
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        Vm = App.Services.GetRequiredService<LiveTvViewModel>();
        InitializeComponent();
        CategoryList.ItemsSource = Vm.Categories;
        ChannelList.ItemsSource = Vm.Channels;
        StatusText.Text = Vm.StatusText;
        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Vm.StatusText)) StatusText.Text = Vm.StatusText;
        };
        Vm.Channels.CollectionChanged += (_, _) => UpdateEmptyState();
        Loaded += (_, _) => UpdateEmptyState();
    }

    /// <summary>
    /// Reload the active playlist's categories every time we navigate
    /// here. Preserves the user's current category selection by Group
    /// name so a no-op nav doesn't clear their place; if the category
    /// disappeared (e.g. they switched to a freshly-synced playlist),
    /// selection is dropped and they pick from the new list.
    /// </summary>
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
                // Re-assign with the *new* CategoryCount instance so the
                // change handler fires and channels reload against the
                // current data.
                Vm.SelectedCategory = match;
                CategoryList.SelectedItem = match;
            }
            else if (match is null)
            {
                // Stale selection — drop it so the user picks again.
                Vm.SelectedCategory = null;
                CategoryList.SelectedItem = null;
                Vm.Channels.Clear();
            }
        }
        UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        var empty = Vm.Channels.Count == 0;
        EmptyState.Visibility = empty ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        ChannelList.Visibility = empty ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;
    }

    private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Vm.SelectedCategory = CategoryList.SelectedItem as CategoryCount;
    }

    private void ChannelList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (ChannelList.SelectedItem is Channel ch)
        {
            Frame.Navigate(typeof(PlayerPage), new PlayRequest(ch.Name, ch.StreamUri));
        }
    }
}

/// <summary>
/// Navigation payload — page-to-page contract. Kept here next to its
/// originator so the dependency graph is obvious.
/// </summary>
public sealed record PlayRequest(string Title, string Url);
