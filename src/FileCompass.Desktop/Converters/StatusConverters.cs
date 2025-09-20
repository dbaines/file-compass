using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using FileCompass.Core.Models;
using FileCompass.Translations;

namespace FileCompass.Desktop.Converters;

public class StatusToColorConverter : IMultiValueConverter
{
    public static readonly StatusToColorConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 0 || values[0] is not LocationStatus status)
            return Brushes.Gray;

        return status switch
        {
            LocationStatus.NeverScanned => Brushes.Gray,
            LocationStatus.Scanning => Brushes.Orange,
            LocationStatus.UpToDate => Brushes.LimeGreen,
            LocationStatus.Outdated => Brushes.Yellow,
            LocationStatus.Offline => Brushes.Red,
            _ => Brushes.Gray
        };
    }
}

public class StatusToTextConverter : IMultiValueConverter
{
    public static readonly StatusToTextConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 0 || values[0] is not LocationStatus status)
            return Strings.LocationStatusUnknown;

        return status switch
        {
            LocationStatus.NeverScanned => Strings.LocationStatusNever,
            LocationStatus.Scanning => Strings.LocationStatusScanning,
            LocationStatus.UpToDate => Strings.LocationStatusCurrent,
            LocationStatus.Outdated => Strings.LocationStatusOutdated,
            LocationStatus.Offline => Strings.LocationStatusInaccessible,
            _ => Strings.LocationStatusUnknown
        };
    }
}

/// <summary>
/// Converts LocationStatus to boolean indicating if currently scanning.
/// One-way converter only (ConvertBack not supported).
/// </summary>
public class IsScanningConverter : IValueConverter
{
    public static readonly IsScanningConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is LocationStatus status && status == LocationStatus.Scanning;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("One-way converter only");
    }
}

/// <summary>
/// Converts LocationStatus to boolean indicating if NOT currently scanning.
/// One-way converter only (ConvertBack not supported).
/// </summary>
public class IsNotScanningConverter : IValueConverter
{
    public static readonly IsNotScanningConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not LocationStatus status || status != LocationStatus.Scanning;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("One-way converter only");
    }
}

/// <summary>
/// Converts LocationStatus to opacity value (0.6 when scanning, 1.0 otherwise).
/// One-way converter only (ConvertBack not supported).
/// </summary>
public class ScanningOpacityConverter : IValueConverter
{
    public static readonly ScanningOpacityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is LocationStatus status && status == LocationStatus.Scanning ? 0.6 : 1.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("One-way converter only");
    }
}
