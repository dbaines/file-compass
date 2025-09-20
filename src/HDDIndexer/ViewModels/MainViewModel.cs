using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HDDIndexer.Services;
using HDDIndexer.Models;
using Microsoft.UI.Xaml.Controls;

namespace HDDIndexer.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IDriveService _driveService;
        private readonly ISettingsService _settingsService;

        [ObservableProperty]
        private object? _selectedView;

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Drive> _drives = new();

        [ObservableProperty]
        private Drive? _selectedDrive;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private bool _isSearchPaneOpen;

        public MainViewModel(
            INavigationService navigationService,
            IDriveService driveService,
            ISettingsService settingsService)
        {
            _navigationService = navigationService;
            _driveService = driveService;
            _settingsService = settingsService;

            Title = "HDD Indexer";
        }

        public override async void Initialize()
        {
            base.Initialize();
            await LoadDrivesAsync();
            await _settingsService.InitializeAsync();
        }

        [RelayCommand]
        private async Task LoadDrivesAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading drives...";

                var drives = await _driveService.GetCatalogedDrivesAsync();
                Drives.Clear();
                foreach (var drive in drives)
                {
                    Drives.Add(drive);
                }

                await _driveService.RefreshDriveStatusAsync();
                StatusMessage = $"Loaded {Drives.Count} drives";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading drives: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void NavigateToView(string viewName)
        {
            switch (viewName)
            {
                case "DriveList":
                    _navigationService.Navigate<Views.DriveListView>();
                    break;
                case "FileBrowser":
                    _navigationService.Navigate<Views.FileBrowserView>(SelectedDrive);
                    break;
                case "Search":
                    _navigationService.Navigate<Views.SearchView>();
                    IsSearchPaneOpen = true;
                    break;
                case "Settings":
                    _navigationService.Navigate<Views.SettingsView>();
                    break;
                case "BackupRestore":
                    _navigationService.Navigate<Views.BackupRestoreView>();
                    break;
            }
        }

        [RelayCommand]
        private async Task AddNewDriveAsync()
        {
            var dialog = new ContentDialog
            {
                Title = "Add New Drive",
                Content = "Select a drive to catalog",
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                XamlRoot = App.Current.m_window?.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await LoadDrivesAsync();
            }
        }

        [RelayCommand]
        private async Task QuickSearchAsync()
        {
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                _navigationService.Navigate<Views.SearchView>(SearchQuery);
                IsSearchPaneOpen = true;
            }
        }

        [RelayCommand]
        private void ShowAbout()
        {
            _ = ShowAboutDialogAsync();
        }

        private async Task ShowAboutDialogAsync()
        {
            var dialog = new ContentDialog
            {
                Title = "About HDD Indexer",
                Content = "HDD Indexer v1.0\n\nA powerful hard drive cataloging application for Windows 11.\n\n© 2024 Your Company",
                CloseButtonText = "OK",
                XamlRoot = App.Current.m_window?.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

        partial void OnSelectedDriveChanged(Drive? value)
        {
            if (value != null)
            {
                StatusMessage = $"Selected: {value.DriveName}";
            }
        }
    }
}