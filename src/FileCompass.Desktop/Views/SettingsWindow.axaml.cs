using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FileCompass.Core.Data;
using FileCompass.Desktop.Services;
using FileCompass.Translations;

namespace FileCompass.Desktop.Views;

public partial class SettingsWindow : Window
{
    private bool _isInitializing = true;

    public SettingsWindow()
    {
        InitializeComponent();
        LoadSettings();
        _isInitializing = false;
    }

    private async void LoadSettings()
    {
        // Load theme setting
        var theme = ServiceLocator.ThemeService.CurrentTheme;
        ThemeComboBox.SelectedIndex = theme switch
        {
            AppTheme.Light => 1,
            AppTheme.Dark => 2,
            _ => 0
        };

        // Load search history count
        var history = await ServiceLocator.SettingsRepository.GetSearchHistoryAsync();
        HistoryCountText.Text = string.Format(CultureInfo.CurrentCulture, Strings.SettingsHistoryCount, history.Count);

        // Database info
        var dbPath = DatabaseService.GetDefaultDatabasePath();
        DatabasePathText.Text = dbPath;

        if (File.Exists(dbPath))
        {
            var fileInfo = new FileInfo(dbPath);
            DatabaseSizeText.Text = FormatSize(fileInfo.Length);
        }
        else
        {
            DatabaseSizeText.Text = Strings.SettingsDatabaseNotCreated;
        }
    }

    private async void OnThemeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing || ThemeComboBox.SelectedIndex < 0) return;

        var theme = ThemeComboBox.SelectedIndex switch
        {
            1 => AppTheme.Light,
            2 => AppTheme.Dark,
            _ => AppTheme.System
        };

        await ServiceLocator.ThemeService.SetThemeAsync(theme);
    }

    private async void OnClearHistoryClick(object? sender, RoutedEventArgs e)
    {
        await ServiceLocator.SettingsRepository.ClearSearchHistoryAsync();
        HistoryCountText.Text = Strings.SettingsHistoryCleared;
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = [Strings.SizeB, Strings.SizeKb, Strings.SizeMb, Strings.SizeGb, Strings.SizeTb];
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
