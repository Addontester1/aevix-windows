using Aevix.Core.Models;
using Aevix_App.Controls;
using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Aevix_App.Pages;

/// <summary>
/// Multi-screen page: 2 / 3 / 4 simultaneously-playing channels. Each cell
/// is backed by its own <see cref="VideoChildWindow"/> mirroring a XAML
/// border, so libVLC paints into a Win32 child of the main WinUI window
/// (same trick as <see cref="PlayerPage"/>).
/// </summary>
public sealed partial class MultiScreenPage : Page
{
    public MultiScreenViewModel Vm { get; }
    private readonly List<VideoChildWindow> _surfaces = new();

    public MultiScreenPage()
    {
        Vm = App.Services.GetRequiredService<MultiScreenViewModel>();
        InitializeComponent();

        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Vm.StatusText)) StatusText.Text = Vm.StatusText;
            if (e.PropertyName == nameof(Vm.Layout))       UpdateLayoutVisibility();
            if (e.PropertyName == nameof(Vm.IsPickerOpen)) PickerOverlay.Visibility = Vm.IsPickerOpen ? Visibility.Visible : Visibility.Collapsed;
        };
        PickerCategoryList.ItemsSource = Vm.PickerCategories;
        PickerChannelList.ItemsSource = Vm.PickerChannels;
        Unloaded += MultiScreenPage_Unloaded;
        UpdateLayoutVisibility();
    }

    private void UpdateLayoutVisibility()
    {
        LayoutPicker.Visibility = Vm.HasLayout ? Visibility.Collapsed : Visibility.Visible;
        CellSurface.Visibility = Vm.HasLayout ? Visibility.Visible : Visibility.Collapsed;
        if (Vm.HasLayout) BuildCellGrid();
        else TearDownCellGrid();
    }

    private async void Layout2_Click(object sender, RoutedEventArgs e) => await Vm.SetLayoutAsync(2);
    private async void Layout3_Click(object sender, RoutedEventArgs e) => await Vm.SetLayoutAsync(3);
    private async void Layout4_Click(object sender, RoutedEventArgs e) => await Vm.SetLayoutAsync(4);

    private void ChangeLayout_Click(object sender, RoutedEventArgs e) => Vm.ClearLayout();

    private void ClosePicker_Click(object sender, RoutedEventArgs e) => Vm.ClosePicker();

    private void PickerCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => Vm.PickerSelectedCategory = PickerCategoryList.SelectedItem as CategoryCount;

    private async void PickerChannel_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (PickerChannelList.SelectedItem is Channel ch && Vm.ActiveSlot >= 0)
        {
            await Vm.AssignChannelAsync(Vm.ActiveSlot, ch);
        }
    }

    // ---- Grid building --------------------------------------------------

    private void BuildCellGrid()
    {
        TearDownCellGrid();
        var count = Vm.Layout;

        CellGrid.RowDefinitions.Clear();
        CellGrid.ColumnDefinitions.Clear();
        CellGrid.Children.Clear();

        // Pick a layout per cell count.
        //  2 → two columns side-by-side
        //  3 → top row split in two, bottom row spans
        //  4 → 2x2 grid
        switch (count)
        {
            case 2:
                CellGrid.ColumnDefinitions.Add(new ColumnDefinition());
                CellGrid.ColumnDefinitions.Add(new ColumnDefinition());
                CellGrid.RowDefinitions.Add(new RowDefinition());
                AddCellBorder(0, row: 0, col: 0, rowSpan: 1, colSpan: 1);
                AddCellBorder(1, row: 0, col: 1, rowSpan: 1, colSpan: 1);
                break;
            case 3:
                CellGrid.ColumnDefinitions.Add(new ColumnDefinition());
                CellGrid.ColumnDefinitions.Add(new ColumnDefinition());
                CellGrid.RowDefinitions.Add(new RowDefinition());
                CellGrid.RowDefinitions.Add(new RowDefinition());
                AddCellBorder(0, row: 0, col: 0, rowSpan: 1, colSpan: 1);
                AddCellBorder(1, row: 0, col: 1, rowSpan: 1, colSpan: 1);
                AddCellBorder(2, row: 1, col: 0, rowSpan: 1, colSpan: 2);
                break;
            case 4:
                CellGrid.ColumnDefinitions.Add(new ColumnDefinition());
                CellGrid.ColumnDefinitions.Add(new ColumnDefinition());
                CellGrid.RowDefinitions.Add(new RowDefinition());
                CellGrid.RowDefinitions.Add(new RowDefinition());
                AddCellBorder(0, row: 0, col: 0, rowSpan: 1, colSpan: 1);
                AddCellBorder(1, row: 0, col: 1, rowSpan: 1, colSpan: 1);
                AddCellBorder(2, row: 1, col: 0, rowSpan: 1, colSpan: 1);
                AddCellBorder(3, row: 1, col: 1, rowSpan: 1, colSpan: 1);
                break;
        }
    }

    private void AddCellBorder(int slot, int row, int col, int rowSpan, int colSpan)
    {
        // Stacked layout: video reservation (Border), label + actions overlay.
        var videoArea = new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.Black),
            CornerRadius = new CornerRadius(6),
        };
        Grid.SetRow(videoArea, row);
        Grid.SetColumn(videoArea, col);
        Grid.SetRowSpan(videoArea, rowSpan);
        Grid.SetColumnSpan(videoArea, colSpan);

        // Tag the border so the click handler knows which slot it belongs to.
        videoArea.Tag = slot;
        videoArea.Tapped += Cell_Tapped;

        // Bottom-overlay row with title and per-cell buttons.
        var overlay = new Border
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xAA, 0, 0, 0)),
            VerticalAlignment = VerticalAlignment.Bottom,
            Padding = new Thickness(8, 4, 8, 4),
        };
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var title = new TextBlock
        {
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            VerticalAlignment = VerticalAlignment.Center,
        };
        // Update title from VM whenever the cell's Channel changes.
        var cell = Vm.Cells[slot];
        title.Text = cell.DisplayTitle;
        cell.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(MultiScreenCell.Channel) or nameof(MultiScreenCell.IsMuted))
            {
                DispatcherQueue.TryEnqueue(() => title.Text = cell.DisplayTitle + (cell.IsMuted ? "  🔇" : ""));
            }
        };
        var pickBtn = new Button { Content = "Pick" };
        pickBtn.Click += (_, _) => Vm.OpenPicker(slot);
        var muteBtn = new Button { Content = "Mute" };
        muteBtn.Click += (_, _) => Vm.ToggleMute(slot);
        var clearBtn = new Button { Content = "Clear" };
        clearBtn.Click += (_, _) => Vm.RemoveChannel(slot);
        panel.Children.Add(title);
        panel.Children.Add(pickBtn);
        panel.Children.Add(muteBtn);
        panel.Children.Add(clearBtn);
        overlay.Child = panel;

        // Container that stacks the overlay on top of the video reservation.
        var container = new Grid();
        container.Children.Add(videoArea);
        container.Children.Add(overlay);
        Grid.SetRow(container, row);
        Grid.SetColumn(container, col);
        Grid.SetRowSpan(container, rowSpan);
        Grid.SetColumnSpan(container, colSpan);
        CellGrid.Children.Add(container);

        // Mint a Win32 child surface for this cell once the page is in the visual tree.
        videoArea.Loaded += (_, _) =>
        {
            var surface = new VideoChildWindow(App.MainWindowInstance);
            surface.Track(videoArea);
            cell.MediaPlayer.Hwnd = surface.Hwnd;
            _surfaces.Add(surface);
        };
    }

    private void Cell_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is int slot)
        {
            Vm.OpenPicker(slot);
        }
    }

    private void TearDownCellGrid()
    {
        foreach (var s in _surfaces) s.Dispose();
        _surfaces.Clear();
        CellGrid.Children.Clear();
        CellGrid.RowDefinitions.Clear();
        CellGrid.ColumnDefinitions.Clear();
    }

    private void MultiScreenPage_Unloaded(object sender, RoutedEventArgs e)
    {
        TearDownCellGrid();
        Vm.ClearLayout();
    }
}
