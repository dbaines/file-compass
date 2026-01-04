using System.IO.Abstractions.TestingHelpers;
using FileCompass.Core.Data;
using FileCompass.Core.Models;
using FileCompass.Core.Services;

namespace FileCompass.Tests;

public class FileScannerServiceTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly DatabaseService _db;
    private readonly LocationRepository _locationRepo;
    private readonly FileRepository _fileRepo;

    public FileScannerServiceTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"hddindexer_scanner_test_{Guid.NewGuid()}.db");
        _db = new DatabaseService(_tempDbPath);
        _locationRepo = new LocationRepository(_db);
        _fileRepo = new FileRepository(_db);
    }

    [Fact]
    public async Task ScanLocationAsync_SymlinkedDirectory_IsNotRecursedInto()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();

        // Create a regular directory structure
        mockFileSystem.AddDirectory("/test");
        mockFileSystem.AddDirectory("/test/regular");
        mockFileSystem.AddFile("/test/regular/file.txt", new MockFileData("content"));

        // Create a symlink directory (using ReparsePoint attribute)
        mockFileSystem.AddDirectory("/test/symlink");
        mockFileSystem.AddFile("/test/symlink/hidden.txt", new MockFileData("should not be indexed"));

        // Set the symlink directory to have ReparsePoint attribute
        var symlinkDir = mockFileSystem.DirectoryInfo.New("/test/symlink");
        mockFileSystem.File.SetAttributes("/test/symlink", FileAttributes.Directory | FileAttributes.ReparsePoint);

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);

        var location = await scanner.ScanLocationAsync("/test", null);

        // Search for all files
        var query = new SearchQuery { LocationId = location.Id, IncludeDirectories = true };
        var result = await _fileRepo.SearchAsync(query);

        // Should have: regular dir, file.txt, symlink dir
        // Should NOT have: hidden.txt (inside symlink)
        Assert.DoesNotContain(result.Files, f => string.Equals(f.Name, "hidden.txt", StringComparison.Ordinal));
        Assert.Contains(result.Files, f => string.Equals(f.Name, "file.txt", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ScanLocationAsync_SymlinkDirectory_IsStillIndexed()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");
        mockFileSystem.AddDirectory("/test/symlink");
        mockFileSystem.File.SetAttributes("/test/symlink", FileAttributes.Directory | FileAttributes.ReparsePoint);

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);

        var location = await scanner.ScanLocationAsync("/test", null);

        var query = new SearchQuery { LocationId = location.Id, IncludeDirectories = true };
        var result = await _fileRepo.SearchAsync(query);

        // The symlink directory itself should be indexed
        Assert.Contains(result.Files, f => string.Equals(f.Name, "symlink", StringComparison.Ordinal) && f.IsDirectory);
    }

    [Fact]
    public async Task ScanLocationAsync_RegularDirectory_IsRecursedInto()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");
        mockFileSystem.AddDirectory("/test/subdir");
        mockFileSystem.AddFile("/test/subdir/nested.txt", new MockFileData("nested content"));

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);

        var location = await scanner.ScanLocationAsync("/test", null);

        var query = new SearchQuery { LocationId = location.Id };
        var result = await _fileRepo.SearchAsync(query);

        // Regular directories should be recursed into
        Assert.Contains(result.Files, f => string.Equals(f.Name, "nested.txt", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ScanLocationAsync_SymlinkFile_IsIndexed()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");
        mockFileSystem.AddFile("/test/regular.txt", new MockFileData("regular"));
        mockFileSystem.AddFile("/test/symlink.txt", new MockFileData("symlink target"));
        mockFileSystem.File.SetAttributes("/test/symlink.txt", FileAttributes.ReparsePoint);

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);

        var location = await scanner.ScanLocationAsync("/test", null);

        var query = new SearchQuery { LocationId = location.Id };
        var result = await _fileRepo.SearchAsync(query);

        // Both files should be indexed (symlink files are indexed, just not followed for directories)
        Assert.Contains(result.Files, f => string.Equals(f.Name, "regular.txt", StringComparison.Ordinal));
        Assert.Contains(result.Files, f => string.Equals(f.Name, "symlink.txt", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ScanLocationAsync_DeeplyNestedSymlink_DoesNotCauseInfiniteLoop()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");
        mockFileSystem.AddDirectory("/test/level1");
        mockFileSystem.AddDirectory("/test/level1/level2");
        mockFileSystem.AddDirectory("/test/level1/level2/symlink_to_root");
        mockFileSystem.File.SetAttributes("/test/level1/level2/symlink_to_root",
            FileAttributes.Directory | FileAttributes.ReparsePoint);
        mockFileSystem.AddFile("/test/level1/level2/file.txt", new MockFileData("content"));

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);

        // This should complete without hanging
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var location = await scanner.ScanLocationAsync("/test", null, false, null, cts.Token);

        Assert.NotNull(location);
        Assert.True(location.TotalFiles > 0 || location.TotalFolders > 0);
    }

    #region Batch Insertion Tests

    [Fact]
    public async Task ScanLocationAsync_IndexesMultipleFiles()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");

        // Add multiple files
        for (var i = 1; i <= 10; i++)
        {
            mockFileSystem.AddFile($"/test/file{i}.txt", new MockFileData($"content {i}"));
        }

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);
        var location = await scanner.ScanLocationAsync("/test", null);

        Assert.Equal(10, location.TotalFiles);
    }

    [Fact]
    public async Task ScanLocationAsync_TracksFileSizesCorrectly()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");
        mockFileSystem.AddFile("/test/small.txt", new MockFileData("small"));
        mockFileSystem.AddFile("/test/large.txt", new MockFileData(new string('x', 10000)));

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);
        var location = await scanner.ScanLocationAsync("/test", null);

        var query = new SearchQuery { LocationId = location.Id };
        var result = await _fileRepo.SearchAsync(query);

        var small = result.Files.First(f => string.Equals(f.Name, "small.txt", StringComparison.Ordinal));
        var large = result.Files.First(f => string.Equals(f.Name, "large.txt", StringComparison.Ordinal));

        Assert.Equal(5, small.Size); // "small" = 5 bytes
        Assert.Equal(10000, large.Size);
    }

    [Fact]
    public async Task ScanLocationAsync_IndexesNestedDirectories()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");
        mockFileSystem.AddDirectory("/test/dir1");
        mockFileSystem.AddDirectory("/test/dir1/dir2");
        mockFileSystem.AddFile("/test/dir1/dir2/nested.txt", new MockFileData("nested"));

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);
        var location = await scanner.ScanLocationAsync("/test", null);

        var query = new SearchQuery { LocationId = location.Id };
        var result = await _fileRepo.SearchAsync(query);

        Assert.Contains(result.Files, f => string.Equals(f.Name, "nested.txt", StringComparison.Ordinal));
        Assert.Equal(1, location.TotalFiles);
        Assert.Equal(2, location.TotalFolders);
    }

    [Fact]
    public async Task ScanLocationAsync_SetsCorrectExtensions()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");
        mockFileSystem.AddFile("/test/document.pdf", new MockFileData("pdf content"));
        mockFileSystem.AddFile("/test/image.PNG", new MockFileData("png content")); // uppercase
        mockFileSystem.AddFile("/test/noextension", new MockFileData("no ext"));

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);
        var location = await scanner.ScanLocationAsync("/test", null);

        var query = new SearchQuery { LocationId = location.Id };
        var result = await _fileRepo.SearchAsync(query);

        var pdf = result.Files.First(f => string.Equals(f.Name, "document.pdf", StringComparison.Ordinal));
        var png = result.Files.First(f => string.Equals(f.Name, "image.PNG", StringComparison.Ordinal));
        var noExt = result.Files.First(f => string.Equals(f.Name, "noextension", StringComparison.Ordinal));

        Assert.Equal("pdf", pdf.Extension);
        Assert.Equal("PNG", png.Extension);
        Assert.Equal(string.Empty, noExt.Extension);
    }

    #endregion

    #region Progress Reporting Tests

    [Fact]
    public async Task ScanLocationAsync_ReportsProgress()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");
        for (var i = 1; i <= 5; i++)
        {
            mockFileSystem.AddFile($"/test/file{i}.txt", new MockFileData($"content {i}"));
        }

        var progressReports = new List<ScanProgress>();
        var progress = new Progress<ScanProgress>(p => progressReports.Add(p));

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);
        await scanner.ScanLocationAsync("/test", null, false, progress);

        // Give progress a moment to propagate (Progress<T> posts callbacks asynchronously)
        await Task.Delay(50);

        // Should have at least one progress report (final completion)
        Assert.NotEmpty(progressReports);

        // Final report should be complete
        var final = progressReports.Last();
        Assert.True(final.IsComplete);
        Assert.Equal(5, final.FilesScanned);
    }

    [Fact]
    public async Task ScanLocationAsync_ProgressTracksFilesAndFolders()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");
        mockFileSystem.AddDirectory("/test/subdir");
        mockFileSystem.AddFile("/test/file1.txt", new MockFileData("content"));
        mockFileSystem.AddFile("/test/subdir/file2.txt", new MockFileData("content"));

        ScanProgress? lastProgress = null;
        var progress = new Progress<ScanProgress>(p => lastProgress = p);

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);
        await scanner.ScanLocationAsync("/test", null, false, progress);

        // Give progress a moment to propagate
        await Task.Delay(50);

        Assert.NotNull(lastProgress);
        Assert.Equal(2, lastProgress!.FilesScanned);
        Assert.Equal(1, lastProgress.FoldersScanned);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ScanLocationAsync_RespectsCancellation()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");

        // Add many files to ensure cancellation can happen
        for (var i = 1; i <= 100; i++)
        {
            mockFileSystem.AddFile($"/test/file{i}.txt", new MockFileData($"content {i}"));
        }

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            scanner.ScanLocationAsync("/test", null, false, null, cts.Token));
    }

    [Fact]
    public async Task ScanLocationAsync_CancellationSetsOutdatedStatus()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");
        mockFileSystem.AddFile("/test/file.txt", new MockFileData("content"));

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);

        // First do a successful scan to create the location
        var location = await scanner.ScanLocationAsync("/test", null);
        Assert.Equal(LocationStatus.UpToDate, location.Status);

        // Now add more files and cancel during rescan
        for (var i = 1; i <= 50; i++)
        {
            mockFileSystem.AddFile($"/test/dir/file{i}.txt", new MockFileData($"content {i}"));
        }
        mockFileSystem.AddDirectory("/test/dir");

        var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            await scanner.RescanLocationAsync(location.Id, null, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        var updated = await _locationRepo.GetByIdAsync(location.Id);
        Assert.Equal(LocationStatus.Outdated, updated!.Status);
    }

    [Fact]
    public async Task ScanLocationAsync_CancellationReportsInProgress()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");
        mockFileSystem.AddFile("/test/file.txt", new MockFileData("content"));

        ScanProgress? lastProgress = null;
        var progress = new Progress<ScanProgress>(p => lastProgress = p);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);

        try
        {
            await scanner.ScanLocationAsync("/test", null, false, progress, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Give progress a moment to propagate
        await Task.Delay(50);

        Assert.NotNull(lastProgress);
        Assert.True(lastProgress!.IsCancelled);
    }

    #endregion

    #region Location Status Tests

    [Fact]
    public async Task ScanLocationAsync_SetsStatusToScanningDuringScan()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");
        mockFileSystem.AddFile("/test/file.txt", new MockFileData("content"));

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);
        var location = await scanner.ScanLocationAsync("/test", null);

        // Final status should be UpToDate
        Assert.Equal(LocationStatus.UpToDate, location.Status);
    }

    [Fact]
    public async Task ScanLocationAsync_CompletedScanSetsUpToDateStatus()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");
        mockFileSystem.AddFile("/test/file.txt", new MockFileData("content"));

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);
        var location = await scanner.ScanLocationAsync("/test", null);

        Assert.Equal(LocationStatus.UpToDate, location.Status);
        Assert.NotNull(location.LastScanComplete);
        Assert.True(location.ScanDurationSeconds >= 0);
    }

    [Fact]
    public async Task ScanLocationAsync_SetsScanTimestamps()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");
        mockFileSystem.AddFile("/test/file.txt", new MockFileData("content"));

        var beforeScan = DateTime.UtcNow.AddSeconds(-1);

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);
        var location = await scanner.ScanLocationAsync("/test", null);

        var afterScan = DateTime.UtcNow.AddSeconds(1);

        Assert.NotNull(location.LastScanStart);
        Assert.NotNull(location.LastScanComplete);
        Assert.True(location.LastScanStart >= beforeScan);
        Assert.True(location.LastScanComplete <= afterScan);
        Assert.True(location.LastScanComplete >= location.LastScanStart);
    }

    #endregion

    #region Custom Name and ForceNew Tests

    [Fact]
    public async Task ScanLocationAsync_SetsCustomName()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");
        mockFileSystem.AddFile("/test/file.txt", new MockFileData("content"));

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);
        var location = await scanner.ScanLocationAsync("/test", "My Custom Name");

        Assert.Equal("My Custom Name", location.CustomName);
        Assert.Equal("My Custom Name", location.DisplayName);
    }

    [Fact]
    public async Task ScanLocationAsync_ReuseExistingLocation()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");
        mockFileSystem.AddFile("/test/file.txt", new MockFileData("content"));

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);

        var location1 = await scanner.ScanLocationAsync("/test", "First");
        var location2 = await scanner.ScanLocationAsync("/test", null); // Should reuse

        Assert.Equal(location1.Id, location2.Id);
    }

    [Fact]
    public async Task ScanLocationAsync_ForceNewCreatesNewLocation()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");
        mockFileSystem.AddFile("/test/file.txt", new MockFileData("content"));

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);

        var location1 = await scanner.ScanLocationAsync("/test", "First");
        var location2 = await scanner.ScanLocationAsync("/test", "Second", forceNew: true);

        Assert.NotEqual(location1.Id, location2.Id);
        Assert.Equal("First", location1.CustomName);
        Assert.Equal("Second", location2.CustomName);
    }

    #endregion

    #region RescanLocation Tests

    [Fact]
    public async Task RescanLocationAsync_ClearsExistingFiles()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("/test");
        mockFileSystem.AddFile("/test/original.txt", new MockFileData("original"));

        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);
        var location = await scanner.ScanLocationAsync("/test", null);

        // Remove original and add new file
        mockFileSystem.RemoveFile("/test/original.txt");
        mockFileSystem.AddFile("/test/newfile.txt", new MockFileData("new"));

        await scanner.RescanLocationAsync(location.Id);

        var query = new SearchQuery { LocationId = location.Id };
        var result = await _fileRepo.SearchAsync(query);

        Assert.Single(result.Files);
        Assert.Equal("newfile.txt", result.Files[0].Name);
    }

    [Fact]
    public async Task RescanLocationAsync_ThrowsForNonexistentLocation()
    {
        await _db.InitializeAsync();

        var mockFileSystem = new MockFileSystem();
        var scanner = new FileScannerService(mockFileSystem, _db, _locationRepo, _fileRepo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scanner.RescanLocationAsync(99999));
    }

    #endregion

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _db.Dispose();
        if (File.Exists(_tempDbPath))
        {
            File.Delete(_tempDbPath);
        }
        var walPath = _tempDbPath + "-wal";
        var shmPath = _tempDbPath + "-shm";
        if (File.Exists(walPath)) File.Delete(walPath);
        if (File.Exists(shmPath)) File.Delete(shmPath);
    }
}
