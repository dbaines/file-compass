namespace FileCompass.Core.Constants;

/// <summary>
/// Application-wide constants and configuration values.
/// </summary>
public static class AppConstants
{
    public const string AppName = "FileCompass";
    public const string AppVersion = "0.2.0";
    public const string DatabaseFileName = "catalog.db";
    public const string BackupFileExtension = ".fci";

    /// <summary>
    /// Number of files to insert per database batch operation.
    /// Balances memory usage vs. transaction overhead. 1000 provides good performance
    /// for most scenarios without excessive memory consumption.
    /// </summary>
    public const int DefaultBatchSize = 1000;

    /// <summary>
    /// File category names for organizing files by type.
    /// </summary>
    public static class FileCategories
    {
        public const string Documents = "Documents";
        public const string Images = "Images";
        public const string Videos = "Videos";
        public const string Audio = "Audio";
        public const string Archives = "Archives";
        public const string Code = "Code";
        public const string Executables = "Executables";
        public const string Other = "Other";
    }

    /// <summary>
    /// Centralized mapping of file extensions to categories.
    /// Each extension should be lowercase without the leading dot.
    /// Used by both FileEntry.FileCategory property and FileRepository search filtering.
    /// </summary>
    public static class FileExtensions
    {
        /// <summary>Document file extensions (Word, PDF, spreadsheets, presentations).</summary>
        public static readonly string[] Documents =
            ["doc", "docx", "pdf", "txt", "rtf", "odt", "xls", "xlsx", "ppt", "pptx"];

        /// <summary>Image file extensions (photos, graphics, icons).</summary>
        public static readonly string[] Images =
            ["jpg", "jpeg", "png", "gif", "bmp", "svg", "webp", "ico", "tiff", "tif"];

        /// <summary>Video file extensions (movies, clips).</summary>
        public static readonly string[] Videos =
            ["mp4", "avi", "mkv", "mov", "wmv", "flv", "webm", "m4v"];

        /// <summary>Audio file extensions (music, sound files).</summary>
        public static readonly string[] Audio =
            ["mp3", "wav", "flac", "aac", "ogg", "wma", "m4a"];

        /// <summary>Archive file extensions (compressed files).</summary>
        public static readonly string[] Archives =
            ["zip", "rar", "7z", "tar", "gz", "bz2", "xz"];

        /// <summary>Source code file extensions (programming languages, markup, config).</summary>
        public static readonly string[] Code =
            ["cs", "js", "ts", "py", "java", "cpp", "c", "h", "html", "css", "json", "xml", "yml", "yaml", "md", "sh", "ps1"];

        /// <summary>Executable file extensions (programs, scripts, installers).</summary>
        public static readonly string[] Executables =
            ["exe", "msi", "app", "bat", "cmd", "sh", "deb", "rpm", "appimage"];

        /// <summary>
        /// Gets the file category for a given extension.
        /// </summary>
        /// <param name="extension">File extension (with or without leading dot).</param>
        /// <returns>The category name from FileCategories, or FileCategories.Other if not matched.</returns>
        public static string GetCategory(string? extension)
        {
            if (string.IsNullOrEmpty(extension))
                return FileCategories.Other;

            var ext = extension.ToLowerInvariant().TrimStart('.');

            if (Documents.Contains(ext, StringComparer.Ordinal)) return FileCategories.Documents;
            if (Images.Contains(ext, StringComparer.Ordinal)) return FileCategories.Images;
            if (Videos.Contains(ext, StringComparer.Ordinal)) return FileCategories.Videos;
            if (Audio.Contains(ext, StringComparer.Ordinal)) return FileCategories.Audio;
            if (Archives.Contains(ext, StringComparer.Ordinal)) return FileCategories.Archives;
            if (Code.Contains(ext, StringComparer.Ordinal)) return FileCategories.Code;
            if (Executables.Contains(ext, StringComparer.Ordinal)) return FileCategories.Executables;

            return FileCategories.Other;
        }

        /// <summary>
        /// Gets all file extensions for a given category.
        /// </summary>
        /// <param name="category">Category name from FileCategories.</param>
        /// <returns>Array of extensions (without dots), or empty array for Other/unknown category.</returns>
        public static string[] GetExtensionsForCategory(string category)
        {
            return category switch
            {
                FileCategories.Documents => Documents,
                FileCategories.Images => Images,
                FileCategories.Videos => Videos,
                FileCategories.Audio => Audio,
                FileCategories.Archives => Archives,
                FileCategories.Code => Code,
                FileCategories.Executables => Executables,
                _ => []
            };
        }
    }
}
