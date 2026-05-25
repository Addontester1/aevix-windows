using System.Collections.ObjectModel;
using Aevix.Core.Models;
using Aevix.Data.Dao;
using Aevix.Player;
using Aevix_App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using LibVLCSharp.Shared;

namespace Aevix_App.ViewModels;

/// <summary>
/// One playable cell in the multi-screen grid. Owns its own
/// <see cref="MediaPlayer"/> minted from the shared libVLC, and the
/// currently-assigned channel (or null if empty).
/// </summary>
public sealed partial class MultiScreenCell : ObservableObject, IDisposable
{
    public int SlotIndex { get; }
    public MediaPlayer MediaPlayer { get; }

    [ObservableProperty] private Channel? _channel;
    [ObservableProperty] private bool _isMuted = true;

    public MultiScreenCell(int slotIndex, MediaPlayer mp)
    {
        SlotIndex = slotIndex;
        MediaPlayer = mp;
        mp.Mute = true; // multi-screen is muted-by-default; user un-mutes one cell at a time
    }

    public string DisplayTitle => Channel?.Name ?? $"Cell {SlotIndex + 1} — click to pick a channel";

    public void Dispose()
    {
        try { MediaPlayer.Stop(); } catch { /* ignore */ }
        MediaPlayer.Dispose();
    }
}

/// <summary>
/// State for the multi-screen page. Manages a list of <see cref="MultiScreenCell"/>
/// keyed to the chosen layout (2 / 3 / 4 cells). Channel picker is exposed
/// as an in-memory list the page binds to.
/// </summary>
public sealed partial class MultiScreenViewModel : ObservableObject, IDisposable
{
    private readonly PlaylistRepository _playlists;
    private readonly ContentRepository _content;
    private readonly SettingsRepository _settings;
    private readonly AevixPlayer _player;
    private readonly ParentalGate _gate;

    public ObservableCollection<MultiScreenCell> Cells { get; } = new();
    public ObservableCollection<CategoryCount> PickerCategories { get; } = new();
    public ObservableCollection<Channel> PickerChannels { get; } = new();

    /// <summary>0 = picker, 2/3/4 = cell count.</summary>
    [ObservableProperty] private int _layout;
    [ObservableProperty] private string _statusText = "Pick a layout to begin.";
    [ObservableProperty] private int _activeSlot = -1;
    [ObservableProperty] private CategoryCount? _pickerSelectedCategory;
    [ObservableProperty] private bool _isPickerOpen;

    public MultiScreenViewModel(
        PlaylistRepository playlists,
        ContentRepository content,
        SettingsRepository settings,
        AevixPlayer player,
        ParentalGate gate)
    {
        _playlists = playlists;
        _content = content;
        _settings = settings;
        _player = player;
        _gate = gate;
    }

    public bool HasLayout => Layout > 0;

    /// <summary>Switch to a new cell count. Re-creates cells from scratch.</summary>
    public async Task SetLayoutAsync(int count, CancellationToken ct = default)
    {
        if (count < 2 || count > 4) return;
        DisposeCells();
        Cells.Clear();

        await _player.InitializeAsync();
        for (var i = 0; i < count; i++)
        {
            Cells.Add(new MultiScreenCell(i, _player.CreateAdditionalMediaPlayer()));
        }
        Layout = count;
        StatusText = $"{count}-cell layout — click any cell to pick a channel.";
        await LoadPickerCategoriesAsync(ct);
    }

    /// <summary>Reset back to the layout picker.</summary>
    public void ClearLayout()
    {
        DisposeCells();
        Cells.Clear();
        Layout = 0;
        StatusText = "Pick a layout to begin.";
    }

    /// <summary>Open the channel picker targeting a specific slot.</summary>
    public void OpenPicker(int slotIndex)
    {
        ActiveSlot = slotIndex;
        IsPickerOpen = true;
    }

    public void ClosePicker()
    {
        IsPickerOpen = false;
        ActiveSlot = -1;
    }

    public async Task AssignChannelAsync(int slotIndex, Channel channel, CancellationToken ct = default)
    {
        if (slotIndex < 0 || slotIndex >= Cells.Count) return;
        var cell = Cells[slotIndex];
        cell.Channel = channel;
        try
        {
            _player.PlayOn(cell.MediaPlayer, channel.StreamUri);
        }
        catch (Exception ex)
        {
            StatusText = $"Cell {slotIndex + 1} failed to start: {ex.Message}";
        }
        ClosePicker();
        await Task.CompletedTask;
    }

    public void ToggleMute(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Cells.Count) return;
        var cell = Cells[slotIndex];
        cell.IsMuted = !cell.IsMuted;
        cell.MediaPlayer.Mute = cell.IsMuted;
    }

    public void RemoveChannel(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Cells.Count) return;
        var cell = Cells[slotIndex];
        try { cell.MediaPlayer.Stop(); } catch { /* ignore */ }
        cell.Channel = null;
    }

    private async Task LoadPickerCategoriesAsync(CancellationToken ct)
    {
        PickerCategories.Clear();
        PickerChannels.Clear();
        var active = await _playlists.GetActiveAsync(ct);
        if (active is null)
        {
            StatusText = "No active playlist — add one first.";
            return;
        }
        var settings = await _settings.GetAsync(ct);
        var hide = _gate.ShouldHideAdultContent(settings.AdultContentBlocked);
        foreach (var c in await _content.GetChannelCategoriesAsync(active.Id, hide, ct))
        {
            PickerCategories.Add(c);
        }
    }

    partial void OnPickerSelectedCategoryChanged(CategoryCount? value) => _ = ReloadPickerChannelsAsync(value);

    private async Task ReloadPickerChannelsAsync(CategoryCount? cat)
    {
        PickerChannels.Clear();
        if (cat is null) return;
        var active = await _playlists.GetActiveAsync();
        if (active is null) return;
        var settings = await _settings.GetAsync();
        var hide = _gate.ShouldHideAdultContent(settings.AdultContentBlocked);
        foreach (var ch in (await _content.GetChannelsAsync(active.Id, hide))
                     .Where(c => string.Equals(c.Group, cat.Group, StringComparison.OrdinalIgnoreCase)))
        {
            PickerChannels.Add(ch);
        }
    }

    private void DisposeCells()
    {
        foreach (var c in Cells) c.Dispose();
    }

    public void Dispose() => DisposeCells();
}
