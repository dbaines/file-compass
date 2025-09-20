namespace FileCompass.Core.Models;

public class FileEntry
{
    public long Id { get; set; }
    public long LocationId { get; set; }
    public string? LocationName { get; set; }
    public long? ParentId { get; set; }
    public required string Name { get; set; }
    public string? Extension { get; set; }
    public required string RelativePath { get; set; }
    public long Size { get; set; }
    public bool IsDirectory { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? Attributes { get; set; }

    public string DisplaySize => IsDirectory ? string.Empty : FormatSize(Size);

    /// <summary>
    /// Gets the category for this file based on its extension.
    /// Uses centralized mapping from AppConstants.FileExtensions.
    /// </summary>
    public string FileCategory => Constants.AppConstants.FileExtensions.GetCategory(Extension);

    /// <summary>
    /// Gets the parent directory path with normalized forward slashes.
    /// </summary>
    public string ParentPath
    {
        get
        {
            if (string.IsNullOrEmpty(RelativePath))
                return string.Empty;

            var dir = Path.GetDirectoryName(RelativePath);
            if (string.IsNullOrEmpty(dir))
                return string.Empty;

            // Normalize to forward slashes and ensure trailing slash
            return dir.Replace('\\', '/') + "/";
        }
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
