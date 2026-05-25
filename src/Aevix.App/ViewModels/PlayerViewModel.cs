using Aevix.Player;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aevix_App.ViewModels;

/// <summary>
/// Wraps <see cref="AevixPlayer"/> for the player page. The actual surface
/// attachment happens in code-behind on the page because it needs the HWND
/// from the WinUI Window — viewmodel stays UI-agnostic.
/// </summary>
public sealed partial class PlayerViewModel : ObservableObject
{
    public AevixPlayer Player { get; }

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _streamUrl = string.Empty;
    [ObservableProperty] private string _statusText = "Idle";

    public PlayerViewModel(AevixPlayer player) => Player = player;

    public async Task PlayAsync(string title, string url)
    {
        Title = title;
        StreamUrl = url;
        StatusText = "Loading…";
        await Player.InitializeAsync();
        Player.Play(url);
        StatusText = "Playing";
    }
}
