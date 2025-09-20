using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HDDIndexer.Data;
using HDDIndexer.Models;
using Microsoft.EntityFrameworkCore;

namespace HDDIndexer.Services
{
    public class ScannerService : IScannerService
    {
        private readonly CatalogDbContext _dbContext;
        private CancellationTokenSource? _pauseTokenSource;
        private readonly object _pauseLock = new();
        private bool _isPaused;
        private bool _isRunning;

        public bool IsScanRunning => _isRunning;
        public bool IsScanPaused => _isPaused;
        public event EventHandler<ScanStatusChangedEventArgs>? ScanStatusChanged;

        public ScannerService(CatalogDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ScanResult> ScanDriveAsync(
            Drive drive,
            IProgress<ScanProgress> progress,
            CancellationToken cancellationToken)
        {
            _isRunning = true;
            OnScanStatusChanged(true, false, drive.DriveName);

            var result = new ScanResult();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Clear existing files for this drive
                var existingFiles = _dbContext.Files.Where(f => f.DriveId == drive.DriveId);
                _dbContext.Files.RemoveRange(existingFiles);
                await _dbContext.SaveChangesAsync();

                // Get drive info
                var driveInfo = new DriveInfo(drive.VolumeLabel ?? "C:");
                if (!driveInfo.IsReady)
                {
                    throw new InvalidOperationException("Drive is not ready");
                }

                // Start scanning
                var scanProgress = new ScanProgress();
                var fileEntries = new List<FileEntry>();

                await ScanDirectoryAsync(
                    driveInfo.RootDirectory,
                    drive.DriveId,
                    null,
                    fileEntries,
                    scanProgress,
                    progress,
                    cancellationToken);

                // Batch insert files
                await BatchInsertFilesAsync(fileEntries);

                // Update drive information
                drive.ScanDate = DateTime.Now;
                drive.FileCount = fileEntries.Count(f => !f.IsDirectory);
                drive.DirectoryCount = fileEntries.Count(f => f.IsDirectory);
                drive.UsedSpace = fileEntries.Where(f => !f.IsDirectory).Sum(f => f.FileSize ?? 0);
                drive.Status = DriveStatus.UpToDate;

                _dbContext.Drives.Update(drive);
                await _dbContext.SaveChangesAsync();

                result.Success = true;
                result.FilesScanned = drive.FileCount;
                result.DirectoriesScanned = drive.DirectoryCount;
                result.TotalSize = drive.UsedSpace;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
                _isRunning = false;
                OnScanStatusChanged(false, false, null);
            }

            return result;
        }

        private async Task ScanDirectoryAsync(
            DirectoryInfo directory,
            int driveId,
            long? parentFileId,
            List<FileEntry> fileEntries,
            ScanProgress scanProgress,
            IProgress<ScanProgress> progress,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            await CheckPauseAsync();

            try
            {
                // Add directory entry
                var dirEntry = new FileEntry
                {
                    DriveId = driveId,
                    FileName = directory.Name,
                    FilePath = directory.Parent?.FullName ?? "",
                    IsDirectory = true,
                    DateCreated = directory.CreationTime,
                    DateModified = directory.LastWriteTime,
                    ParentFileId = parentFileId,
                    Attributes = directory.Attributes.ToString()
                };
                fileEntries.Add(dirEntry);
                scanProgress.CurrentDirectories++;

                // Get files in directory
                foreach (var file in directory.EnumerateFiles())
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    try
                    {
                        var fileEntry = new FileEntry
                        {
                            DriveId = driveId,
                            FileName = file.Name,
                            FilePath = file.DirectoryName ?? "",
                            FileSize = file.Length,
                            IsDirectory = false,
                            DateCreated = file.CreationTime,
                            DateModified = file.LastWriteTime,
                            FileExtension = file.Extension.ToLower(),
                            ParentFileId = dirEntry.FileId,
                            Attributes = file.Attributes.ToString()
                        };
                        fileEntries.Add(fileEntry);
                        scanProgress.CurrentFiles++;
                        scanProgress.BytesProcessed += file.Length;

                        // Report progress
                        if (scanProgress.CurrentFiles % 100 == 0)
                        {
                            scanProgress.CurrentPath = file.FullName;
                            progress.Report(scanProgress);
                        }
                    }
                    catch
                    {
                        scanProgress.ErrorCount++;
                    }
                }

                // Recursively scan subdirectories
                foreach (var subDir in directory.EnumerateDirectories())
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    await ScanDirectoryAsync(
                        subDir,
                        driveId,
                        dirEntry.FileId,
                        fileEntries,
                        scanProgress,
                        progress,
                        cancellationToken);
                }
            }
            catch (UnauthorizedAccessException)
            {
                scanProgress.ErrorCount++;
            }
            catch (DirectoryNotFoundException)
            {
                scanProgress.ErrorCount++;
            }
        }

        private async Task BatchInsertFilesAsync(List<FileEntry> files)
        {
            const int batchSize = 1000;
            for (int i = 0; i < files.Count; i += batchSize)
            {
                var batch = files.Skip(i).Take(batchSize);
                _dbContext.Files.AddRange(batch);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<ScanResult> UpdateScanAsync(
            Drive drive,
            IProgress<ScanProgress> progress,
            CancellationToken cancellationToken)
        {
            // For incremental update, we would compare file timestamps
            // For now, just do a full scan
            return await ScanDriveAsync(drive, progress, cancellationToken);
        }

        public void PauseScan()
        {
            lock (_pauseLock)
            {
                if (_isRunning && !_isPaused)
                {
                    _isPaused = true;
                    _pauseTokenSource = new CancellationTokenSource();
                    OnScanStatusChanged(true, true, null);
                }
            }
        }

        public void ResumeScan()
        {
            lock (_pauseLock)
            {
                if (_isPaused)
                {
                    _isPaused = false;
                    _pauseTokenSource?.Cancel();
                    OnScanStatusChanged(true, false, null);
                }
            }
        }

        private async Task CheckPauseAsync()
        {
            if (_isPaused)
            {
                var tcs = new TaskCompletionSource<bool>();
                using var registration = _pauseTokenSource?.Token.Register(() => tcs.SetResult(true));
                await tcs.Task;
            }
        }

        private void OnScanStatusChanged(bool isRunning, bool isPaused, string? currentDrive)
        {
            ScanStatusChanged?.Invoke(this, new ScanStatusChangedEventArgs
            {
                IsRunning = isRunning,
                IsPaused = isPaused,
                CurrentDrive = currentDrive
            });
        }
    }

}