using System.Globalization;
using System.Resources;

namespace FileCompass.Translations;

/// <summary>
/// Strongly-typed resource class for accessing localized strings.
/// </summary>
public static class Strings
{
    private static readonly ResourceManager ResourceManager = new("FileCompass.Translations.Strings", typeof(Strings).Assembly);

    private static string GetString(string name) => ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    // App Info
    public static string AppName => GetString("AppName");

    // Menu - File
    public static string MenuFile => GetString("MenuFile");
    public static string MenuFileAddLocation => GetString("MenuFileAddLocation");
    public static string MenuFileExport => GetString("MenuFileExport");
    public static string MenuFileExportCsv => GetString("MenuFileExportCsv");
    public static string MenuFileExportExcel => GetString("MenuFileExportExcel");
    public static string MenuFileExportHtml => GetString("MenuFileExportHtml");
    public static string MenuFileBackup => GetString("MenuFileBackup");
    public static string MenuFileBackupCreate => GetString("MenuFileBackupCreate");
    public static string MenuFileBackupRestore => GetString("MenuFileBackupRestore");
    public static string MenuFileSettings => GetString("MenuFileSettings");
    public static string MenuFileTags => GetString("MenuFileTags");
    public static string MenuFileExit => GetString("MenuFileExit");

    // Menu - View
    public static string MenuView => GetString("MenuView");
    public static string MenuViewTheme => GetString("MenuViewTheme");
    public static string MenuViewThemeSystem => GetString("MenuViewThemeSystem");
    public static string MenuViewThemeLight => GetString("MenuViewThemeLight");
    public static string MenuViewThemeDark => GetString("MenuViewThemeDark");

    // Menu - Help
    public static string MenuHelp => GetString("MenuHelp");
    public static string MenuHelpAbout => GetString("MenuHelpAbout");

    // Buttons
    public static string ButtonSearch => GetString("ButtonSearch");
    public static string ButtonClear => GetString("ButtonClear");
    public static string ButtonAdvanced => GetString("ButtonAdvanced");
    public static string ButtonRefresh => GetString("ButtonRefresh");
    public static string ButtonExport => GetString("ButtonExport");
    public static string ButtonCancel => GetString("ButtonCancel");
    public static string ButtonPause => GetString("ButtonPause");
    public static string ButtonResume => GetString("ButtonResume");
    public static string ButtonOk => GetString("ButtonOk");
    public static string ButtonClose => GetString("ButtonClose");
    public static string ButtonAll => GetString("ButtonAll");
    public static string ButtonNone => GetString("ButtonNone");
    public static string ButtonAdd => GetString("ButtonAdd");

    // View Mode
    public static string ViewModeTree => GetString("ViewModeTree");
    public static string ViewModeList => GetString("ViewModeList");
    public static string BreadcrumbRoot => GetString("BreadcrumbRoot");
    public static string NavigateUp => GetString("NavigateUp");
    public static string ButtonDelete => GetString("ButtonDelete");
    public static string ButtonEdit => GetString("ButtonEdit");
    public static string ButtonSave => GetString("ButtonSave");

    // Context Menu
    public static string ContextRename => GetString("ContextRename");
    public static string ContextTag => GetString("ContextTag");
    public static string ContextRescan => GetString("ContextRescan");
    public static string ContextCancelScan => GetString("ContextCancelScan");
    public static string ContextViewErrors => GetString("ContextViewErrors");
    public static string ContextRemove => GetString("ContextRemove");

    // Locations Panel
    public static string LocationsTitle => GetString("LocationsTitle");
    public static string LocationsAddTooltip => GetString("LocationsAddTooltip");
    public static string LocationsScanning => GetString("LocationsScanning");
    public static string LocationsFilesCount => GetString("LocationsFilesCount");
    public static string LocationsFoldersCount => GetString("LocationsFoldersCount");

    // Filters
    public static string FilterShowFolders => GetString("FilterShowFolders");
    public static string FilterAllTypes => GetString("FilterAllTypes");
    public static string FilterNoTypes => GetString("FilterNoTypes");
    public static string FilterTypesCount => GetString("FilterTypesCount");

    // Search
    public static string SearchPlaceholderAll => GetString("SearchPlaceholderAll");
    public static string SearchPlaceholderLocation => GetString("SearchPlaceholderLocation");

    // Empty States
    public static string EmptyNoLocationTitle => GetString("EmptyNoLocationTitle");
    public static string EmptyNoLocationMessage => GetString("EmptyNoLocationMessage");
    public static string EmptyNoFilesTitle => GetString("EmptyNoFilesTitle");
    public static string EmptyNoFilesMessage => GetString("EmptyNoFilesMessage");
    public static string EmptyNoResultsTitle => GetString("EmptyNoResultsTitle");
    public static string EmptyNoResultsMessage => GetString("EmptyNoResultsMessage");

    // Loading
    public static string Loading => GetString("Loading");
    public static string LoadingFiles => GetString("LoadingFiles");

    // DataGrid Columns
    public static string ColumnIcon => GetString("ColumnIcon");
    public static string ColumnName => GetString("ColumnName");
    public static string ColumnLocation => GetString("ColumnLocation");
    public static string ColumnSize => GetString("ColumnSize");
    public static string ColumnType => GetString("ColumnType");
    public static string ColumnModified => GetString("ColumnModified");
    public static string ColumnPath => GetString("ColumnPath");
    public static string ColumnColumns => GetString("ColumnColumns");

    // Status Messages
    public static string StatusReady => GetString("StatusReady");
    public static string StatusInitializing => GetString("StatusInitializing");
    public static string StatusLoading => GetString("StatusLoading");
    public static string StatusLoaded => GetString("StatusLoaded");
    public static string StatusSearching => GetString("StatusSearching");
    public static string StatusSearchingSimple => GetString("StatusSearchingSimple");
    public static string StatusFound => GetString("StatusFound");
    public static string StatusFoundFiltered => GetString("StatusFoundFiltered");
    public static string StatusScanning => GetString("StatusScanning");
    public static string StatusScanProgress => GetString("StatusScanProgress");
    public static string StatusScanComplete => GetString("StatusScanComplete");
    public static string StatusScanCancelled => GetString("StatusScanCancelled");
    public static string StatusScanPaused => GetString("StatusScanPaused");
    public static string StatusScanResumed => GetString("StatusScanResumed");
    public static string StatusExportingCsv => GetString("StatusExportingCsv");
    public static string StatusExportingExcel => GetString("StatusExportingExcel");
    public static string StatusExportingHtml => GetString("StatusExportingHtml");
    public static string StatusExported => GetString("StatusExported");
    public static string StatusExportedExcel => GetString("StatusExportedExcel");
    public static string StatusExportedHtml => GetString("StatusExportedHtml");
    public static string StatusBackupCreating => GetString("StatusBackupCreating");
    public static string StatusBackupCreated => GetString("StatusBackupCreated");
    public static string StatusBackupRestoring => GetString("StatusBackupRestoring");
    public static string StatusBackupRestored => GetString("StatusBackupRestored");
    public static string StatusLocationRemoved => GetString("StatusLocationRemoved");
    public static string StatusLocationRenamed => GetString("StatusLocationRenamed");

    // Errors
    public static string ErrorGeneric => GetString("ErrorGeneric");
    public static string ErrorLoading => GetString("ErrorLoading");
    public static string ErrorSearch => GetString("ErrorSearch");
    public static string ErrorScan => GetString("ErrorScan");
    public static string ErrorExport => GetString("ErrorExport");
    public static string ErrorBackup => GetString("ErrorBackup");
    public static string ErrorRestore => GetString("ErrorRestore");
    public static string ErrorScanInProgress => GetString("ErrorScanInProgress");
    public static string ErrorLocationNotFound => GetString("ErrorLocationNotFound");
    public static string ErrorLocationAccessDeniedTitle => GetString("ErrorLocationAccessDeniedTitle");
    public static string ErrorLocationAccessDeniedMessage => GetString("ErrorLocationAccessDeniedMessage");
    public static string ErrorLocationNotFoundTitle => GetString("ErrorLocationNotFoundTitle");
    public static string ErrorLocationNotFoundMessage => GetString("ErrorLocationNotFoundMessage");
    public static string ErrorUnexpectedTitle => GetString("ErrorUnexpectedTitle");

    // Dialogs
    public static string DialogAboutTitle => GetString("DialogAboutTitle");
    public static string DialogAboutVersion => GetString("DialogAboutVersion");
    public static string DialogAboutDescription => GetString("DialogAboutDescription");
    public static string DialogAboutAuthor => GetString("DialogAboutAuthor");
    public static string DialogAboutLicense => GetString("DialogAboutLicense");
    public static string DialogAddLocationTitle => GetString("DialogAddLocationTitle");
    public static string DialogAddLocationUnsupportedTitle => GetString("DialogAddLocationUnsupportedTitle");
    public static string DialogAddLocationUnsupportedMessage => GetString("DialogAddLocationUnsupportedMessage");
    public static string DialogLocationExistsTitle => GetString("DialogLocationExistsTitle");
    public static string DialogLocationExistsMessage => GetString("DialogLocationExistsMessage");
    public static string DialogLocationExistsAddNew => GetString("DialogLocationExistsAddNew");
    public static string DialogLocationExistsRescan => GetString("DialogLocationExistsRescan");
    public static string DialogLocationExistsNeverScanned => GetString("DialogLocationExistsNeverScanned");
    public static string DialogNameLocationTitle => GetString("DialogNameLocationTitle");
    public static string DialogNameLocationLabel => GetString("DialogNameLocationLabel");
    public static string DialogRenameLocationTitle => GetString("DialogRenameLocationTitle");
    public static string DialogRenameLocationLabel => GetString("DialogRenameLocationLabel");
    public static string DialogCancelScanTitle => GetString("DialogCancelScanTitle");
    public static string DialogCancelScanMessage => GetString("DialogCancelScanMessage");
    public static string DialogCancelScanConfirm => GetString("DialogCancelScanConfirm");
    public static string DialogExportCsvTitle => GetString("DialogExportCsvTitle");
    public static string DialogExportCsvFilename => GetString("DialogExportCsvFilename");
    public static string DialogExportExcelTitle => GetString("DialogExportExcelTitle");
    public static string DialogExportExcelFilename => GetString("DialogExportExcelFilename");
    public static string DialogExportHtmlTitle => GetString("DialogExportHtmlTitle");
    public static string DialogExportHtmlFilename => GetString("DialogExportHtmlFilename");
    public static string DialogBackupCreateTitle => GetString("DialogBackupCreateTitle");
    public static string DialogBackupRestoreTitle => GetString("DialogBackupRestoreTitle");
    public static string DialogUnsupportedSaveTitle => GetString("DialogUnsupportedSaveTitle");
    public static string DialogUnsupportedSaveMessage => GetString("DialogUnsupportedSaveMessage");
    public static string DialogUnsupportedOpenMessage => GetString("DialogUnsupportedOpenMessage");

    // Tags
    public static string DialogTagsTitle => GetString("DialogTagsTitle");
    public static string TagsAddNew => GetString("TagsAddNew");
    public static string TagsNamePlaceholder => GetString("TagsNamePlaceholder");
    public static string TagsCreateNew => GetString("TagsCreateNew");
    public static string TagsSelectColour => GetString("TagsSelectColour");
    public static string TagsOptional => GetString("TagsOptional");
    public static string TagsNoTags => GetString("TagsNoTags");
    public static string DialogDeleteTagTitle => GetString("DialogDeleteTagTitle");
    public static string DialogDeleteTagMessage => GetString("DialogDeleteTagMessage");
    public static string DialogDeleteTagConfirmMessage => GetString("DialogDeleteTagConfirmMessage");

    // File Types
    public static string FiletypeCsv => GetString("FiletypeCsv");
    public static string FiletypeExcel => GetString("FiletypeExcel");
    public static string FiletypeHtml => GetString("FiletypeHtml");
    public static string FiletypeBackup => GetString("FiletypeBackup");
    public static string FiletypeAll => GetString("FiletypeAll");

    // Advanced Search
    public static string AdvancedTitle => GetString("AdvancedTitle");
    public static string AdvancedClearFilters => GetString("AdvancedClearFilters");
    public static string AdvancedSearchTerm => GetString("AdvancedSearchTerm");
    public static string AdvancedSearchPlaceholder => GetString("AdvancedSearchPlaceholder");
    public static string AdvancedLocation => GetString("AdvancedLocation");
    public static string AdvancedLocationAll => GetString("AdvancedLocationAll");
    public static string AdvancedExtension => GetString("AdvancedExtension");
    public static string AdvancedExtensionAny => GetString("AdvancedExtensionAny");
    public static string AdvancedCategory => GetString("AdvancedCategory");
    public static string AdvancedCategoryAny => GetString("AdvancedCategoryAny");
    public static string AdvancedFileSize => GetString("AdvancedFileSize");
    public static string AdvancedSizeMin => GetString("AdvancedSizeMin");
    public static string AdvancedSizeMax => GetString("AdvancedSizeMax");
    public static string AdvancedSizeTo => GetString("AdvancedSizeTo");
    public static string AdvancedModifiedDate => GetString("AdvancedModifiedDate");
    public static string AdvancedDateFrom => GetString("AdvancedDateFrom");
    public static string AdvancedDateTo => GetString("AdvancedDateTo");
    public static string AdvancedOptions => GetString("AdvancedOptions");
    public static string AdvancedIncludeFolders => GetString("AdvancedIncludeFolders");

    // Settings
    public static string SettingsTitle => GetString("SettingsTitle");
    public static string SettingsAppearance => GetString("SettingsAppearance");
    public static string SettingsTheme => GetString("SettingsTheme");
    public static string SettingsThemeSystem => GetString("SettingsThemeSystem");
    public static string SettingsThemeLight => GetString("SettingsThemeLight");
    public static string SettingsThemeDark => GetString("SettingsThemeDark");
    public static string SettingsSearch => GetString("SettingsSearch");
    public static string SettingsSearchHistory => GetString("SettingsSearchHistory");
    public static string SettingsClearHistory => GetString("SettingsClearHistory");
    public static string SettingsHistoryCount => GetString("SettingsHistoryCount");
    public static string SettingsHistoryCleared => GetString("SettingsHistoryCleared");
    public static string SettingsDatabase => GetString("SettingsDatabase");
    public static string SettingsDatabaseLocation => GetString("SettingsDatabaseLocation");
    public static string SettingsDatabaseSize => GetString("SettingsDatabaseSize");
    public static string SettingsDatabaseNotCreated => GetString("SettingsDatabaseNotCreated");

    // Size Units
    public static string SizeB => GetString("SizeB");
    public static string SizeKb => GetString("SizeKb");
    public static string SizeMb => GetString("SizeMb");
    public static string SizeGb => GetString("SizeGb");
    public static string SizeTb => GetString("SizeTb");

    // Location Status
    public static string LocationStatusNever => GetString("LocationStatusNever");
    public static string LocationStatusScanning => GetString("LocationStatusScanning");
    public static string LocationStatusCurrent => GetString("LocationStatusCurrent");
    public static string LocationStatusOutdated => GetString("LocationStatusOutdated");
    public static string LocationStatusInaccessible => GetString("LocationStatusInaccessible");
    public static string LocationStatusUnknown => GetString("LocationStatusUnknown");

    // File Categories
    public static string CategoryDocuments => GetString("CategoryDocuments");
    public static string CategoryImages => GetString("CategoryImages");
    public static string CategoryVideos => GetString("CategoryVideos");
    public static string CategoryAudio => GetString("CategoryAudio");
    public static string CategoryArchives => GetString("CategoryArchives");
    public static string CategoryCode => GetString("CategoryCode");
    public static string CategoryExecutables => GetString("CategoryExecutables");
    public static string CategoryOther => GetString("CategoryOther");

    // Scan Errors
    public static string ScanErrorsTitle => GetString("ScanErrorsTitle");
    public static string ScanErrorsNoErrors => GetString("ScanErrorsNoErrors");
    public static string ScanErrorsColumnPath => GetString("ScanErrorsColumnPath");
    public static string ScanErrorsColumnError => GetString("ScanErrorsColumnError");
    public static string ScanErrorsColumnType => GetString("ScanErrorsColumnType");
}
