using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Aevix_App.Converters;

/// <summary>
/// Binding-safe string → <see cref="BitmapImage"/> converter.
///
/// WinUI's default auto-conversion for <c>Image.Source = "..."</c> blows
/// up with <c>ArgumentException("The parameter is incorrect.")</c> when the
/// string isn't a valid absolute URI (spaces, missing scheme, encoded
/// junk). Some IPTV providers ship logo URLs that don't survive URI
/// parsing, and one bad row would kill the whole page binding pass.
///
/// This converter tries the parse + returns null on failure so the Image
/// just shows blank instead of crashing.
/// </summary>
public sealed class SafeImageSourceConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim();
        if (!Uri.TryCreate(s, UriKind.Absolute, out var uri)) return null;
        try
        {
            return new BitmapImage(uri);
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
