using System.ComponentModel;
using System.Globalization;
using Avalonia.Controls;
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

    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow();
        await dialog.ShowDialog(this);

        if (DataContext is MainWindowViewModel vm)
        {
            var history = await ServiceLocator.SettingsRepository.GetSearchHistoryAsync();
            vm.SearchHistory = new System.Collections.ObjectModel.ObservableCollection<string>(history);
        }
    }

    private async void OnThemeSystemClick(object? sender, RoutedEventArgs e)
    {
        await ServiceLocator.ThemeService.SetThemeAsync(AppTheme.System);
    }

    private async void OnThemeLightClick(object? sender, RoutedEventArgs e)
    {
        await ServiceLocator.ThemeService.SetThemeAsync(AppTheme.Light);
    }

    private async void OnThemeDarkClick(object? sender, RoutedEventArgs e)
    {
        await ServiceLocator.ThemeService.SetThemeAsync(AppTheme.Dark);
    }

    private async void OnAboutClick(object? sender, RoutedEventArgs e)
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

        await dialog.ShowDialog(this);
    }

    private async void OnAddLocationClick(object? sender, RoutedEventArgs e)
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
                var customName = await ShowNamePromptDialogAsync(path);
                if (customName is null) return;

                await vm.AddLocationAsync(path, customName, forceNew: true);
            }
            else if (result.RescanLocationId.HasValue)
            {
                await vm.RescanLocationAsync(result.RescanLocationId.Value);
            }
        }
        else
        {
            var customName = await ShowNamePromptDialogAsync(path);
            if (customName is null) return;

            await vm.AddLocationAsync(path, customName);
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

        var dialog = new Window
        {
            Title = Strings.DialogLocationExistsTitle,
            Width = 500,
            Height = 200 + existingLocations.Count * 30,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = string.Format(CultureInfo.CurrentCulture, Strings.DialogLocationExistsMessage, path, existingLocations.Count),
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    radioButtons,
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Margin = new Avalonia.Thickness(0, 8, 0, 0),
                        Spacing = 8,
                        Children =
                        {
                            new Button { Content = Strings.ButtonCancel, Tag = "cancel" },
                            new Button { Content = Strings.ButtonOk, Tag = "ok" }
                        }
                    }
                }
            }
        };

        if (dialog.Content is StackPanel panel && panel.Children[2] is StackPanel buttonPanel)
        {
            foreach (var child in buttonPanel.Children)
            {
                if (child is Button button)
                {
                    button.Click += (s, args) =>
                    {
                        if (string.Equals(button.Tag?.ToString(), "ok", StringComparison.Ordinal))
                        {
                            foreach (var rb in radioButtons.Children.OfType<RadioButton>())
                            {
                                if (rb.IsChecked == true)
                                {
                                    if (string.Equals(rb.Tag?.ToString(), "new", StringComparison.Ordinal))
                                    {
                                        result = new LocationConflictResult(true, null);
                                    }
                                    else if (rb.Tag is long locationId)
                                    {
                                        result = new LocationConflictResult(false, locationId);
                                    }
                                    break;
                                }
                            }
                        }
                        dialog.Close();
                    };
                }
            }
        }

        await dialog.ShowDialog(this);
        return result;
    }

    private async Task<string?> ShowNamePromptDialogAsync(string path)
    {
        // Default name: last segment of path, or full path for root drives
        var defaultName = System.IO.Path.GetFileName(path);
        if (string.IsNullOrEmpty(defaultName))
            defaultName = path;

        string? result = null;

        var textBox = new TextBox
        {
            Text = defaultName,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };

        var dialog = new Window
        {
            Title = Strings.DialogNameLocationTitle,
            Width = 400,
            Height = 170,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
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
                    },
                    new StackPanel
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
                    }
                }
            }
        };

        if (dialog.Content is StackPanel panel && panel.Children[3] is StackPanel buttonPanel)
        {
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
        }

        await dialog.ShowDialog(this);
        return result;
    }

    private async void OnExportCsvClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel is null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Strings.DialogExportCsvTitle,
            SuggestedFileName = Strings.DialogExportCsvFilename,
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
    }

    private async void OnExportExcelClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel is null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Strings.DialogExportExcelTitle,
            SuggestedFileName = Strings.DialogExportExcelFilename,
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
    }

    private async void OnExportHtmlClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel is null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = Strings.DialogExportHtmlTitle,
            SuggestedFileName = Strings.DialogExportHtmlFilename,
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
    }

    private async void OnBackupClick(object? sender, RoutedEventArgs e)
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
    }

    private async void OnRestoreClick(object? sender, RoutedEventArgs e)
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
    }

    private async void OnAdvancedSearchClick(object? sender, RoutedEventArgs e)
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
    }

    private async void OnRenameLocationClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || vm.SelectedLocation is null) return;

        var location = vm.SelectedLocation;
        var textBox = new TextBox
        {
            Text = location.CustomName ?? location.DisplayName,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
        };

        var dialog = new Window
        {
            Title = Strings.DialogRenameLocationTitle,
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Children =
                {
                    new TextBlock { Text = Strings.DialogRenameLocationLabel },
                    textBox,
                    new StackPanel
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
                    }
                }
            }
        };

        string? result = null;

        if (dialog.Content is StackPanel panel && panel.Children[2] is StackPanel buttonPanel)
        {
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
        }

        await dialog.ShowDialog(this);

        if (result is not null)
            await vm.RenameLocationAsync(result);
    }

    private async Task ShowErrorDialogAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    },
                    new Button
                    {
                        Content = Strings.ButtonOk,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Margin = new Avalonia.Thickness(0, 16, 0, 0),
                    },
                },
            },
        };

        if (dialog.Content is StackPanel panel && panel.Children[1] is Button okButton)
        {
            okButton.Click += (_, _) => dialog.Close();
        }

        await dialog.ShowDialog(this);
    }

    private void OnLocationContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
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
        RescanMenuItem.IsVisible = !isScanning;
        CancelScanMenuItem.IsVisible = isScanning;
        LocationMenuSeparator.IsVisible = !isScanning;
        RemoveMenuItem.IsVisible = !isScanning;
    }

    private async void OnCancelScanClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        var confirmed = await ShowConfirmationDialogAsync(
            Strings.DialogCancelScanTitle,
            Strings.DialogCancelScanMessage);

        if (confirmed)
        {
            vm.CancelCurrentScan();
        }
    }

    private async Task<bool> ShowConfirmationDialogAsync(string title, string message)
    {
        bool result = false;

        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 170,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    },
                    new StackPanel
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
                    }
                }
            }
        };

        if (dialog.Content is StackPanel panel && panel.Children[1] is StackPanel buttonPanel)
        {
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
        }

        await dialog.ShowDialog(this);
        return result;
    }
}
