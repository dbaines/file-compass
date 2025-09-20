using System;
using System.Collections.Generic;

namespace HDDIndexer.Models
{
    public class FileEntry
    {
        public long FileId { get; set; }
        public int DriveId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public bool IsDirectory { get; set; }
        public string? FileExtension { get; set; }
        public long? ParentFileId { get; set; }
        public string? Attributes { get; set; }
        public string? Hash { get; set; }

        // Navigation properties
        public virtual Drive Drive { get; set; } = null!;
        public virtual FileEntry? ParentFile { get; set; }
        public virtual ICollection<FileEntry> ChildFiles { get; set; } = new List<FileEntry>();

        // Computed properties
        public string FullPath => System.IO.Path.Combine(FilePath, FileName);
        public string FileType => GetFileType();

        private string GetFileType()
        {
            if (IsDirectory) return "Folder";
            if (string.IsNullOrEmpty(FileExtension)) return "File";

            return FileExtension.ToLower() switch
            {
                ".exe" or ".msi" or ".app" => "Application",
                ".doc" or ".docx" or ".txt" or ".pdf" or ".rtf" => "Document",
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".svg" => "Image",
                ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" => "Video",
                ".mp3" or ".wav" or ".flac" or ".aac" or ".wma" => "Audio",
                ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "Archive",
                ".cs" or ".cpp" or ".py" or ".js" or ".java" => "Source Code",
                _ => "File"
            };
        }
    }
}