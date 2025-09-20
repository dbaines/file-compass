using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using FileCompass.Core.Constants;
using FileCompass.Core.Data;

namespace FileCompass.Core.Services;

public class BackupService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly DatabaseService _db;

    public BackupService(DatabaseService db)
    {
        _db = db;
    }

    /// <summary>
    /// Creates a backup of the database as a .hdi file (ZIP with database and metadata)
    /// </summary>
    public async Task CreateBackupAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        var dbPath = _db.DatabasePath;

        if (!File.Exists(dbPath))
            throw new InvalidOperationException("Database file not found");

        // Create temporary directory for backup contents
        var tempDir = Path.Combine(Path.GetTempPath(), $"hdi-backup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Checkpoint the WAL to ensure all data is in the main database file
            var conn = await _db.GetConnectionAsync();
            using var checkpointCmd = conn.CreateCommand();
            checkpointCmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
            await checkpointCmd.ExecuteNonQueryAsync(cancellationToken);

            // Copy database file
            var dbBackupPath = Path.Combine(tempDir, AppConstants.DatabaseFileName);
            File.Copy(dbPath, dbBackupPath, overwrite: true);

            // Create metadata file
            var metadata = new BackupMetadata
            {
                AppName = AppConstants.AppName,
                AppVersion = AppConstants.AppVersion,
                BackupDate = DateTime.UtcNow,
                LocationCount = await GetLocationCountAsync(cancellationToken),
                FileCount = await GetFileCountAsync(cancellationToken)
            };

            var metadataPath = Path.Combine(tempDir, "metadata.json");
            var metadataJson = JsonSerializer.Serialize(metadata, JsonOptions);
            await File.WriteAllTextAsync(metadataPath, metadataJson, cancellationToken);

            // Create ZIP file
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            ZipFile.CreateFromDirectory(tempDir, outputPath, CompressionLevel.Optimal, false);
        }
        finally
        {
            // Cleanup temp directory
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Restores a backup from a .hdi file, replacing the current database.
    /// Performs safe extraction with path traversal validation.
    /// </summary>
    public static async Task RestoreBackupAsync(string backupPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(backupPath))
            throw new FileNotFoundException("Backup file not found", backupPath);

        var dbPath = DatabaseService.GetDefaultDatabasePath();
        var tempDir = Path.Combine(Path.GetTempPath(), $"hdi-restore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Extract backup safely with path traversal validation
            await ExtractZipSafelyAsync(backupPath, tempDir, cancellationToken);

            // Verify metadata
            var metadataPath = Path.Combine(tempDir, "metadata.json");
            if (!File.Exists(metadataPath))
                throw new InvalidOperationException("Invalid backup file: missing metadata");

            var metadataJson = await File.ReadAllTextAsync(metadataPath, cancellationToken);
            var metadata = JsonSerializer.Deserialize<BackupMetadata>(metadataJson);

            if (!string.Equals(metadata?.AppName, AppConstants.AppName, StringComparison.Ordinal))
                throw new InvalidOperationException($"Invalid backup file: not a {AppConstants.AppName} backup");

            // Find database file
            var dbBackupPath = Path.Combine(tempDir, AppConstants.DatabaseFileName);
            if (!File.Exists(dbBackupPath))
                throw new InvalidOperationException("Invalid backup file: missing database");

            // Create backup of current database before restoring
            var currentBackupPath = dbPath + ".pre-restore-backup";
            if (File.Exists(dbPath))
            {
                File.Copy(dbPath, currentBackupPath, overwrite: true);
            }

            // Replace database file
            File.Copy(dbBackupPath, dbPath, overwrite: true);

            // Also delete WAL and SHM files if they exist
            var walPath = dbPath + "-wal";
            var shmPath = dbPath + "-shm";
            if (File.Exists(walPath)) File.Delete(walPath);
            if (File.Exists(shmPath)) File.Delete(shmPath);
        }
        finally
        {
            // Cleanup temp directory
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Reads metadata from a backup file without extracting the database
    /// </summary>
    public static async Task<BackupMetadata?> ReadBackupMetadataAsync(string backupPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(backupPath))
            return null;

        try
        {
            using var archive = ZipFile.OpenRead(backupPath);
            var metadataEntry = archive.GetEntry("metadata.json");
            if (metadataEntry is null)
                return null;

            using var stream = metadataEntry.Open();
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync(cancellationToken);
            return JsonSerializer.Deserialize<BackupMetadata>(json);
        }
        catch
        {
            return null;
        }
    }

    private async Task<int> GetLocationCountAsync(CancellationToken cancellationToken)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM locations";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    private async Task<long> GetFileCountAsync(CancellationToken cancellationToken)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM files";
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Safely extracts a ZIP file with path traversal validation.
    /// Prevents "Zip Slip" attacks where malicious archives contain entries with paths like "../../../etc/passwd".
    /// </summary>
    private static async Task ExtractZipSafelyAsync(string zipPath, string destinationDir, CancellationToken cancellationToken)
    {
        var fullDestinationDir = Path.GetFullPath(destinationDir);

        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip directories (they're created automatically when extracting files)
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            var destinationPath = Path.GetFullPath(Path.Combine(fullDestinationDir, entry.FullName));

            // Validate that the destination path is within the target directory (prevents path traversal)
            if (!destinationPath.StartsWith(fullDestinationDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                !destinationPath.Equals(fullDestinationDir, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Invalid backup file: entry '{entry.FullName}' would extract outside the target directory (potential path traversal attack)");
            }

            // Ensure the directory exists
            var entryDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(entryDir))
                Directory.CreateDirectory(entryDir);

            // Extract the entry
            await using var entryStream = entry.Open();
            await using var fileStream = File.Create(destinationPath);
            await entryStream.CopyToAsync(fileStream, cancellationToken);
        }
    }
}

public class BackupMetadata
{
    public string AppName { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public DateTime BackupDate { get; set; }
    public int LocationCount { get; set; }
    public long FileCount { get; set; }
}
