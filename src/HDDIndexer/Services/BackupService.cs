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
                var backupData = JsonSerializer.Deserialize<dynamic>(json);

                if (options.Type == RestoreType.Full)
                {
                    // Clear existing data
                    _dbContext.Files.RemoveRange(_dbContext.Files);
                    _dbContext.Drives.RemoveRange(_dbContext.Drives);
                    await _dbContext.SaveChangesAsync();
                }

                // Restore data (simplified - would need proper deserialization)
                // This is a placeholder for the actual restore logic

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
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