using Microsoft.UI.Xaml.Data;
using System;

namespace HDDIndexer.Converters
{
    public class FileIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string extension && !string.IsNullOrEmpty(extension))
            {
                return extension.ToLower() switch
                {
                    ".exe" or ".msi" => "\uE8A7", // Application
                    ".doc" or ".docx" => "\uE8A5", // Document
                    ".pdf" => "\uE8A5", // PDF
                    ".txt" => "\uE8A5", // Text
                    ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "\uE8B9", // Image
                    ".mp4" or ".avi" or ".mkv" or ".mov" => "\uE8B2", // Video
                    ".mp3" or ".wav" or ".flac" => "\uE8D6", // Audio
                    ".zip" or ".rar" or ".7z" => "\uE8B5", // Archive
                    ".cs" or ".cpp" or ".py" or ".js" => "\uE943", // Code
                    ".xlsx" or ".xls" => "\uE8A4", // Excel
                    ".pptx" or ".ppt" => "\uE8A3", // PowerPoint
                    "" => "\uE8B7", // Folder
                    _ => "\uE8A5" // Generic file
                };
            }
            return "\uE8B7"; // Default folder icon
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}