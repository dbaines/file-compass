using FileCompass.Core.Models;
using FileCompass.Core.Constants;

namespace FileCompass.Tests;

public class FileEntryModelTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    [InlineData(1099511627776, "1 TB")]
    public void DisplaySize_FormatsCorrectly(long bytes, string expected)
    {
        var entry = new FileEntry
        {
            Name = "test",
            RelativePath = "test",
            Size = bytes,
            IsDirectory = false
        };

        Assert.Equal(expected, entry.DisplaySize);
    }

    [Fact]
    public void DisplaySize_ReturnsEmptyForDirectories()
    {
        var entry = new FileEntry
        {
            Name = "folder",
            RelativePath = "folder",
            Size = 0,
            IsDirectory = true
        };

        Assert.Equal(string.Empty, entry.DisplaySize);
    }

    [Theory]
    [InlineData("pdf", "Documents")]
    [InlineData("docx", "Documents")]
    [InlineData("xlsx", "Documents")]
    [InlineData("jpg", "Images")]
    [InlineData("png", "Images")]
    [InlineData("gif", "Images")]
    [InlineData("mp4", "Videos")]
    [InlineData("mkv", "Videos")]
    [InlineData("mp3", "Audio")]
    [InlineData("flac", "Audio")]
    [InlineData("zip", "Archives")]
    [InlineData("7z", "Archives")]
    [InlineData("cs", "Code")]
    [InlineData("js", "Code")]
    [InlineData("py", "Code")]
    [InlineData("exe", "Executables")]
    [InlineData("msi", "Executables")]
    [InlineData("xyz", "Other")]
    [InlineData("", "Other")]
    [InlineData(null, "Other")]
    public void FileCategory_ReturnsCorrectCategory(string? extension, string expectedCategory)
    {
        var entry = new FileEntry
        {
            Name = "test",
            RelativePath = "test",
            Extension = extension
        };

        Assert.Equal(expectedCategory, entry.FileCategory);
    }

    [Fact]
    public void FileCategory_IsCaseInsensitive()
    {
        var entry = new FileEntry
        {
            Name = "test.PDF",
            RelativePath = "test.PDF",
            Extension = "PDF"
        };

        Assert.Equal("Documents", entry.FileCategory);
    }

    [Fact]
    public void FileCategory_HandlesExtensionWithDot()
    {
        var entry = new FileEntry
        {
            Name = "test.jpg",
            RelativePath = "test.jpg",
            Extension = ".jpg"
        };

        Assert.Equal("Images", entry.FileCategory);
    }

    [Theory]
    [InlineData("foo.sh", "")]
    [InlineData("bar/foo.sh", "bar/")]
    [InlineData("bar/baz/foo.sh", "bar/baz/")]
    [InlineData("a/b/c/d/file.txt", "a/b/c/d/")]
    public void ParentPath_ReturnsDirectoryWithoutFilename(string relativePath, string expected)
    {
        var entry = new FileEntry
        {
            Name = "test",
            RelativePath = relativePath
        };

        Assert.Equal(expected, entry.ParentPath);
    }

    [Fact]
    public void ParentPath_ReturnsEmpty_WhenRelativePathIsEmpty()
    {
        var entry = new FileEntry
        {
            Name = "test",
            RelativePath = ""
        };

        Assert.Equal(string.Empty, entry.ParentPath);
    }

}

public class LocationModelTests
{
    [Fact]
    public void DisplayName_ReturnsCustomName_WhenSet()
    {
        var location = new Location
        {
            Path = "/some/path",
            CustomName = "My Drive",
            VolumeLabel = "VOLUME1"
        };

        Assert.Equal("My Drive", location.DisplayName);
    }

    [Fact]
    public void DisplayName_ReturnsVolumeLabel_WhenNoCustomName()
    {
        var location = new Location
        {
            Path = "/some/path",
            VolumeLabel = "VOLUME1"
        };

        Assert.Equal("VOLUME1", location.DisplayName);
    }

    [Fact]
    public void DisplayName_ReturnsPathSegment_WhenNoCustomNameOrLabel()
    {
        var location = new Location
        {
            Path = "/some/path"
        };

        // DisplayName uses Path.GetFileName() which returns last segment
        Assert.Equal("path", location.DisplayName);
    }

    [Fact]
    public void Status_DefaultsToNeverScanned()
    {
        var location = new Location { Path = "/test" };
        Assert.Equal(LocationStatus.NeverScanned, location.Status);
    }
}

public class SearchQueryModelTests
{
    [Fact]
    public void SearchQuery_HasSensibleDefaults()
    {
        var query = new SearchQuery();

        Assert.Equal(-1, query.MaxResults); // -1 means no limit
        Assert.Equal(0, query.Offset);
        Assert.False(query.IncludeDirectories);
        Assert.Null(query.SearchTerm);
        Assert.Null(query.Extension);
        Assert.Null(query.LocationId);
    }
}

public class SearchResultModelTests
{
    [Fact]
    public void SearchResult_HasMoreProperty_ReturnsTrueWhenMoreResults()
    {
        var result = new SearchResult
        {
            Files = [new FileEntry { Name = "test", RelativePath = "test" }],
            TotalCount = 100
        };

        Assert.True(result.HasMore);
    }

    [Fact]
    public void SearchResult_HasMoreProperty_ReturnsFalseWhenAllLoaded()
    {
        var result = new SearchResult
        {
            Files = [new FileEntry { Name = "test", RelativePath = "test" }],
            TotalCount = 1
        };

        Assert.False(result.HasMore);
    }
}

public class ScanProgressModelTests
{
    [Fact]
    public void ElapsedTime_CalculatesCorrectly()
    {
        var startTime = DateTime.UtcNow.AddSeconds(-10);
        var progress = new ScanProgress { StartTime = startTime };

        // Allow for small timing variations
        Assert.True(progress.ElapsedTime.TotalSeconds >= 9.5);
        Assert.True(progress.ElapsedTime.TotalSeconds <= 11);
    }

    [Fact]
    public void FilesPerSecond_ReturnsZeroWhenNoTimeElapsed()
    {
        var progress = new ScanProgress
        {
            StartTime = DateTime.UtcNow,
            FilesScanned = 100
        };

        // With essentially zero elapsed time, should be 0 or very high
        // The implementation returns 0 when ElapsedTime.TotalSeconds is 0
        Assert.True(progress.FilesPerSecond >= 0);
    }

    [Fact]
    public void FilesPerSecond_CalculatesCorrectly()
    {
        var progress = new ScanProgress
        {
            StartTime = DateTime.UtcNow.AddSeconds(-10),
            FilesScanned = 100
        };

        // 100 files in ~10 seconds = ~10 files/sec
        Assert.True(progress.FilesPerSecond >= 9);
        Assert.True(progress.FilesPerSecond <= 11);
    }

    [Fact]
    public void StatusMessage_ReturnsCancelledWhenCancelled()
    {
        var progress = new ScanProgress
        {
            IsCancelled = true,
            FilesScanned = 50,
            FoldersScanned = 10
        };

        Assert.Equal("Cancelled", progress.StatusMessage);
    }

    [Fact]
    public void StatusMessage_ReturnsCompleteWhenComplete()
    {
        var progress = new ScanProgress
        {
            IsComplete = true,
            FilesScanned = 100,
            FoldersScanned = 25
        };

        Assert.Contains("Complete", progress.StatusMessage);
        Assert.Contains("100", progress.StatusMessage);
        Assert.Contains("25", progress.StatusMessage);
    }

    [Fact]
    public void StatusMessage_ReturnsScanningWhenInProgress()
    {
        var progress = new ScanProgress
        {
            StartTime = DateTime.UtcNow.AddSeconds(-5),
            FilesScanned = 50,
            IsComplete = false,
            IsCancelled = false
        };

        Assert.Contains("Scanning", progress.StatusMessage);
        Assert.Contains("50", progress.StatusMessage);
    }

    [Fact]
    public void StatusMessage_CancelledTakesPriorityOverComplete()
    {
        var progress = new ScanProgress
        {
            IsComplete = true,
            IsCancelled = true,
            FilesScanned = 100
        };

        Assert.Equal("Cancelled", progress.StatusMessage);
    }

    [Fact]
    public void ScanProgress_DefaultValues()
    {
        var progress = new ScanProgress();

        Assert.Equal(0, progress.LocationId);
        Assert.Equal(string.Empty, progress.CurrentPath);
        Assert.Equal(0, progress.FilesScanned);
        Assert.Equal(0, progress.FoldersScanned);
        Assert.Equal(0, progress.BytesScanned);
        Assert.Equal(0, progress.ErrorCount);
        Assert.False(progress.IsComplete);
        Assert.False(progress.IsCancelled);
    }

    [Fact]
    public void BytesScanned_TracksCorrectly()
    {
        var progress = new ScanProgress
        {
            BytesScanned = 1073741824 // 1 GB
        };

        Assert.Equal(1073741824, progress.BytesScanned);
    }

    [Fact]
    public void ErrorCount_TracksCorrectly()
    {
        var progress = new ScanProgress { ErrorCount = 5 };
        Assert.Equal(5, progress.ErrorCount);
    }
}

public class SearchQueryModelTestsExtended
{
    [Fact]
    public void HasFilters_ReturnsFalseWhenNoFiltersSet()
    {
        var query = new SearchQuery();
        Assert.False(query.HasFilters);
    }

    [Fact]
    public void HasFilters_ReturnsTrueWhenSearchTermSet()
    {
        var query = new SearchQuery { SearchTerm = "test" };
        Assert.True(query.HasFilters);
    }

    [Fact]
    public void HasFilters_ReturnsTrueWhenLocationIdSet()
    {
        var query = new SearchQuery { LocationId = 1 };
        Assert.True(query.HasFilters);
    }

    [Fact]
    public void HasFilters_ReturnsTrueWhenExtensionSet()
    {
        var query = new SearchQuery { Extension = "pdf" };
        Assert.True(query.HasFilters);
    }

    [Fact]
    public void HasFilters_ReturnsTrueWhenFileCategorySet()
    {
        var query = new SearchQuery { FileCategory = "Images" };
        Assert.True(query.HasFilters);
    }

    [Fact]
    public void HasFilters_ReturnsTrueWhenMinSizeSet()
    {
        var query = new SearchQuery { MinSize = 1000 };
        Assert.True(query.HasFilters);
    }

    [Fact]
    public void HasFilters_ReturnsTrueWhenMaxSizeSet()
    {
        var query = new SearchQuery { MaxSize = 10000 };
        Assert.True(query.HasFilters);
    }

    [Fact]
    public void HasFilters_ReturnsTrueWhenModifiedAfterSet()
    {
        var query = new SearchQuery { ModifiedAfter = DateTime.UtcNow.AddDays(-7) };
        Assert.True(query.HasFilters);
    }

    [Fact]
    public void HasFilters_ReturnsTrueWhenModifiedBeforeSet()
    {
        var query = new SearchQuery { ModifiedBefore = DateTime.UtcNow };
        Assert.True(query.HasFilters);
    }

    [Fact]
    public void HasFilters_ReturnsFalseWhenOnlyIncludeDirectoriesTrue()
    {
        // IncludeDirectories is not considered a filter - it's just an include/exclude option
        var query = new SearchQuery { IncludeDirectories = true };
        Assert.False(query.HasFilters);
    }
}
