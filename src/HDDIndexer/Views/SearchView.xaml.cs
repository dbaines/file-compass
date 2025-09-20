using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using HDDIndexer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HDDIndexer.Views
{
    public sealed partial class SearchView : Page
    {
        public SearchViewModel ViewModel { get; }

        public SearchView()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetRequiredService<SearchViewModel>();
            this.DataContext = ViewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string query)
            {
                ViewModel.SetInitialQuery(query);
            }
        }
    }
}