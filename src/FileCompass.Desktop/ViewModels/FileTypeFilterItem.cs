using CommunityToolkit.Mvvm.ComponentModel;

namespace FileCompass.Desktop.ViewModels;

public partial class FileTypeFilterItem : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected = true;

    public required string Category { get; init; }
    public required string DisplayName { get; init; }

    public Action? FilterChanged { get; set; }

    partial void OnIsSelectedChanged(bool value)
    {
        FilterChanged?.Invoke();
    }
}
