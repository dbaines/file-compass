using Microsoft.UI.Xaml.Data;
using System;
using HDDIndexer.Models;
using Windows.UI;

namespace HDDIndexer.Converters
{
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DriveStatus status)
            {
                return status switch
                {
                    DriveStatus.NeverScanned => Color.FromArgb(255, 255, 165, 0), // Orange
                    DriveStatus.Scanning => Color.FromArgb(255, 30, 144, 255), // DodgerBlue
                    DriveStatus.UpToDate => Color.FromArgb(255, 34, 139, 34), // ForestGreen
                    DriveStatus.Outdated => Color.FromArgb(255, 255, 215, 0), // Gold
                    DriveStatus.Offline => Color.FromArgb(255, 128, 128, 128), // Gray
                    DriveStatus.Error => Color.FromArgb(255, 220, 20, 60), // Crimson
                    _ => Color.FromArgb(255, 128, 128, 128) // Default Gray
                };
            }
            return Color.FromArgb(255, 128, 128, 128);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}