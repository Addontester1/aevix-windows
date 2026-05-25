using Aevix.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Aevix_App.Models;

/// <summary>
/// Thin presentation wrapper around <see cref="Playlist"/> that adds the
/// derived properties XAML needs (active indicator, relative sync time,
/// error visibility). Lives in the App layer so the Core model stays
/// UI-agnostic.
/// </summary>
public sealed class PlaylistView
{
    public Playlist Source { get; }

    public PlaylistView(Playlist source) => Source = source;

    public string Id => Source.Id;
    public string Name => Source.Name;
    public PlaylistType Type => Source.Type;
    public string Url => Source.Url;
    public int ChannelCount => Source.ChannelCount;
    public int VodCount => Source.VodCount;
    public int SeriesCount => Source.SeriesCount;
    public string? LastError => Source.LastError;

    /// <summary>Coral fill if active, transparent otherwise — bound by XAML for the leading dot.</summary>
    public Brush ActiveFill =>
        Source.IsActive
            ? (Application.Current.Resources["AevixCoralBrush"] as Brush ?? new SolidColorBrush(Colors.Coral))
            : new SolidColorBrush(Colors.Transparent);

    public string LastSyncText
    {
        get
        {
            if (Source.LastSyncTimestamp == 0) return "Never synced";
            var when = DateTimeOffset.FromUnixTimeMilliseconds(Source.LastSyncTimestamp).LocalDateTime;
            var delta = DateTime.Now - when;
            if (delta < TimeSpan.FromMinutes(1)) return "Synced just now";
            if (delta < TimeSpan.FromHours(1))   return $"Synced {(int)delta.TotalMinutes}m ago";
            if (delta < TimeSpan.FromDays(1))    return $"Synced {(int)delta.TotalHours}h ago";
            if (delta < TimeSpan.FromDays(7))    return $"Synced {(int)delta.TotalDays}d ago";
            return $"Synced {when:MMM d}";
        }
    }

    public Visibility HasError =>
        string.IsNullOrWhiteSpace(Source.LastError) ? Visibility.Collapsed : Visibility.Visible;
}
