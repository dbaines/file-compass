using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Input;
using HDDIndexer.ViewModels;
using HDDIndexer.Models;
using Microsoft.Extensions.DependencyInjection;

namespace HDDIndexer.Views
{
    public sealed partial class FileBrowserView : Page
    {
        public FileBrowserViewModel ViewModel { get; }

        public FileBrowserView()
        {
            this.InitializeComponent();
            ViewModel = App.Current.Services.GetRequiredService<FileBrowserViewModel>();
            this.DataContext = ViewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is Drive drive)
            {
                ViewModel.SetDrive(drive);
            }
        }

        private void OnFileListDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (ViewModel.SelectedFile?.IsDirectory == true)
            {
                ViewModel.NavigateToFolderCommand.Execute(ViewModel.SelectedFile);
            }
        }
    }
}