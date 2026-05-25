using Aevix.Core.Models;
using Aevix.Core.Services;
using Aevix.Data.Dao;
using Microsoft.Extensions.DependencyInjection;

namespace Aevix_App.Services;

/// <summary>
/// Bridges <see cref="IContentSink"/> (Core abstraction) to the EF Core
/// repositories. Each call resolves a fresh scope so we don't hold a long
/// DbContext open across a multi-minute sync.
/// </summary>
public sealed class EfContentSink : IContentSink
{
    private readonly IServiceProvider _root;
    public EfContentSink(IServiceProvider root) => _root = root;

    public async Task WriteChannelsAsync(string playlistId, IReadOnlyList<Channel> channels, CancellationToken ct)
    {
        await using var scope = _root.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ContentRepository>();
        await repo.UpsertChannelsAsync(channels, ct);
    }

    public async Task WriteVodAsync(string playlistId, IReadOnlyList<VodItem> vod, CancellationToken ct)
    {
        await using var scope = _root.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ContentRepository>();
        await repo.UpsertVodAsync(vod, ct);
    }

    public async Task WriteSeriesAsync(string playlistId, IReadOnlyList<Series> series, CancellationToken ct)
    {
        await using var scope = _root.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ContentRepository>();
        await repo.UpsertSeriesAsync(series, ct);
    }
}
