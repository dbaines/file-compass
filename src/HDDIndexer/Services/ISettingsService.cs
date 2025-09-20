using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace HDDIndexer.Services
{
    public interface ISettingsService
    {
        Task InitializeAsync();
        Task SaveSettingsAsync();

        // Application settings
        ElementTheme Theme { get; set; }
        bool AutoScanOnStartup { get; set; }
        bool ShowHiddenFiles { get; set; }
        bool ShowSystemFiles { get; set; }
        int ScanThreadCount { get; set; }
        int BatchSize { get; set; }

        // Search settings
        int MaxSearchResults { get; set; }
        bool EnableSearchHistory { get; set; }
        bool EnableAutoComplete { get; set; }

        // Backup settings
        bool EnableAutoBackup { get; set; }
        string BackupDirectory { get; set; }
        int BackupRetentionDays { get; set; }

        // UI settings
        bool ShowStatusBar { get; set; }
        bool ShowFilePreview { get; set; }
        string DefaultView { get; set; }
        int DefaultFontSize { get; set; }
    }
}