using System.ComponentModel;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using FileCompass.Core.Constants;
using FileCompass.Desktop.Services;
using FileCompass.Desktop.ViewModels;
using FileCompass.Translations;

namespace FileCompass.Desktop.Views;

public partial class MainWindow : Window
{
    // Store reference to current ViewModel for proper event handler cleanup
    private MainWindowViewModel? _currentViewModel;

    public MainWindow()
    {
        InitializeComponent();
        KeyDown += OnWindowKeyDown;
        DataContextChanged += OnDataContextChanged;
        Closing += OnWindowClosing;

        // Use AddHandler with Tunnel routing to catch clicks before ListBox selection
        LocationsListBox.AddHandler(Avalonia.Input.InputElement.PointerPressedEvent, OnLocationsListBoxPointerPressed, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Helper method to safely execute async operations in event handlers.
    /// Catches exceptions and displays an error dialog to prevent silent failures.
    /// </summary>
    private async void SafeExecuteAsync(Func<Task> asyncOperation)
    {
        try
        {
            await asyncOperation();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in async operation: {ex}");
            await ShowErrorDialogAsync(Strings.ErrorUnexpectedTitle, ex.Message);
        }
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Unsubscribe from ViewModel events and dispose to prevent memory leaks
        if (_currentViewModel is not null)
        {
            _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _currentViewModel.Dispose();
            _currentViewModel = null;
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from previous ViewModel to prevent memory leaks
        if (_currentViewModel is not null)
        {
            _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (DataContext is MainWindowViewModel vm)
        {
            _currentViewModel = vm;
            vm.PropertyChanged += OnViewModelPropertyChanged;
            // Initialize column visibility after a short delay to ensure columns are created
            Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateAllColumnVisibility(vm), Avalonia.Threading.DispatcherPriority.Loaded);
        }
        else
        {
            _currentViewModel = null;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        var columns = FilesDataGrid.Columns;
        if (columns.Count < 7) return;

        switch (e.PropertyName)
        {
            case nameof(MainWindowViewModel.IsIconColumnVisible):
                columns[0].IsVisible = vm.IsIconColumnVisible;
                IconColumnMenuItem.IsChecked = vm.IsIconColumnVisible;
                break;
            case nameof(MainWindowViewModel.IsNameColumnVisible):
                columns[1].IsVisible = vm.IsNameColumnVisible;
                NameColumnMenuItem.IsChecked = vm.IsNameColumnVisible;
                break;
            case nameof(MainWindowViewModel.IsLocationColumnVisible):
                // Location column is automatic, no menu item
                columns[2].IsVisible = vm.IsLocationColumnVisible;
                break;
            case nameof(MainWindowViewModel.IsSizeColumnVisible):
                columns[3].IsVisible = vm.IsSizeColumnVisible;
                SizeColumnMenuItem.IsChecked = vm.IsSizeColumnVisible;
                break;
            case nameof(MainWindowViewModel.IsTypeColumnVisible):
                columns[4].IsVisible = vm.IsTypeColumnVisible;
                TypeColumnMenuItem.IsChecked = vm.IsTypeColumnVisible;
                break;
            case nameof(MainWindowViewModel.IsModifiedColumnVisible):
                columns[5].IsVisible = vm.IsModifiedColumnVisible;
                ModifiedColumnMenuItem.IsChecked = vm.IsModifiedColumnVisible;
                break;
            case nameof(MainWindowViewModel.IsPathColumnVisible):
                columns[6].IsVisible = vm.IsPathColumnVisible;
                PathColumnMenuItem.IsChecked = vm.IsPathColumnVisible;
                break;
        }
    }

    private void UpdateAllColumnVisibility(MainWindowViewModel vm)
    {
        var columns = FilesDataGrid.Columns;
        if (columns.Count < 7) return;

        columns[0].IsVisible = vm.IsIconColumnVisible;
        IconColumnMenuItem.IsChecked = vm.IsIconColumnVisible;

        columns[1].IsVisible = vm.IsNameColumnVisible;
        NameColumnMenuItem.IsChecked = vm.IsNameColumnVisible;

        // Location column is automatic, no menu item
        columns[2].IsVisible = vm.IsLocationColumnVisible;

        columns[3].IsVisible = vm.IsSizeColumnVisible;
        SizeColumnMenuItem.IsChecked = vm.IsSizeColumnVisible;

        columns[4].IsVisible = vm.IsTypeColumnVisible;
        TypeColumnMenuItem.IsChecked = vm.IsTypeColumnVisible;

        columns[5].IsVisible = vm.IsModifiedColumnVisible;
        ModifiedColumnMenuItem.IsChecked = vm.IsModifiedColumnVisible;

        columns[6].IsVisible = vm.IsPathColumnVisible;
        PathColumnMenuItem.IsChecked = vm.IsPathColumnVisible;
    }

    private void OnColumnVisibilityClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem || DataContext is not MainWindowViewModel vm) return;

        var isChecked = menuItem.IsChecked;

        if (menuItem == IconColumnMenuItem)
            vm.IsIconColumnVisible = isChecked;
        else if (menuItem == NameColumnMenuItem)
            vm.IsNameColumnVisible = isChecked;
        // Location column is automatic, no menu item
        else if (menuItem == SizeColumnMenuItem)
            vm.IsSizeColumnVisible = isChecked;
        else if (menuItem == TypeColumnMenuItem)
            vm.IsTypeColumnVisible = isChecked;
        else if (menuItem == ModifiedColumnMenuItem)
            vm.IsModifiedColumnVisible = isChecked;
        else if (menuItem == PathColumnMenuItem)
            vm.IsPathColumnVisible = isChecked;
    }

    private void OnLocationsListBoxPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        var point = e.GetCurrentPoint(null);

        // Get the source element and walk up to find the ListBoxItem
        var element = e.Source as Avalonia.Visual;
        while (element != null && element is not ListBoxItem)
        {
            element = element.GetVisualParent() as Avalonia.Visual;
        }

        var clickedLocation = (element as ListBoxItem)?.DataContext as FileCompass.Core.Models.Location;

        // Prevent interaction with scanning locations (except right-click for context menu)
        if (clickedLocation?.Status == FileCompass.Core.Models.LocationStatus.Scanning)
        {
            if (point.Properties.IsLeftButtonPressed)
            {
                e.Handled = true; // Block left-click selection
                return;
            }
            // Right-click is allowed for context menu (to cancel scan)
        }

        // For right-click, prevent ListBox from changing selection (let context menu handle it)
        if (point.Properties.IsRightButtonPressed)
        {
            if (clickedLocation is not null && clickedLocation.Status != FileCompass.Core.Models.LocationStatus.Scanning)
            {
                vm.SelectedLocation = clickedLocation;
            }
            e.Handled = true; // Prevent further processing
            return;
        }

        // Left-click: handle deselection when clicking on already-selected item
        if (!point.Properties.IsLeftButtonPressed) return;
        if (vm.SelectedLocation is null) return;

        if (element is ListBoxItem clickedItem && clickedItem.DataContext == vm.SelectedLocation)
        {
            // Clicked on already selected item - deselect it
            // Use Dispatcher to deselect after the ListBox processes the click
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                vm.SelectedLocation = null;
            });
        }
    }

    private void OnWindowKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        // Ctrl+F: Focus search box
        if (e.Key == Avalonia.Input.Key.F && e.KeyModifiers == Avalonia.Input.KeyModifiers.Control)
        {
            SearchBox.Focus();
            e.Handled = true;
        }
        // Ctrl+N: Add location
        else if (e.Key == Avalonia.Input.Key.N && e.KeyModifiers == Avalonia.Input.KeyModifiers.Control)
        {
            OnAddLocationClick(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnSettingsClick(object? sender, RoutedEventArgs e) => SafeExecuteAsync(async () =>
    {
        var dialog = new SettingsWindow();
        await dialog.ShowDialog(this);

        if (DataContext is MainWindowViewModel vm)
        {
            var history = await ServiceLocator.SettingsRepository.GetSearchHistoryAsync();
            vm.SearchHistory = new System.Collections.ObjectModel.ObservableCollection<string>(history);
        }
    });

    private void OnTagsManagementClick(object? sender, RoutedEventArgs e) => SafeExecuteAsync(async () =>
    {
        var vm = new TagManagementViewModel();
        await vm.LoadTagsAsync();
        var dialog = new TagManagementWindow { DataContext = vm };
        await dialog.ShowDialog(this);

        if (DataContext is MainWindowViewModel mainVm)
        {
            await mainVm.LoadLocationsAsync();
        }
    });

    private void OnThemeSystemClick(object? sender, RoutedEventArgs e) =>
        SafeExecuteAsync(() => ServiceLocator.ThemeService.SetThemeAsync(AppTheme.System));

    private void OnThemeLightClick(object? sender, RoutedEventArgs e) =>
        SafeExecuteAsync(() => ServiceLocator.ThemeService.SetThemeAsync(AppTheme.Light));

    private void OnThemeDarkClick(object? sender, RoutedEventArgs e) =>
        SafeExecuteAsync(() => ServiceLocator.ThemeService.SetThemeAsync(AppTheme.Dark));

    private void OnAboutClick(object? sender, RoutedEventArgs e) => SafeExecuteAsync(async () =>
    {
        var githubLink = new Button
        {
            Content = "github.com/dbaines/file-compass",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Padding = new Avalonia.Thickness(0),
            Background = Avalonia.Media.Brushes.Transparent,
            BorderThickness = new Avalonia.Thickness(0),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
        };
        githubLink.Click += (_, _) =>
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/dbaines/file-compass",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch { }
        };

        var dialog = new Window
        {
            Title = Strings.DialogAboutTitle,
            Width = 350,
            Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(24),
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = Strings.AppName,
                        FontSize = 24,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = string.Format(CultureInfo.CurrentCulture, Strings.DialogAboutVersion, AppConstants.AppVersion),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = Strings.DialogAboutDescription,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = Strings.DialogAboutAuthor,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Avalonia.Thickness(0, 12, 0, 0)
                    },
                    githubLink,
                    new TextBlock
                    {
                        Text = Strings.DialogAboutLicense,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        Margin = new Avalonia.Thickness(0, 12, 0, 0)
                    }
                }
            }
        };

        dialog.KeyDown += (s, args) =>
        {
            if (args.Key == Avalonia.Input.Key.Escape || args.Key == Avalonia.Input.Key.Enter)
            {
                dialog.Close();
                args.Handled = true;
            }
        };

        await dialog.ShowDialog(this);
    });

    private void OnAddLocationClick(object? sender, RoutedEventArgs e) => SafeExecuteAsync(async () =>
    {
        var topLevel = GetTopLevel(this);
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = Strings.DialogAddLocationTitle,
            AllowMultiple = false
        });

        if (folders.Count == 0) return;

        var folder = folders[0];
        var path = folder.TryGetLocalPath();
        if (string.IsNullOrEmpty(path))
        {
            await ShowErrorDialogAsync(Strings.DialogAddLocationUnsupportedTitle, Strings.DialogAddLocationUnsupportedMessage);
            return;
        }

        if (DataContext is not MainWindowViewModel vm) return;

        var existingLocations = await ServiceLocator.LocationRepository.GetAllByPathAsync(path);

        // Validate the path is accessible before proceeding
        if (!await ValidateLocationAccessAsync(path))
        {
            return;
        }

        if (existingLocations.Count > 0)
        {
            var result = await ShowLocationConflictDialogAsync(path, existingLocations);
            if (result is null) return;

            if (result.IsNewLocation)
            {
                var promptResult = await ShowNamePromptDialogAsync(path);
                if (promptResult is null) return;

                await vm.AddLocationAsync(path, promptResult.Name, forceNew: true);
                await AssignTagsToNewLocationAsync(path, promptResult.TagIds, vm);
            }
            else if (result.RescanLocationId.HasValue)
            {
                await vm.RescanLocationAsync(result.RescanLocationId.Value);
            }
        }
        else
        {
            var promptResult = await ShowNamePromptDialogAsync(path);
            if (promptResult is null) return;

            await vm.AddLocationAsync(path, promptResult.Name);
            await AssignTagsToNewLocationAsync(path, promptResult.TagIds, vm);
        }
    });

    private static async Task AssignTagsToNewLocationAsync(string path, List<long> tagIds, MainWindowViewModel vm)
    {
        if (tagIds.Count == 0) return;

        // Find the newly created location by path
        var location = await ServiceLocator.LocationRepository.GetByPathAsync(path);
        if (location is not null)
        {
            await ServiceLocator.LocationTagRepository.SetTagsForLocationAsync(location.Id, tagIds);
            await vm.LoadLocationsAsync();
        }
    }

    /// <summary>
    /// Validates that the given path is accessible before attempting to scan.
    /// Shows an error dialog if access is denied or the path doesn't exist.
    /// </summary>
    private async Task<bool> ValidateLocationAccessAsync(string path)
    {
        try
        {
            // Try to enumerate the directory to verify access
            var dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists)
            {
                await ShowErrorDialogAsync(
                    Strings.ErrorLocationNotFoundTitle,
                    Strings.ErrorLocationNotFoundMessage);
                return false;
            }

            // Try to actually read the directory contents - this will throw if access is denied
            _ = dirInfo.EnumerateFileSystemInfos().FirstOrDefault();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            await ShowErrorDialogAsync(
                Strings.ErrorLocationAccessDeniedTitle,
                Strings.ErrorLocationAccessDeniedMessage);
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            await ShowErrorDialogAsync(
                Strings.ErrorLocationNotFoundTitle,
                Strings.ErrorLocationNotFoundMessage);
            return false;
        }
        catch (IOException)
        {
            await ShowErrorDialogAsync(
                Strings.ErrorLocationNotFoundTitle,
                Strings.ErrorLocationNotFoundMessage);
            return false;
        }
    }

    private sealed record LocationConflictResult(bool IsNewLocation, long? RescanLocationId);

    private async Task<LocationConflictResult?> ShowLocationConflictDialogAsync(string path, IReadOnlyList<FileCompass.Core.Models.Location> existingLocations)
    {
        LocationConflictResult? result = null;

        var radioButtons = new StackPanel { Spacing = 8 };

        var newLocationRadio = new RadioButton
        {
            Content = Strings.DialogLocationExistsAddNew,
            GroupName = "LocationChoice",
            IsChecked = true,
            Tag = "new"
        };
        radioButtons.Children.Add(newLocationRadio);

        foreach (var loc in existingLocations)
        {
            var lastScanned = loc.LastScanComplete?.ToString("g", CultureInfo.CurrentCulture) ?? Strings.DialogLocationExistsNeverScanned;
            var rescanRadio = new RadioButton
            {
                Content = string.Format(CultureInfo.CurrentCulture, Strings.DialogLocationExistsRescan, loc.DisplayName, lastScanned),
                GroupName = "LocationChoice",
                Tag = loc.Id
            };
            radioButtons.Children.Add(rescanRadio);
        }

        var scrollableContent = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = string.Format(CultureInfo.CurrentCulture, Strings.DialogLocationExistsMessage, path, existingLocations.Count),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                },
                radioButtons
            }
        };

        var contentScrollViewer = new ScrollViewer
        {
            Content = scrollableContent,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(0, 16, 0, 0),
            Spacing = 8,
            Children =
            {
                new Button { Content = Strings.ButtonCancel, Tag = "cancel" },
                new Button { Content = Strings.ButtonOk, Tag = "ok" }
            }
        };
        DockPanel.SetDock(buttonPanel, Dock.Bottom);

        // Calculate dialog height with max cap
        var calculatedHeight = Math.Min(200 + existingLocations.Count * 30, 450);

        var dialog = new Window
        {
            Title = Strings.DialogLocationExistsTitle,
            Width = 500,
            Height = calculatedHeight,
            MinHeight = 200,
            MaxHeight = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true,
            Content = new DockPanel
            {
                Margin = new Avalonia.Thickness(16),
                Children =
                {
                    buttonPanel,
                    contentScrollViewer
                }
            }
        };

        // Helper to get result from selected radio button
        LocationConflictResult? getResultFromSelection()
        {
            foreach (var rb in radioButtons.Children.OfType<RadioButton>())
            {
                if (rb.IsChecked == true)
                {
                    if (string.Equals(rb.Tag?.ToString(), "new", StringComparison.Ordinal))
                        return new LocationConflictResult(true, null);
                    else if (rb.Tag is long locationId)
                        return new LocationConflictResult(false, locationId);
                }
            }
            return null;
        }

        foreach (var child in buttonPanel.Children)
        {
            if (child is Button button)
            {
                button.Click += (s, args) =>
                {
                    if (string.Equals(button.Tag?.ToString(), "ok", StringComparison.Ordinal))
                        result = getResultFromSelection();
                    dialog.Close();
                };
            }
        }

        dialog.KeyDown += (s, args) =>
        {
            if (args.Key == Avalonia.Input.Key.Escape)
            {
                dialog.Close();
                args.Handled = true;
            }
            else if (args.Key == Avalonia.Input.Key.Enter)
            {
                result = getResultFromSelection();
                dialog.Close();
                args.Handled = true;
            }
        };

        await dialog.ShowDialog(this);
        return result;
    }

    private sealed record NameAndTagsResult(string Name, List<long> TagIds);

    private async Task<NameAndTagsResult?> ShowNamePromptDialogAsync(string path)
    {
        // Default name: last segment of path, or full path for root drives
        var defaultName = System.IO.Path.GetFileName(path);
        if (string.IsNullOrEmpty(defaultName))
            defaultName = path;

        NameAndTagsResult? result = null;

        var textBox = new TextBox
        {
            Text = defaultName,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };

        // Load available tags
        var allTags = await ServiceLocator.TagRepository.GetAllAsync();
        var tagCheckBoxes = new List<(CheckBox cb, long tagId)>();

        // Build scrollable content area
        var scrollableContent = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = Strings.DialogNameLocationLabel },
                textBox,
                new TextBlock
                {
                    Text = path,
                    FontSize = 11,
                    Foreground = Avalonia.Media.Brushes.Gray,
                    Margin = new Avalonia.Thickness(0, 4, 0, 0)
                }
            }
        };

        // Add tag selection if tags exist
        if (allTags.Count > 0)
        {
            var tagsLabel = new TextBlock
            {
                Text = Strings.TagsOptional,
                Margin = new Avalonia.Thickness(0, 12, 0, 4)
            };
            scrollableContent.Children.Add(tagsLabel);

            var tagPanel = new WrapPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
            foreach (var tag in allTags)
            {
                var ellipse = new Avalonia.Controls.Shapes.Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Fill = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(tag.Colour)),
                    Margin = new Avalonia.Thickness(0, 0, 4, 0)
                };

                var cb = new CheckBox
                {
                    Content = new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Children = { ellipse, new TextBlock { Text = tag.Name } }
                    },
                    Margin = new Avalonia.Thickness(0, 0, 12, 4)
                };
                tagCheckBoxes.Add((cb, tag.Id));
                tagPanel.Children.Add(cb);
            }
            scrollableContent.Children.Add(tagPanel);
        }

        var contentScrollViewer = new ScrollViewer
        {
            Content = scrollableContent,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(0, 16, 0, 0),
            Spacing = 8,
            Children =
            {
                new Button { Content = Strings.ButtonCancel, Tag = "cancel" },
                new Button { Content = Strings.ButtonOk, Tag = "ok" }
            }
        };
        DockPanel.SetDock(buttonPanel, Dock.Bottom);

        var dialogHeight = allTags.Count > 0 ? Math.Min(250 + allTags.Count * 5, 400) : 170;
        var dialog = new Window
        {
            Title = Strings.DialogNameLocationTitle,
            Width = 450,
            Height = dialogHeight,
            MinHeight = 170,
            MaxHeight = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true,
            Content = new DockPanel
            {
                Margin = new Avalonia.Thickness(16),
                Children =
                {
                    buttonPanel,
                    contentScrollViewer
                }
            }
        };

        // Helper to get result if valid
        NameAndTagsResult? tryGetResult()
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
                return null;
            var selectedTagIds = tagCheckBoxes
                .Where(x => x.cb.IsChecked == true)
                .Select(x => x.tagId)
                .ToList();
            return new NameAndTagsResult(textBox.Text, selectedTagIds);
        }

        foreach (var child in buttonPanel.Children)
        {
            if (child is Button button)
            {
                button.Click += (s, args) =>
                {
                    if (string.Equals(button.Tag?.ToString(), "ok", StringComparison.Ordinal))
                        result = tryGetResult();
                    dialog.Close();
                };
            }
        }

        dialog.KeyDown += (s, args) =>
        {
            if (args.Key == Avalonia.Input.Key.Escape)
            {
                dialog.Close();
                args.Handled = true;
            }
            else if (args.Key == Avalonia.Input.Key.Enter)
            {
                result = tryGetResult();
                if (result is not null)
                    dialog.Close();
                args.Handled = true;
            }
        };

        await dialog.ShowDialog(this);
        return result;
    }

    private string GenerateExportFilename(string extension)
    {
        var vm = DataContext as MainWindowViewModel;
        var parts = new List<string> { "filecompass" };

        // Location name or "global"
        parts.Add(vm?.SelectedLocation?.DisplayName ?? "global");

        // Search term if searching
        if (!string.IsNullOrWhiteSpace(vm?.SearchText))
        {
            parts.Add(vm.SearchText);
        }

        // Datetime
        parts.Add(DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));

        // Export suffix
        parts.Add("export");

        // Sanitize and join
        var filename = string.Join("-", parts.Select(SanitizeFilename));
        return $"{filename}.{extension}";
    }

    private static string SanitizeFilename(string input)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(input.Select(c => invalid.Contains(c) ? '_' : c));
    }

    private void OnExportCsvClick(object? sender, RoutedEventArgs e) => SafeExecuteAsync(async () =>
    {
        var topLevel = GetTopLevel(this);
        if (topLevel is null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Strings.DialogExportCsvTitle,
            SuggestedFileName = GenerateExportFilename("csv"),
            FileTypeChoices =
            [
                new FilePickerFileType(Strings.FiletypeCsv) { Patterns = ["*.csv"] },
                new FilePickerFileType(Strings.FiletypeAll) { Patterns = ["*"] }
            ]
        });

        if (file is not null && DataContext is MainWindowViewModel vm)
        {
            var path = file.TryGetLocalPath();
            if (string.IsNullOrEmpty(path))
            {
                await ShowErrorDialogAsync(Strings.DialogUnsupportedSaveTitle, Strings.DialogUnsupportedSaveMessage);
                return;
            }
            await vm.ExportToCsvAsync(path);
        }
    });

    private void OnExportExcelClick(object? sender, RoutedEventArgs e) => SafeExecuteAsync(async () =>
    {
        var topLevel = GetTopLevel(this);
        if (topLevel is null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Strings.DialogExportExcelTitle,
            SuggestedFileName = GenerateExportFilename("xlsx"),
            FileTypeChoices =
            [
                new FilePickerFileType(Strings.FiletypeExcel) { Patterns = ["*.xlsx"] },
                new FilePickerFileType(Strings.FiletypeAll) { Patterns = ["*"] }
            ]
        });

        if (file is not null && DataContext is MainWindowViewModel vm)
        {
            var path = file.TryGetLocalPath();
            if (string.IsNullOrEmpty(path))
            {
                await ShowErrorDialogAsync(Strings.DialogUnsupportedSaveTitle, Strings.DialogUnsupportedSaveMessage);
                return;
            }
            await vm.ExportToExcelAsync(path);
        }
    });

    private void OnExportHtmlClick(object? sender, RoutedEventArgs e) => SafeExecuteAsync(async () =>
    {
        var topLevel = GetTopLevel(this);
        if (topLevel is null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Strings.DialogExportHtmlTitle,
            SuggestedFileName = GenerateExportFilename("html"),
            FileTypeChoices =
            [
                new FilePickerFileType(Strings.FiletypeHtml) { Patterns = ["*.html", "*.htm"] },
                new FilePickerFileType(Strings.FiletypeAll) { Patterns = ["*"] }
            ]
        });

        if (file is not null && DataContext is MainWindowViewModel vm)
        {
            var path = file.TryGetLocalPath();
            if (string.IsNullOrEmpty(path))
            {
                await ShowErrorDialogAsync(Strings.DialogUnsupportedSaveTitle, Strings.DialogUnsupportedSaveMessage);
                return;
            }
            await vm.ExportToHtmlAsync(path);
        }
    });

    private void OnBackupClick(object? sender, RoutedEventArgs e) => SafeExecuteAsync(async () =>
    {
        var topLevel = GetTopLevel(this);
        if (topLevel is null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Strings.DialogBackupCreateTitle,
            SuggestedFileName = $"filecompass-backup-{DateTime.Now:yyyyMMdd}.hdi",
            FileTypeChoices =
            [
                new FilePickerFileType(Strings.FiletypeBackup) { Patterns = ["*.hdi"] },
                new FilePickerFileType(Strings.FiletypeAll) { Patterns = ["*"] }
            ]
        });

        if (file is not null && DataContext is MainWindowViewModel vm)
        {
            var path = file.TryGetLocalPath();
            if (string.IsNullOrEmpty(path))
            {
                await ShowErrorDialogAsync(Strings.DialogUnsupportedSaveTitle, Strings.DialogUnsupportedSaveMessage);
                return;
            }
            await vm.CreateBackupAsync(path);
        }
    });

    private void OnRestoreClick(object? sender, RoutedEventArgs e) => SafeExecuteAsync(async () =>
    {
        var topLevel = GetTopLevel(this);
        if (topLevel is null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Strings.DialogBackupRestoreTitle,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType(Strings.FiletypeBackup) { Patterns = ["*.hdi"] },
                new FilePickerFileType(Strings.FiletypeAll) { Patterns = ["*"] }
            ]
        });

        if (files.Count > 0 && DataContext is MainWindowViewModel vm)
        {
            var path = files[0].TryGetLocalPath();
            if (string.IsNullOrEmpty(path))
            {
                await ShowErrorDialogAsync(Strings.DialogUnsupportedSaveTitle, Strings.DialogUnsupportedOpenMessage);
                return;
            }
            await vm.RestoreBackupAsync(path);
        }
    });

    private void OnAdvancedSearchClick(object? sender, RoutedEventArgs e) => SafeExecuteAsync(async () =>
    {
        if (DataContext is not MainWindowViewModel vm) return;

        var searchVm = new AdvancedSearchViewModel
        {
            Locations = vm.Locations,
            SelectedLocation = vm.SelectedLocation
        };

        var dialog = new AdvancedSearchWindow
        {
            DataContext = searchVm
        };

        var result = await dialog.ShowDialog<bool?>(this);

        if (result == true && dialog.Result is not null)
        {
            await vm.AdvancedSearchAsync(dialog.Result);
        }
    });

    private void OnFileDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm &&
            vm.SelectedFile?.IsDirectory == true &&
            vm.ViewMode == FileViewMode.Tree)
        {
            vm.NavigateToFolderCommand.Execute(vm.SelectedFile);
        }
    }

    private void OnRenameLocationClick(object? sender, RoutedEventArgs e) => SafeExecuteAsync(async () =>
    {
        if (DataContext is not MainWindowViewModel vm || vm.SelectedLocation is null) return;

        var location = vm.SelectedLocation;
        var textBox = new TextBox
        {
            Text = location.CustomName ?? location.DisplayName,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };

        var scrollableContent = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = Strings.DialogRenameLocationLabel },
                textBox
            }
        };

        var contentScrollViewer = new ScrollViewer
        {
            Content = scrollableContent,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(0, 16, 0, 0),
            Spacing = 8,
            Children =
            {
                new Button { Content = Strings.ButtonCancel, Tag = "cancel" },
                new Button { Content = Strings.ButtonOk, Tag = "ok" }
            }
        };
        DockPanel.SetDock(buttonPanel, Dock.Bottom);

        var dialog = new Window
        {
            Title = Strings.DialogRenameLocationTitle,
            Width = 400,
            Height = 160,
            MinHeight = 150,
            MaxHeight = 300,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true,
            Content = new DockPanel
            {
                Margin = new Avalonia.Thickness(16),
                Children =
                {
                    buttonPanel,
                    contentScrollViewer
                }
            }
        };

        string? result = null;

        foreach (var child in buttonPanel.Children)
        {
            if (child is Button button)
            {
                button.Click += (s, args) =>
                {
                    if (string.Equals(button.Tag?.ToString(), "ok", StringComparison.Ordinal))
                        result = textBox.Text;
                    dialog.Close();
                };
            }
        }

        dialog.KeyDown += (s, args) =>
        {
            if (args.Key == Avalonia.Input.Key.Escape)
            {
                dialog.Close();
                args.Handled = true;
            }
            else if (args.Key == Avalonia.Input.Key.Enter)
            {
                result = textBox.Text;
                dialog.Close();
                args.Handled = true;
            }
        };

        await dialog.ShowDialog(this);

        if (result is not null)
            await vm.RenameLocationAsync(result);
    });

    private async Task ShowErrorDialogAsync(string title, string message)
    {
        var messageScrollViewer = new ScrollViewer
        {
            Content = new TextBlock
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            },
            MaxHeight = 300,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var okButton = new Button
        {
            Content = Strings.ButtonOk,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(0, 16, 0, 0),
        };
        DockPanel.SetDock(okButton, Dock.Bottom);

        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 180,
            MinHeight = 150,
            MaxHeight = 450,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true,
            Content = new DockPanel
            {
                Margin = new Avalonia.Thickness(16),
                Children =
                {
                    okButton,
                    messageScrollViewer,
                },
            },
        };

        okButton.Click += (_, _) => dialog.Close();

        dialog.KeyDown += (s, args) =>
        {
            if (args.Key == Avalonia.Input.Key.Escape || args.Key == Avalonia.Input.Key.Enter)
            {
                dialog.Close();
                args.Handled = true;
            }
        };

        await dialog.ShowDialog(this);
    }

    private async void OnLocationContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        var contextMenu = sender as ContextMenu;

        FileCompass.Core.Models.Location? targetLocation = null;

        if (contextMenu?.PlacementTarget is ListBoxItem item)
            targetLocation = item.DataContext as FileCompass.Core.Models.Location;
        else
            targetLocation = vm.SelectedLocation;

        if (targetLocation is null)
        {
            e.Cancel = true;
            return;
        }

        var isScanning = targetLocation.Status == FileCompass.Core.Models.LocationStatus.Scanning;
        RenameMenuItem.IsVisible = !isScanning;
        TagMenuItem.IsVisible = !isScanning;
        RescanMenuItem.IsVisible = !isScanning;
        CancelScanMenuItem.IsVisible = isScanning;
        LocationMenuSeparator.IsVisible = !isScanning;
        RemoveMenuItem.IsVisible = !isScanning;

        // Check for scan errors
        try
        {
            var errorCount = await ServiceLocator.ScanErrorRepository.GetCountByLocationAsync(targetLocation.Id);
            ViewErrorsMenuItem.IsVisible = errorCount > 0 && !isScanning;
            ViewErrorsMenuItem.Header = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                Strings.ContextViewErrors,
                errorCount);
            ViewErrorsMenuItem.Tag = targetLocation;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking scan errors: {ex}");
            ViewErrorsMenuItem.IsVisible = false;
        }

        // Build Tag submenu dynamically
        try
        {
            await BuildTagSubmenuAsync(targetLocation, vm);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error building tag submenu: {ex}");
        }
    }

    private async Task BuildTagSubmenuAsync(FileCompass.Core.Models.Location targetLocation, MainWindowViewModel vm)
    {
        // Show loading placeholder to prevent empty menu flicker
        TagMenuItem.Items.Clear();
        TagMenuItem.Items.Add(new MenuItem { Header = Strings.Loading, IsEnabled = false });

        var allTags = await ServiceLocator.TagRepository.GetAllAsync();
        var locationTags = await ServiceLocator.LocationTagRepository.GetTagsForLocationAsync(targetLocation.Id);

        // Clear loading placeholder and populate with actual items
        TagMenuItem.Items.Clear();
        var assignedTagIds = locationTags.Select(t => t.Id).ToHashSet();

        foreach (var tag in allTags)
        {
            var isAssigned = assignedTagIds.Contains(tag.Id);
            var tagId = tag.Id;
            var locationId = targetLocation.Id;

            var ellipse = new Avalonia.Controls.Shapes.Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(tag.Colour)),
                Margin = new Avalonia.Thickness(0, 0, 8, 0)
            };

            var textBlock = new TextBlock { Text = tag.Name };

            var headerPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Children = { ellipse, textBlock }
            };

            var menuItem = new MenuItem
            {
                Header = headerPanel,
                Icon = isAssigned ? new Avalonia.Controls.Shapes.Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = Avalonia.Media.Brushes.LimeGreen
                } : null
            };

            menuItem.Click += (s, args) => SafeExecuteAsync(async () =>
            {
                if (isAssigned)
                    await ServiceLocator.LocationTagRepository.UnassignTagAsync(locationId, tagId);
                else
                    await ServiceLocator.LocationTagRepository.AssignTagAsync(locationId, tagId);

                await vm.LoadLocationsAsync();
            });

            TagMenuItem.Items.Add(menuItem);
        }

        // Add separator and "Create new tag..." option
        if (allTags.Count > 0)
        {
            TagMenuItem.Items.Add(new Separator());
        }

        var createNewItem = new MenuItem { Header = Strings.TagsCreateNew };
        createNewItem.Click += (s, args) => SafeExecuteAsync(async () =>
        {
            var tagVm = new TagManagementViewModel();
            await tagVm.LoadTagsAsync();
            var dialog = new TagManagementWindow { DataContext = tagVm };
            await dialog.ShowDialog(this);

            // If a new tag was created, assign it to this location
            if (tagVm.NewlyCreatedTag is not null)
            {
                await ServiceLocator.LocationTagRepository.AssignTagAsync(targetLocation.Id, tagVm.NewlyCreatedTag.Id);
            }

            await vm.LoadLocationsAsync();
        });
        TagMenuItem.Items.Add(createNewItem);
    }

    private void OnCancelScanClick(object? sender, RoutedEventArgs e) => SafeExecuteAsync(async () =>
    {
        if (DataContext is not MainWindowViewModel vm) return;

        var confirmed = await ShowConfirmationDialogAsync(
            Strings.DialogCancelScanTitle,
            Strings.DialogCancelScanMessage);

        if (confirmed)
        {
            vm.CancelCurrentScan();
        }
    });

    private void OnViewErrorsClick(object? sender, RoutedEventArgs e) => SafeExecuteAsync(async () =>
    {
        if (sender is not MenuItem menuItem) return;
        if (menuItem.Tag is not FileCompass.Core.Models.Location location) return;

        var errors = await ServiceLocator.ScanErrorRepository.GetByLocationAsync(location.Id);
        await ShowScanErrorsDialogAsync(location.DisplayName, errors.ToList());
    });

    private async Task ShowScanErrorsDialogAsync(string locationName, List<FileCompass.Core.Models.ScanError> errors)
    {
        var title = string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            Strings.ScanErrorsTitle,
            locationName);

        if (errors.Count == 0)
        {
            await ShowErrorDialogAsync(title, Strings.ScanErrorsNoErrors);
            return;
        }

        var dataGrid = new DataGrid
        {
            ItemsSource = errors,
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserResizeColumns = true,
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            MaxHeight = 400,
        };

        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = Strings.ScanErrorsColumnPath,
            Binding = new Binding("Path"),
            Width = new DataGridLength(300),
        });
        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = Strings.ScanErrorsColumnError,
            Binding = new Binding("ErrorMessage"),
            Width = new DataGridLength(200),
        });
        dataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = Strings.ScanErrorsColumnType,
            Binding = new Binding("ErrorType"),
            Width = new DataGridLength(150),
        });

        var dialog = new Window
        {
            Title = title,
            Width = 700,
            Height = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new DockPanel
            {
                Margin = new Avalonia.Thickness(16),
                Children =
                {
                    new Button
                    {
                        Content = Strings.ButtonClose,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Margin = new Avalonia.Thickness(0, 8, 0, 0),
                        [DockPanel.DockProperty] = Dock.Bottom,
                    },
                    dataGrid,
                },
            },
        };

        if (dialog.Content is DockPanel panel)
        {
            var closeButton = panel.Children.OfType<Button>().FirstOrDefault();
            if (closeButton != null)
            {
                closeButton.Click += (_, _) => dialog.Close();
            }
        }

        await dialog.ShowDialog(this);
    }

    private async Task<bool> ShowConfirmationDialogAsync(string title, string message)
    {
        bool result = false;

        var messageScrollViewer = new ScrollViewer
        {
            Content = new TextBlock
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            },
            MaxHeight = 250,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Margin = new Avalonia.Thickness(0, 16, 0, 0),
            Spacing = 8,
            Children =
            {
                new Button { Content = Strings.ButtonCancel, Tag = "cancel" },
                new Button { Content = Strings.DialogCancelScanConfirm, Tag = "confirm" }
            }
        };

        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 180,
            MinHeight = 150,
            MaxHeight = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = true,
            Content = new DockPanel
            {
                Margin = new Avalonia.Thickness(16),
                Children =
                {
                    buttonPanel,
                    messageScrollViewer,
                }
            }
        };

        DockPanel.SetDock(buttonPanel, Dock.Bottom);

        foreach (var child in buttonPanel.Children)
        {
            if (child is Button button)
            {
                button.Click += (s, args) =>
                {
                    result = string.Equals(button.Tag?.ToString(), "confirm", StringComparison.Ordinal);
                    dialog.Close();
                };
            }
        }

        dialog.KeyDown += (s, args) =>
        {
            if (args.Key == Avalonia.Input.Key.Escape)
            {
                result = false;
                dialog.Close();
                args.Handled = true;
            }
            else if (args.Key == Avalonia.Input.Key.Enter)
            {
                result = true;
                dialog.Close();
                args.Handled = true;
            }
        };

        await dialog.ShowDialog(this);
        return result;
    }
}
