using System.IO.Abstractions;
using FileCompass.Core.Data;
using FileCompass.Core.Services;

namespace FileCompass.Desktop.Services;

public static class ServiceLocator
{
    private static DatabaseService? _databaseService;
    private static LocationRepository? _locationRepository;
    private static FileRepository? _fileRepository;
    private static SettingsRepository? _settingsRepository;
    private static TagRepository? _tagRepository;
    private static LocationTagRepository? _locationTagRepository;
    private static ScanErrorRepository? _scanErrorRepository;
    private static FileScannerService? _fileScannerService;
    private static ExportService? _exportService;
    private static BackupService? _backupService;
    private static ThemeService? _themeService;
    private static IFileSystem? _fileSystem;

    public static IFileSystem FileSystem => _fileSystem ??= new FileSystem();

    public static DatabaseService DatabaseService => _databaseService ??= new DatabaseService();

    public static LocationRepository LocationRepository =>
        _locationRepository ??= new LocationRepository(DatabaseService);

    public static FileRepository FileRepository =>
        _fileRepository ??= new FileRepository(DatabaseService);

    public static SettingsRepository SettingsRepository =>
        _settingsRepository ??= new SettingsRepository(DatabaseService);

    public static TagRepository TagRepository =>
        _tagRepository ??= new TagRepository(DatabaseService);

    public static LocationTagRepository LocationTagRepository =>
        _locationTagRepository ??= new LocationTagRepository(DatabaseService);

    public static ScanErrorRepository ScanErrorRepository =>
        _scanErrorRepository ??= new ScanErrorRepository(DatabaseService);

    public static FileScannerService FileScannerService =>
        _fileScannerService ??= new FileScannerService(
            FileSystem,
            DatabaseService,
            LocationRepository,
            FileRepository,
            SettingsRepository);

    public static ExportService ExportService =>
        _exportService ??= new ExportService();

    public static BackupService BackupService =>
        _backupService ??= new BackupService(DatabaseService);

    public static ThemeService ThemeService =>
        _themeService ??= new ThemeService(DatabaseService);

    public static async Task InitializeAsync()
    {
        await DatabaseService.InitializeAsync();
        await ThemeService.InitializeAsync();
    }
}
