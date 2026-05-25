using Aevix.Core.Models;
using Aevix.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aevix.Data.Dao;

/// <summary>
/// Read/write access for playlists. "Active" is enforced as a single-row
/// invariant via <see cref="SetActiveAsync"/> — picking a new active row
/// atomically deactivates the previous one.
/// </summary>
public sealed class PlaylistRepository
{
    private readonly AevixDbContext _db;
    public PlaylistRepository(AevixDbContext db) => _db = db;

    public async Task<IReadOnlyList<Playlist>> GetAllAsync(CancellationToken ct = default)
        => (await _db.Playlists.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct))
            .Select(Mappers.ToModel).ToList();

    public async Task<Playlist?> GetActiveAsync(CancellationToken ct = default)
    {
        var entity = await _db.Playlists.AsNoTracking().FirstOrDefaultAsync(p => p.IsActive, ct);
        return entity?.ToModel();
    }

    public async Task<Playlist?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Playlists.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return entity?.ToModel();
    }

    public async Task UpsertAsync(Playlist playlist, CancellationToken ct = default)
    {
        var existing = await _db.Playlists.FirstOrDefaultAsync(p => p.Id == playlist.Id, ct);
        if (existing is null)
        {
            await _db.Playlists.AddAsync(playlist.ToEntity(), ct);
        }
        else
        {
            _db.Entry(existing).CurrentValues.SetValues(playlist.ToEntity());
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await _db.Playlists.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return;
        _db.Playlists.Remove(entity);
        // Cascade-clean the dependent rows so we don't leave orphaned channels.
        await _db.Channels.Where(c => c.PlaylistId == id).ExecuteDeleteAsync(ct);
        await _db.VodItems.Where(v => v.PlaylistId == id).ExecuteDeleteAsync(ct);
        await _db.Series.Where(s => s.PlaylistId == id).ExecuteDeleteAsync(ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetActiveAsync(string id, CancellationToken ct = default)
    {
        await _db.Playlists.Where(p => p.IsActive).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false), ct);
        await _db.Playlists.Where(p => p.Id == id).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, true), ct);
    }
}
