using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using HDDIndexer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HDDIndexer.Views
{
    public sealed partial class SettingsView : Page
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsView()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetRequiredService<SettingsViewModel>();
            this.DataContext = ViewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }
    }
}