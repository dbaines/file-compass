using System;
using System.Threading;
using System.Threading.Tasks;
using HDDIndexer.Models;

namespace HDDIndexer.Services
{
    public interface IScannerService
    {
        Task<ScanResult> ScanDriveAsync(
            Drive drive,
            IProgress<ScanProgress> progress,
            CancellationToken cancellationToken);

        Task<ScanResult> UpdateScanAsync(
            Drive drive,
            IProgress<ScanProgress> progress,
            CancellationToken cancellationToken);

        void PauseScan();
        void ResumeScan();
        bool IsScanRunning { get; }
        bool IsScanPaused { get; }
        event EventHandler<ScanStatusChangedEventArgs> ScanStatusChanged;
    }

    public class ScanResult
    {
        public bool Success { get; set; }
        public int FilesScanned { get; set; }
        public int DirectoriesScanned { get; set; }
        public long TotalSize { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public int ErrorCount { get; set; }
    }

    public class ScanProgress
    {
        public int CurrentFiles { get; set; }
        public int CurrentDirectories { get; set; }
        public string CurrentPath { get; set; } = string.Empty;
        public double PercentComplete { get; set; }
        public long BytesProcessed { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public TimeSpan EstimatedTimeRemaining { get; set; }
        public double FilesPerSecond { get; set; }
    }

    public class ScanStatusChangedEventArgs : EventArgs
    {
        public bool IsRunning { get; set; }
        public bool IsPaused { get; set; }
        public string? CurrentDrive { get; set; }
    }
}