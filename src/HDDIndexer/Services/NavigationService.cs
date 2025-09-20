using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace HDDIndexer.Services
{
    public class NavigationService : INavigationService
    {
        private Frame? _frame;

        public bool CanGoBack => _frame?.CanGoBack ?? false;

        public event EventHandler<NavigationEventArgs>? Navigated;

        public void Initialize(Frame frame)
        {
            _frame = frame;
            _frame.Navigated += OnFrameNavigated;
        }

        public bool Navigate<T>(object? parameter = null) where T : Page
        {
            return Navigate(typeof(T), parameter);
        }

        public bool Navigate(Type pageType, object? parameter = null)
        {
            if (_frame == null) return false;
            return _frame.Navigate(pageType, parameter);
        }

        public bool GoBack()
        {
            if (_frame?.CanGoBack == true)
            {
                _frame.GoBack();
                return true;
            }
            return false;
        }

        public void ClearHistory()
        {
            _frame?.BackStack.Clear();
        }

        private void OnFrameNavigated(object sender, NavigationEventArgs e)
        {
            Navigated?.Invoke(this, new Services.NavigationEventArgs
            {
                PageType = e.SourcePageType,
                Parameter = e.Parameter
            });
        }
    }
}