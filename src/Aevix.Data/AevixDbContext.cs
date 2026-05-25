using Aevix.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aevix.Data;

/// <summary>
/// The single SQLite-backed EF Core context that holds every Aevix table.
/// One context per request, so we register it as Transient in the
/// composition root. The DB file lives at
/// <c>%LOCALAPPDATA%\Aevix\aevix.db</c>.
/// </summary>
public sealed class AevixDbContext : DbContext
{
    public DbSet<PlaylistEntity> Playlists => Set<PlaylistEntity>();
    public DbSet<ChannelEntity> Channels => Set<ChannelEntity>();
    public DbSet<VodItemEntity> VodItems => Set<VodItemEntity>();
    public DbSet<SeriesEntity> Series => Set<SeriesEntity>();
    public DbSet<SeasonEntity> Seasons => Set<SeasonEntity>();
    public DbSet<EpisodeEntity> Episodes => Set<EpisodeEntity>();
    public DbSet<EpgProgramEntity> EpgPrograms => Set<EpgProgramEntity>();
    public DbSet<EpgSourceEntity> EpgSources => Set<EpgSourceEntity>();
    public DbSet<FavoriteEntity> Favorites => Set<FavoriteEntity>();
    public DbSet<PlaybackProgressEntity> PlaybackProgress => Set<PlaybackProgressEntity>();
    public DbSet<UserSettingsEntity> UserSettings => Set<UserSettingsEntity>();

    public AevixDbContext(DbContextOptions<AevixDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Hot lookups for sync + browse screens.
        mb.Entity<ChannelEntity>().HasIndex(c => new { c.PlaylistId, c.Group });
        mb.Entity<ChannelEntity>().HasIndex(c => c.Name);
        mb.Entity<VodItemEntity>().HasIndex(v => new { v.PlaylistId, v.Genre });
        mb.Entity<VodItemEntity>().HasIndex(v => v.Title);
        mb.Entity<SeriesEntity>().HasIndex(s => new { s.PlaylistId, s.Genre });
        mb.Entity<SeriesEntity>().HasIndex(s => s.Title);
        mb.Entity<SeasonEntity>().HasIndex(s => s.SeriesId);
        mb.Entity<EpisodeEntity>().HasIndex(e => new { e.SeriesId, e.SeasonNumber, e.EpisodeNumber });
        mb.Entity<EpgProgramEntity>().HasIndex(p => new { p.ChannelId, p.StartTimestamp });
        mb.Entity<FavoriteEntity>().HasIndex(f => new { f.ItemType, f.ItemId }).IsUnique();

        base.OnModelCreating(mb);
    }
}
