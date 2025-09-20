using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using HDDIndexer.ViewModels;
using HDDIndexer.Views;
using System;

namespace HDDIndexer
{
    public sealed partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            this.InitializeComponent();
            Title = "HDD Indexer - File Catalog Manager";

            // Get view model from DI container
            _viewModel = App.Current.Services.GetService(typeof(MainViewModel)) as MainViewModel
                ?? throw new InvalidOperationException("MainViewModel not found in services");

            // Initialize navigation
            var navigationService = App.Current.Services.GetService(typeof(Services.INavigationService)) as Services.INavigationService;
            navigationService?.Initialize(ContentFrame);

            // Set initial page
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[0];
            ContentFrame.Navigate(typeof(DashboardView));

            // Initialize view model
            _viewModel.Initialize();

            // Subscribe to view model events
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.StatusMessage):
                    StatusText.Text = _viewModel.StatusMessage;
                    break;
                case nameof(MainViewModel.IsBusy):
                    StatusProgressRing.IsActive = _viewModel.IsBusy;
                    break;
                case nameof(MainViewModel.Drives):
                    DriveCountText.Text = $"{_viewModel.Drives.Count} Drives";
                    break;
            }
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            {
                Type? pageType = tag switch
                {
                    "Dashboard" => typeof(DashboardView),
                    "Drives" => typeof(DriveListView),
                    "Browse" => typeof(FileBrowserView),
                    "Search" => typeof(SearchView),
                    "Backup" => typeof(BackupRestoreView),
                    "Print" => typeof(PrintExportView),
                    _ => null
                };

                if (pageType != null)
                {
                    ContentFrame.Navigate(pageType);
                }
            }
            else if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(SettingsView));
            }
        }

        private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.QueryText))
            {
                _viewModel.SearchQuery = args.QueryText;
                await _viewModel.QuickSearchCommand.ExecuteAsync(null);

                // Navigate to search view
                NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[3]; // Search item
                ContentFrame.Navigate(typeof(SearchView), args.QueryText);
            }
        }

        private async void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                // Get search suggestions
                if (sender.Text.Length >= 2)
                {
                    var searchService = App.Current.Services.GetService(typeof(Services.ISearchService)) as Services.ISearchService;
                    if (searchService != null)
                    {
                        var suggestions = await searchService.GetSearchSuggestionsAsync(sender.Text);
                        sender.ItemsSource = suggestions;
                    }
                }
            }
        }
    }
}