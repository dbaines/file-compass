using System;
using System.Collections.Generic;

namespace HDDIndexer.Models
{
    public class Drive
    {
        public int DriveId { get; set; }
        public string DriveName { get; set; } = string.Empty;
        public string? VolumeLabel { get; set; }
        public string? SerialNumber { get; set; }
        public long TotalSize { get; set; }
        public string? FileSystem { get; set; }
        public DateTime ScanDate { get; set; }
        public string? Description { get; set; }
        public int FileCount { get; set; }
        public int DirectoryCount { get; set; }
        public long UsedSpace { get; set; }

        // Navigation property
        public virtual ICollection<FileEntry> Files { get; set; } = new List<FileEntry>();

        // Computed properties
        public long FreeSpace => TotalSize - UsedSpace;
        public double UsagePercentage => TotalSize > 0 ? (double)UsedSpace / TotalSize * 100 : 0;
        public bool IsOnline { get; set; }
        public DriveStatus Status { get; set; }
    }

    public enum DriveStatus
    {
        NeverScanned,
        Scanning,
        UpToDate,
        Outdated,
        Offline,
        Error
    }
}