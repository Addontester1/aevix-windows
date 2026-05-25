using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Aevix_App.Pages;

/// <summary>
/// Hosts libVLC playback. In WinUI 3 the simplest reliable approach for
/// LibVLCSharp 3.x is to hand the engine the main window HWND — VLC then
/// paints over the entire client area. A SwapChainPanel + D3D11 path is
/// possible but adds a lot of native interop for marginal benefit.
/// </summary>
public sealed partial class PlayerPage : Page
{
    public PlayerViewModel Vm { get; }
    private PlayRequest? _request;

    public PlayerPage()
    {
        Vm = App.Services.GetRequiredService<PlayerViewModel>();
        InitializeComponent();
        Vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Vm.Title)) TitleText.Text = Vm.Title;
            if (e.PropertyName == nameof(Vm.StatusText)) StatusText.Text = Vm.StatusText;
        };
        Loaded += PlayerPage_Loaded;
        Unloaded += PlayerPage_Unloaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _request = e.Parameter as PlayRequest;
    }

    private async void PlayerPage_Loaded(object sender, RoutedEventArgs e)
    {
        await Vm.Player.InitializeAsync();

        // Attach VLC to the main window's HWND. WinUI 3 doesn't expose a
        // native child HWND for sub-rectangles without the Windows App
        // SDK's content-island APIs, so for v1 we paint over the window.
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
        Vm.Player.MediaPlayer.Hwnd = hwnd;

        if (_request is not null)
        {
            await Vm.PlayAsync(_request.Title, _request.Url);
        }
    }

    private void PlayerPage_Unloaded(object sender, RoutedEventArgs e)
    {
        Vm.Player.Stop();
        // Detach so navigating away doesn't keep VLC painting on the window.
        Vm.Player.MediaPlayer.Hwnd = IntPtr.Zero;
    }

    private void Play_Click(object sender, RoutedEventArgs e) => Vm.Player.Resume();
    private void Pause_Click(object sender, RoutedEventArgs e) => Vm.Player.Pause();
    private void Stop_Click(object sender, RoutedEventArgs e) => Vm.Player.Stop();
}
