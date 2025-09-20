using Avalonia.Controls;
using Avalonia.Interactivity;
using FileCompass.Core.Models;
using FileCompass.Desktop.ViewModels;

namespace FileCompass.Desktop.Views;

public partial class AdvancedSearchWindow : Window
{
    public SearchQuery? Result { get; private set; }

    public AdvancedSearchWindow()
    {
        InitializeComponent();
    }

    private void OnSearchClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AdvancedSearchViewModel vm)
        {
            Result = vm.BuildQuery();
            Close(true);
        }
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close(false);
    }
}
