using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using HDDIndexer.Services;
using HDDIndexer.Models;
using Windows.Foundation;

namespace HDDIndexer.Views
{
    public sealed partial class AdvancedSearchDialog : ContentDialog
    {
        public SearchCriteria SearchCriteria { get; set; } = new();
        public ObservableCollection<Drive> AvailableDrives { get; set; } = new();

        public string ExtensionsText
        {
            get => SearchCriteria.Extensions != null ? string.Join(", ", SearchCriteria.Extensions) : "";
            set => SearchCriteria.Extensions = value?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(e => e.Trim()).ToList();
        }

        public double MinSizeMB
        {
            get => SearchCriteria.MinSize.HasValue ? SearchCriteria.MinSize.Value / (1024.0 * 1024.0) : 0;
            set => SearchCriteria.MinSize = value > 0 ? (long)(value * 1024 * 1024) : null;
        }

        public double MaxSizeMB
        {
            get => SearchCriteria.MaxSize.HasValue ? SearchCriteria.MaxSize.Value / (1024.0 * 1024.0) : 0;
            set => SearchCriteria.MaxSize = value > 0 ? (long)(value * 1024 * 1024) : null;
        }

        public DateTimeOffset StartDate
        {
            get => SearchCriteria.StartDate ?? DateTimeOffset.Now.AddMonths(-1);
            set => SearchCriteria.StartDate = value.DateTime;
        }

        public DateTimeOffset EndDate
        {
            get => SearchCriteria.EndDate ?? DateTimeOffset.Now;
            set => SearchCriteria.EndDate = value.DateTime;
        }

        public bool IsSearchEnabled
        {
            get => !string.IsNullOrWhiteSpace(SearchCriteria.FileName) ||
                   !string.IsNullOrWhiteSpace(ExtensionsText) ||
                   SearchCriteria.MinSize.HasValue ||
                   SearchCriteria.MaxSize.HasValue;
        }

        public AdvancedSearchDialog()
        {
            this.InitializeComponent();
        }

        public void SetAvailableDrives(System.Collections.Generic.IEnumerable<Drive> drives)
        {
            AvailableDrives.Clear();
            foreach (var drive in drives)
            {
                AvailableDrives.Add(drive);
            }
        }

        public System.Collections.Generic.List<int> GetSelectedDriveIds()
        {
            return AvailableDrives.Where(d => d.IsSelected).Select(d => d.DriveId).ToList();
        }
    }
}