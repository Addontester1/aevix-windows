using System.Text.Json;
using Aevix.Core.Models;
using Aevix.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aevix.Data.Dao;

/// <summary>
/// Settings are stored as a single JSON blob in row Id=1 so we can add new
/// settings without an EF migration. The JSON-vs-table tradeoff is worth it
/// at this scale — settings are written rarely and read on startup.
/// </summary>
public sealed class SettingsRepository
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    private readonly AevixDbContext _db;
    public SettingsRepository(AevixDbContext db) => _db = db;

    public async Task<UserSettings> GetAsync(CancellationToken ct = default)
    {
        var row = await _db.UserSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1, ct);
        if (row is null || string.IsNullOrWhiteSpace(row.Json) || row.Json == "{}")
        {
            return new UserSettings();
        }
        try
        {
            return JsonSerializer.Deserialize<UserSettings>(row.Json, JsonOpts) ?? new UserSettings();
        }
        catch
        {
            // Corrupt blob — fall back to defaults rather than crash on launch.
            return new UserSettings();
        }
    }

    public async Task SaveAsync(UserSettings settings, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(settings, JsonOpts);
        var row = await _db.UserSettings.FirstOrDefaultAsync(s => s.Id == 1, ct);
        if (row is null)
        {
            await _db.UserSettings.AddAsync(new UserSettingsEntity { Id = 1, Json = json }, ct);
        }
        else
        {
            row.Json = json;
        }
        await _db.SaveChangesAsync(ct);
    }
}
