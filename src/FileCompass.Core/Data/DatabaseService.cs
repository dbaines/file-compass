using Microsoft.Data.Sqlite;
using FileCompass.Core.Constants;

namespace FileCompass.Core.Data;

public class DatabaseService : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;
    private bool _disposed;

    public string DatabasePath { get; }

    public DatabaseService(string? databasePath = null)
    {
        DatabasePath = databasePath ?? GetDefaultDatabasePath();
        _connectionString = $"Data Source={DatabasePath}";
    }

    public static string GetDefaultDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, AppConstants.AppName);
        Directory.CreateDirectory(appFolder);
        return Path.Combine(appFolder, AppConstants.DatabaseFileName);
    }

    public async Task InitializeAsync()
    {
        await EnsureConnectionAsync();
        await CreateSchemaAsync();
    }

    public async Task<SqliteConnection> GetConnectionAsync()
    {
        await EnsureConnectionAsync();
        return _connection!;
    }

    private async Task EnsureConnectionAsync()
    {
        if (_connection is null)
        {
            _connection = new SqliteConnection(_connectionString);
            await _connection.OpenAsync();

            // Enable foreign keys and WAL mode for better performance
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL;";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private async Task CreateSchemaAsync()
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = Schema;
        await cmd.ExecuteNonQueryAsync();
    }

    private const string Schema = """
        -- Cataloged locations (drives/folders)
        -- Note: path is NOT unique to support multiple drives mounted at the same path
        CREATE TABLE IF NOT EXISTS locations (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            path TEXT NOT NULL,
            custom_name TEXT,
            volume_label TEXT,
            file_system TEXT,
            total_size INTEGER,
            free_space INTEGER,
            total_files INTEGER DEFAULT 0,
            total_folders INTEGER DEFAULT 0,
            last_scan_start TEXT,
            last_scan_complete TEXT,
            scan_duration_seconds INTEGER,
            status TEXT DEFAULT 'never_scanned',
            created_at TEXT DEFAULT CURRENT_TIMESTAMP
        );

        -- Indexed files and folders
        CREATE TABLE IF NOT EXISTS files (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            location_id INTEGER NOT NULL,
            parent_id INTEGER,
            name TEXT NOT NULL,
            extension TEXT,
            relative_path TEXT NOT NULL,
            size INTEGER DEFAULT 0,
            is_directory INTEGER DEFAULT 0,
            modified_at TEXT,
            created_at TEXT,
            attributes TEXT,
            FOREIGN KEY (location_id) REFERENCES locations(id) ON DELETE CASCADE,
            FOREIGN KEY (parent_id) REFERENCES files(id) ON DELETE CASCADE
        );

        -- Full-text search virtual table
        CREATE VIRTUAL TABLE IF NOT EXISTS files_fts USING fts5(
            name,
            relative_path,
            content='files',
            content_rowid='id'
        );

        -- Triggers to keep FTS in sync
        CREATE TRIGGER IF NOT EXISTS files_ai AFTER INSERT ON files BEGIN
            INSERT INTO files_fts(rowid, name, relative_path)
            VALUES (new.id, new.name, new.relative_path);
        END;

        CREATE TRIGGER IF NOT EXISTS files_ad AFTER DELETE ON files BEGIN
            INSERT INTO files_fts(files_fts, rowid, name, relative_path)
            VALUES ('delete', old.id, old.name, old.relative_path);
        END;

        CREATE TRIGGER IF NOT EXISTS files_au AFTER UPDATE ON files BEGIN
            INSERT INTO files_fts(files_fts, rowid, name, relative_path)
            VALUES ('delete', old.id, old.name, old.relative_path);
            INSERT INTO files_fts(rowid, name, relative_path)
            VALUES (new.id, new.name, new.relative_path);
        END;

        -- Performance indexes
        CREATE INDEX IF NOT EXISTS idx_files_location ON files(location_id);
        CREATE INDEX IF NOT EXISTS idx_files_parent ON files(parent_id);
        CREATE INDEX IF NOT EXISTS idx_files_extension ON files(extension);
        CREATE INDEX IF NOT EXISTS idx_files_name ON files(name);
        CREATE INDEX IF NOT EXISTS idx_files_is_directory ON files(is_directory);

        -- Scan error log
        CREATE TABLE IF NOT EXISTS scan_errors (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            location_id INTEGER NOT NULL,
            path TEXT NOT NULL,
            error_message TEXT,
            error_type TEXT,
            occurred_at TEXT DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (location_id) REFERENCES locations(id) ON DELETE CASCADE
        );

        -- Application settings
        CREATE TABLE IF NOT EXISTS settings (
            key TEXT PRIMARY KEY,
            value TEXT
        );

        -- Tags for organizing locations
        CREATE TABLE IF NOT EXISTS tags (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL UNIQUE,
            colour TEXT NOT NULL DEFAULT '#808080',
            created_at TEXT DEFAULT CURRENT_TIMESTAMP
        );

        -- Junction table for many-to-many relationship between locations and tags
        CREATE TABLE IF NOT EXISTS location_tags (
            location_id INTEGER NOT NULL,
            tag_id INTEGER NOT NULL,
            PRIMARY KEY (location_id, tag_id),
            FOREIGN KEY (location_id) REFERENCES locations(id) ON DELETE CASCADE,
            FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE
        );

        -- Index for efficient queries by tag
        CREATE INDEX IF NOT EXISTS idx_location_tags_tag ON location_tags(tag_id);
        """;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _connection?.Dispose();
            }
            _disposed = true;
        }
    }
}
