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

    /// <summary>
    /// Per-launch crash log path. Anything that escapes a UI callback lands
    /// here so we can diagnose 0xc000027b-style XAML faults.
    /// </summary>
    private static readonly string CrashLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Aevix", "crash.log");

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) => Log("App.UnhandledException", e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log("AppDomain.UnhandledException", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) => Log("TaskScheduler.UnobservedTaskException", e.Exception);
    }

    public static IServiceProvider Services => AppHost.Services;

    /// <summary>
    /// The main window — exposed so the player page can pass its HWND to
    /// libVLC for video output. Null before <see cref="OnLaunched"/>.
    /// </summary>
    public static Window MainWindowInstance { get; private set; } = null!;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            AppHost.Build();
            _window = new MainWindow();
            MainWindowInstance = _window;
            _window.Activate();
        }
        catch (Exception ex)
        {
            Log("OnLaunched", ex);
            throw;
        }
    }

    /// <summary>Append a timestamped entry to the crash log; swallow any logging errors.</summary>
    private static void Log(string source, Exception? ex)
    {
        if (ex is null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CrashLogPath)!);
            File.AppendAllText(CrashLogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { /* logging must not crash the crash handler */ }
    }

    /// <summary>
    /// Public diagnostic logger — pages can call this to record exceptions
    /// they handled inline (so the user sees a friendly message AND we
    /// retain a stack trace).
    /// </summary>
    public static void LogDiagnostic(string source, Exception ex) => Log(source, ex);

    /// <summary>Public diagnostic info logger — for non-exception events worth tracing.</summary>
    public static void LogInfo(string source, string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CrashLogPath)!);
            File.AppendAllText(CrashLogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO {source}: {message}{Environment.NewLine}");
        }
        catch { }
    }
}
