using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FileCompass.Desktop.Converters;

/// <summary>
/// Converts a hex color string to a contrasting text color (white or black).
/// Uses luminance calculation to determine the best contrast.
/// </summary>
public class ContrastTextColorConverter : IValueConverter
{
    public static readonly ContrastTextColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrEmpty(hex))
        {
            try
            {
                var color = Color.Parse(hex);
                // Calculate relative luminance using sRGB formula
                var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
                // Use white text for dark backgrounds, black for light backgrounds
                return luminance > 0.5 ? Colors.Black : Colors.White;
            }
            catch
            {
                return Colors.Black;
            }
        }
        return Colors.Black;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
