using Avalonia.Data.Converters;
using Avalonia.Media;
using IconPacks.Avalonia.Material;
using System.Globalization;

namespace FileCompass.Desktop.Converters;

/// <summary>
/// Converts file category and IsDirectory to a Material icon kind.
/// </summary>
public class FileIconKindConverter : IMultiValueConverter
{
    public static readonly FileIconKindConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return PackIconMaterialKind.File;

        var isDirectory = values[0] as bool? ?? false;
        var category = values[1] as string ?? "Other";

        if (isDirectory)
            return PackIconMaterialKind.Folder;

        return category switch
        {
            "Images" => PackIconMaterialKind.FileImage,
            "Videos" => PackIconMaterialKind.FileVideo,
            "Documents" => PackIconMaterialKind.FileDocument,
            "Audio" => PackIconMaterialKind.MusicNote,
            "Code" => PackIconMaterialKind.FileCode,
            "Archives" => PackIconMaterialKind.FolderZip,
            "Executables" => PackIconMaterialKind.Application,
            _ => PackIconMaterialKind.File
        };
    }
}

/// <summary>
/// Converts file category and IsDirectory to a colored brush for the icon.
/// </summary>
public class FileIconColorConverter : IMultiValueConverter
{
    public static readonly FileIconColorConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return Brushes.Gray;

        var isDirectory = values[0] as bool? ?? false;
        var category = values[1] as string ?? "Other";

        if (isDirectory)
            return new SolidColorBrush(Color.Parse("#FFC107")); // Yellow for folders

        return category switch
        {
            "Documents" => new SolidColorBrush(Color.Parse("#2196F3")), // Blue
            "Images" => new SolidColorBrush(Color.Parse("#4CAF50")),    // Green
            "Videos" => new SolidColorBrush(Color.Parse("#9C27B0")),    // Purple
            "Audio" => new SolidColorBrush(Color.Parse("#FF9800")),     // Orange
            "Archives" => new SolidColorBrush(Color.Parse("#795548")),  // Brown
            "Code" => new SolidColorBrush(Color.Parse("#00BCD4")),      // Cyan
            "Executables" => new SolidColorBrush(Color.Parse("#F44336")), // Red
            _ => new SolidColorBrush(Color.Parse("#9E9E9E"))            // Gray
        };
    }
}
