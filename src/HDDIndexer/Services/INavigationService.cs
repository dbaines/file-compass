using System;
using Microsoft.UI.Xaml.Controls;

namespace HDDIndexer.Services
{
    public interface INavigationService
    {
        void Initialize(Frame frame);
        bool Navigate<T>(object? parameter = null) where T : Page;
        bool Navigate(Type pageType, object? parameter = null);
        bool GoBack();
        bool CanGoBack { get; }
        void ClearHistory();
        event EventHandler<NavigationEventArgs> Navigated;
    }

    public class NavigationEventArgs : EventArgs
    {
        public Type PageType { get; set; } = null!;
        public object? Parameter { get; set; }
    }
}