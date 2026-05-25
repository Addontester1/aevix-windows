using Aevix.Core.Models;
using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;

namespace Aevix_App.Pages;

public sealed partial class PlaylistFormPage : Page
{
    public PlaylistFormViewModel Vm { get; }

    /// <summary>True while we're hydrating from the VM so change handlers don't fight back.</summary>
    private bool _hydrating;

    public PlaylistFormPage()
    {
        Vm = App.Services.GetRequiredService<PlaylistFormViewModel>();
        InitializeComponent();
        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Vm.StatusText)) StatusText.Text = Vm.StatusText;
            if (e.PropertyName == nameof(Vm.FormTitle)) FormTitleText.Text = Vm.FormTitle;
        };
        // Safe to set now — all named elements have been materialised.
        _hydrating = true;
        TypeBox.SelectedIndex = 0;
        _hydrating = false;
        ApplyTypeVisibility(PlaylistType.M3UUrl);
    }

    /// <summary>If the caller passes a playlist id we load it for editing.</summary>
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string id && !string.IsNullOrWhiteSpace(id))
        {
            if (await Vm.LoadForEditAsync(id))
            {
                Hydrate();
            }
        }
    }

    /// <summary>Push VM values into the controls. The same Hydrate runs after loading an edit target.</summary>
    private void Hydrate()
    {
        _hydrating = true;
        try
        {
            NameBox.Text = Vm.Name;
            // Type
            for (var i = 0; i < TypeBox.Items.Count; i++)
            {
                if (TypeBox.Items[i] is ComboBoxItem ci && ci.Tag?.ToString() == Vm.Type.ToString().Replace("UUrl", "UUrl").Replace("UFile", "UFile"))
                {
                    TypeBox.SelectedIndex = i;
                    break;
                }
            }
            ApplyTypeVisibility(Vm.Type);
            switch (Vm.Type)
            {
                case PlaylistType.M3UUrl:
                    M3uUrlBox.Text = Vm.Url;
                    break;
                case PlaylistType.M3UFile:
                    M3uFileBox.Text = Vm.Url;
                    break;
                case PlaylistType.Xtream:
                    XtreamUrlBox.Text = Vm.Url;
                    XtreamUserBox.Text = Vm.Username ?? string.Empty;
                    XtreamPassBox.Password = Vm.Password ?? string.Empty;
                    break;
                case PlaylistType.Stalker:
                    StalkerUrlBox.Text = Vm.Url;
                    StalkerMacBox.Text = Vm.MacAddress ?? string.Empty;
                    break;
            }
        }
        finally
        {
            _hydrating = false;
        }
    }

    private void TypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_hydrating) return;
        var type = (TypeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() switch
        {
            "M3UFile" => PlaylistType.M3UFile,
            "Xtream"  => PlaylistType.Xtream,
            "Stalker" => PlaylistType.Stalker,
            _         => PlaylistType.M3UUrl,
        };
        ApplyTypeVisibility(type);
    }

    private void ApplyTypeVisibility(PlaylistType type)
    {
        // Defensive: SelectionChanged can theoretically fire before all the
        // named XAML elements have been materialised. Don't crash on it.
        if (M3uUrlGroup is null || M3uFileGroup is null || XtreamGroup is null || StalkerGroup is null) return;
        M3uUrlGroup.Visibility  = type == PlaylistType.M3UUrl  ? Visibility.Visible : Visibility.Collapsed;
        M3uFileGroup.Visibility = type == PlaylistType.M3UFile ? Visibility.Visible : Visibility.Collapsed;
        XtreamGroup.Visibility  = type == PlaylistType.Xtream  ? Visibility.Visible : Visibility.Collapsed;
        StalkerGroup.Visibility = type == PlaylistType.Stalker ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Win32 file open picker — `.m3u`/`.m3u8` filter.</summary>
    private async void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".m3u");
        picker.FileTypeFilter.Add(".m3u8");
        // In unpackaged WinUI 3 we have to give the picker our window handle
        // ourselves or it'll throw.
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var file = await picker.PickSingleFileAsync();
        if (file is not null) M3uFileBox.Text = file.Path;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Vm.Name = NameBox.Text;
            Vm.Type = (TypeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() switch
            {
                "M3UFile" => PlaylistType.M3UFile,
                "Xtream"  => PlaylistType.Xtream,
                "Stalker" => PlaylistType.Stalker,
                _         => PlaylistType.M3UUrl,
            };

            // Pull values from whichever group is currently visible.
            switch (Vm.Type)
            {
                case PlaylistType.M3UUrl:
                    Vm.Url = M3uUrlBox.Text;
                    Vm.Username = null; Vm.Password = null; Vm.MacAddress = null;
                    break;
                case PlaylistType.M3UFile:
                    Vm.Url = M3uFileBox.Text;
                    Vm.Username = null; Vm.Password = null; Vm.MacAddress = null;
                    break;
                case PlaylistType.Xtream:
                    Vm.Url = XtreamUrlBox.Text;
                    Vm.Username = XtreamUserBox.Text;
                    Vm.Password = XtreamPassBox.Password;
                    Vm.MacAddress = null;
                    break;
                case PlaylistType.Stalker:
                    Vm.Url = StalkerUrlBox.Text;
                    Vm.MacAddress = StalkerMacBox.Text;
                    Vm.Username = null; Vm.Password = null;
                    break;
            }

            var saved = await Vm.SaveAsync();
            if (saved is not null && Frame.CanGoBack) Frame.GoBack();
        }
        catch (Exception ex)
        {
            // Surface the actual error inline AND log it so we can debug
            // "Save fails silently" reports without a crash.
            StatusText.Text = $"Save error: {ex.Message}";
            App.LogDiagnostic("PlaylistFormPage.Save_Click", ex);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }
}
