using Aevix.Core.Models;
using Aevix.Data.Entities;

namespace Aevix.Data.Dao;

/// <summary>
/// Pure conversion between the immutable <c>Core.Models</c> records and the
/// mutable EF entities. Kept in one place so changes to either side fail
/// compilation here first and don't silently desync.
/// </summary>
internal static class Mappers
{
    public static PlaylistEntity ToEntity(this Playlist p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Type = p.Type.ToString(),
        Url = p.Url,
        Username = p.Username,
        Password = p.Password,
        MacAddress = p.MacAddress,
        Icon = p.Icon,
        LastSyncTimestamp = p.LastSyncTimestamp,
        ExpiryTimestamp = p.ExpiryTimestamp,
        ChannelCount = p.ChannelCount,
        VodCount = p.VodCount,
        SeriesCount = p.SeriesCount,
        IsActive = p.IsActive,
        LastError = p.LastError,
    };

    public static Playlist ToModel(this PlaylistEntity e) => new(
        Id: e.Id,
        Name: e.Name,
        Type: Enum.TryParse<PlaylistType>(e.Type, ignoreCase: true, out var t) ? t : PlaylistType.M3UUrl,
        Url: e.Url,
        Username: e.Username,
        Password: e.Password,
        MacAddress: e.MacAddress,
        Icon: e.Icon,
        LastSyncTimestamp: e.LastSyncTimestamp,
        ExpiryTimestamp: e.ExpiryTimestamp,
        ChannelCount: e.ChannelCount,
        VodCount: e.VodCount,
        SeriesCount: e.SeriesCount,
        IsActive: e.IsActive,
        LastError: e.LastError);

    public static ChannelEntity ToEntity(this Channel c) => new()
    {
        Id = c.Id, Name = c.Name, Group = c.Group, StreamUri = c.StreamUri,
        PlaylistId = c.PlaylistId, LogoUrl = c.LogoUrl, TvgId = c.TvgId,
        TvgName = c.TvgName, SortOrder = c.SortOrder, IsAdult = c.IsAdult,
    };

    public static Channel ToModel(this ChannelEntity e) => new(
        Id: e.Id, Name: e.Name, Group: e.Group, StreamUri: e.StreamUri,
        PlaylistId: e.PlaylistId, LogoUrl: e.LogoUrl, TvgId: e.TvgId,
        TvgName: e.TvgName, SortOrder: e.SortOrder, IsAdult: e.IsAdult);

    public static VodItemEntity ToEntity(this VodItem v) => new()
    {
        Id = v.Id, Title = v.Title, StreamUri = v.StreamUri, PlaylistId = v.PlaylistId,
        Genre = v.Genre, Year = v.Year, Description = v.Description, PosterUrl = v.PosterUrl,
        Duration = v.Duration, Rating = v.Rating, TmdbId = v.TmdbId,
        ContainerExtension = v.ContainerExtension, IsAdult = v.IsAdult,
    };

    public static VodItem ToModel(this VodItemEntity e) => new(
        Id: e.Id, Title: e.Title, StreamUri: e.StreamUri, PlaylistId: e.PlaylistId,
        Genre: e.Genre, Year: e.Year, Description: e.Description, PosterUrl: e.PosterUrl,
        Duration: e.Duration, Rating: e.Rating, TmdbId: e.TmdbId,
        ContainerExtension: e.ContainerExtension, IsAdult: e.IsAdult);

    public static SeriesEntity ToEntity(this Series s) => new()
    {
        Id = s.Id, Title = s.Title, PlaylistId = s.PlaylistId, Genre = s.Genre,
        Year = s.Year, Description = s.Description, PosterUrl = s.PosterUrl,
        TmdbId = s.TmdbId, Rating = s.Rating, IsAdult = s.IsAdult,
    };

    public static Series ToModel(this SeriesEntity e) => new(
        Id: e.Id, Title: e.Title, PlaylistId: e.PlaylistId, Genre: e.Genre,
        Year: e.Year, Description: e.Description, PosterUrl: e.PosterUrl,
        TmdbId: e.TmdbId, Rating: e.Rating, IsAdult: e.IsAdult);

    public static EpgProgramEntity ToEntity(this EpgProgram p) => new()
    {
        ChannelId = p.ChannelId, Title = p.Title,
        StartTimestamp = p.StartTimestamp, EndTimestamp = p.EndTimestamp,
        Description = p.Description, Category = p.Category,
    };

    public static EpgProgram ToModel(this EpgProgramEntity e) => new(
        ChannelId: e.ChannelId, Title: e.Title,
        StartTimestamp: e.StartTimestamp, EndTimestamp: e.EndTimestamp,
        Description: e.Description, Category: e.Category);

    public static PlaybackProgressEntity ToEntity(this PlaybackProgress p) => new()
    {
        ItemId = p.ItemId, PositionSec = p.PositionSec, DurationSec = p.DurationSec,
        Percentage = p.Percentage, LastWatchedTimestamp = p.LastWatchedTimestamp,
        ItemType = p.ItemType.ToString(), ItemTitle = p.ItemTitle,
        PosterUrl = p.PosterUrl, StreamUri = p.StreamUri,
    };

    public static PlaybackProgress ToModel(this PlaybackProgressEntity e) => new(
        ItemId: e.ItemId, PositionSec: e.PositionSec, DurationSec: e.DurationSec,
        Percentage: e.Percentage, LastWatchedTimestamp: e.LastWatchedTimestamp,
        ItemType: Enum.TryParse<ContentType>(e.ItemType, ignoreCase: true, out var t) ? t : ContentType.Vod,
        ItemTitle: e.ItemTitle, PosterUrl: e.PosterUrl, StreamUri: e.StreamUri);
}
