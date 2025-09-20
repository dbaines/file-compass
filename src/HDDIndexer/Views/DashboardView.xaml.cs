using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using HDDIndexer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HDDIndexer.Views
{
    public sealed partial class DashboardView : Page
    {
        public MainViewModel ViewModel { get; }

        public DashboardView()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetRequiredService<MainViewModel>();
            this.DataContext = ViewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.Initialize();
        }
    }
}