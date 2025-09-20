using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HDDIndexer.Models;
using HDDIndexer.Services;

namespace HDDIndexer.ViewModels
{
    public partial class SearchViewModel : ViewModelBase
    {
        private readonly ISearchService _searchService;
        private readonly IPrintService _printService;

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private ObservableCollection<FileEntry> _searchResults = new();

        [ObservableProperty]
        private ObservableCollection<string> _searchSuggestions = new();

        [ObservableProperty]
        private SearchCriteria _searchCriteria = new();

        [ObservableProperty]
        private bool _isAdvancedSearchOpen;

        [ObservableProperty]
        private int _resultCount;

        [ObservableProperty]
        private string _searchStatus = "Ready to search";

        public SearchViewModel(ISearchService searchService, IPrintService printService)
        {
            _searchService = searchService;
            _printService = printService;
            Title = "Search Files";
        }

        public void SetInitialQuery(string query)
        {
            SearchQuery = query;
            _ = PerformQuickSearchAsync();
        }

        [RelayCommand]
        private async Task PerformQuickSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery)) return;

            try
            {
                IsBusy = true;
                SearchStatus = "Searching...";
                SearchResults.Clear();

                var results = await _searchService.QuickSearchAsync(SearchQuery);

                foreach (var result in results)
                {
                    SearchResults.Add(result);
                }

                ResultCount = SearchResults.Count;
                SearchStatus = $"Found {ResultCount} results";
            }
            catch (Exception ex)
            {
                SearchStatus = $"Search failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task PerformAdvancedSearchAsync()
        {
            try
            {
                IsBusy = true;
                SearchStatus = "Performing advanced search...";
                SearchResults.Clear();

                SearchCriteria.FileName = SearchQuery;
                var results = await _searchService.AdvancedSearchAsync(SearchCriteria);

                foreach (var result in results)
                {
                    SearchResults.Add(result);
                }

                ResultCount = SearchResults.Count;
                SearchStatus = $"Found {ResultCount} results";
                IsAdvancedSearchOpen = false;
            }
            catch (Exception ex)
            {
                SearchStatus = $"Search failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GetSuggestionsAsync()
        {
            if (SearchQuery.Length < 2)
            {
                SearchSuggestions.Clear();
                return;
            }

            var suggestions = await _searchService.GetSearchSuggestionsAsync(SearchQuery);
            SearchSuggestions.Clear();
            foreach (var suggestion in suggestions.Take(10))
            {
                SearchSuggestions.Add(suggestion);
            }
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchQuery = string.Empty;
            SearchResults.Clear();
            SearchSuggestions.Clear();
            ResultCount = 0;
            SearchStatus = "Ready to search";
        }

        [RelayCommand]
        private async Task ToggleAdvancedSearchAsync()
        {
            var dialog = new Views.AdvancedSearchDialog();

            // Get available drives
            var driveService = App.Current.Services.GetService(typeof(Services.IDriveService)) as Services.IDriveService;
            if (driveService != null)
            {
                var drives = await driveService.GetCatalogedDrivesAsync();
                dialog.SetAvailableDrives(drives);
            }

            dialog.XamlRoot = App.Current.m_window?.Content.XamlRoot;
            var result = await dialog.ShowAsync();

            if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                // Update search criteria with selected drives
                dialog.SearchCriteria.DriveIds = dialog.GetSelectedDriveIds();

                // Perform advanced search
                await PerformAdvancedSearchWithCriteriaAsync(dialog.SearchCriteria);
            }
        }

        private async Task PerformAdvancedSearchWithCriteriaAsync(SearchCriteria criteria)
        {
            try
            {
                IsBusy = true;
                SearchStatus = "Performing advanced search...";
                SearchResults.Clear();

                var results = await _searchService.AdvancedSearchAsync(criteria);

                foreach (var result in results)
                {
                    SearchResults.Add(result);
                }

                ResultCount = SearchResults.Count;
                SearchStatus = $"Found {ResultCount} results";
            }
            catch (Exception ex)
            {
                SearchStatus = $"Search failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ExportResultsAsync(string format)
        {
            if (!SearchResults.Any()) return;

            try
            {
                IsBusy = true;
                string filePath;

                switch (format.ToLower())
                {
                    case "pdf":
                        filePath = await _printService.GeneratePdfAsync(SearchResults, new PrintOptions
                        {
                            Title = $"Search Results - {SearchQuery}",
                            IncludeDate = true,
                            SortBy = SortOrder.Name
                        });
                        break;

                    case "excel":
                        filePath = await _printService.GenerateExcelAsync(SearchResults, new ExportOptions
                        {
                            AutoFilter = true,
                            FreezeHeader = true
                        });
                        break;

                    case "csv":
                        filePath = await _printService.GenerateCsvAsync(SearchResults);
                        break;

                    default:
                        return;
                }

                SearchStatus = $"Results exported to: {filePath}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SaveSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery)) return;

            var name = $"Search - {SearchQuery} - {DateTime.Now:yyyy-MM-dd HH:mm}";
            await _searchService.SaveSearchAsync(name, SearchCriteria);
            SearchStatus = "Search saved";
        }

        partial void OnSearchQueryChanged(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _ = GetSuggestionsAsync();
            }
        }
    }
}