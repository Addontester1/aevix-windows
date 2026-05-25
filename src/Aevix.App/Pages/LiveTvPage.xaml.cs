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
        Loaded += async (_, _) =>
        {
            // Page is cached — only do the initial expensive load once. On
            // subsequent visits the categories + selected channel list are
            // already populated.
            if (!_initialLoadDone)
            {
                await Vm.LoadAsync();
                _initialLoadDone = true;
            }
            UpdateEmptyState();
        };
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
