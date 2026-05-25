using Aevix.Core.Models;
using Aevix.Data.Dao;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aevix_App.ViewModels;

/// <summary>
/// Add / edit a single playlist. The page sets <see cref="Type"/> via a
/// combo box and the visible fields adapt to the type (URL only for
/// M3U URL, user/pass for Xtream, MAC for Stalker, file path for M3U file).
///
/// In edit mode (<see cref="EditingId"/> set), <see cref="SaveAsync"/>
/// updates the existing row instead of inserting a new one and preserves
/// the active flag + sync counters.
/// </summary>
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
    [ObservableProperty] private string _formTitle = "Add playlist";

    /// <summary>When set, <see cref="SaveAsync"/> updates this row instead of inserting.</summary>
    [ObservableProperty] private string? _editingId;

    public PlaylistFormViewModel(PlaylistRepository playlists) => _playlists = playlists;

    public bool CanSave =>
        !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Url);

    /// <summary>Populate the form for editing an existing playlist. Returns true on success.</summary>
    public async Task<bool> LoadForEditAsync(string playlistId, CancellationToken ct = default)
    {
        var p = await _playlists.GetByIdAsync(playlistId, ct);
        if (p is null) { StatusText = "Playlist not found."; return false; }
        EditingId = p.Id;
        FormTitle = $"Edit “{p.Name}”";
        Name = p.Name;
        Url = p.Url;
        Username = p.Username;
        Password = p.Password;
        MacAddress = p.MacAddress;
        Type = p.Type;
        return true;
    }

    public async Task<Playlist?> SaveAsync(CancellationToken ct = default)
    {
        Aevix_App.App.LogInfo("PlaylistFormVM.Save",
            $"type={Type} name='{Name}' urlSet={!string.IsNullOrWhiteSpace(Url)} userSet={!string.IsNullOrWhiteSpace(Username)} passSet={!string.IsNullOrWhiteSpace(Password)} macSet={!string.IsNullOrWhiteSpace(MacAddress)} editing={(EditingId ?? "no")}");

        if (!CanSave) { StatusText = "Name and URL are required."; return null; }

        // Validate per-type requirements so users see the issue here, not
        // mid-sync. Same checks the Android app enforces in its form VM.
        switch (Type)
        {
            case PlaylistType.Xtream when string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password):
                StatusText = "Xtream requires username and password.";
                return null;
            case PlaylistType.Stalker when string.IsNullOrWhiteSpace(MacAddress):
                StatusText = "Stalker requires a MAC address (e.g. 00:1A:79:AA:BB:CC).";
                return null;
        }

        if (EditingId is null)
        {
            // Create
            var p = new Playlist(
                Id: Guid.NewGuid().ToString("n"),
                Name: Name.Trim(),
                Type: Type,
                Url: Url.Trim(),
                Username: NullIfBlank(Username),
                Password: NullIfBlank(Password),
                MacAddress: NullIfBlank(MacAddress));
            await _playlists.UpsertAsync(p, ct);
            var all = await _playlists.GetAllAsync(ct);
            if (all.Count == 1) await _playlists.SetActiveAsync(p.Id, ct);
            StatusText = "Created.";
            return p;
        }
        else
        {
            // Edit — keep existing IsActive + counters + timestamps so the
            // user doesn't lose sync state by renaming.
            var existing = await _playlists.GetByIdAsync(EditingId, ct);
            if (existing is null) { StatusText = "Playlist disappeared."; return null; }
            var updated = existing with
            {
                Name = Name.Trim(),
                Type = Type,
                Url = Url.Trim(),
                Username = NullIfBlank(Username),
                Password = NullIfBlank(Password),
                MacAddress = NullIfBlank(MacAddress),
                LastError = null, // clear stale errors so the next sync starts fresh
            };
            await _playlists.UpsertAsync(updated, ct);
            StatusText = "Saved.";
            return updated;
        }
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
