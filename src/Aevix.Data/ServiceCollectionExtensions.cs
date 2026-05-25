using Aevix.Data.Dao;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Aevix.Data;

/// <summary>One-call data-layer registration for the App composition root.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="AevixDbContext"/> against a SQLite file at
    /// <paramref name="dbPath"/> and every repository as scoped. Call
    /// <see cref="EnsureCreatedAsync"/> once at startup to materialise the
    /// schema on first launch.
    /// </summary>
    public static IServiceCollection AddAevixData(this IServiceCollection services, string dbPath)
    {
        services.AddDbContext<AevixDbContext>(opts => opts.UseSqlite($"Data Source={dbPath}"));
        services.AddScoped<PlaylistRepository>();
        services.AddScoped<ContentRepository>();
        services.AddScoped<FavoriteRepository>();
        services.AddScoped<ProgressRepository>();
        services.AddScoped<SettingsRepository>();
        return services;
    }

    /// <summary>Materialises the SQLite schema if the DB doesn't exist yet.</summary>
    public static async Task EnsureCreatedAsync(this IServiceProvider sp, CancellationToken ct = default)
    {
        await using var scope = sp.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AevixDbContext>();
        await db.Database.EnsureCreatedAsync(ct);
    }
}
