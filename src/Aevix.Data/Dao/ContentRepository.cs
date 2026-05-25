using Aevix.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Aevix.Data.Dao;

/// <summary>
/// Channels / VOD / Series queries scoped to the active playlist. UI screens
/// only ever talk to this repository — they never see EF entities.
/// </summary>
public sealed class ContentRepository
{
    private readonly AevixDbContext _db;
    public ContentRepository(AevixDbContext db) => _db = db;

    // -------- Channels ---------------------------------------------------

    public async Task<IReadOnlyList<Channel>> GetChannelsAsync(string playlistId, bool blockAdult, CancellationToken ct = default)
    {
        var query = _db.Channels.AsNoTracking().Where(c => c.PlaylistId == playlistId);
        if (blockAdult) query = query.Where(c => !c.IsAdult);
        return (await query.OrderBy(c => c.SortOrder).ToListAsync(ct)).Select(Mappers.ToModel).ToList();
    }

    public async Task<IReadOnlyList<CategoryCount>> GetChannelCategoriesAsync(string playlistId, bool blockAdult, CancellationToken ct = default)
    {
        var query = _db.Channels.AsNoTracking().Where(c => c.PlaylistId == playlistId);
        if (blockAdult) query = query.Where(c => !c.IsAdult);
        var groups = await query.GroupBy(c => c.Group).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        return groups.Select(g => new CategoryCount(g.Key, g.Count)).OrderBy(g => g.Group).ToList();
    }

    public async Task UpsertChannelsAsync(IEnumerable<Channel> channels, CancellationToken ct = default)
    {
        var entities = channels.Select(Mappers.ToEntity).ToList();
        if (entities.Count == 0) return;

        // ExecuteDelete + AddRange is far faster than per-row updates for a sync.
        var playlistIds = entities.Select(c => c.PlaylistId).Distinct().ToArray();
        await _db.Channels.Where(c => playlistIds.Contains(c.PlaylistId)).ExecuteDeleteAsync(ct);
        await _db.Channels.AddRangeAsync(entities, ct);
        await _db.SaveChangesAsync(ct);
    }

    // -------- VOD --------------------------------------------------------

    public async Task<IReadOnlyList<VodItem>> GetVodAsync(string playlistId, bool blockAdult, CancellationToken ct = default)
    {
        var query = _db.VodItems.AsNoTracking().Where(v => v.PlaylistId == playlistId);
        if (blockAdult) query = query.Where(v => !v.IsAdult);
        return (await query.OrderBy(v => v.Title).ToListAsync(ct)).Select(Mappers.ToModel).ToList();
    }

    public async Task<IReadOnlyList<CategoryCount>> GetVodCategoriesAsync(string playlistId, bool blockAdult, CancellationToken ct = default)
    {
        var query = _db.VodItems.AsNoTracking().Where(v => v.PlaylistId == playlistId);
        if (blockAdult) query = query.Where(v => !v.IsAdult);
        var groups = await query.GroupBy(v => v.Genre).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        return groups.Select(g => new CategoryCount(g.Key, g.Count)).OrderBy(g => g.Group).ToList();
    }

    public async Task UpsertVodAsync(IEnumerable<VodItem> items, CancellationToken ct = default)
    {
        var entities = items.Select(Mappers.ToEntity).ToList();
        if (entities.Count == 0) return;
        var playlistIds = entities.Select(v => v.PlaylistId).Distinct().ToArray();
        await _db.VodItems.Where(v => playlistIds.Contains(v.PlaylistId)).ExecuteDeleteAsync(ct);
        await _db.VodItems.AddRangeAsync(entities, ct);
        await _db.SaveChangesAsync(ct);
    }

    // -------- Series -----------------------------------------------------

    public async Task<IReadOnlyList<Series>> GetSeriesAsync(string playlistId, bool blockAdult, CancellationToken ct = default)
    {
        var query = _db.Series.AsNoTracking().Where(s => s.PlaylistId == playlistId);
        if (blockAdult) query = query.Where(s => !s.IsAdult);
        return (await query.OrderBy(s => s.Title).ToListAsync(ct)).Select(Mappers.ToModel).ToList();
    }

    public async Task UpsertSeriesAsync(IEnumerable<Series> items, CancellationToken ct = default)
    {
        var entities = items.Select(Mappers.ToEntity).ToList();
        if (entities.Count == 0) return;
        var playlistIds = entities.Select(s => s.PlaylistId).Distinct().ToArray();
        await _db.Series.Where(s => playlistIds.Contains(s.PlaylistId)).ExecuteDeleteAsync(ct);
        await _db.Series.AddRangeAsync(entities, ct);
        await _db.SaveChangesAsync(ct);
    }

    // -------- Unified search --------------------------------------------

    public async Task<SearchResults> SearchAsync(string playlistId, string query, bool blockAdult, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return new SearchResults();
        var like = $"%{query}%";

        var channels = await _db.Channels.AsNoTracking()
            .Where(c => c.PlaylistId == playlistId && (!blockAdult || !c.IsAdult) && EF.Functions.Like(c.Name, like))
            .Take(100).ToListAsync(ct);
        var vod = await _db.VodItems.AsNoTracking()
            .Where(v => v.PlaylistId == playlistId && (!blockAdult || !v.IsAdult) && EF.Functions.Like(v.Title, like))
            .Take(100).ToListAsync(ct);
        var series = await _db.Series.AsNoTracking()
            .Where(s => s.PlaylistId == playlistId && (!blockAdult || !s.IsAdult) && EF.Functions.Like(s.Title, like))
            .Take(100).ToListAsync(ct);

        return new SearchResults
        {
            Channels = channels.Select(Mappers.ToModel).ToList(),
            Vod = vod.Select(Mappers.ToModel).ToList(),
            Series = series.Select(Mappers.ToModel).ToList(),
        };
    }
}
