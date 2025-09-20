using System.IO.Abstractions;
using FileCompass.Core.Constants;
using FileCompass.Core.Data;
using FileCompass.Core.Models;

namespace FileCompass.Core.Services;

public class FileScannerService
{
    private readonly IFileSystem _fileSystem;
    private readonly DatabaseService _db;
    private readonly LocationRepository _locationRepo;
    private readonly FileRepository _fileRepo;

    public FileScannerService(
        IFileSystem fileSystem,
        DatabaseService db,
        LocationRepository locationRepo,
        FileRepository fileRepo)
    {
        _fileSystem = fileSystem;
        _db = db;
        _locationRepo = locationRepo;
        _fileRepo = fileRepo;
    }

    public async Task<Location> ScanLocationAsync(
        string path,
        string? customName,
        bool forceNew = false,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Location location;

        if (forceNew)
        {
            // Always create a new location entry
            location = await CreateLocationAsync(path, customName);
        }
        else
        {
            // Get existing or create new location
            var existing = await _locationRepo.GetByPathAsync(path);
            if (existing is null)
            {
                location = await CreateLocationAsync(path, customName);
            }
            else
            {
                location = existing;
                if (customName is not null)
                {
                    location.CustomName = customName;
                }
            }
        }

        return await ScanLocationCoreAsync(location, path, progress, cancellationToken);
    }

    public async Task<Location> RescanLocationAsync(
        long locationId,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var location = await _locationRepo.GetByIdAsync(locationId)
            ?? throw new InvalidOperationException($"Location with ID {locationId} not found");

        return await ScanLocationCoreAsync(location, location.Path, progress, cancellationToken);
    }

    private async Task<Location> ScanLocationCoreAsync(
        Location location,
        string path,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        // Update status to scanning
        location.Status = LocationStatus.Scanning;
        location.LastScanStart = DateTime.UtcNow;
        await _locationRepo.UpdateAsync(location);

        var scanProgress = new ScanProgress
        {
            LocationId = location.Id,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Clear existing files for this location
            await _fileRepo.DeleteByLocationAsync(location.Id);

            // Scan the directory
            var batch = new List<FileEntry>();
            await ScanDirectoryAsync(
                path,
                location.Id,
                null,
                string.Empty,
                batch,
                scanProgress,
                progress,
                cancellationToken);

            // Insert any remaining files
            if (batch.Count > 0)
            {
                await _fileRepo.AddBatchAsync(batch);
            }

            // Update location with final stats
            location.TotalFiles = scanProgress.FilesScanned;
            location.TotalFolders = scanProgress.FoldersScanned;
            location.LastScanComplete = DateTime.UtcNow;
            location.ScanDurationSeconds = (int)scanProgress.ElapsedTime.TotalSeconds;
            location.Status = LocationStatus.UpToDate;
            await _locationRepo.UpdateAsync(location);

            scanProgress.IsComplete = true;
            progress?.Report(scanProgress);

            return location;
        }
        catch (OperationCanceledException)
        {
            location.TotalFiles = scanProgress.FilesScanned;
            location.TotalFolders = scanProgress.FoldersScanned;
            location.Status = LocationStatus.Outdated;
            await _locationRepo.UpdateAsync(location);

            scanProgress.IsCancelled = true;
            progress?.Report(scanProgress);

            throw;
        }
        catch
        {
            location.Status = LocationStatus.Outdated;
            await _locationRepo.UpdateAsync(location);
            throw;
        }
    }

    private async Task<Location> CreateLocationAsync(string path, string? customName)
    {
        var dirInfo = _fileSystem.DirectoryInfo.New(path);
        var driveInfo = TryGetDriveInfo(path);

        var location = new Location
        {
            Path = path,
            CustomName = customName,
            VolumeLabel = driveInfo?.VolumeLabel,
            FileSystem = driveInfo?.DriveFormat,
            TotalSize = driveInfo?.TotalSize,
            FreeSpace = driveInfo?.AvailableFreeSpace,
            Status = LocationStatus.NeverScanned,
            CreatedAt = DateTime.UtcNow
        };

        return await _locationRepo.AddAsync(location);
    }

    private IDriveInfo? TryGetDriveInfo(string path)
    {
        try
        {
            var drives = _fileSystem.DriveInfo.GetDrives();
            return drives.FirstOrDefault(d =>
                d.IsReady && path.StartsWith(d.RootDirectory.FullName, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return null;
        }
    }

    private async Task ScanDirectoryAsync(
        string directoryPath,
        long locationId,
        long? parentId,
        string relativePath,
        List<FileEntry> batch,
        ScanProgress scanProgress,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<IFileSystemInfo> entries;
        try
        {
            var dirInfo = _fileSystem.DirectoryInfo.New(directoryPath);
            entries = dirInfo.EnumerateFileSystemInfos();
        }
        catch (UnauthorizedAccessException ex)
        {
            await LogScanErrorAsync(locationId, directoryPath, ex);
            scanProgress.ErrorCount++;
            return;
        }
        catch (IOException ex)
        {
            await LogScanErrorAsync(locationId, directoryPath, ex);
            scanProgress.ErrorCount++;
            return;
        }

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var entryRelativePath = string.IsNullOrEmpty(relativePath)
                    ? entry.Name
                    : Path.Combine(relativePath, entry.Name);

                // Validate the relative path doesn't contain path traversal sequences
                if (!IsValidRelativePath(entryRelativePath))
                {
                    await LogScanErrorAsync(locationId, entry.FullName,
                        new InvalidOperationException("Invalid path: contains traversal sequences"));
                    scanProgress.ErrorCount++;
                    continue;
                }

                var fileEntry = new FileEntry
                {
                    LocationId = locationId,
                    ParentId = parentId,
                    Name = entry.Name,
                    Extension = entry is IFileInfo ? Path.GetExtension(entry.Name).TrimStart('.') : null,
                    RelativePath = entryRelativePath,
                    Size = entry is IFileInfo file ? file.Length : 0,
                    IsDirectory = entry is IDirectoryInfo,
                    ModifiedAt = entry.LastWriteTimeUtc,
                    CreatedAt = entry.CreationTimeUtc,
                    Attributes = entry.Attributes.ToString()
                };

                batch.Add(fileEntry);

                if (fileEntry.IsDirectory)
                {
                    scanProgress.FoldersScanned++;
                }
                else
                {
                    scanProgress.FilesScanned++;
                    scanProgress.BytesScanned += fileEntry.Size;
                }

                scanProgress.CurrentPath = entryRelativePath;

                // Insert batch when it reaches the batch size
                if (batch.Count >= AppConstants.DefaultBatchSize)
                {
                    await _fileRepo.AddBatchAsync(batch);
                    batch.Clear();
                    progress?.Report(scanProgress);
                }

                // Recursively scan subdirectories (skip symlinks to prevent infinite loops)
                if (entry is IDirectoryInfo && !IsSymlink(entry))
                {
                    await ScanDirectoryAsync(
                        entry.FullName,
                        locationId,
                        null, // We're not tracking parent IDs for simplicity in this version
                        entryRelativePath,
                        batch,
                        scanProgress,
                        progress,
                        cancellationToken);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                await LogScanErrorAsync(locationId, entry.FullName, ex);
                scanProgress.ErrorCount++;
            }
            catch (FileNotFoundException ex)
            {
                await LogScanErrorAsync(locationId, entry.FullName, ex);
                scanProgress.ErrorCount++;
            }
            catch (DirectoryNotFoundException ex)
            {
                await LogScanErrorAsync(locationId, entry.FullName, ex);
                scanProgress.ErrorCount++;
            }
            catch (IOException ex)
            {
                await LogScanErrorAsync(locationId, entry.FullName, ex);
                scanProgress.ErrorCount++;
            }
        }
    }

    /// <summary>
    /// Validates that a relative path doesn't contain path traversal sequences.
    /// Prevents storing paths with ".." that could be exploited during reconstruction.
    /// </summary>
    private static bool IsValidRelativePath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return false;

        // Check for path traversal sequences
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (var segment in segments)
        {
            // Reject ".." segments and paths starting with drive letters or root
            if (string.Equals(segment, "..", StringComparison.Ordinal) ||
                string.Equals(segment, ".", StringComparison.Ordinal) ||
                (segment.Length >= 2 && segment[1] == ':')) // Windows drive letter
            {
                return false;
            }
        }

        // Also reject absolute paths
        if (Path.IsPathRooted(relativePath))
            return false;

        return true;
    }

    private static bool IsSymlink(IFileSystemInfo entry)
    {
        // Check for ReparsePoint attribute (works on Windows for junctions/symlinks)
        // and LinkTarget property (works cross-platform on .NET 6+)
        return entry.Attributes.HasFlag(FileAttributes.ReparsePoint) ||
               entry.LinkTarget != null;
    }

    private async Task LogScanErrorAsync(long locationId, string path, Exception ex)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            INSERT INTO scan_errors (location_id, path, error_message, error_type, occurred_at)
            VALUES (@locationId, @path, @message, @type, @occurredAt)
            """;

        cmd.Parameters.AddWithValue("@locationId", locationId);
        cmd.Parameters.AddWithValue("@path", path);
        cmd.Parameters.AddWithValue("@message", ex.Message);
        cmd.Parameters.AddWithValue("@type", ex.GetType().Name);
        cmd.Parameters.AddWithValue("@occurredAt", DateTime.UtcNow.ToString("o"));

        await cmd.ExecuteNonQueryAsync();
    }
}
