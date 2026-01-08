namespace FileCompass.Core.Models;

public class ScanProgress
{
    public long LocationId { get; set; }
    public string CurrentPath { get; set; } = string.Empty;
    public int FilesScanned { get; set; }
    public int FoldersScanned { get; set; }
    public long BytesScanned { get; set; }
    public int ErrorCount { get; set; }
    public DateTime StartTime { get; set; }
    public bool IsComplete { get; set; }
    public bool IsCancelled { get; set; }
    public bool IsPaused { get; set; }

    public TimeSpan ElapsedTime => DateTime.UtcNow - StartTime;

    public double FilesPerSecond => ElapsedTime.TotalSeconds > 0
        ? FilesScanned / ElapsedTime.TotalSeconds
        : 0;

    public string ElapsedTimeFormatted
    {
        get
        {
            var elapsed = ElapsedTime;
            if (elapsed.TotalHours >= 1)
                return $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m {elapsed.Seconds}s";
            if (elapsed.TotalMinutes >= 1)
                return $"{elapsed.Minutes}m {elapsed.Seconds}s";
            return $"{elapsed.Seconds}s";
        }
    }

    public string StatusMessage
    {
        get
        {
            var errorSuffix = ErrorCount > 0 ? $", {ErrorCount:N0} errors" : "";
            if (IsPaused) return $"Paused: {FilesScanned:N0} files, {FoldersScanned:N0} folders ({ElapsedTimeFormatted}){errorSuffix}";
            if (IsCancelled) return "Cancelled";
            if (IsComplete) return $"Complete: {FilesScanned:N0} files, {FoldersScanned:N0} folders in {ElapsedTimeFormatted}{errorSuffix}";
            return $"{FilesScanned:N0} files, {FoldersScanned:N0} folders ({FilesPerSecond:N0}/sec){errorSuffix}";
        }
    }
}
