using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using HDDIndexer.Models;

namespace HDDIndexer.Converters
{
    public class ScanButtonVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DriveStatus status)
            {
                return status switch
                {
                    DriveStatus.NeverScanned => Visibility.Visible,
                    DriveStatus.Outdated => Visibility.Visible,
                    DriveStatus.UpToDate => Visibility.Visible,
                    DriveStatus.Offline => Visibility.Collapsed,
                    DriveStatus.Scanning => Visibility.Collapsed,
                    DriveStatus.Error => Visibility.Visible,
                    _ => Visibility.Visible
                };
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}