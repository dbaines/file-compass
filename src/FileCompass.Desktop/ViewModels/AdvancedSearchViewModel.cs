using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileCompass.Core.Constants;
using FileCompass.Core.Models;

namespace FileCompass.Desktop.ViewModels;

public partial class AdvancedSearchViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private string? _selectedExtension;

    [ObservableProperty]
    private string? _selectedCategory;

    [ObservableProperty]
    private long? _minSizeValue;

    [ObservableProperty]
    private string _minSizeUnit = "KB";

    [ObservableProperty]
    private long? _maxSizeValue;

    [ObservableProperty]
    private string _maxSizeUnit = "MB";

    [ObservableProperty]
    private DateTime? _modifiedAfter;

    [ObservableProperty]
    private DateTime? _modifiedBefore;

    [ObservableProperty]
    private bool _includeDirectories;

    [ObservableProperty]
    private Location? _selectedLocation;

    [ObservableProperty]
    private ObservableCollection<Location> _locations = [];

    public ObservableCollection<string> SizeUnits { get; } = ["B", "KB", "MB", "GB"];

    public ObservableCollection<string> FileCategories { get; } =
    [
        string.Empty,
        AppConstants.FileCategories.Documents,
        AppConstants.FileCategories.Images,
        AppConstants.FileCategories.Videos,
        AppConstants.FileCategories.Audio,
        AppConstants.FileCategories.Archives,
        AppConstants.FileCategories.Code,
        AppConstants.FileCategories.Executables
    ];

    public ObservableCollection<string> CommonExtensions { get; } =
    [
        string.Empty,
        "pdf", "doc", "docx", "txt", "xls", "xlsx",
        "jpg", "jpeg", "png", "gif", "bmp", "svg",
        "mp4", "avi", "mkv", "mov",
        "mp3", "wav", "flac",
        "zip", "rar", "7z",
        "cs", "js", "ts", "py", "html", "css", "json"
    ];

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(SearchTerm) ||
        !string.IsNullOrWhiteSpace(SelectedExtension) ||
        !string.IsNullOrWhiteSpace(SelectedCategory) ||
        MinSizeValue.HasValue ||
        MaxSizeValue.HasValue ||
        ModifiedAfter.HasValue ||
        ModifiedBefore.HasValue ||
        SelectedLocation is not null;

    public SearchQuery BuildQuery()
    {
        return new SearchQuery
        {
            SearchTerm = string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm,
            Extension = string.IsNullOrWhiteSpace(SelectedExtension) ? null : SelectedExtension,
            FileCategory = string.IsNullOrWhiteSpace(SelectedCategory) ? null : SelectedCategory,
            MinSize = ConvertToBytes(MinSizeValue, MinSizeUnit),
            MaxSize = ConvertToBytes(MaxSizeValue, MaxSizeUnit),
            ModifiedAfter = ModifiedAfter,
            ModifiedBefore = ModifiedBefore,
            LocationId = SelectedLocation?.Id,
            IncludeDirectories = IncludeDirectories,
            MaxResults = 1000
        };
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchTerm = string.Empty;
        SelectedExtension = null;
        SelectedCategory = null;
        MinSizeValue = null;
        MaxSizeValue = null;
        ModifiedAfter = null;
        ModifiedBefore = null;
        SelectedLocation = null;
        IncludeDirectories = false;
    }

    private static long? ConvertToBytes(long? value, string unit)
    {
        if (!value.HasValue) return null;

        return unit switch
        {
            "B" => value.Value,
            "KB" => value.Value * 1024,
            "MB" => value.Value * 1024 * 1024,
            "GB" => value.Value * 1024 * 1024 * 1024,
            _ => value.Value
        };
    }
}
