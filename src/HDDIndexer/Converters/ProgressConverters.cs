using Microsoft.UI.Xaml.Data;
using System;

namespace HDDIndexer.Converters
{
    public class PauseResumeTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isPaused)
            {
                return isPaused ? "Resume" : "Pause";
            }
            return "Pause";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class FilesScannedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int fileCount)
            {
                return $"{fileCount:N0} files";
            }
            return "0 files";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class FilesPerSecondConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double filesPerSecond)
            {
                return $"{filesPerSecond:F1} files/s";
            }
            return "0 files/s";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}