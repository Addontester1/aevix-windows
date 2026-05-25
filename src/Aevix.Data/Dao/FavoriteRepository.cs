using Aevix.Core.Models;
using Aevix.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aevix.Data.Dao;

/// <summary>
/// Tiny join table — we only persist (ItemType, ItemId, AddedTimestamp).
/// The UI resolves the actual title/poster by re-querying the source table.
/// </summary>
public sealed class FavoriteRepository
{
    private readonly AevixDbContext _db;
    public FavoriteRepository(AevixDbContext db) => _db = db;

    public async Task<bool> IsFavoriteAsync(ContentType type, string itemId, CancellationToken ct = default)
    {
        var t = type.ToString();
        return await _db.Favorites.AsNoTracking().AnyAsync(f => f.ItemType == t && f.ItemId == itemId, ct);
    }

    public async Task AddAsync(ContentType type, string itemId, CancellationToken ct = default)
    {
        var t = type.ToString();
        if (await _db.Favorites.AnyAsync(f => f.ItemType == t && f.ItemId == itemId, ct)) return;
        await _db.Favorites.AddAsync(new FavoriteEntity
        {
            ItemType = t,
            ItemId = itemId,
            AddedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        }, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(ContentType type, string itemId, CancellationToken ct = default)
    {
        var t = type.ToString();
        await _db.Favorites.Where(f => f.ItemType == t && f.ItemId == itemId).ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlyList<(ContentType Type, string ItemId, long AddedAt)>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await _db.Favorites.AsNoTracking()
            .OrderByDescending(f => f.AddedTimestamp).ToListAsync(ct);
        return rows.Select(r =>
                (Enum.TryParse<ContentType>(r.ItemType, ignoreCase: true, out var t) ? t : ContentType.Vod,
                 r.ItemId, r.AddedTimestamp))
            .ToList();
    }
}
