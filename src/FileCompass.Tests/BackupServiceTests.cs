using System.IO.Compression;
using System.Text.Json;
using FileCompass.Core.Constants;
using FileCompass.Core.Data;
using FileCompass.Core.Models;
using FileCompass.Core.Services;

namespace FileCompass.Tests;

public class BackupServiceTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly string _tempDir;
    private readonly DatabaseService _db;
    private readonly LocationRepository _locationRepo;
    private readonly FileRepository _fileRepo;
    private readonly BackupService _backupService;

    public BackupServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"backup_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _tempDbPath = Path.Combine(_tempDir, "test.db");
        _db = new DatabaseService(_tempDbPath);
        _locationRepo = new LocationRepository(_db);
        _fileRepo = new FileRepository(_db);
        _backupService = new BackupService(_db);
    }

    #region CreateBackupAsync Tests

    [Fact]
    public async Task CreateBackupAsync_CreatesZipFile()
    {
        await _db.InitializeAsync();

        var backupPath = Path.Combine(_tempDir, "backup.fci");
        await _backupService.CreateBackupAsync(backupPath);

        Assert.True(File.Exists(backupPath));
    }

    [Fact]
    public async Task CreateBackupAsync_ContainsDatabaseFile()
    {
        await _db.InitializeAsync();

        var backupPath = Path.Combine(_tempDir, "backup.fci");
        await _backupService.CreateBackupAsync(backupPath);

        using var archive = ZipFile.OpenRead(backupPath);
        var dbEntry = archive.GetEntry(AppConstants.DatabaseFileName);
        Assert.NotNull(dbEntry);
    }

    [Fact]
    public async Task CreateBackupAsync_ContainsMetadata()
    {
        await _db.InitializeAsync();

        var backupPath = Path.Combine(_tempDir, "backup.fci");
        await _backupService.CreateBackupAsync(backupPath);

        using var archive = ZipFile.OpenRead(backupPath);
        var metadataEntry = archive.GetEntry("metadata.json");
        Assert.NotNull(metadataEntry);

        using var stream = metadataEntry.Open();
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        var metadata = JsonSerializer.Deserialize<BackupMetadata>(json);

        Assert.NotNull(metadata);
        Assert.Equal(AppConstants.AppName, metadata.AppName);
        Assert.Equal(AppConstants.AppVersion, metadata.AppVersion);
    }

    [Fact]
    public async Task CreateBackupAsync_MetadataContainsCorrectCounts()
    {
        await _db.InitializeAsync();

        // Add some test data
        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });
        var files = new List<FileEntry>
        {
            new() { LocationId = location.Id, Name = "file1.txt", RelativePath = "file1.txt", Size = 100 },
            new() { LocationId = location.Id, Name = "file2.txt", RelativePath = "file2.txt", Size = 200 }
        };
        await _fileRepo.AddBatchAsync(files);

        var backupPath = Path.Combine(_tempDir, "backup.fci");
        await _backupService.CreateBackupAsync(backupPath);

        var metadata = await BackupService.ReadBackupMetadataAsync(backupPath);

        Assert.NotNull(metadata);
        Assert.Equal(1, metadata.LocationCount);
        Assert.Equal(2, metadata.FileCount);
    }

    [Fact]
    public async Task CreateBackupAsync_OverwritesExistingFile()
    {
        await _db.InitializeAsync();

        var backupPath = Path.Combine(_tempDir, "backup.fci");

        // Create initial backup
        await _backupService.CreateBackupAsync(backupPath);
        var initialSize = new FileInfo(backupPath).Length;

        // Add more data
        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });
        var files = Enumerable.Range(1, 100).Select(i => new FileEntry
        {
            LocationId = location.Id,
            Name = $"file{i}.txt",
            RelativePath = $"file{i}.txt",
            Size = i * 1000
        }).ToList();
        await _fileRepo.AddBatchAsync(files);

        // Create second backup
        await _backupService.CreateBackupAsync(backupPath);
        var newSize = new FileInfo(backupPath).Length;

        // New backup should be larger (more data)
        Assert.True(newSize > initialSize);
    }

    [Fact]
    public async Task CreateBackupAsync_ThrowsWhenDatabaseNotInitialized()
    {
        // Don't initialize - database won't have tables
        var backupPath = Path.Combine(_tempDir, "backup.fci");

        // Should throw some exception (SqliteException for missing tables or InvalidOperationException)
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _backupService.CreateBackupAsync(backupPath));
    }

    #endregion

    #region ReadBackupMetadataAsync Tests

    [Fact]
    public async Task ReadBackupMetadataAsync_ReturnsNullForNonexistentFile()
    {
        var result = await BackupService.ReadBackupMetadataAsync("/nonexistent/path.fci");
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadBackupMetadataAsync_ReturnsNullForInvalidZip()
    {
        var invalidPath = Path.Combine(_tempDir, "invalid.fci");
        await File.WriteAllTextAsync(invalidPath, "not a zip file");

        var result = await BackupService.ReadBackupMetadataAsync(invalidPath);
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadBackupMetadataAsync_ReturnsNullForZipWithoutMetadata()
    {
        var zipPath = Path.Combine(_tempDir, "no_metadata.fci");
        var contentDir = Path.Combine(_tempDir, "zip_content");
        Directory.CreateDirectory(contentDir);
        await File.WriteAllTextAsync(Path.Combine(contentDir, "dummy.txt"), "content");
        ZipFile.CreateFromDirectory(contentDir, zipPath);

        var result = await BackupService.ReadBackupMetadataAsync(zipPath);
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadBackupMetadataAsync_ReadsBackupDate()
    {
        await _db.InitializeAsync();

        var beforeBackup = DateTime.UtcNow.AddSeconds(-1);
        var backupPath = Path.Combine(_tempDir, "backup.fci");
        await _backupService.CreateBackupAsync(backupPath);
        var afterBackup = DateTime.UtcNow.AddSeconds(1);

        var metadata = await BackupService.ReadBackupMetadataAsync(backupPath);

        Assert.NotNull(metadata);
        Assert.True(metadata.BackupDate >= beforeBackup);
        Assert.True(metadata.BackupDate <= afterBackup);
    }

    #endregion

    #region RestoreBackupAsync Tests

    [Fact]
    public async Task RestoreBackupAsync_ThrowsForNonexistentFile()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            BackupService.RestoreBackupAsync("/nonexistent/path.fci"));
    }

    [Fact]
    public async Task RestoreBackupAsync_ThrowsForMissingMetadata()
    {
        var zipPath = Path.Combine(_tempDir, "no_metadata.fci");
        var contentDir = Path.Combine(_tempDir, "zip_content_restore");
        Directory.CreateDirectory(contentDir);
        await File.WriteAllTextAsync(Path.Combine(contentDir, "dummy.txt"), "content");
        ZipFile.CreateFromDirectory(contentDir, zipPath);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BackupService.RestoreBackupAsync(zipPath));
    }

    [Fact]
    public async Task RestoreBackupAsync_ThrowsForWrongAppName()
    {
        var zipPath = Path.Combine(_tempDir, "wrong_app.fci");
        var contentDir = Path.Combine(_tempDir, "wrong_app_content");
        Directory.CreateDirectory(contentDir);

        var metadata = new BackupMetadata
        {
            AppName = "WrongApp",
            AppVersion = "1.0.0",
            BackupDate = DateTime.UtcNow
        };
        await File.WriteAllTextAsync(
            Path.Combine(contentDir, "metadata.json"),
            JsonSerializer.Serialize(metadata));

        ZipFile.CreateFromDirectory(contentDir, zipPath);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BackupService.RestoreBackupAsync(zipPath));
        Assert.Contains(AppConstants.AppName, ex.Message);
    }

    [Fact]
    public async Task RestoreBackupAsync_ThrowsForMissingDatabase()
    {
        var zipPath = Path.Combine(_tempDir, "no_db.fci");
        var contentDir = Path.Combine(_tempDir, "no_db_content");
        Directory.CreateDirectory(contentDir);

        var metadata = new BackupMetadata
        {
            AppName = AppConstants.AppName,
            AppVersion = "1.0.0",
            BackupDate = DateTime.UtcNow
        };
        await File.WriteAllTextAsync(
            Path.Combine(contentDir, "metadata.json"),
            JsonSerializer.Serialize(metadata));

        ZipFile.CreateFromDirectory(contentDir, zipPath);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BackupService.RestoreBackupAsync(zipPath));
        Assert.Contains("missing database", ex.Message);
    }

    #endregion

    #region Zip Slip Protection Tests

    [Fact]
    public async Task RestoreBackupAsync_PreventsPathTraversalAttack()
    {
        var maliciousZipPath = Path.Combine(_tempDir, "malicious.fci");

        // Create a ZIP with path traversal entry
        using (var zipStream = new FileStream(maliciousZipPath, FileMode.Create))
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
        {
            // Add valid metadata
            var metadataEntry = archive.CreateEntry("metadata.json");
            using (var metadataWriter = new StreamWriter(metadataEntry.Open()))
            {
                var metadata = new BackupMetadata
                {
                    AppName = AppConstants.AppName,
                    AppVersion = "1.0.0",
                    BackupDate = DateTime.UtcNow
                };
                await metadataWriter.WriteAsync(JsonSerializer.Serialize(metadata));
            }

            // Add malicious entry with path traversal
            var maliciousEntry = archive.CreateEntry("../../../evil.txt");
            using (var writer = new StreamWriter(maliciousEntry.Open()))
            {
                await writer.WriteAsync("malicious content");
            }

            // Add the database file
            var dbEntry = archive.CreateEntry(AppConstants.DatabaseFileName);
            using (var dbWriter = new StreamWriter(dbEntry.Open()))
            {
                await dbWriter.WriteAsync("fake db content");
            }
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BackupService.RestoreBackupAsync(maliciousZipPath));
        Assert.Contains("path traversal", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public async Task RestoreBackupAsync_PreventsAbsolutePathAttack()
    {
        var maliciousZipPath = Path.Combine(_tempDir, "absolute_path.fci");

        // Create a ZIP with absolute path entry
        using (var zipStream = new FileStream(maliciousZipPath, FileMode.Create))
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
        {
            // Add valid metadata
            var metadataEntry = archive.CreateEntry("metadata.json");
            using (var metadataWriter = new StreamWriter(metadataEntry.Open()))
            {
                var metadata = new BackupMetadata
                {
                    AppName = AppConstants.AppName,
                    AppVersion = "1.0.0",
                    BackupDate = DateTime.UtcNow
                };
                await metadataWriter.WriteAsync(JsonSerializer.Serialize(metadata));
            }

            // Add malicious entry with absolute path (Linux-style)
            var maliciousEntry = archive.CreateEntry("/etc/passwd");
            using (var writer = new StreamWriter(maliciousEntry.Open()))
            {
                await writer.WriteAsync("malicious content");
            }

            // Add the database file
            var dbEntry = archive.CreateEntry(AppConstants.DatabaseFileName);
            using (var dbWriter = new StreamWriter(dbEntry.Open()))
            {
                await dbWriter.WriteAsync("fake db content");
            }
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BackupService.RestoreBackupAsync(maliciousZipPath));
        Assert.Contains("path traversal", ex.Message.ToLowerInvariant());
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task CreateBackupAsync_RespectsCancellationToken()
    {
        await _db.InitializeAsync();

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var backupPath = Path.Combine(_tempDir, "backup.fci");

        // TaskCanceledException derives from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _backupService.CreateBackupAsync(backupPath, cts.Token));
    }

    #endregion

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _db.Dispose();

        // Clean up temp directory
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}
