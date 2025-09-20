using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HDDIndexer.Services;
using Microsoft.UI.Xaml;

namespace HDDIndexer.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;

        public int SelectedTheme
        {
            get => (int)_settingsService.Theme;
            set => _settingsService.Theme = (ElementTheme)value;
        }

        [ObservableProperty]
        private bool _autoScanOnStartup;

        [ObservableProperty]
        private bool _showHiddenFiles;

        [ObservableProperty]
        private bool _showSystemFiles;

        [ObservableProperty]
        private int _scanThreadCount;

        [ObservableProperty]
        private int _batchSize;

        [ObservableProperty]
        private int _maxSearchResults;

        [ObservableProperty]
        private bool _enableSearchHistory;

        [ObservableProperty]
        private bool _enableAutoComplete;

        [ObservableProperty]
        private bool _enableAutoBackup;

        [ObservableProperty]
        private string _backupDirectory = string.Empty;

        [ObservableProperty]
        private int _backupRetentionDays;

        [ObservableProperty]
        private bool _showStatusBar;

        [ObservableProperty]
        private bool _showFilePreview;

        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            Title = "Settings";
            LoadSettings();
        }

        private void LoadSettings()
        {
            SelectedTheme = _settingsService.Theme;
            AutoScanOnStartup = _settingsService.AutoScanOnStartup;
            ShowHiddenFiles = _settingsService.ShowHiddenFiles;
            ShowSystemFiles = _settingsService.ShowSystemFiles;
            ScanThreadCount = _settingsService.ScanThreadCount;
            BatchSize = _settingsService.BatchSize;
            MaxSearchResults = _settingsService.MaxSearchResults;
            EnableSearchHistory = _settingsService.EnableSearchHistory;
            EnableAutoComplete = _settingsService.EnableAutoComplete;
            EnableAutoBackup = _settingsService.EnableAutoBackup;
            BackupDirectory = _settingsService.BackupDirectory;
            BackupRetentionDays = _settingsService.BackupRetentionDays;
            ShowStatusBar = _settingsService.ShowStatusBar;
            ShowFilePreview = _settingsService.ShowFilePreview;
        }

        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            _settingsService.Theme = SelectedTheme;
            _settingsService.AutoScanOnStartup = AutoScanOnStartup;
            _settingsService.ShowHiddenFiles = ShowHiddenFiles;
            _settingsService.ShowSystemFiles = ShowSystemFiles;
            _settingsService.ScanThreadCount = ScanThreadCount;
            _settingsService.BatchSize = BatchSize;
            _settingsService.MaxSearchResults = MaxSearchResults;
            _settingsService.EnableSearchHistory = EnableSearchHistory;
            _settingsService.EnableAutoComplete = EnableAutoComplete;
            _settingsService.EnableAutoBackup = EnableAutoBackup;
            _settingsService.BackupDirectory = BackupDirectory;
            _settingsService.BackupRetentionDays = BackupRetentionDays;
            _settingsService.ShowStatusBar = ShowStatusBar;
            _settingsService.ShowFilePreview = ShowFilePreview;

            await _settingsService.SaveSettingsAsync();
        }

        [RelayCommand]
        private void ResetToDefaults()
        {
            SelectedTheme = ElementTheme.Default;
            AutoScanOnStartup = false;
            ShowHiddenFiles = false;
            ShowSystemFiles = false;
            ScanThreadCount = 4;
            BatchSize = 1000;
            MaxSearchResults = 1000;
            EnableSearchHistory = true;
            EnableAutoComplete = true;
            EnableAutoBackup = false;
            BackupDirectory = string.Empty;
            BackupRetentionDays = 30;
            ShowStatusBar = true;
            ShowFilePreview = true;
        }

        [RelayCommand]
        private async Task SelectBackupDirectoryAsync()
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            // Make sure to associate the picker with the app window
            var window = App.Current.m_window;
            if (window != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                BackupDirectory = folder.Path;
            }
        }
    }
}