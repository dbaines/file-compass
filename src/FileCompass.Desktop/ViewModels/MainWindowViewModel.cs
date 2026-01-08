using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileCompass.Core.Constants;
using FileCompass.Core.Models;
using FileCompass.Core.Services;
using FileCompass.Desktop.Services;
using FileCompass.Translations;

namespace FileCompass.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private bool _disposed;
    [ObservableProperty]
    private string _windowTitle = AppConstants.AppName;

    [ObservableProperty]
    private ObservableCollection<Location> _locations = [];

    [ObservableProperty]
    private Location? _selectedLocation;

    [ObservableProperty]
    private ObservableCollection<FileEntry> _files = [];

    [ObservableProperty]
    private FileEntry? _selectedFile;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _searchHistory = [];

    [ObservableProperty]
    private string _statusMessage = Strings.StatusReady;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private double _scanProgress;

    [ObservableProperty]
    private string _scanProgressText = string.Empty;

    [ObservableProperty]
    private int _totalFiles;

    [ObservableProperty]
    private int _totalFolders;

    [ObservableProperty]
    private bool _isIconColumnVisible;

    [ObservableProperty]
    private bool _isNameColumnVisible = true;

    [ObservableProperty]
    private bool _isLocationColumnVisible = true;

    [ObservableProperty]
    private bool _isSizeColumnVisible = true;

    [ObservableProperty]
    private bool _isTypeColumnVisible = true;

    [ObservableProperty]
    private bool _isModifiedColumnVisible;

    [ObservableProperty]
    private bool _isPathColumnVisible = true;

    [ObservableProperty]
    private bool _showDirectories;

    [ObservableProperty]
    private ObservableCollection<FileTypeFilterItem> _fileTypeFilters = [];

    public string FileTypeFilterText => GetFileTypeFilterText();

    [ObservableProperty]
    private bool _isLoadingFiles;

    private bool _isShowingSearchResults;

    public bool ShowNoLocationState => SelectedLocation is null && Files.Count == 0 && !_isShowingSearchResults && !IsLoadingFiles;
    public bool ShowNoFilesState => SelectedLocation is not null && Files.Count == 0 && !_isShowingSearchResults && !IsLoadingFiles;
    public bool ShowNoResultsState => Files.Count == 0 && _isShowingSearchResults && !IsLoadingFiles;
    public bool ShowEmptyState => ShowNoLocationState || ShowNoFilesState || ShowNoResultsState;
    public bool ShowFileList => !IsLoadingFiles && !ShowEmptyState;

    public string SearchPlaceholder => SelectedLocation is null
        ? Strings.SearchPlaceholderAll
        : string.Format(CultureInfo.CurrentCulture, Strings.SearchPlaceholderLocation, SelectedLocation.DisplayName);

    private List<FileEntry> _allFiles = [];
    private CancellationTokenSource? _scanCts;
    private CancellationTokenSource? _loadFilesCts;
    private CancellationTokenSource? _filterCts;
    private long? _scanningLocationId;

    public bool IsLocationScanning(long locationId) => _scanningLocationId == locationId;
    public bool CanAddLocation => _scanningLocationId is null;

    public MainWindowViewModel()
    {
        SafeFireAndForget(InitializeAsync());
    }

    /// <summary>
    /// Safely executes an async task, catching and logging any exceptions.
    /// Use for fire-and-forget async operations that shouldn't crash the app.
    /// </summary>
    private static async void SafeFireAndForget(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fire-and-forget async error: {ex}");
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            StatusMessage = Strings.StatusInitializing;
            await ServiceLocator.InitializeAsync();
            await LoadLocationsAsync();
            await LoadSearchHistoryAsync();
            await LoadColumnVisibilityAsync();
            await LoadShowDirectoriesAsync();
            await LoadFileTypeFiltersAsync();
            StatusMessage = Strings.StatusReady;
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.ErrorGeneric, ex.Message);
        }
    }

    private async Task LoadShowDirectoriesAsync()
    {
        var value = await ServiceLocator.SettingsRepository.GetAsync("show_directories");
        ShowDirectories = value?.Equals("True", StringComparison.OrdinalIgnoreCase) == true;
    }

    private async Task LoadColumnVisibilityAsync()
    {
        var visibility = await ServiceLocator.SettingsRepository.GetColumnVisibilityAsync();
        IsIconColumnVisible = visibility.GetValueOrDefault("icon", false);
        IsNameColumnVisible = visibility.GetValueOrDefault("name", true);
        IsSizeColumnVisible = visibility.GetValueOrDefault("size", true);
        IsTypeColumnVisible = visibility.GetValueOrDefault("type", true);
        IsModifiedColumnVisible = visibility.GetValueOrDefault("modified", false);
        IsPathColumnVisible = visibility.GetValueOrDefault("path", true);
    }

    private async Task SaveColumnVisibilityAsync()
    {
        var visibility = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["icon"] = IsIconColumnVisible,
            ["name"] = IsNameColumnVisible,
            ["size"] = IsSizeColumnVisible,
            ["type"] = IsTypeColumnVisible,
            ["modified"] = IsModifiedColumnVisible,
            ["path"] = IsPathColumnVisible
        };
        await ServiceLocator.SettingsRepository.SetColumnVisibilityAsync(visibility);
    }

    partial void OnIsIconColumnVisibleChanged(bool value) => SafeFireAndForget(SaveColumnVisibilityAsync());
    partial void OnIsNameColumnVisibleChanged(bool value) => SafeFireAndForget(SaveColumnVisibilityAsync());
    partial void OnIsSizeColumnVisibleChanged(bool value) => SafeFireAndForget(SaveColumnVisibilityAsync());
    partial void OnIsTypeColumnVisibleChanged(bool value) => SafeFireAndForget(SaveColumnVisibilityAsync());
    partial void OnIsModifiedColumnVisibleChanged(bool value) => SafeFireAndForget(SaveColumnVisibilityAsync());
    partial void OnIsPathColumnVisibleChanged(bool value) => SafeFireAndForget(SaveColumnVisibilityAsync());

    partial void OnIsLoadingFilesChanged(bool value) => NotifyEmptyStateChanged();

    partial void OnShowDirectoriesChanged(bool value)
    {
        SafeFireAndForget(ApplyFiltersAsync());
        SafeFireAndForget(ServiceLocator.SettingsRepository.SetAsync("show_directories", value.ToString()));
    }

    private async Task LoadFileTypeFiltersAsync()
    {
        var categories = new[]
        {
            AppConstants.FileCategories.Documents,
            AppConstants.FileCategories.Images,
            AppConstants.FileCategories.Videos,
            AppConstants.FileCategories.Audio,
            AppConstants.FileCategories.Archives,
            AppConstants.FileCategories.Code,
            AppConstants.FileCategories.Executables,
            AppConstants.FileCategories.Other
        };

        var savedFilters = await LoadSavedFileTypeFiltersAsync();

        foreach (var category in categories)
        {
            var item = new FileTypeFilterItem
            {
                Category = category,
                DisplayName = category,
                IsSelected = savedFilters?.GetValueOrDefault(category, true) ?? true
            };
            item.FilterChanged = OnFileTypeFilterChanged;
            FileTypeFilters.Add(item);
        }
    }

    private static async Task<Dictionary<string, bool>?> LoadSavedFileTypeFiltersAsync()
    {
        var json = await ServiceLocator.SettingsRepository.GetAsync("file_type_filters");
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
        }
        catch
        {
            return null;
        }
    }

    private void OnFileTypeFilterChanged()
    {
        SafeFireAndForget(ApplyFiltersAsync());
        SafeFireAndForget(SaveFileTypeFiltersAsync());
        OnPropertyChanged(nameof(FileTypeFilterText));
    }

    private async Task SaveFileTypeFiltersAsync()
    {
        var filters = FileTypeFilters.ToDictionary(f => f.Category, f => f.IsSelected, StringComparer.Ordinal);
        var json = JsonSerializer.Serialize(filters);
        await ServiceLocator.SettingsRepository.SetAsync("file_type_filters", json);
    }

    [RelayCommand]
    private void SelectAllFileTypes()
    {
        // Temporarily disable callbacks to batch the update
        foreach (var filter in FileTypeFilters)
            filter.FilterChanged = null;

        foreach (var filter in FileTypeFilters)
            filter.IsSelected = true;

        // Re-enable callbacks and apply filter once
        foreach (var filter in FileTypeFilters)
            filter.FilterChanged = OnFileTypeFilterChanged;

        OnFileTypeFilterChanged();
    }

    [RelayCommand]
    private void DeselectAllFileTypes()
    {
        // Temporarily disable callbacks to batch the update
        foreach (var filter in FileTypeFilters)
            filter.FilterChanged = null;

        foreach (var filter in FileTypeFilters)
            filter.IsSelected = false;

        // Re-enable callbacks and apply filter once
        foreach (var filter in FileTypeFilters)
            filter.FilterChanged = OnFileTypeFilterChanged;

        OnFileTypeFilterChanged();
    }

    private string GetFileTypeFilterText()
    {
        if (FileTypeFilters.Count == 0) return "All types";

        var selectedCount = FileTypeFilters.Count(f => f.IsSelected);
        var totalCount = FileTypeFilters.Count;

        if (selectedCount == 0) return "No types";
        if (selectedCount == totalCount) return "All types";
        if (selectedCount == 1) return FileTypeFilters.First(f => f.IsSelected).DisplayName;
        return $"{selectedCount} types";
    }

    private async Task ApplyFiltersAsync()
    {
        // Cancel any pending filter operation
        _filterCts?.Cancel();
        _filterCts?.Dispose();
        _filterCts = new CancellationTokenSource();
        var cancellationToken = _filterCts.Token;

        if (_allFiles.Count == 0)
        {
            Files = [];
            return;
        }

        var selectedCategories = FileTypeFilters
            .Where(f => f.IsSelected)
            .Select(f => f.Category)
            .ToHashSet(StringComparer.Ordinal);

        try
        {
            var filtered = await Task.Run(() =>
            {
                IEnumerable<FileEntry> result = _allFiles;

                // Apply directory filter
                if (!ShowDirectories)
                {
                    result = result.Where(f => !f.IsDirectory);
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Apply file type filter (directories pass through, files must match category)
                if (selectedCategories.Count > 0 && selectedCategories.Count < FileTypeFilters.Count)
                {
                    result = result.Where(f => f.IsDirectory || selectedCategories.Contains(f.FileCategory));
                }
                else if (selectedCategories.Count == 0)
                {
                    // No types selected - show nothing (except directories if enabled)
                    result = result.Where(f => f.IsDirectory);
                }

                cancellationToken.ThrowIfCancellationRequested();

                return result.ToList();
            }, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            Files = new ObservableCollection<FileEntry>(filtered);
        }
        catch (OperationCanceledException)
        {
            // Filter was cancelled by a newer filter operation - ignore
        }
    }

    private void ApplyFilters()
    {
        if (_allFiles.Count == 0)
        {
            Files = [];
            return;
        }

        var selectedCategories = FileTypeFilters
            .Where(f => f.IsSelected)
            .Select(f => f.Category)
            .ToHashSet(StringComparer.Ordinal);

        IEnumerable<FileEntry> result = _allFiles;

        if (!ShowDirectories)
        {
            result = result.Where(f => !f.IsDirectory);
        }

        if (selectedCategories.Count > 0 && selectedCategories.Count < FileTypeFilters.Count)
        {
            result = result.Where(f => f.IsDirectory || selectedCategories.Contains(f.FileCategory));
        }
        else if (selectedCategories.Count == 0)
        {
            result = result.Where(f => f.IsDirectory);
        }

        Files = new ObservableCollection<FileEntry>(result.ToList());
    }

    private async Task LoadSearchHistoryAsync()
    {
        var history = await ServiceLocator.SettingsRepository.GetSearchHistoryAsync();
        SearchHistory = new ObservableCollection<string>(history);
    }

    public async Task LoadLocationsAsync()
    {
        var locations = await ServiceLocator.LocationRepository.GetAllAsync();

        // Load tags for each location
        foreach (var location in locations)
        {
            var tags = await ServiceLocator.LocationTagRepository.GetTagsForLocationAsync(location.Id);
            location.Tags = tags.ToList();
        }

        Locations = new ObservableCollection<Location>(locations);
    }

    partial void OnSelectedLocationChanged(Location? value)
    {
        OnPropertyChanged(nameof(SearchPlaceholder));

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            SafeFireAndForget(SearchAsync());
        }
        else if (value is not null)
        {
            SafeFireAndForget(LoadFilesForLocationAsync(value));
        }
        else
        {
            _isShowingSearchResults = false;
            _allFiles.Clear();
            Files.Clear();
            StatusMessage = Strings.StatusReady;
        }
        NotifyEmptyStateChanged();
    }

    partial void OnFilesChanged(ObservableCollection<FileEntry> value)
    {
        NotifyEmptyStateChanged();
    }

    private async Task LoadFilesForLocationAsync(Location location)
    {
        _loadFilesCts?.Cancel();
        _loadFilesCts?.Dispose();
        _loadFilesCts = new CancellationTokenSource();
        var cancellationToken = _loadFilesCts.Token;

        try
        {
            _isShowingSearchResults = false;
            IsLoadingFiles = true;
            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.StatusLoading, location.DisplayName);

            var allFiles = await Task.Run(async () =>
            {
                var files = await ServiceLocator.FileRepository.GetByParentAsync(location.Id, null);
                cancellationToken.ThrowIfCancellationRequested();
                return files.ToList();
            }, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            _allFiles = allFiles;
            ApplyFilters();
            TotalFiles = location.TotalFiles;
            TotalFolders = location.TotalFolders;
            IsLocationColumnVisible = false;

            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.StatusLoaded, Files.Count);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.ErrorLoading, ex.Message);
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                IsLoadingFiles = false;
            }
        }
    }

    public async Task AddLocationAsync(string path, string? customName = null, bool forceNew = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (_scanningLocationId is not null)
        {
            StatusMessage = Strings.ErrorScanInProgress;
            return;
        }

        var location = await ServiceLocator.LocationRepository.GetByPathAsync(path);
        if (location is null || forceNew)
        {
            location = new Location
            {
                Path = path,
                CustomName = customName,
                Status = LocationStatus.Scanning,
                CreatedAt = DateTime.UtcNow
            };
            location = await ServiceLocator.LocationRepository.AddAsync(location);
        }
        else if (customName is not null)
        {
            location.CustomName = customName;
            location.Status = LocationStatus.Scanning;
            await ServiceLocator.LocationRepository.UpdateAsync(location);
        }
        else
        {
            location.Status = LocationStatus.Scanning;
            await ServiceLocator.LocationRepository.UpdateAsync(location);
        }

        await LoadLocationsAsync();
        OnPropertyChanged(nameof(CanAddLocation));
        StartBackgroundScan(location.Id, path, customName, forceNew);
    }

    private void StartBackgroundScan(long locationId, string path, string? customName, bool forceNew)
    {
        _scanningLocationId = locationId;
        _scanCts = new CancellationTokenSource();
        IsScanning = true;
        StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.StatusScanning, path);
        OnPropertyChanged(nameof(CanAddLocation));

        var progress = new Progress<ScanProgress>(p =>
        {
            ScanProgressText = p.StatusMessage;
            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.StatusScanProgress, p.CurrentPath);
        });

        SafeFireAndForget(Task.Run(async () =>
        {
            try
            {
                var location = await ServiceLocator.FileScannerService.RescanLocationAsync(
                    locationId,
                    progress,
                    _scanCts.Token);

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await LoadLocationsAsync();
                    StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.StatusScanComplete, location.TotalFiles, location.TotalFolders);
                });
            }
            catch (OperationCanceledException)
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await LoadLocationsAsync();
                    StatusMessage = Strings.StatusScanCancelled;
                });
            }
            catch (Exception ex)
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await LoadLocationsAsync();
                    StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.ErrorScan, ex.Message);
                });
            }
            finally
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _scanningLocationId = null;
                    IsScanning = false;
                    _scanCts?.Dispose();
                    _scanCts = null;
                    OnPropertyChanged(nameof(CanAddLocation));
                });
            }
        }));
    }

    public async Task RescanLocationAsync(long locationId)
    {
        if (_scanningLocationId is not null)
        {
            StatusMessage = Strings.ErrorScanInProgress;
            return;
        }

        var location = await ServiceLocator.LocationRepository.GetByIdAsync(locationId);
        if (location is null)
        {
            StatusMessage = Strings.ErrorLocationNotFound;
            return;
        }

        location.Status = LocationStatus.Scanning;
        await ServiceLocator.LocationRepository.UpdateAsync(location);
        await LoadLocationsAsync();
        StartBackgroundScan(locationId, location.Path, location.CustomName, false);
    }

    public void CancelCurrentScan()
    {
        _scanCts?.Cancel();
    }

    public long? GetScanningLocationId() => _scanningLocationId;

    [RelayCommand]
    private void CancelScan()
    {
        _scanCts?.Cancel();
    }

    [RelayCommand]
    private async Task ClearSearchAsync()
    {
        SearchText = string.Empty;
        _isShowingSearchResults = false;
        if (SelectedLocation is not null)
        {
            await LoadFilesForLocationAsync(SelectedLocation);
        }
        else
        {
            _allFiles.Clear();
            Files.Clear();
            StatusMessage = Strings.StatusReady;
        }
        NotifyEmptyStateChanged();
    }

    private void NotifyEmptyStateChanged()
    {
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(ShowNoLocationState));
        OnPropertyChanged(nameof(ShowNoFilesState));
        OnPropertyChanged(nameof(ShowNoResultsState));
        OnPropertyChanged(nameof(ShowFileList));
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            if (SelectedLocation is not null)
            {
                await LoadFilesForLocationAsync(SelectedLocation);
            }
            IsLocationColumnVisible = SelectedLocation is null;
            return;
        }

        try
        {
            IsLoadingFiles = true;
            var isGlobalSearch = SelectedLocation is null;
            var searchContext = isGlobalSearch ? Strings.AdvancedLocationAll : SelectedLocation!.DisplayName;
            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.StatusSearching, searchContext, SearchText);

            var query = new SearchQuery
            {
                SearchTerm = SearchText,
                LocationId = SelectedLocation?.Id,
                IncludeDirectories = true
            };

            var result = await Task.Run(async () =>
            {
                return await ServiceLocator.FileRepository.SearchAsync(query);
            });

            _isShowingSearchResults = true;
            _allFiles = result.Files.ToList();
            ApplyFilters();
            IsLocationColumnVisible = isGlobalSearch;

            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.StatusFound, Files.Count, result.SearchDuration.TotalMilliseconds);

            await ServiceLocator.SettingsRepository.AddSearchHistoryAsync(SearchText);
            await LoadSearchHistoryAsync();

            NotifyEmptyStateChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.ErrorSearch, ex.Message);
        }
        finally
        {
            IsLoadingFiles = false;
        }
    }

    public void SelectSearchHistory(string searchTerm)
    {
        SearchText = searchTerm;
    }

    public async Task AdvancedSearchAsync(SearchQuery query)
    {
        try
        {
            StatusMessage = Strings.StatusSearchingSimple;
            query.IncludeDirectories = true; // Always fetch directories, filter client-side
            var result = await ServiceLocator.FileRepository.SearchAsync(query);
            _allFiles = result.Files.ToList();
            ApplyFilters();

            var filterDesc = BuildFilterDescription(query);
            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.StatusFoundFiltered, Files.Count, filterDesc, result.SearchDuration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.ErrorSearch, ex.Message);
        }
    }

    private static string BuildFilterDescription(SearchQuery query)
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            filters.Add($"'{query.SearchTerm}'");
        if (!string.IsNullOrWhiteSpace(query.Extension))
            filters.Add($".{query.Extension}");
        if (!string.IsNullOrWhiteSpace(query.FileCategory))
            filters.Add(query.FileCategory);
        if (query.MinSize.HasValue || query.MaxSize.HasValue)
            filters.Add("size filter");
        if (query.ModifiedAfter.HasValue || query.ModifiedBefore.HasValue)
            filters.Add("date filter");

        return filters.Count > 0 ? $" ({string.Join(", ", filters)})" : string.Empty;
    }

    public async Task ExportToCsvAsync(string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath) || Files.Count == 0)
            return;

        try
        {
            StatusMessage = Strings.StatusExportingCsv;
            await ExportService.ExportToCsvAsync(Files, outputPath);
            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.StatusExported, Files.Count, outputPath);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.ErrorExport, ex.Message);
        }
    }

    public async Task ExportToExcelAsync(string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath) || Files.Count == 0)
            return;

        try
        {
            StatusMessage = Strings.StatusExportingExcel;
            await ExportService.ExportToExcelAsync(Files, outputPath);
            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.StatusExportedExcel, Files.Count);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.ErrorExport, ex.Message);
        }
    }

    public async Task ExportToHtmlAsync(string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath) || Files.Count == 0)
            return;

        try
        {
            StatusMessage = Strings.StatusExportingHtml;
            await ExportService.ExportToHtmlAsync(Files, outputPath);
            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.StatusExportedHtml, Files.Count);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.ErrorExport, ex.Message);
        }
    }

    public async Task CreateBackupAsync(string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
            return;

        try
        {
            StatusMessage = Strings.StatusBackupCreating;
            await ServiceLocator.BackupService.CreateBackupAsync(outputPath);
            StatusMessage = Strings.StatusBackupCreated;
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.ErrorBackup, ex.Message);
        }
    }

    public async Task RestoreBackupAsync(string backupPath)
    {
        if (string.IsNullOrWhiteSpace(backupPath))
            return;

        try
        {
            StatusMessage = Strings.StatusBackupRestoring;
            await BackupService.RestoreBackupAsync(backupPath);

            // Reload locations after restore
            await LoadLocationsAsync();
            Files.Clear();
            SelectedLocation = null;
            StatusMessage = Strings.StatusBackupRestored;
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.ErrorRestore, ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteLocationAsync()
    {
        if (SelectedLocation is null)
            return;

        try
        {
            await ServiceLocator.LocationRepository.DeleteAsync(SelectedLocation.Id);
            await LoadLocationsAsync();
            SelectedLocation = null;
            Files.Clear();
            StatusMessage = Strings.StatusLocationRemoved;
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.ErrorGeneric, ex.Message);
        }
    }

    [RelayCommand]
    private async Task RefreshLocationAsync()
    {
        if (SelectedLocation is null)
            return;

        await AddLocationAsync(SelectedLocation.Path);
    }

    public async Task RenameLocationAsync(string newName)
    {
        if (SelectedLocation is null)
            return;

        try
        {
            SelectedLocation.CustomName = string.IsNullOrWhiteSpace(newName) ? null : newName;
            await ServiceLocator.LocationRepository.UpdateAsync(SelectedLocation);
            await LoadLocationsAsync();

            // Re-select the location
            SelectedLocation = Locations.FirstOrDefault(l => l.Id == SelectedLocation.Id);
            StatusMessage = Strings.StatusLocationRenamed;
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(CultureInfo.CurrentCulture, Strings.ErrorGeneric, ex.Message);
        }
    }

    /// <summary>
    /// Disposes of resources held by this ViewModel, including CancellationTokenSource instances.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Cancel any ongoing operations
            _scanCts?.Cancel();
            _loadFilesCts?.Cancel();
            _filterCts?.Cancel();

            // Dispose managed resources
            _scanCts?.Dispose();
            _loadFilesCts?.Dispose();
            _filterCts?.Dispose();

            _scanCts = null;
            _loadFilesCts = null;
            _filterCts = null;
        }

        _disposed = true;
    }
}
