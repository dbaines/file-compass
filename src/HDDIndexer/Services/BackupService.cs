using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using HDDIndexer.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HDDIndexer.Services
{
    public class BackupService : IBackupService
    {
        private readonly CatalogDbContext _dbContext;
        private BackupSchedule? _currentSchedule;

        public BackupService(CatalogDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<BackupResult> CreateFullBackupAsync(string backupPath)
        {
            var result = new BackupResult
            {
                BackupDate = DateTime.Now,
                Type = BackupType.Full
            };

            try
            {
                // Ensure backup directory exists
                var backupDir = Path.GetDirectoryName(backupPath);
                if (!string.IsNullOrEmpty(backupDir))
                    Directory.CreateDirectory(backupDir);

                // Create backup filename with timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFile = Path.Combine(backupPath, $"HDDIndexer_Full_{timestamp}.backup");

                // Get all data
                var drives = await _dbContext.Drives.ToListAsync();
                var files = await _dbContext.Files.ToListAsync();

                result.DrivesIncluded = drives.Count;

                // Create backup data structure
                var backupData = new
                {
                    Version = "1.0",
                    BackupDate = result.BackupDate,
                    Type = BackupType.Full,
                    Drives = drives,
                    Files = files
                };

                // Serialize to JSON
                var json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Compress and save
                using (var fileStream = File.Create(backupFile))
                using (var archive = new GZipStream(fileStream, CompressionLevel.Optimal))
                using (var writer = new StreamWriter(archive))
                {
                    await writer.WriteAsync(json);
                }

                result.BackupPath = backupFile;
                result.BackupSize = new FileInfo(backupFile).Length;
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<BackupResult> CreateIncrementalBackupAsync(string backupPath)
        {
            // For incremental backup, we would track changes since last backup
            // For now, create a full backup
            return await CreateFullBackupAsync(backupPath);
        }

        public async Task<RestoreResult> RestoreBackupAsync(string backupPath, RestoreOptions options)
        {
            var result = new RestoreResult
            {
                ConflictResolution = options.ConflictResolution
            };

            try
            {
                // Read and decompress backup file
                string json;
                using (var fileStream = File.OpenRead(backupPath))
                using (var archive = new GZipStream(fileStream, CompressionMode.Decompress))
                using (var reader = new StreamReader(archive))
                {
                    json = await reader.ReadToEndAsync();
                }

                // Deserialize backup data
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // Validate backup version
                if (!root.TryGetProperty("Version", out var versionElement) ||
                    versionElement.GetString() != "1.0")
                {
                    throw new InvalidOperationException("Unsupported backup version");
                }

                result.BackupDate = root.GetProperty("BackupDate").GetDateTime();

                // Deserialize drives and files
                var drivesJson = root.GetProperty("Drives").GetRawText();
                var filesJson = root.GetProperty("Files").GetRawText();

                var backupDrives = JsonSerializer.Deserialize<List<Drive>>(drivesJson) ?? new List<Drive>();
                var backupFiles = JsonSerializer.Deserialize<List<FileEntry>>(filesJson) ?? new List<FileEntry>();

                result.DrivesRestored = backupDrives.Count;
                result.FilesRestored = backupFiles.Count;

                if (options.Type == RestoreType.Full)
                {
                    // Clear existing data for full restore
                    _dbContext.Files.RemoveRange(_dbContext.Files);
                    _dbContext.Drives.RemoveRange(_dbContext.Drives);
                    await _dbContext.SaveChangesAsync();

                    // Add all backup data
                    await _dbContext.Drives.AddRangeAsync(backupDrives);
                    await _dbContext.Files.AddRangeAsync(backupFiles);
                }
                else
                {
                    // Merge restore with conflict resolution
                    await RestoreMergeWithConflictResolution(backupDrives, backupFiles, options.ConflictResolution, result);
                }

                await _dbContext.SaveChangesAsync();

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private async Task RestoreMergeWithConflictResolution(
            List<Drive> backupDrives,
            List<FileEntry> backupFiles,
            ConflictResolution resolution,
            RestoreResult result)
        {
            // Restore drives with conflict resolution
            foreach (var backupDrive in backupDrives)
            {
                var existingDrive = await _dbContext.Drives
                    .FirstOrDefaultAsync(d => d.SerialNumber == backupDrive.SerialNumber);

                if (existingDrive != null)
                {
                    result.ConflictsFound++;

                    switch (resolution)
                    {
                        case ConflictResolution.KeepExisting:
                            // Skip backup drive
                            break;
                        case ConflictResolution.OverwriteWithBackup:
                            // Update existing drive with backup data
                            existingDrive.DriveName = backupDrive.DriveName;
                            existingDrive.VolumeLabel = backupDrive.VolumeLabel;
                            existingDrive.ScanDate = backupDrive.ScanDate;
                            existingDrive.FileCount = backupDrive.FileCount;
                            existingDrive.DirectoryCount = backupDrive.DirectoryCount;
                            break;
                        case ConflictResolution.KeepBoth:
                            // Create new drive with modified name
                            backupDrive.DriveName += $" (Restored {DateTime.Now:yyyy-MM-dd})";
                            backupDrive.DriveId = 0; // Reset ID for new insert
                            await _dbContext.Drives.AddAsync(backupDrive);
                            break;
                    }
                }
                else
                {
                    // No conflict, add new drive
                    backupDrive.DriveId = 0; // Reset ID for new insert
                    await _dbContext.Drives.AddAsync(backupDrive);
                }
            }

            // Save drive changes to get new IDs
            await _dbContext.SaveChangesAsync();

            // Restore files with conflict resolution
            foreach (var backupFile in backupFiles)
            {
                // Find corresponding drive by serial number
                var targetDrive = await _dbContext.Drives
                    .FirstOrDefaultAsync(d => d.SerialNumber ==
                        backupDrives.FirstOrDefault(bd => bd.DriveId == backupFile.DriveId)?.SerialNumber);

                if (targetDrive != null)
                {
                    backupFile.DriveId = targetDrive.DriveId;

                    var existingFile = await _dbContext.Files
                        .FirstOrDefaultAsync(f => f.DriveId == backupFile.DriveId &&
                                                 f.FullPath == backupFile.FullPath);

                    if (existingFile != null)
                    {
                        result.ConflictsFound++;

                        switch (resolution)
                        {
                            case ConflictResolution.KeepExisting:
                                // Skip backup file
                                break;
                            case ConflictResolution.OverwriteWithBackup:
                                // Update existing file with backup data
                                existingFile.FileSize = backupFile.FileSize;
                                existingFile.ModificationDate = backupFile.ModificationDate;
                                existingFile.CreationDate = backupFile.CreationDate;
                                break;
                            case ConflictResolution.KeepBoth:
                                // This would be complex for files, just overwrite for now
                                existingFile.FileSize = backupFile.FileSize;
                                existingFile.ModificationDate = backupFile.ModificationDate;
                                existingFile.CreationDate = backupFile.CreationDate;
                                break;
                        }
                    }
                    else
                    {
                        // No conflict, add new file
                        backupFile.FileId = 0; // Reset ID for new insert
                        await _dbContext.Files.AddAsync(backupFile);
                    }
                }
            }
        }

        public async Task<bool> ValidateBackupAsync(string backupPath)
        {
            try
            {
                using (var fileStream = File.OpenRead(backupPath))
                using (var archive = new GZipStream(fileStream, CompressionMode.Decompress))
                using (var reader = new StreamReader(archive))
                {
                    var json = await reader.ReadToEndAsync();
                    var backupData = JsonSerializer.Deserialize<dynamic>(json);
                    return backupData != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task ScheduleBackupAsync(BackupSchedule schedule)
        {
            _currentSchedule = schedule;
            // In a real implementation, would use a background service or Windows Task Scheduler
            await Task.CompletedTask;
        }

        public async Task<BackupInfo> GetBackupInfoAsync(string backupPath)
        {
            var info = new BackupInfo();

            try
            {
                using (var fileStream = File.OpenRead(backupPath))
                using (var archive = new GZipStream(fileStream, CompressionMode.Decompress))
                using (var reader = new StreamReader(archive))
                {
                    var json = await reader.ReadToEndAsync();
                    // Parse backup info from JSON
                    info.Size = new FileInfo(backupPath).Length;
                    info.CreatedDate = File.GetCreationTime(backupPath);
                    info.Version = "1.0";
                }
            }
            catch
            {
                // Return empty info on error
            }

            return info;
        }
    }
}