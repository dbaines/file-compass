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

    public string StatusMessage
    {
        get
        {
            if (IsPaused) return $"Paused: {FilesScanned:N0} files, {FoldersScanned:N0} folders";
            if (IsCancelled) return "Cancelled";
            if (IsComplete) return $"Complete: {FilesScanned:N0} files, {FoldersScanned:N0} folders";
            return $"Scanning: {FilesScanned:N0} files ({FilesPerSecond:N0}/sec)";
        }
    }
}
