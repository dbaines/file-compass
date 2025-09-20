using Microsoft.UI.Xaml.Data;
using System;

namespace HDDIndexer.Converters
{
    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dateTime)
            {
                if (dateTime == DateTime.MinValue)
                    return "Never scanned";

                var timeSpan = DateTime.Now - dateTime;
                if (timeSpan.TotalDays < 1)
                    return "Today";
                else if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays} days ago";
                else
                    return dateTime.ToString("MMM dd, yyyy");
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}