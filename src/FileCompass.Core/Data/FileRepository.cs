using System.Globalization;
using Microsoft.Data.Sqlite;
using FileCompass.Core.Models;

namespace FileCompass.Core.Data;

public class FileRepository
{
    private readonly DatabaseService _db;

    public FileRepository(DatabaseService db)
    {
        _db = db;
    }

    public async Task<long> AddBatchAsync(IEnumerable<FileEntry> files)
    {
        var conn = await _db.GetConnectionAsync();
        using var transaction = conn.BeginTransaction();

        try
        {
            long count = 0;
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;

            cmd.CommandText = """
                INSERT INTO files (location_id, parent_id, name, extension, relative_path, size, is_directory, modified_at, created_at, attributes)
                VALUES (@locationId, @parentId, @name, @extension, @relativePath, @size, @isDirectory, @modifiedAt, @createdAt, @attributes)
                """;

            var locationIdParam = cmd.Parameters.Add("@locationId", SqliteType.Integer);
            var parentIdParam = cmd.Parameters.Add("@parentId", SqliteType.Integer);
            var nameParam = cmd.Parameters.Add("@name", SqliteType.Text);
            var extensionParam = cmd.Parameters.Add("@extension", SqliteType.Text);
            var relativePathParam = cmd.Parameters.Add("@relativePath", SqliteType.Text);
            var sizeParam = cmd.Parameters.Add("@size", SqliteType.Integer);
            var isDirectoryParam = cmd.Parameters.Add("@isDirectory", SqliteType.Integer);
            var modifiedAtParam = cmd.Parameters.Add("@modifiedAt", SqliteType.Text);
            var createdAtParam = cmd.Parameters.Add("@createdAt", SqliteType.Text);
            var attributesParam = cmd.Parameters.Add("@attributes", SqliteType.Text);

            foreach (var file in files)
            {
                locationIdParam.Value = file.LocationId;
                parentIdParam.Value = file.ParentId.HasValue ? file.ParentId.Value : DBNull.Value;
                nameParam.Value = file.Name;
                extensionParam.Value = file.Extension ?? (object)DBNull.Value;
                relativePathParam.Value = file.RelativePath;
                sizeParam.Value = file.Size;
                isDirectoryParam.Value = file.IsDirectory ? 1 : 0;
                modifiedAtParam.Value = file.ModifiedAt?.ToString("o") ?? (object)DBNull.Value;
                createdAtParam.Value = file.CreatedAt?.ToString("o") ?? (object)DBNull.Value;
                attributesParam.Value = file.Attributes ?? (object)DBNull.Value;

                await cmd.ExecuteNonQueryAsync();
                count++;
            }

            transaction.Commit();
            return count;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task DeleteByLocationAsync(long locationId)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "DELETE FROM files WHERE location_id = @locationId";
        cmd.Parameters.AddWithValue("@locationId", locationId);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<FileEntry>> GetByParentAsync(long locationId, long? parentId)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        if (parentId.HasValue)
        {
            cmd.CommandText = """
                SELECT * FROM files
                WHERE location_id = @locationId AND parent_id = @parentId
                ORDER BY is_directory DESC, name
                """;
            cmd.Parameters.AddWithValue("@parentId", parentId.Value);
        }
        else
        {
            cmd.CommandText = """
                SELECT * FROM files
                WHERE location_id = @locationId AND parent_id IS NULL
                ORDER BY is_directory DESC, name
                """;
        }
        cmd.Parameters.AddWithValue("@locationId", locationId);

        var files = new List<FileEntry>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            files.Add(MapFileEntry(reader));
        }
        return files;
    }

    public async Task<SearchResult> SearchAsync(SearchQuery query)
    {
        var startTime = DateTime.UtcNow;
        var conn = await _db.GetConnectionAsync();

        var whereClauses = new List<string>();
        var parameters = new List<SqliteParameter>();

        // Build WHERE clauses based on query
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            whereClauses.Add("f.id IN (SELECT rowid FROM files_fts WHERE files_fts MATCH @searchTerm)");
            parameters.Add(new SqliteParameter("@searchTerm", $"{EscapeFtsQuery(query.SearchTerm)}*"));
        }

        if (query.LocationId.HasValue)
        {
            whereClauses.Add("f.location_id = @locationId");
            parameters.Add(new SqliteParameter("@locationId", query.LocationId.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.Extension))
        {
            whereClauses.Add("f.extension = @extension");
            parameters.Add(new SqliteParameter("@extension", query.Extension.TrimStart('.')));
        }

        if (!string.IsNullOrWhiteSpace(query.FileCategory))
        {
            var extensions = GetExtensionsForCategory(query.FileCategory);
            if (extensions.Length > 0)
            {
                var extParams = new List<string>();
                for (int i = 0; i < extensions.Length; i++)
                {
                    var paramName = $"@catExt{i}";
                    extParams.Add(paramName);
                    parameters.Add(new SqliteParameter(paramName, extensions[i]));
                }
                whereClauses.Add($"f.extension IN ({string.Join(", ", extParams)})");
            }
        }

        if (query.MinSize.HasValue)
        {
            whereClauses.Add("f.size >= @minSize");
            parameters.Add(new SqliteParameter("@minSize", query.MinSize.Value));
        }

        if (query.MaxSize.HasValue)
        {
            whereClauses.Add("f.size <= @maxSize");
            parameters.Add(new SqliteParameter("@maxSize", query.MaxSize.Value));
        }

        if (query.ModifiedAfter.HasValue)
        {
            whereClauses.Add("f.modified_at >= @modifiedAfter");
            parameters.Add(new SqliteParameter("@modifiedAfter", query.ModifiedAfter.Value.ToString("o")));
        }

        if (query.ModifiedBefore.HasValue)
        {
            whereClauses.Add("f.modified_at <= @modifiedBefore");
            parameters.Add(new SqliteParameter("@modifiedBefore", query.ModifiedBefore.Value.ToString("o")));
        }

        if (!query.IncludeDirectories)
        {
            whereClauses.Add("f.is_directory = 0");
        }

        var whereClause = whereClauses.Count > 0
            ? "WHERE " + string.Join(" AND ", whereClauses)
            : "";

        // Get total count
        using var countCmd = conn.CreateCommand();
        countCmd.CommandText = $"SELECT COUNT(*) FROM files f {whereClause}";
        foreach (var p in parameters)
        {
            countCmd.Parameters.Add(new SqliteParameter(p.ParameterName, p.Value));
        }
        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);

        // Get results with location info
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"""
            SELECT f.*,
                   COALESCE(l.custom_name, l.volume_label, l.path) as location_name
            FROM files f
            JOIN locations l ON f.location_id = l.id
            {whereClause}
            ORDER BY f.name
            LIMIT @limit OFFSET @offset
            """;

        foreach (var p in parameters)
        {
            cmd.Parameters.Add(new SqliteParameter(p.ParameterName, p.Value));
        }
        cmd.Parameters.AddWithValue("@limit", query.MaxResults);
        cmd.Parameters.AddWithValue("@offset", query.Offset);

        var files = new List<FileEntry>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            files.Add(MapFileEntryWithLocation(reader));
        }

        return new SearchResult
        {
            Files = files,
            TotalCount = totalCount,
            SearchDuration = DateTime.UtcNow - startTime,
            Query = query.SearchTerm
        };
    }

    /// <summary>
    /// Escapes special FTS5 characters in a search query.
    /// FTS5 uses special characters like *, ", ? for pattern matching.
    /// </summary>
    private static string EscapeFtsQuery(string query)
    {
        return query
            .Replace("\"", "\"\"")
            .Replace("*", "")
            .Replace("?", "");
    }

    private static FileEntry MapFileEntry(SqliteDataReader reader)
    {
        return new FileEntry
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            LocationId = reader.GetInt64(reader.GetOrdinal("location_id")),
            ParentId = reader.IsDBNull(reader.GetOrdinal("parent_id")) ? null : reader.GetInt64(reader.GetOrdinal("parent_id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Extension = reader.IsDBNull(reader.GetOrdinal("extension")) ? null : reader.GetString(reader.GetOrdinal("extension")),
            RelativePath = reader.GetString(reader.GetOrdinal("relative_path")),
            Size = reader.GetInt64(reader.GetOrdinal("size")),
            IsDirectory = reader.GetInt32(reader.GetOrdinal("is_directory")) == 1,
            ModifiedAt = reader.IsDBNull(reader.GetOrdinal("modified_at")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("modified_at")), CultureInfo.InvariantCulture),
            CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at")), CultureInfo.InvariantCulture),
            Attributes = reader.IsDBNull(reader.GetOrdinal("attributes")) ? null : reader.GetString(reader.GetOrdinal("attributes"))
        };
    }

    private static FileEntry MapFileEntryWithLocation(SqliteDataReader reader)
    {
        var entry = MapFileEntry(reader);
        var locationNameOrdinal = reader.GetOrdinal("location_name");
        entry.LocationName = reader.IsDBNull(locationNameOrdinal) ? null : reader.GetString(locationNameOrdinal);
        return entry;
    }

    /// <summary>
    /// Gets file extensions for a category using the centralized mapping.
    /// </summary>
    private static string[] GetExtensionsForCategory(string category) =>
        Constants.AppConstants.FileExtensions.GetExtensionsForCategory(category);
}
