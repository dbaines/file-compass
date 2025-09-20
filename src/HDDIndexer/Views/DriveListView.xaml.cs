using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using HDDIndexer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HDDIndexer.Views
{
    public sealed partial class DriveListView : Page
    {
        public DriveListViewModel ViewModel { get; }

        public DriveListView()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetRequiredService<DriveListViewModel>();
            this.DataContext = ViewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.Initialize();
        }
    }
}