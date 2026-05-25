using Aevix.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Aevix.Data.Dao;

/// <summary>
/// Persists playback resume points. The primary key is <c>ItemId</c> so a
/// re-watch naturally overwrites the previous entry.
/// </summary>
public sealed class ProgressRepository
{
    private readonly AevixDbContext _db;
    public ProgressRepository(AevixDbContext db) => _db = db;

    public async Task<IReadOnlyList<PlaybackProgress>> GetContinueWatchingAsync(int limit = 20, CancellationToken ct = default)
    {
        var rows = await _db.PlaybackProgress.AsNoTracking()
            .Where(p => p.Percentage > 0.02f && p.Percentage < 0.95f)
            .OrderByDescending(p => p.LastWatchedTimestamp)
            .Take(limit)
            .ToListAsync(ct);
        return rows.Select(Mappers.ToModel).ToList();
    }

    public async Task<PlaybackProgress?> GetAsync(string itemId, CancellationToken ct = default)
    {
        var row = await _db.PlaybackProgress.AsNoTracking().FirstOrDefaultAsync(p => p.ItemId == itemId, ct);
        return row?.ToModel();
    }

    public async Task UpsertAsync(PlaybackProgress progress, CancellationToken ct = default)
    {
        var existing = await _db.PlaybackProgress.FirstOrDefaultAsync(p => p.ItemId == progress.ItemId, ct);
        if (existing is null)
        {
            await _db.PlaybackProgress.AddAsync(progress.ToEntity(), ct);
        }
        else
        {
            _db.Entry(existing).CurrentValues.SetValues(progress.ToEntity());
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(string itemId, CancellationToken ct = default)
    {
        await _db.PlaybackProgress.Where(p => p.ItemId == itemId).ExecuteDeleteAsync(ct);
    }
}
