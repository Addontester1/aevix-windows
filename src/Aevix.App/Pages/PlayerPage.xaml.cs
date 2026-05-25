using Aevix_App.Controls;
using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Aevix_App.Pages;

/// <summary>
/// Hosts libVLC playback inside a Win32 child window that mirrors the
/// <c>VideoArea</c> XAML rectangle's bounds. WinUI 3 renders its visual
/// tree via DirectComposition — handing libVLC the main window HWND
/// causes the video to be painted behind the compositor (invisible).
/// </summary>
public sealed partial class PlayerPage : Page
{
    public PlayerViewModel Vm { get; }
    private PlayRequest? _request;
    private VideoChildWindow? _surface;

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
        try
        {
            await Vm.Player.InitializeAsync();

            // Build a real Win32 child HWND parented to the WinUI main
            // window and have it follow the layout of VideoArea.
            _surface ??= new VideoChildWindow(App.MainWindowInstance);
            _surface.Track(VideoArea);
            Vm.Player.MediaPlayer.Hwnd = _surface.Hwnd;

            if (_request is not null)
            {
                await Vm.PlayAsync(_request.Title, _request.Url);
            }
        }
        catch (Exception ex)
        {
            Vm.StatusText = "Player init failed: " + ex.Message;
        }
    }

    private void PlayerPage_Unloaded(object sender, RoutedEventArgs e)
    {
        Vm.Player.Stop();
        if (Vm.Player.MediaPlayer is { } mp)
        {
            mp.Hwnd = IntPtr.Zero;
        }
        _surface?.Dispose();
        _surface = null;
    }

    private void Play_Click(object sender, RoutedEventArgs e) => Vm.Player.Resume();
    private void Pause_Click(object sender, RoutedEventArgs e) => Vm.Player.Pause();
    private void Stop_Click(object sender, RoutedEventArgs e) => Vm.Player.Stop();
}
