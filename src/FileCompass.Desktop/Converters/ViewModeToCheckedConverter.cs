using System.Globalization;
using Avalonia.Data.Converters;
using FileCompass.Desktop.ViewModels;

namespace FileCompass.Desktop.Converters;

/// <summary>
/// Converts FileViewMode to a boolean for ToggleButton IsChecked binding.
/// Use the static instances TreeInstance and ListInstance for each button.
/// </summary>
public class ViewModeToCheckedConverter : IValueConverter
{
    public static readonly ViewModeToCheckedConverter TreeInstance = new(FileViewMode.Tree);
    public static readonly ViewModeToCheckedConverter ListInstance = new(FileViewMode.List);

    private readonly FileViewMode _targetMode;

    private ViewModeToCheckedConverter(FileViewMode targetMode)
    {
        _targetMode = targetMode;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is FileViewMode mode && mode == _targetMode;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // One-way converter - commands handle view mode changes
        return Avalonia.Data.BindingOperations.DoNothing;
    }
}
