using Microsoft.Data.Sqlite;
using FileCompass.Core.Models;

namespace FileCompass.Core.Data;

public class TagRepository
{
    private readonly DatabaseService _db;

    public TagRepository(DatabaseService db)
    {
        _db = db;
    }

    public async Task<Tag> AddAsync(Tag tag)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            INSERT INTO tags (name, colour, created_at)
            VALUES (@name, @colour, @createdAt);
            SELECT last_insert_rowid();
            """;

        cmd.Parameters.AddWithValue("@name", tag.Name);
        cmd.Parameters.AddWithValue("@colour", tag.Colour);
        cmd.Parameters.AddWithValue("@createdAt", tag.CreatedAt.ToString("o"));

        var id = (long)(await cmd.ExecuteScalarAsync())!;
        tag.Id = id;
        return tag;
    }

    public async Task<Tag?> GetByIdAsync(long id)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT * FROM tags WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapTag(reader);
        }
        return null;
    }

    public async Task<Tag?> GetByNameAsync(string name)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT * FROM tags WHERE name = @name COLLATE NOCASE";
        cmd.Parameters.AddWithValue("@name", name);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapTag(reader);
        }
        return null;
    }

    public async Task<IReadOnlyList<Tag>> GetAllAsync()
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT * FROM tags ORDER BY name COLLATE NOCASE";

        var tags = new List<Tag>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tags.Add(MapTag(reader));
        }
        return tags;
    }

    public async Task UpdateAsync(Tag tag)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = """
            UPDATE tags SET
                name = @name,
                colour = @colour
            WHERE id = @id
            """;

        cmd.Parameters.AddWithValue("@id", tag.Id);
        cmd.Parameters.AddWithValue("@name", tag.Name);
        cmd.Parameters.AddWithValue("@colour", tag.Colour);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(long id)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "DELETE FROM tags WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> GetLocationCountAsync(long tagId)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "SELECT COUNT(*) FROM location_tags WHERE tag_id = @tagId";
        cmd.Parameters.AddWithValue("@tagId", tagId);

        return Convert.ToInt32(await cmd.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
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
