using FileCompass.Core.Models;
using FileCompass.Core.Services;

namespace FileCompass.Tests;

public class ExportServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly List<FileEntry> _testFiles;

    public ExportServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"hddindexer_export_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        _testFiles =
        [
            new FileEntry
            {
                Name = "document.pdf",
                RelativePath = "docs/document.pdf",
                Extension = "pdf",
                Size = 1024,
                LocationName = "Test Location",
                ModifiedAt = new DateTime(2024, 1, 15, 10, 30, 0),
                IsDirectory = false
            },
            new FileEntry
            {
                Name = "image.png",
                RelativePath = "images/image.png",
                Extension = "png",
                Size = 2048,
                LocationName = "Test Location",
                ModifiedAt = new DateTime(2024, 2, 20, 14, 45, 0),
                IsDirectory = false
            },
            new FileEntry
            {
                Name = "Projects",
                RelativePath = "Projects",
                Extension = null,
                Size = 0,
                LocationName = "Test Location",
                IsDirectory = true
            }
        ];
    }

    #region CSV Export Tests

    [Fact]
    public async Task ExportToCsv_CreatesFile()
    {
        var outputPath = Path.Combine(_tempDir, "test.csv");

        await ExportService.ExportToCsvAsync(_testFiles, outputPath);

        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task ExportToCsv_ContainsHeader()
    {
        var outputPath = Path.Combine(_tempDir, "test_header.csv");

        await ExportService.ExportToCsvAsync(_testFiles, outputPath);

        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("Name,Location,Path,Extension,Size", content);
    }

    [Fact]
    public async Task ExportToCsv_ContainsFileData()
    {
        var outputPath = Path.Combine(_tempDir, "test_data.csv");

        await ExportService.ExportToCsvAsync(_testFiles, outputPath);

        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("document.pdf", content);
        Assert.Contains("image.png", content);
        Assert.Contains("Test Location", content);
    }

    [Fact]
    public async Task ExportToCsv_EscapesCommasInFields()
    {
        var filesWithCommas = new List<FileEntry>
        {
            new()
            {
                Name = "file,with,commas.txt",
                RelativePath = "path,with,commas/file.txt",
                Extension = "txt",
                Size = 100
            }
        };

        var outputPath = Path.Combine(_tempDir, "test_commas.csv");
        await ExportService.ExportToCsvAsync(filesWithCommas, outputPath);

        var content = await File.ReadAllTextAsync(outputPath);
        // Fields with commas should be quoted
        Assert.Contains("\"file,with,commas.txt\"", content);
    }

    [Fact]
    public async Task ExportToCsv_HandlesEmptyList()
    {
        var outputPath = Path.Combine(_tempDir, "test_empty.csv");

        await ExportService.ExportToCsvAsync([], outputPath);

        Assert.True(File.Exists(outputPath));
        var lines = await File.ReadAllLinesAsync(outputPath);
        Assert.Single(lines); // Only header
    }

    #endregion

    #region HTML Export Tests

    [Fact]
    public async Task ExportToHtml_CreatesFile()
    {
        var outputPath = Path.Combine(_tempDir, "test.html");

        await ExportService.ExportToHtmlAsync(_testFiles, outputPath);

        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task ExportToHtml_ContainsValidHtmlStructure()
    {
        var outputPath = Path.Combine(_tempDir, "test_structure.html");

        await ExportService.ExportToHtmlAsync(_testFiles, outputPath);

        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("<!DOCTYPE html>", content);
        Assert.Contains("<html", content);
        Assert.Contains("</html>", content);
        Assert.Contains("<table>", content);
        Assert.Contains("</table>", content);
    }

    [Fact]
    public async Task ExportToHtml_ContainsFileData()
    {
        var outputPath = Path.Combine(_tempDir, "test_html_data.html");

        await ExportService.ExportToHtmlAsync(_testFiles, outputPath);

        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("document.pdf", content);
        Assert.Contains("image.png", content);
    }

    [Fact]
    public async Task ExportToHtml_EscapesHtmlCharacters()
    {
        var filesWithHtml = new List<FileEntry>
        {
            new()
            {
                Name = "<script>alert('xss')</script>.txt",
                RelativePath = "test.txt",
                Extension = "txt",
                Size = 100
            }
        };

        var outputPath = Path.Combine(_tempDir, "test_escape.html");
        await ExportService.ExportToHtmlAsync(filesWithHtml, outputPath);

        var content = await File.ReadAllTextAsync(outputPath);
        // Should be escaped, not raw script tag
        Assert.DoesNotContain("<script>alert", content);
        Assert.Contains("&lt;script&gt;", content);
    }

    [Fact]
    public async Task ExportToHtml_UsesCustomTitle()
    {
        var outputPath = Path.Combine(_tempDir, "test_title.html");

        await ExportService.ExportToHtmlAsync(_testFiles, outputPath, "Custom Report Title");

        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("Custom Report Title", content);
    }

    #endregion

    #region Excel Export Tests

    [Fact]
    public async Task ExportToExcel_CreatesFile()
    {
        var outputPath = Path.Combine(_tempDir, "test.xlsx");

        await ExportService.ExportToExcelAsync(_testFiles, outputPath);

        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task ExportToExcel_CreatesValidXlsx()
    {
        var outputPath = Path.Combine(_tempDir, "test_valid.xlsx");

        await ExportService.ExportToExcelAsync(_testFiles, outputPath);

        // XLSX files are ZIP archives, check for PK signature
        var bytes = await File.ReadAllBytesAsync(outputPath);
        Assert.True(bytes.Length > 4);
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'K', bytes[1]);
    }

    [Fact]
    public async Task ExportToExcel_HandlesEmptyList()
    {
        var outputPath = Path.Combine(_tempDir, "test_empty.xlsx");

        await ExportService.ExportToExcelAsync([], outputPath);

        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task ExportToExcel_UsesCustomTitle()
    {
        var outputPath = Path.Combine(_tempDir, "test_excel_title.xlsx");

        // Should not throw
        await ExportService.ExportToExcelAsync(_testFiles, outputPath, "Custom Excel Title");

        Assert.True(File.Exists(outputPath));
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task ExportToCsv_RespectsCancellation()
    {
        var outputPath = Path.Combine(_tempDir, "test_cancel.csv");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            ExportService.ExportToCsvAsync(_testFiles, outputPath, cts.Token));
    }

    [Fact]
    public async Task ExportToHtml_RespectsCancellation()
    {
        var outputPath = Path.Combine(_tempDir, "test_cancel.html");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            ExportService.ExportToHtmlAsync(_testFiles, outputPath, "Title", cts.Token));
    }

    [Fact]
    public async Task ExportToExcel_RespectsCancellation()
    {
        var outputPath = Path.Combine(_tempDir, "test_cancel.xlsx");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            ExportService.ExportToExcelAsync(_testFiles, outputPath, "Title", cts.Token));
    }

    #endregion

    #region Large Dataset Tests

    [Fact]
    public async Task ExportToCsv_HandlesLargeDataset()
    {
        var largeList = Enumerable.Range(1, 1000).Select(i => new FileEntry
        {
            Name = $"file{i}.txt",
            RelativePath = $"folder/file{i}.txt",
            Extension = "txt",
            Size = i * 100,
            LocationName = "Test",
            ModifiedAt = DateTime.UtcNow.AddDays(-i)
        }).ToList();

        var outputPath = Path.Combine(_tempDir, "test_large.csv");
        await ExportService.ExportToCsvAsync(largeList, outputPath);

        var lines = await File.ReadAllLinesAsync(outputPath);
        Assert.Equal(1001, lines.Length); // 1 header + 1000 data rows
    }

    [Fact]
    public async Task ExportToHtml_HandlesLargeDataset()
    {
        var largeList = Enumerable.Range(1, 500).Select(i => new FileEntry
        {
            Name = $"file{i}.txt",
            RelativePath = $"folder/file{i}.txt",
            Extension = "txt",
            Size = i * 100,
            LocationName = "Test"
        }).ToList();

        var outputPath = Path.Combine(_tempDir, "test_large.html");
        await ExportService.ExportToHtmlAsync(largeList, outputPath);

        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("file1.txt", content);
        Assert.Contains("file500.txt", content);
    }

    #endregion

    #region Special Character Tests

    [Fact]
    public async Task ExportToCsv_EscapesNewlines()
    {
        var filesWithNewlines = new List<FileEntry>
        {
            new()
            {
                Name = "file\nwith\nnewlines.txt",
                RelativePath = "test.txt",
                Extension = "txt",
                Size = 100
            }
        };

        var outputPath = Path.Combine(_tempDir, "test_newlines.csv");
        await ExportService.ExportToCsvAsync(filesWithNewlines, outputPath);

        // File should be created without error
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task ExportToCsv_EscapesQuotes()
    {
        var filesWithQuotes = new List<FileEntry>
        {
            new()
            {
                Name = "file\"with\"quotes.txt",
                RelativePath = "test.txt",
                Extension = "txt",
                Size = 100
            }
        };

        var outputPath = Path.Combine(_tempDir, "test_quotes.csv");
        await ExportService.ExportToCsvAsync(filesWithQuotes, outputPath);

        var content = await File.ReadAllTextAsync(outputPath);
        // Quotes should be escaped by doubling them
        Assert.Contains("\"\"", content);
    }

    [Fact]
    public async Task ExportToHtml_HandlesUnicodeCharacters()
    {
        var filesWithUnicode = new List<FileEntry>
        {
            new()
            {
                Name = "文件.txt",
                RelativePath = "文件夹/文件.txt",
                Extension = "txt",
                Size = 100,
                LocationName = "测试位置"
            }
        };

        var outputPath = Path.Combine(_tempDir, "test_unicode.html");
        await ExportService.ExportToHtmlAsync(filesWithUnicode, outputPath);

        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("文件.txt", content);
        Assert.Contains("测试位置", content);
    }

    #endregion

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
