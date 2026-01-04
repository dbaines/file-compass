using Microsoft.Data.Sqlite;
using FileCompass.Core.Models;

namespace FileCompass.Core.Data;

public class LocationRepository
{
    private readonly DatabaseService _db;

    public LocationRepository(DatabaseService db)
    {
        _db = db;
    }

    public async Task<Location> AddAsync(Location location)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            INSERT INTO locations (path, custom_name, volume_label, file_system, total_size, free_space, status, created_at)
            VALUES (@path, @customName, @volumeLabel, @fileSystem, @totalSize, @freeSpace, @status, @createdAt);
            SELECT last_insert_rowid();
            """;

        cmd.Parameters.AddWithValue("@path", location.Path);
        cmd.Parameters.AddWithValue("@customName", (object?)location.CustomName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@volumeLabel", (object?)location.VolumeLabel ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@fileSystem", (object?)location.FileSystem ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@totalSize", (object?)location.TotalSize ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@freeSpace", (object?)location.FreeSpace ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", location.Status.ToString().ToLowerInvariant());
        cmd.Parameters.AddWithValue("@createdAt", location.CreatedAt.ToString("o"));

        var id = (long)(await cmd.ExecuteScalarAsync())!;
        location.Id = id;
        return location;
    }

    public async Task<Location?> GetByIdAsync(long id)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT * FROM locations WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapLocation(reader);
        }
        return null;
    }

    public async Task<Location?> GetByPathAsync(string path)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT * FROM locations WHERE path = @path";
        cmd.Parameters.AddWithValue("@path", path);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapLocation(reader);
        }
        return null;
    }

    public async Task<IReadOnlyList<Location>> GetAllByPathAsync(string path)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT * FROM locations WHERE path = @path ORDER BY custom_name COLLATE NOCASE, created_at";
        cmd.Parameters.AddWithValue("@path", path);

        var locations = new List<Location>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            locations.Add(MapLocation(reader));
        }
        return locations;
    }

    public async Task<IReadOnlyList<Location>> GetAllAsync()
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT * FROM locations ORDER BY custom_name COLLATE NOCASE, path COLLATE NOCASE";

        var locations = new List<Location>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            locations.Add(MapLocation(reader));
        }
        return locations;
    }

    public async Task UpdateAsync(Location location)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            UPDATE locations SET
                custom_name = @customName,
                volume_label = @volumeLabel,
                file_system = @fileSystem,
                total_size = @totalSize,
                free_space = @freeSpace,
                total_files = @totalFiles,
                total_folders = @totalFolders,
                last_scan_start = @lastScanStart,
                last_scan_complete = @lastScanComplete,
                scan_duration_seconds = @scanDuration,
                status = @status
            WHERE id = @id
            """;

        cmd.Parameters.AddWithValue("@id", location.Id);
        cmd.Parameters.AddWithValue("@customName", (object?)location.CustomName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@volumeLabel", (object?)location.VolumeLabel ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@fileSystem", (object?)location.FileSystem ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@totalSize", (object?)location.TotalSize ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@freeSpace", (object?)location.FreeSpace ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@totalFiles", location.TotalFiles);
        cmd.Parameters.AddWithValue("@totalFolders", location.TotalFolders);
        cmd.Parameters.AddWithValue("@lastScanStart", location.LastScanStart?.ToString("o") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@lastScanComplete", location.LastScanComplete?.ToString("o") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@scanDuration", (object?)location.ScanDurationSeconds ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", location.Status.ToString().ToLowerInvariant());

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "DELETE FROM locations WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        await cmd.ExecuteNonQueryAsync();
    }

    private static Location MapLocation(SqliteDataReader reader)
    {
        return new Location
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            Path = reader.GetString(reader.GetOrdinal("path")),
            CustomName = reader.IsDBNull(reader.GetOrdinal("custom_name")) ? null : reader.GetString(reader.GetOrdinal("custom_name")),
            VolumeLabel = reader.IsDBNull(reader.GetOrdinal("volume_label")) ? null : reader.GetString(reader.GetOrdinal("volume_label")),
            FileSystem = reader.IsDBNull(reader.GetOrdinal("file_system")) ? null : reader.GetString(reader.GetOrdinal("file_system")),
            TotalSize = reader.IsDBNull(reader.GetOrdinal("total_size")) ? null : reader.GetInt64(reader.GetOrdinal("total_size")),
            FreeSpace = reader.IsDBNull(reader.GetOrdinal("free_space")) ? null : reader.GetInt64(reader.GetOrdinal("free_space")),
            TotalFiles = reader.GetInt32(reader.GetOrdinal("total_files")),
            TotalFolders = reader.GetInt32(reader.GetOrdinal("total_folders")),
            LastScanStart = ParseNullableDateTime(reader, "last_scan_start"),
            LastScanComplete = ParseNullableDateTime(reader, "last_scan_complete"),
            ScanDurationSeconds = reader.IsDBNull(reader.GetOrdinal("scan_duration_seconds")) ? null : reader.GetInt32(reader.GetOrdinal("scan_duration_seconds")),
            Status = ParseLocationStatus(reader.GetString(reader.GetOrdinal("status"))),
            CreatedAt = ParseDateTime(reader.GetString(reader.GetOrdinal("created_at")))
        };
    }

    /// <summary>
    /// Parses a nullable DateTime from the database using ISO 8601 format with fallback.
    /// </summary>
    private static DateTime? ParseNullableDateTime(SqliteDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return null;

        return ParseDateTime(reader.GetString(ordinal));
    }

    /// <summary>
    /// Parses a DateTime string, expecting ISO 8601 format ("o") but falling back to general parsing.
    /// Uses invariant culture to avoid locale-specific parsing issues.
    /// </summary>
    private static DateTime ParseDateTime(string dateString)
    {
        // Try ISO 8601 format first (how we store dates)
        if (DateTime.TryParseExact(dateString, "o", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind, out var result))
        {
            return result;
        }

        // Fallback for legacy data or other formats
        if (DateTime.TryParse(dateString, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out result))
        {
            return result;
        }

        // Last resort: return epoch (better than throwing for corrupted data)
        return DateTime.UnixEpoch;
    }

    /// <summary>
    /// Safely parses a LocationStatus enum value with fallback to Outdated for invalid values.
    /// </summary>
    private static LocationStatus ParseLocationStatus(string statusString)
    {
        if (Enum.TryParse<LocationStatus>(statusString, ignoreCase: true, out var status))
        {
            return status;
        }

        // Return a safe default for corrupted data rather than throwing
        return LocationStatus.Outdated;
    }
}
