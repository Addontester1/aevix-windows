using Aevix.Core.Network.Stalker;
using Aevix.Core.Network.Xtream;
using Aevix.Core.Parsers;
using Aevix.Core.Services;
using Aevix.Data;
using Aevix.Player;
using Aevix_App.Services;
using Aevix_App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevix_App.Hosting;

/// <summary>
/// One static <see cref="IServiceProvider"/> wired up at process start so
/// XAML pages can resolve their view-models. We deliberately keep this
/// simple instead of pulling in <c>Microsoft.Extensions.Hosting</c>'s
/// generic host — WinUI pages are constructed by the XAML loader and don't
/// participate in a host lifecycle anyway.
/// </summary>
public static class AppHost
{
    private static IServiceProvider? _services;

    /// <summary>Composition root. Returns the same provider on subsequent calls.</summary>
    public static IServiceProvider Services => _services ?? throw new InvalidOperationException("AppHost not initialised.");

    public static IServiceProvider Build()
    {
        if (_services is not null) return _services;

        var dbPath = ResolveDbPath();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug().SetMinimumLevel(LogLevel.Information));

        // -- HttpClient shared by Xtream + Stalker + raw M3U download. ----
        // We pin a desktop browser User-Agent and a generous timeout —
        // some IPTV panels (Xtream especially) reject GETs that come in
        // with the default .NET UA, and category fetches on large
        // playlists routinely take 30s+.
        services.AddHttpClient("aevix", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36 Aevix/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/plain, */*");
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddSingleton(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("aevix"));

        // -- Core services ------------------------------------------------
        services.AddSingleton<M3uParser>();
        services.AddSingleton<XtreamClient>();
        services.AddSingleton<StalkerClient>();
        services.AddSingleton<SyncService>();
        services.AddSingleton<AevixPlayer>();
        services.AddSingleton<IContentSink, EfContentSink>();

        // -- Data ---------------------------------------------------------
        services.AddAevixData(dbPath);

        // -- ViewModels (transient — created per page) --------------------
        services.AddTransient<HomeViewModel>();
        services.AddTransient<LiveTvViewModel>();
        services.AddTransient<MoviesViewModel>();
        services.AddTransient<SeriesViewModel>();
        services.AddTransient<SearchViewModel>();
        services.AddTransient<FavoritesViewModel>();
        services.AddTransient<PlaylistsViewModel>();
        services.AddTransient<PlaylistFormViewModel>();
        services.AddTransient<PlayerViewModel>();
        services.AddTransient<EpgViewModel>();
        services.AddTransient<MultiScreenViewModel>();
        services.AddTransient<ParentalViewModel>();
        services.AddTransient<SettingsViewModel>();

        _services = services.BuildServiceProvider();

        // Materialise SQLite schema synchronously on first launch so the
        // first page navigation doesn't race the EnsureCreated.
        _services.EnsureCreatedAsync().GetAwaiter().GetResult();

        return _services;
    }

    private static string ResolveDbPath()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aevix");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "aevix.db");
    }
}
