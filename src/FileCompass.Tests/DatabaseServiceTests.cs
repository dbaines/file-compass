using FileCompass.Core.Data;
using FileCompass.Core.Models;

namespace FileCompass.Tests;

public class DatabaseServiceTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly DatabaseService _db;
    private readonly LocationRepository _locationRepo;
    private readonly FileRepository _fileRepo;
    private readonly SettingsRepository _settingsRepo;

    public DatabaseServiceTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"hddindexer_test_{Guid.NewGuid()}.db");
        _db = new DatabaseService(_tempDbPath);
        _locationRepo = new LocationRepository(_db);
        _fileRepo = new FileRepository(_db);
        _settingsRepo = new SettingsRepository(_db);
    }

    #region DatabaseService Tests

    [Fact]
    public async Task DatabaseService_InitializesSuccessfully()
    {
        await _db.InitializeAsync();
        Assert.True(File.Exists(_tempDbPath));
    }

    [Fact]
    public async Task DatabaseService_CanGetConnection()
    {
        await _db.InitializeAsync();
        var conn = await _db.GetConnectionAsync();
        Assert.NotNull(conn);
        Assert.Equal(System.Data.ConnectionState.Open, conn.State);
    }

    #endregion

    #region LocationRepository Tests

    [Fact]
    public async Task LocationRepository_AddAndRetrieve()
    {
        await _db.InitializeAsync();

        var location = new Location
        {
            Path = "/test/path",
            CustomName = "Test Location"
        };

        var added = await _locationRepo.AddAsync(location);
        Assert.True(added.Id > 0);

        var retrieved = await _locationRepo.GetByIdAsync(added.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("/test/path", retrieved.Path);
        Assert.Equal("Test Location", retrieved.CustomName);
    }

    [Fact]
    public async Task LocationRepository_GetAll()
    {
        await _db.InitializeAsync();

        await _locationRepo.AddAsync(new Location { Path = "/path1" });
        await _locationRepo.AddAsync(new Location { Path = "/path2" });

        var all = await _locationRepo.GetAllAsync();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task LocationRepository_Update()
    {
        await _db.InitializeAsync();

        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });
        location.CustomName = "Updated Name";
        location.TotalFiles = 100;
        location.TotalFolders = 10;

        await _locationRepo.UpdateAsync(location);

        var retrieved = await _locationRepo.GetByIdAsync(location.Id);
        Assert.Equal("Updated Name", retrieved!.CustomName);
        Assert.Equal(100, retrieved.TotalFiles);
        Assert.Equal(10, retrieved.TotalFolders);
    }

    [Fact]
    public async Task LocationRepository_Delete()
    {
        await _db.InitializeAsync();

        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });
        await _locationRepo.DeleteAsync(location.Id);

        var retrieved = await _locationRepo.GetByIdAsync(location.Id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task LocationRepository_GetByPath()
    {
        await _db.InitializeAsync();

        await _locationRepo.AddAsync(new Location { Path = "/unique/path" });

        var retrieved = await _locationRepo.GetByPathAsync("/unique/path");
        Assert.NotNull(retrieved);
        Assert.Equal("/unique/path", retrieved.Path);
    }

    [Fact]
    public async Task LocationRepository_GetByPath_ReturnsNullForNonexistent()
    {
        await _db.InitializeAsync();

        var retrieved = await _locationRepo.GetByPathAsync("/nonexistent");
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task LocationRepository_GetAllByPath_ReturnsMultipleLocations()
    {
        await _db.InitializeAsync();

        // Add multiple locations with the same path (simulating different drives at same mount point)
        await _locationRepo.AddAsync(new Location { Path = "/mnt/external", CustomName = "Drive A" });
        await _locationRepo.AddAsync(new Location { Path = "/mnt/external", CustomName = "Drive B" });
        await _locationRepo.AddAsync(new Location { Path = "/mnt/other" });

        var retrieved = await _locationRepo.GetAllByPathAsync("/mnt/external");
        Assert.Equal(2, retrieved.Count);
        Assert.Contains(retrieved, l => string.Equals(l.CustomName, "Drive A", StringComparison.Ordinal));
        Assert.Contains(retrieved, l => string.Equals(l.CustomName, "Drive B", StringComparison.Ordinal));
    }

    [Fact]
    public async Task LocationRepository_GetAllByPath_ReturnsEmptyForNonexistent()
    {
        await _db.InitializeAsync();

        var retrieved = await _locationRepo.GetAllByPathAsync("/nonexistent");
        Assert.Empty(retrieved);
    }

    #endregion

    #region FileRepository Tests

    [Fact]
    public async Task FileRepository_AddBatchAndSearch()
    {
        await _db.InitializeAsync();

        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });

        var files = new List<FileEntry>
        {
            new() { LocationId = location.Id, Name = "document.pdf", Extension = "pdf", RelativePath = "document.pdf", Size = 1024 },
            new() { LocationId = location.Id, Name = "image.png", Extension = "png", RelativePath = "image.png", Size = 2048 },
            new() { LocationId = location.Id, Name = "another_document.pdf", Extension = "pdf", RelativePath = "another_document.pdf", Size = 512 }
        };

        await _fileRepo.AddBatchAsync(files);

        var query = new SearchQuery { SearchTerm = "document" };
        var result = await _fileRepo.SearchAsync(query);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task FileRepository_SearchByExtension()
    {
        await _db.InitializeAsync();

        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });

        var files = new List<FileEntry>
        {
            new() { LocationId = location.Id, Name = "doc1.pdf", Extension = "pdf", RelativePath = "doc1.pdf", Size = 100 },
            new() { LocationId = location.Id, Name = "doc2.pdf", Extension = "pdf", RelativePath = "doc2.pdf", Size = 200 },
            new() { LocationId = location.Id, Name = "image.png", Extension = "png", RelativePath = "image.png", Size = 300 }
        };

        await _fileRepo.AddBatchAsync(files);

        var query = new SearchQuery { Extension = "pdf" };
        var result = await _fileRepo.SearchAsync(query);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task FileRepository_SearchBySizeRange()
    {
        await _db.InitializeAsync();

        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });

        var files = new List<FileEntry>
        {
            new() { LocationId = location.Id, Name = "small.txt", Extension = "txt", RelativePath = "small.txt", Size = 100 },
            new() { LocationId = location.Id, Name = "medium.txt", Extension = "txt", RelativePath = "medium.txt", Size = 5000 },
            new() { LocationId = location.Id, Name = "large.txt", Extension = "txt", RelativePath = "large.txt", Size = 100000 }
        };

        await _fileRepo.AddBatchAsync(files);

        var query = new SearchQuery { MinSize = 1000, MaxSize = 10000 };
        var result = await _fileRepo.SearchAsync(query);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("medium.txt", result.Files[0].Name);
    }

    [Fact]
    public async Task FileRepository_SearchByCategory()
    {
        await _db.InitializeAsync();

        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });

        var files = new List<FileEntry>
        {
            new() { LocationId = location.Id, Name = "photo.jpg", Extension = "jpg", RelativePath = "photo.jpg", Size = 1000 },
            new() { LocationId = location.Id, Name = "photo2.png", Extension = "png", RelativePath = "photo2.png", Size = 2000 },
            new() { LocationId = location.Id, Name = "doc.pdf", Extension = "pdf", RelativePath = "doc.pdf", Size = 500 }
        };

        await _fileRepo.AddBatchAsync(files);

        var query = new SearchQuery { FileCategory = "Images" };
        var result = await _fileRepo.SearchAsync(query);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task FileRepository_SearchByLocation()
    {
        await _db.InitializeAsync();

        var location1 = await _locationRepo.AddAsync(new Location { Path = "/location1" });
        var location2 = await _locationRepo.AddAsync(new Location { Path = "/location2" });

        var files = new List<FileEntry>
        {
            new() { LocationId = location1.Id, Name = "file1.txt", Extension = "txt", RelativePath = "file1.txt", Size = 100 },
            new() { LocationId = location2.Id, Name = "file2.txt", Extension = "txt", RelativePath = "file2.txt", Size = 200 }
        };

        await _fileRepo.AddBatchAsync(files);

        var query = new SearchQuery { LocationId = location1.Id };
        var result = await _fileRepo.SearchAsync(query);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("file1.txt", result.Files[0].Name);
    }

    [Fact]
    public async Task FileRepository_SearchExcludesDirectoriesByDefault()
    {
        await _db.InitializeAsync();

        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });

        var files = new List<FileEntry>
        {
            new() { LocationId = location.Id, Name = "folder", Extension = null, RelativePath = "folder", Size = 0, IsDirectory = true },
            new() { LocationId = location.Id, Name = "file.txt", Extension = "txt", RelativePath = "file.txt", Size = 100, IsDirectory = false }
        };

        await _fileRepo.AddBatchAsync(files);

        var query = new SearchQuery { IncludeDirectories = false };
        var result = await _fileRepo.SearchAsync(query);

        Assert.Equal(1, result.TotalCount);
        Assert.False(result.Files[0].IsDirectory);
    }

    [Fact]
    public async Task FileRepository_SearchIncludesDirectories()
    {
        await _db.InitializeAsync();

        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });

        var files = new List<FileEntry>
        {
            new() { LocationId = location.Id, Name = "folder", Extension = null, RelativePath = "folder", Size = 0, IsDirectory = true },
            new() { LocationId = location.Id, Name = "file.txt", Extension = "txt", RelativePath = "file.txt", Size = 100, IsDirectory = false }
        };

        await _fileRepo.AddBatchAsync(files);

        var query = new SearchQuery { IncludeDirectories = true };
        var result = await _fileRepo.SearchAsync(query);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task FileRepository_DeleteByLocation()
    {
        await _db.InitializeAsync();

        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });

        var files = new List<FileEntry>
        {
            new() { LocationId = location.Id, Name = "file1.txt", Extension = "txt", RelativePath = "file1.txt", Size = 100 },
            new() { LocationId = location.Id, Name = "file2.txt", Extension = "txt", RelativePath = "file2.txt", Size = 200 }
        };

        await _fileRepo.AddBatchAsync(files);
        await _fileRepo.DeleteByLocationAsync(location.Id);

        var query = new SearchQuery { LocationId = location.Id };
        var result = await _fileRepo.SearchAsync(query);

        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task FileRepository_GetByParent()
    {
        await _db.InitializeAsync();

        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });

        var files = new List<FileEntry>
        {
            new() { LocationId = location.Id, Name = "root.txt", Extension = "txt", RelativePath = "root.txt", Size = 100, ParentId = null }
        };

        await _fileRepo.AddBatchAsync(files);

        var rootFiles = await _fileRepo.GetByParentAsync(location.Id, null);
        Assert.Single(rootFiles);
        Assert.Equal("root.txt", rootFiles[0].Name);
    }

    [Fact]
    public async Task FileRepository_SearchReturnsLocationName()
    {
        await _db.InitializeAsync();

        var location = await _locationRepo.AddAsync(new Location { Path = "/test", CustomName = "My Drive" });

        var files = new List<FileEntry>
        {
            new() { LocationId = location.Id, Name = "file.txt", Extension = "txt", RelativePath = "file.txt", Size = 100 }
        };

        await _fileRepo.AddBatchAsync(files);

        var query = new SearchQuery { SearchTerm = "file" };
        var result = await _fileRepo.SearchAsync(query);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("My Drive", result.Files[0].LocationName);
    }

    [Fact]
    public async Task FileRepository_SearchPagination()
    {
        await _db.InitializeAsync();

        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });

        var files = Enumerable.Range(1, 50).Select(i => new FileEntry
        {
            LocationId = location.Id,
            Name = $"file{i:D2}.txt",
            Extension = "txt",
            RelativePath = $"file{i:D2}.txt",
            Size = i * 100
        }).ToList();

        await _fileRepo.AddBatchAsync(files);

        var query = new SearchQuery { MaxResults = 10, Offset = 0 };
        var result = await _fileRepo.SearchAsync(query);

        Assert.Equal(50, result.TotalCount);
        Assert.Equal(10, result.Files.Count);
    }

    #endregion

    #region SettingsRepository Tests

    [Fact]
    public async Task SettingsRepository_SetAndGet()
    {
        await _db.InitializeAsync();

        await _settingsRepo.SetAsync("test_key", "test_value");
        var value = await _settingsRepo.GetAsync("test_key");

        Assert.Equal("test_value", value);
    }

    [Fact]
    public async Task SettingsRepository_GetNonexistent_ReturnsNull()
    {
        await _db.InitializeAsync();

        var value = await _settingsRepo.GetAsync("nonexistent");
        Assert.Null(value);
    }

    [Fact]
    public async Task SettingsRepository_UpdateExisting()
    {
        await _db.InitializeAsync();

        await _settingsRepo.SetAsync("key", "value1");
        await _settingsRepo.SetAsync("key", "value2");

        var value = await _settingsRepo.GetAsync("key");
        Assert.Equal("value2", value);
    }

    [Fact]
    public async Task SettingsRepository_DeleteBySettingNull()
    {
        await _db.InitializeAsync();

        await _settingsRepo.SetAsync("key", "value");
        await _settingsRepo.SetAsync("key", null);

        var value = await _settingsRepo.GetAsync("key");
        Assert.Null(value);
    }

    [Fact]
    public async Task SettingsRepository_SearchHistory_AddAndRetrieve()
    {
        await _db.InitializeAsync();

        await _settingsRepo.AddSearchHistoryAsync("search1");
        await _settingsRepo.AddSearchHistoryAsync("search2");

        var history = await _settingsRepo.GetSearchHistoryAsync();

        Assert.Equal(2, history.Count);
        Assert.Equal("search2", history[0]); // Most recent first
        Assert.Equal("search1", history[1]);
    }

    [Fact]
    public async Task SettingsRepository_SearchHistory_NoDuplicates()
    {
        await _db.InitializeAsync();

        await _settingsRepo.AddSearchHistoryAsync("search1");
        await _settingsRepo.AddSearchHistoryAsync("search2");
        await _settingsRepo.AddSearchHistoryAsync("search1"); // Duplicate

        var history = await _settingsRepo.GetSearchHistoryAsync();

        Assert.Equal(2, history.Count);
        Assert.Equal("search1", history[0]); // Moved to top
    }

    [Fact]
    public async Task SettingsRepository_SearchHistory_Clear()
    {
        await _db.InitializeAsync();

        await _settingsRepo.AddSearchHistoryAsync("search1");
        await _settingsRepo.ClearSearchHistoryAsync();

        var history = await _settingsRepo.GetSearchHistoryAsync();
        Assert.Empty(history);
    }

    [Fact]
    public async Task SettingsRepository_SearchHistory_IgnoresEmpty()
    {
        await _db.InitializeAsync();

        await _settingsRepo.AddSearchHistoryAsync("");
        await _settingsRepo.AddSearchHistoryAsync("   ");

        var history = await _settingsRepo.GetSearchHistoryAsync();
        Assert.Empty(history);
    }

    #endregion

    #region Error Path Tests - LocationRepository

    [Fact]
    public async Task LocationRepository_GetByIdAsync_ReturnsNullForNonexistentId()
    {
        await _db.InitializeAsync();

        var result = await _locationRepo.GetByIdAsync(99999);
        Assert.Null(result);
    }

    [Fact]
    public async Task LocationRepository_DeleteAsync_HandlesNonexistentId()
    {
        await _db.InitializeAsync();

        // Should not throw
        await _locationRepo.DeleteAsync(99999);

        // Verify nothing was affected
        var all = await _locationRepo.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task LocationRepository_UpdateAsync_HandlesCompleteLocation()
    {
        await _db.InitializeAsync();

        var location = new Location
        {
            Path = "/test/complete",
            CustomName = "Complete Location",
            VolumeLabel = "TestVol",
            FileSystem = "ext4",
            TotalSize = 1000000000,
            FreeSpace = 500000000,
            TotalFiles = 1000,
            TotalFolders = 100,
            LastScanStart = DateTime.UtcNow.AddMinutes(-5),
            LastScanComplete = DateTime.UtcNow,
            ScanDurationSeconds = 300,
            Status = LocationStatus.UpToDate,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var added = await _locationRepo.AddAsync(location);
        added.CustomName = "Updated Complete";
        added.Status = LocationStatus.Outdated;

        await _locationRepo.UpdateAsync(added);

        var retrieved = await _locationRepo.GetByIdAsync(added.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Updated Complete", retrieved.CustomName);
        Assert.Equal(LocationStatus.Outdated, retrieved.Status);
        Assert.Equal("TestVol", retrieved.VolumeLabel);
    }

    #endregion

    #region Error Path Tests - FileRepository

    [Fact]
    public async Task FileRepository_SearchAsync_HandlesEmptyDatabase()
    {
        await _db.InitializeAsync();

        var query = new SearchQuery { SearchTerm = "anything" };
        var result = await _fileRepo.SearchAsync(query);

        Assert.Empty(result.Files);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task FileRepository_SearchAsync_HandlesSpecialCharactersInSearchTerm()
    {
        await _db.InitializeAsync();

        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });

        var files = new List<FileEntry>
        {
            new() { LocationId = location.Id, Name = "test_file.txt", Extension = "txt", RelativePath = "test_file.txt", Size = 100 },
            new() { LocationId = location.Id, Name = "another_test.txt", Extension = "txt", RelativePath = "another_test.txt", Size = 100 }
        };
        await _fileRepo.AddBatchAsync(files);

        // Search with underscore (which is safe in FTS5)
        var query = new SearchQuery { SearchTerm = "test_file" };
        var result = await _fileRepo.SearchAsync(query);

        // Should find results without throwing
        Assert.True(result.TotalCount >= 1);
    }

    [Fact]
    public async Task FileRepository_SearchAsync_HandlesQuotesInSearchTerm()
    {
        await _db.InitializeAsync();

        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });

        var files = new List<FileEntry>
        {
            new() { LocationId = location.Id, Name = "file with 'quotes'.txt", Extension = "txt", RelativePath = "file with 'quotes'.txt", Size = 100 }
        };
        await _fileRepo.AddBatchAsync(files);

        // Quotes should be handled properly
        var query = new SearchQuery { SearchTerm = "quotes" };
        var result = await _fileRepo.SearchAsync(query);

        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task FileRepository_AddBatchAsync_HandlesEmptyBatch()
    {
        await _db.InitializeAsync();

        // Should not throw
        await _fileRepo.AddBatchAsync([]);

        var query = new SearchQuery();
        var result = await _fileRepo.SearchAsync(query);
        Assert.Empty(result.Files);
    }

    [Fact]
    public async Task FileRepository_DeleteByLocationAsync_HandlesNonexistentLocation()
    {
        await _db.InitializeAsync();

        // Should not throw
        await _fileRepo.DeleteByLocationAsync(99999);
    }

    [Fact]
    public async Task FileRepository_GetByParentAsync_ReturnsEmptyForNonexistentLocation()
    {
        await _db.InitializeAsync();

        var result = await _fileRepo.GetByParentAsync(99999, null);
        Assert.Empty(result);
    }

    [Fact]
    public async Task FileRepository_SearchAsync_DateFilterWorks()
    {
        await _db.InitializeAsync();

        var location = await _locationRepo.AddAsync(new Location { Path = "/test" });
        var oldDate = DateTime.UtcNow.AddDays(-30);
        var recentDate = DateTime.UtcNow.AddDays(-1);

        var files = new List<FileEntry>
        {
            new() { LocationId = location.Id, Name = "old.txt", Extension = "txt", RelativePath = "old.txt", Size = 100, ModifiedAt = oldDate },
            new() { LocationId = location.Id, Name = "recent.txt", Extension = "txt", RelativePath = "recent.txt", Size = 100, ModifiedAt = recentDate }
        };
        await _fileRepo.AddBatchAsync(files);

        var query = new SearchQuery { ModifiedAfter = DateTime.UtcNow.AddDays(-7) };
        var result = await _fileRepo.SearchAsync(query);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("recent.txt", result.Files[0].Name);
    }

    [Fact]
    public async Task FileRepository_SearchAsync_CombinesMultipleFilters()
    {
        await _db.InitializeAsync();

        var location1 = await _locationRepo.AddAsync(new Location { Path = "/loc1" });
        var location2 = await _locationRepo.AddAsync(new Location { Path = "/loc2" });

        var files = new List<FileEntry>
        {
            new() { LocationId = location1.Id, Name = "small.pdf", Extension = "pdf", RelativePath = "small.pdf", Size = 100 },
            new() { LocationId = location1.Id, Name = "large.pdf", Extension = "pdf", RelativePath = "large.pdf", Size = 10000 },
            new() { LocationId = location2.Id, Name = "small.pdf", Extension = "pdf", RelativePath = "small.pdf", Size = 100 },
            new() { LocationId = location1.Id, Name = "small.txt", Extension = "txt", RelativePath = "small.txt", Size = 100 }
        };
        await _fileRepo.AddBatchAsync(files);

        // Combine location, extension, and size filters
        var query = new SearchQuery
        {
            LocationId = location1.Id,
            Extension = "pdf",
            MaxSize = 500
        };
        var result = await _fileRepo.SearchAsync(query);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("small.pdf", result.Files[0].Name);
    }

    #endregion

    #region Error Path Tests - SettingsRepository

    [Fact]
    public async Task SettingsRepository_GetColumnVisibilityAsync_ReturnsDefaultsForEmptyDb()
    {
        await _db.InitializeAsync();

        var visibility = await _settingsRepo.GetColumnVisibilityAsync();

        Assert.NotEmpty(visibility);
        Assert.True(visibility.ContainsKey("name"));
        Assert.True(visibility["name"]); // name should be visible by default
    }

    [Fact]
    public async Task SettingsRepository_SetColumnVisibilityAsync_PersistsChanges()
    {
        await _db.InitializeAsync();

        var customVisibility = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["name"] = true,
            ["size"] = false,
            ["modified"] = true
        };

        await _settingsRepo.SetColumnVisibilityAsync(customVisibility);
        var retrieved = await _settingsRepo.GetColumnVisibilityAsync();

        Assert.True(retrieved["name"]);
        Assert.False(retrieved["size"]);
        Assert.True(retrieved["modified"]);
    }

    [Fact]
    public async Task SettingsRepository_SearchHistory_LimitsTo50Items()
    {
        await _db.InitializeAsync();

        // Add 55 items
        for (var i = 1; i <= 55; i++)
        {
            await _settingsRepo.AddSearchHistoryAsync($"search{i}");
        }

        var history = await _settingsRepo.GetSearchHistoryAsync();

        Assert.Equal(50, history.Count);
        Assert.Equal("search55", history[0]); // Most recent first
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
        // Clean up WAL files
        var walPath = _tempDbPath + "-wal";
        var shmPath = _tempDbPath + "-shm";
        if (File.Exists(walPath)) File.Delete(walPath);
        if (File.Exists(shmPath)) File.Delete(shmPath);
    }
}
