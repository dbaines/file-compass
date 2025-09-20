using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace HDDIndexer.Services
{
    public class SettingsService : ISettingsService
    {
        private const string ThemeKey = "AppTheme";
        private const string AutoScanKey = "AutoScanOnStartup";
        private const string ShowHiddenFilesKey = "ShowHiddenFiles";
        private const string ShowSystemFilesKey = "ShowSystemFiles";
        private const string ScanThreadCountKey = "ScanThreadCount";
        private const string BatchSizeKey = "BatchSize";
        private const string MaxSearchResultsKey = "MaxSearchResults";
        private const string EnableSearchHistoryKey = "EnableSearchHistory";
        private const string EnableAutoCompleteKey = "EnableAutoComplete";
        private const string EnableAutoBackupKey = "EnableAutoBackup";
        private const string BackupDirectoryKey = "BackupDirectory";
        private const string BackupRetentionDaysKey = "BackupRetentionDays";
        private const string ShowStatusBarKey = "ShowStatusBar";
        private const string ShowFilePreviewKey = "ShowFilePreview";
        private const string DefaultViewKey = "DefaultView";
        private const string DefaultFontSizeKey = "DefaultFontSize";

        private readonly ApplicationDataContainer _localSettings;

        public SettingsService()
        {
            _localSettings = ApplicationData.Current.LocalSettings;
        }

        public ElementTheme Theme
        {
            get => GetSetting(ThemeKey, ElementTheme.Default);
            set => SaveSetting(ThemeKey, value.ToString());
        }

        public bool AutoScanOnStartup
        {
            get => GetSetting(AutoScanKey, false);
            set => SaveSetting(AutoScanKey, value);
        }

        public bool ShowHiddenFiles
        {
            get => GetSetting(ShowHiddenFilesKey, false);
            set => SaveSetting(ShowHiddenFilesKey, value);
        }

        public bool ShowSystemFiles
        {
            get => GetSetting(ShowSystemFilesKey, false);
            set => SaveSetting(ShowSystemFilesKey, value);
        }

        public int ScanThreadCount
        {
            get => GetSetting(ScanThreadCountKey, 4);
            set => SaveSetting(ScanThreadCountKey, value);
        }

        public int BatchSize
        {
            get => GetSetting(BatchSizeKey, 1000);
            set => SaveSetting(BatchSizeKey, value);
        }

        public int MaxSearchResults
        {
            get => GetSetting(MaxSearchResultsKey, 1000);
            set => SaveSetting(MaxSearchResultsKey, value);
        }

        public bool EnableSearchHistory
        {
            get => GetSetting(EnableSearchHistoryKey, true);
            set => SaveSetting(EnableSearchHistoryKey, value);
        }

        public bool EnableAutoComplete
        {
            get => GetSetting(EnableAutoCompleteKey, true);
            set => SaveSetting(EnableAutoCompleteKey, value);
        }

        public bool EnableAutoBackup
        {
            get => GetSetting(EnableAutoBackupKey, false);
            set => SaveSetting(EnableAutoBackupKey, value);
        }

        public string BackupDirectory
        {
            get => GetSetting(BackupDirectoryKey, "");
            set => SaveSetting(BackupDirectoryKey, value);
        }

        public int BackupRetentionDays
        {
            get => GetSetting(BackupRetentionDaysKey, 30);
            set => SaveSetting(BackupRetentionDaysKey, value);
        }

        public bool ShowStatusBar
        {
            get => GetSetting(ShowStatusBarKey, true);
            set => SaveSetting(ShowStatusBarKey, value);
        }

        public bool ShowFilePreview
        {
            get => GetSetting(ShowFilePreviewKey, true);
            set => SaveSetting(ShowFilePreviewKey, value);
        }

        public string DefaultView
        {
            get => GetSetting(DefaultViewKey, "Details");
            set => SaveSetting(DefaultViewKey, value);
        }

        public int DefaultFontSize
        {
            get => GetSetting(DefaultFontSizeKey, 12);
            set => SaveSetting(DefaultFontSizeKey, value);
        }

        public async Task InitializeAsync()
        {
            // Load settings from file or set defaults
            await Task.CompletedTask;
        }

        public async Task SaveSettingsAsync()
        {
            // Force save all settings
            await Task.CompletedTask;
        }

        private T GetSetting<T>(string key, T defaultValue)
        {
            if (_localSettings.Values.ContainsKey(key))
            {
                var value = _localSettings.Values[key];
                if (value is T typedValue)
                    return typedValue;

                // Handle enum conversion
                if (typeof(T).IsEnum && value is string stringValue)
                {
                    return (T)System.Enum.Parse(typeof(T), stringValue);
                }

                // Handle type conversion
                try
                {
                    return (T)System.Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        private void SaveSetting<T>(string key, T value)
        {
            _localSettings.Values[key] = value;
        }
    }
}