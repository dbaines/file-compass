namespace FileCompass.Core.Models;

public enum LocationStatus
{
    NeverScanned,
    Scanning,
    UpToDate,
    Outdated,
    Offline
}

public class Location
{
    public long Id { get; set; }
    public required string Path { get; set; }
    public string? CustomName { get; set; }
    public string? VolumeLabel { get; set; }
    public string? FileSystem { get; set; }
    public long? TotalSize { get; set; }
    public long? FreeSpace { get; set; }
    public int TotalFiles { get; set; }
    public int TotalFolders { get; set; }
    public DateTime? LastScanStart { get; set; }
    public DateTime? LastScanComplete { get; set; }
    public int? ScanDurationSeconds { get; set; }
    public LocationStatus Status { get; set; } = LocationStatus.NeverScanned;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public IList<Tag> Tags { get; set; } = [];

    public string DisplayName => CustomName ?? VolumeLabel ?? System.IO.Path.GetFileName(Path) ?? Path;
}
