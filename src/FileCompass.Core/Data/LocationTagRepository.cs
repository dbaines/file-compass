using Microsoft.Data.Sqlite;
using FileCompass.Core.Models;

namespace FileCompass.Core.Data;

public class LocationTagRepository
{
    private readonly DatabaseService _db;

    public LocationTagRepository(DatabaseService db)
    {
        _db = db;
    }

    public async Task AssignTagAsync(long locationId, long tagId)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            INSERT OR IGNORE INTO location_tags (location_id, tag_id)
            VALUES (@locationId, @tagId)
            """;

        cmd.Parameters.AddWithValue("@locationId", locationId);
        cmd.Parameters.AddWithValue("@tagId", tagId);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UnassignTagAsync(long locationId, long tagId)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            DELETE FROM location_tags
            WHERE location_id = @locationId AND tag_id = @tagId
            """;

        cmd.Parameters.AddWithValue("@locationId", locationId);
        cmd.Parameters.AddWithValue("@tagId", tagId);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<Tag>> GetTagsForLocationAsync(long locationId)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            SELECT t.* FROM tags t
            INNER JOIN location_tags lt ON t.id = lt.tag_id
            WHERE lt.location_id = @locationId
            ORDER BY t.name COLLATE NOCASE
            """;

        cmd.Parameters.AddWithValue("@locationId", locationId);

        var tags = new List<Tag>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tags.Add(MapTag(reader));
        }
        return tags;
    }

    public async Task<IReadOnlyList<long>> GetLocationIdsForTagAsync(long tagId)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT location_id FROM location_tags WHERE tag_id = @tagId";
        cmd.Parameters.AddWithValue("@tagId", tagId);

        var locationIds = new List<long>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            locationIds.Add(reader.GetInt64(0));
        }
        return locationIds;
    }

    public async Task SetTagsForLocationAsync(long locationId, IEnumerable<long> tagIds)
    {
        var conn = await _db.GetConnectionAsync();

        using var deleteCmd = conn.CreateCommand();
        deleteCmd.CommandText = "DELETE FROM location_tags WHERE location_id = @locationId";
        deleteCmd.Parameters.AddWithValue("@locationId", locationId);
        await deleteCmd.ExecuteNonQueryAsync();

        foreach (var tagId in tagIds)
        {
            using var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = """
                INSERT INTO location_tags (location_id, tag_id)
                VALUES (@locationId, @tagId)
                """;
            insertCmd.Parameters.AddWithValue("@locationId", locationId);
            insertCmd.Parameters.AddWithValue("@tagId", tagId);
            await insertCmd.ExecuteNonQueryAsync();
        }
    }

    private static Tag MapTag(SqliteDataReader reader)
    {
        return new Tag
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Colour = reader.GetString(reader.GetOrdinal("colour")),
            CreatedAt = ParseDateTime(reader.GetString(reader.GetOrdinal("created_at")))
        };
    }

    private static DateTime ParseDateTime(string dateString)
    {
        if (DateTime.TryParseExact(dateString, "o", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind, out var result))
        {
            return result;
        }

        if (DateTime.TryParse(dateString, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out result))
        {
            return result;
        }

        return DateTime.UnixEpoch;
    }
}
