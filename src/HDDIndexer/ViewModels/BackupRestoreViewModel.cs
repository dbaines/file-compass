using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HDDIndexer.Services;

namespace HDDIndexer.ViewModels
{
    public partial class BackupRestoreViewModel : ViewModelBase
    {
        private readonly IBackupService _backupService;

        [ObservableProperty]
        private ObservableCollection<BackupInfo> _availableBackups = new();

        [ObservableProperty]
        private BackupInfo? _selectedBackup;

        [ObservableProperty]
        private string _backupPath = string.Empty;

        [ObservableProperty]
        private string _backupStatus = "Ready";

        [ObservableProperty]
        private BackupSchedule? _currentSchedule;

        public BackupRestoreViewModel(IBackupService backupService)
        {
            _backupService = backupService;
            Title = "Backup & Restore";
        }

        [RelayCommand]
        private async Task CreateFullBackupAsync()
        {
            try
            {
                IsBusy = true;
                BackupStatus = "Creating full backup...";

                var picker = new Windows.Storage.Pickers.FileSavePicker();
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                picker.FileTypeChoices.Add("Backup Files", new[] { ".backup" });
                picker.SuggestedFileName = $"HDDIndexer_Backup_{DateTime.Now:yyyyMMdd}";

                var window = App.Current.m_window;
                if (window != null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                }

                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    var result = await _backupService.CreateFullBackupAsync(file.Path);

                    if (result.Success)
                    {
                        BackupStatus = $"Backup created successfully: {result.BackupPath}";
                        await RefreshBackupsAsync();
                    }
                    else
                    {
                        BackupStatus = $"Backup failed: {result.ErrorMessage}";
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RestoreBackupAsync()
        {
            try
            {
                IsBusy = true;
                BackupStatus = "Selecting backup file...";

                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(".backup");

                var window = App.Current.m_window;
                if (window != null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                }

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    BackupStatus = "Restoring backup...";

                    var options = new RestoreOptions
                    {
                        Type = RestoreType.Full,
                        ConflictResolution = RestoreConflictResolution.Overwrite
                    };

                    var result = await _backupService.RestoreBackupAsync(file.Path, options);

                    if (result.Success)
                    {
                        BackupStatus = $"Restore completed: {result.DrivesRestored} drives, {result.FilesRestored} files";
                    }
                    else
                    {
                        BackupStatus = $"Restore failed: {result.ErrorMessage}";
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ValidateBackupAsync()
        {
            if (SelectedBackup == null) return;

            try
            {
                IsBusy = true;
                BackupStatus = "Validating backup...";

                var isValid = await _backupService.ValidateBackupAsync(BackupPath);
                BackupStatus = isValid ? "Backup is valid" : "Backup is corrupted or invalid";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RefreshBackupsAsync()
        {
            // In a real implementation, would scan backup directory
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task ScheduleBackupAsync()
        {
            CurrentSchedule = new BackupSchedule
            {
                Frequency = ScheduleFrequency.Daily,
                Time = new TimeSpan(2, 0, 0), // 2 AM
                BackupDirectory = BackupPath,
                Type = BackupType.Incremental,
                IsEnabled = true
            };

            await _backupService.ScheduleBackupAsync(CurrentSchedule);
            BackupStatus = "Backup scheduled successfully";
        }

        [RelayCommand]
        private void CancelSchedule()
        {
            if (CurrentSchedule != null)
            {
                CurrentSchedule.IsEnabled = false;
                BackupStatus = "Scheduled backup cancelled";
            }
        }
    }
}