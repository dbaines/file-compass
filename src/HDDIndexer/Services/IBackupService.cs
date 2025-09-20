using System;
using System.Threading.Tasks;

namespace HDDIndexer.Services
{
    public interface IBackupService
    {
        Task<BackupResult> CreateFullBackupAsync(string backupPath);
        Task<BackupResult> CreateIncrementalBackupAsync(string backupPath);
        Task<RestoreResult> RestoreBackupAsync(string backupPath, RestoreOptions options);
        Task<bool> ValidateBackupAsync(string backupPath);
        Task ScheduleBackupAsync(BackupSchedule schedule);
        Task<BackupInfo> GetBackupInfoAsync(string backupPath);
    }

    public class BackupResult
    {
        public bool Success { get; set; }
        public string BackupPath { get; set; } = string.Empty;
        public long BackupSize { get; set; }
        public DateTime BackupDate { get; set; }
        public BackupType Type { get; set; }
        public int DrivesIncluded { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class RestoreResult
    {
        public bool Success { get; set; }
        public int DrivesRestored { get; set; }
        public int FilesRestored { get; set; }
        public string? ErrorMessage { get; set; }
        public RestoreConflictResolution ConflictResolution { get; set; }
    }

    public class RestoreOptions
    {
        public RestoreType Type { get; set; }
        public RestoreConflictResolution ConflictResolution { get; set; }
        public List<int>? SelectedDriveIds { get; set; }
    }

    public class BackupSchedule
    {
        public ScheduleFrequency Frequency { get; set; }
        public TimeSpan Time { get; set; }
        public string BackupDirectory { get; set; } = string.Empty;
        public BackupType Type { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class BackupInfo
    {
        public DateTime CreatedDate { get; set; }
        public long Size { get; set; }
        public BackupType Type { get; set; }
        public int DriveCount { get; set; }
        public int FileCount { get; set; }
        public string Version { get; set; } = string.Empty;
    }

    public enum BackupType
    {
        Full,
        Incremental,
        Selective
    }

    public enum RestoreType
    {
        Full,
        Merge,
        Selective
    }

    public enum RestoreConflictResolution
    {
        Skip,
        Overwrite,
        Rename,
        Ask
    }

    public enum ScheduleFrequency
    {
        Daily,
        Weekly,
        Monthly
    }
}