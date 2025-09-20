using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace FileCompass.Core.Data;

public class SettingsRepository
{
    private readonly DatabaseService _db;
    private const int MaxSearchHistoryItems = 20;

    public SettingsRepository(DatabaseService db)
    {
        _db = db;
    }

    public async Task<string?> GetAsync(string key)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM settings WHERE key = @key";
        cmd.Parameters.AddWithValue("@key", key);

        var result = await cmd.ExecuteScalarAsync();
        return result as string;
    }

    public async Task SetAsync(string key, string? value)
    {
        var conn = await _db.GetConnectionAsync();
        using var cmd = conn.CreateCommand();

        if (value is null)
        {
            cmd.CommandText = "DELETE FROM settings WHERE key = @key";
            cmd.Parameters.AddWithValue("@key", key);
        }
        else
        {
            cmd.CommandText = """
                INSERT INTO settings (key, value) VALUES (@key, @value)
                ON CONFLICT(key) DO UPDATE SET value = @value
                """;
            cmd.Parameters.AddWithValue("@key", key);
            cmd.Parameters.AddWithValue("@value", value);
        }

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<string>> GetSearchHistoryAsync()
    {
        var json = await GetAsync("search_history");
        if (string.IsNullOrEmpty(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task AddSearchHistoryAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return;

        var existingHistory = await GetSearchHistoryAsync();
        var history = new List<string>(existingHistory);

        // Remove if already exists (will be re-added at top)
        history.Remove(searchTerm);

        // Add to beginning
        history.Insert(0, searchTerm);

        // Trim to max size
        if (history.Count > MaxSearchHistoryItems)
            history = history.Take(MaxSearchHistoryItems).ToList();

        var json = JsonSerializer.Serialize(history);
        await SetAsync("search_history", json);
    }

    public async Task ClearSearchHistoryAsync()
    {
        await SetAsync("search_history", null);
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetColumnVisibilityAsync()
    {
        var defaults = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["icon"] = true,
            ["name"] = true,
            ["location"] = true,
            ["size"] = true,
            ["type"] = true,
            ["modified"] = false,
            ["path"] = true
        };

        var json = await GetAsync("column_visibility");
        if (string.IsNullOrEmpty(json))
            return defaults;

        try
        {
            var saved = JsonSerializer.Deserialize<Dictionary<string, bool>>(json) ?? defaults;
            // Merge with defaults to handle new columns
            foreach (var kvp in defaults)
            {
                if (!saved.ContainsKey(kvp.Key))
                    saved[kvp.Key] = kvp.Value;
            }
            return saved;
        }
        catch
        {
            return defaults;
        }
    }

    public async Task SetColumnVisibilityAsync(IReadOnlyDictionary<string, bool> visibility)
    {
        var json = JsonSerializer.Serialize(visibility);
        await SetAsync("column_visibility", json);
    }
}
