using Aevix.Core.Models;
using Aevix.Data.Dao;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aevix_App.ViewModels;

/// <summary>Add / edit a single playlist. The page sets <see cref="Type"/> via a combo box.</summary>
public sealed partial class PlaylistFormViewModel : ObservableObject
{
    private readonly PlaylistRepository _playlists;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _url = string.Empty;
    [ObservableProperty] private string? _username;
    [ObservableProperty] private string? _password;
    [ObservableProperty] private string? _macAddress;
    [ObservableProperty] private PlaylistType _type = PlaylistType.M3UUrl;
    [ObservableProperty] private string _statusText = string.Empty;

    public PlaylistFormViewModel(PlaylistRepository playlists) => _playlists = playlists;

    public bool CanSave =>
        !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Url);

    public async Task<Playlist?> SaveAsync(CancellationToken ct = default)
    {
        if (!CanSave) { StatusText = "Name and URL required."; return null; }
        var p = new Playlist(
            Id: Guid.NewGuid().ToString("n"),
            Name: Name.Trim(),
            Type: Type,
            Url: Url.Trim(),
            Username: string.IsNullOrWhiteSpace(Username) ? null : Username,
            Password: string.IsNullOrWhiteSpace(Password) ? null : Password,
            MacAddress: string.IsNullOrWhiteSpace(MacAddress) ? null : MacAddress);
        await _playlists.UpsertAsync(p, ct);

        // First-ever playlist becomes the active one so the rest of the UI lights up.
        var all = await _playlists.GetAllAsync(ct);
        if (all.Count == 1) await _playlists.SetActiveAsync(p.Id, ct);

        StatusText = "Saved.";
        return p;
    }
}
