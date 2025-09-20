using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HDDIndexer.Models;
using HDDIndexer.Services;
using Microsoft.UI.Xaml.Controls;

namespace HDDIndexer.ViewModels
{
    public partial class DriveListViewModel : ViewModelBase
    {
        private readonly IDriveService _driveService;
        private readonly IScannerService _scannerService;
        private CancellationTokenSource? _scanCancellationTokenSource;

        [ObservableProperty]
        private ObservableCollection<Drive> _drives = new();

        [ObservableProperty]
        private ObservableCollection<DriveInfo> _availableDrives = new();

        [ObservableProperty]
        private Drive? _selectedDrive;

        [ObservableProperty]
        private bool _isScanning;

        [ObservableProperty]
        private ScanProgress? _currentScanProgress;

        [ObservableProperty]
        private string _scanStatus = string.Empty;

        public DriveListViewModel(IDriveService driveService, IScannerService scannerService)
        {
            _driveService = driveService;
            _scannerService = scannerService;
            Title = "Drive Management";
        }

        public override async void Initialize()
        {
            base.Initialize();
            await RefreshDrivesAsync();
            await RefreshAvailableDrivesAsync();
        }

        [RelayCommand]
        private async Task RefreshDrivesAsync()
        {
            try
            {
                IsBusy = true;
                var drives = await _driveService.GetCatalogedDrivesAsync();
                Drives.Clear();
                foreach (var drive in drives)
                {
                    Drives.Add(drive);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RefreshAvailableDrivesAsync()
        {
            try
            {
                var availableDrives = await _driveService.GetAvailableDrivesAsync();
                AvailableDrives.Clear();
                foreach (var drive in availableDrives)
                {
                    AvailableDrives.Add(drive);
                }
            }
            catch
            {
                // Handle error
            }
        }

        [RelayCommand]
        private async Task AddDriveAsync(DriveInfo driveInfo)
        {
            var dialog = new ContentDialog
            {
                Title = "Add Drive",
                XamlRoot = App.Current.m_window?.Content.XamlRoot
            };

            var textBox = new TextBox
            {
                PlaceholderText = "Enter a name for this drive",
                Text = driveInfo.VolumeLabel ?? driveInfo.Name
            };

            dialog.Content = textBox;
            dialog.PrimaryButtonText = "Add";
            dialog.CloseButtonText = "Cancel";

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                await _driveService.AddDriveAsync(textBox.Text, driveInfo);
                await RefreshDrivesAsync();
            }
        }

        [RelayCommand]
        private async Task ScanDriveAsync(Drive? drive)
        {
            if (drive == null) return;

            try
            {
                IsScanning = true;
                drive.Status = DriveStatus.Scanning;
                ScanStatus = $"Scanning {drive.DriveName}...";

                _scanCancellationTokenSource = new CancellationTokenSource();
                var progress = new Progress<ScanProgress>(p =>
                {
                    CurrentScanProgress = p;
                    ScanStatus = $"Scanning: {p.CurrentFiles} files, {p.CurrentDirectories} directories";
                });

                var result = await _scannerService.ScanDriveAsync(
                    drive,
                    progress,
                    _scanCancellationTokenSource.Token);

                if (result.Success)
                {
                    ScanStatus = $"Scan complete: {result.FilesScanned} files, {result.DirectoriesScanned} directories";
                    drive.Status = DriveStatus.UpToDate;
                }
                else
                {
                    ScanStatus = $"Scan failed: {result.ErrorMessage}";
                    drive.Status = DriveStatus.Error;
                }
            }
            finally
            {
                IsScanning = false;
                _scanCancellationTokenSource?.Dispose();
                _scanCancellationTokenSource = null;
            }
        }

        [RelayCommand]
        private void CancelScan()
        {
            _scanCancellationTokenSource?.Cancel();
            ScanStatus = "Scan cancelled";
        }

        [RelayCommand]
        private void PauseScan()
        {
            if (_scannerService.IsScanRunning)
            {
                if (_scannerService.IsScanPaused)
                {
                    _scannerService.ResumeScan();
                    ScanStatus = "Scan resumed";
                }
                else
                {
                    _scannerService.PauseScan();
                    ScanStatus = "Scan paused";
                }
            }
        }

        [RelayCommand]
        private async Task RemoveDriveAsync(Drive? drive)
        {
            if (drive == null) return;

            var dialog = new ContentDialog
            {
                Title = "Remove Drive",
                Content = $"Are you sure you want to remove '{drive.DriveName}' from the catalog?",
                PrimaryButtonText = "Remove",
                CloseButtonText = "Cancel",
                XamlRoot = App.Current.m_window?.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await _driveService.RemoveDriveAsync(drive.DriveId);
                await RefreshDrivesAsync();
            }
        }

        [RelayCommand]
        private async Task RenameDriveAsync(Drive? drive)
        {
            if (drive == null) return;

            var dialog = new ContentDialog
            {
                Title = "Rename Drive",
                XamlRoot = App.Current.m_window?.Content.XamlRoot
            };

            var textBox = new TextBox
            {
                Text = drive.DriveName,
                PlaceholderText = "Enter new name"
            };

            dialog.Content = textBox;
            dialog.PrimaryButtonText = "Rename";
            dialog.CloseButtonText = "Cancel";

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                drive.DriveName = textBox.Text;
                await _driveService.UpdateDriveAsync(drive);
                await RefreshDrivesAsync();
            }
        }

        partial void OnSelectedDriveChanged(Drive? value)
        {
            if (value != null)
            {
                // Update UI or perform actions when drive selection changes
            }
        }
    }
}