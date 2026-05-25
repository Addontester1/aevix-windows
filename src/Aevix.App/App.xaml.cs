using Aevix_App.Hosting;
using Microsoft.UI.Xaml;

namespace Aevix_App;

/// <summary>
/// Application entry point. Builds the DI container before activating the
/// main window so XAML pages can resolve services in their constructors.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Materialised once at process start — null until <see cref="OnLaunched"/>
    /// completes. Pages should resolve services via constructor injection
    /// (page constructors call <c>AppHost.Services.GetRequiredService&lt;T&gt;()</c>).
    /// </summary>
    public static IServiceProvider Services => AppHost.Services;

    /// <summary>
    /// The main window — exposed so the player page can pass its HWND to
    /// libVLC for video output. Null before <see cref="OnLaunched"/>.
    /// </summary>
    public static Window MainWindowInstance { get; private set; } = null!;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        AppHost.Build();
        _window = new MainWindow();
        MainWindowInstance = _window;
        _window.Activate();
    }
}
