using System.Globalization;
using FileCompass.Core.Models;

namespace FileCompass.Core.Data;

public class ScanErrorRepository
{
    private readonly DatabaseService _db;

    public ScanErrorRepository(DatabaseService db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ScanError>> GetByLocationAsync(long locationId)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            SELECT id, location_id, path, error_message, error_type, occurred_at
            FROM scan_errors
            WHERE location_id = @locationId
            ORDER BY occurred_at DESC
            """;

        cmd.Parameters.AddWithValue("@locationId", locationId);

        var errors = new List<ScanError>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            errors.Add(MapScanError(reader));
        }

        return errors;
    }

    public async Task<int> GetCountByLocationAsync(long locationId)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM scan_errors WHERE location_id = @locationId";
        cmd.Parameters.AddWithValue("@locationId", locationId);

        return Convert.ToInt32(await cmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    public async Task DeleteByLocationAsync(long locationId)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "DELETE FROM scan_errors WHERE location_id = @locationId";
        cmd.Parameters.AddWithValue("@locationId", locationId);

        await cmd.ExecuteNonQueryAsync();
    }

    private static ScanError MapScanError(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        return new ScanError
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            LocationId = reader.GetInt64(reader.GetOrdinal("location_id")),
            Path = reader.GetString(reader.GetOrdinal("path")),
            ErrorMessage = reader.IsDBNull(reader.GetOrdinal("error_message")) ? null : reader.GetString(reader.GetOrdinal("error_message")),
            ErrorType = reader.IsDBNull(reader.GetOrdinal("error_type")) ? null : reader.GetString(reader.GetOrdinal("error_type")),
            OccurredAt = reader.IsDBNull(reader.GetOrdinal("occurred_at"))
                ? DateTime.UtcNow
                : DateTime.Parse(reader.GetString(reader.GetOrdinal("occurred_at")), CultureInfo.InvariantCulture)
        };
    }
}
