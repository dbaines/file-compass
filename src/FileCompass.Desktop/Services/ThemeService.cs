using Avalonia;
using Avalonia.Styling;
using FileCompass.Core.Data;
using FileCompass.Desktop.ViewModels;

namespace FileCompass.Desktop.Services;

public enum AppTheme
{
    System,
    Light,
    Dark
}

public class ThemeService
{
    private const string ThemeSettingKey = "theme";
    private readonly DatabaseService _db;

    public ThemeService(DatabaseService db)
    {
        _db = db;
    }

    public AppTheme CurrentTheme { get; private set; } = AppTheme.System;

    public async Task InitializeAsync()
    {
        var savedTheme = await LoadThemeSettingAsync();
        CurrentTheme = savedTheme;
        ApplyTheme(savedTheme);
    }

    public async Task SetThemeAsync(AppTheme theme)
    {
        CurrentTheme = theme;
        ApplyTheme(theme);
        await SaveThemeSettingAsync(theme);
    }

    public static void ApplyTheme(AppTheme theme)
    {
        if (Application.Current is null) return;

        Application.Current.RequestedThemeVariant = theme switch
        {
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    private async Task<AppTheme> LoadThemeSettingAsync()
    {
        try
        {
            var conn = await _db.GetConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT value FROM settings WHERE key = @key";
            cmd.Parameters.AddWithValue("@key", ThemeSettingKey);

            var result = await cmd.ExecuteScalarAsync();
            if (result is string themeStr && Enum.TryParse<AppTheme>(themeStr, true, out var theme))
            {
                return theme;
            }
        }
        catch
        {
            // Ignore errors loading settings
        }

        return AppTheme.System;
    }

    private async Task SaveThemeSettingAsync(AppTheme theme)
    {
        try
        {
            var conn = await _db.GetConnectionAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO settings (key, value) VALUES (@key, @value)
                ON CONFLICT(key) DO UPDATE SET value = @value
                """;
            cmd.Parameters.AddWithValue("@key", ThemeSettingKey);
            cmd.Parameters.AddWithValue("@value", theme.ToString());
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Ignore errors saving settings
        }
    }
}
