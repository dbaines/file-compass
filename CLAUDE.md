# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build
dotnet build

# Run application
dotnet run --project src/FileCompass.Desktop

# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~DatabaseServiceTests"

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Publish for current platform (outputs FileCompass executable)
dotnet publish src/FileCompass.Desktop -c Release -o publish

# Self-contained builds with platform-version naming (e.g., FileCompass-linux-x64-0.1.0)
dotnet publish src/FileCompass.Desktop -c Release -r linux-x64 --self-contained -o publish && mv publish/FileCompass publish/FileCompass-linux-x64-0.1.0
dotnet publish src/FileCompass.Desktop -c Release -r win-x64 --self-contained -o publish  # then rename FileCompass.exe
dotnet publish src/FileCompass.Desktop -c Release -r osx-arm64 --self-contained -o publish && mv publish/FileCompass publish/FileCompass-osx-arm64-0.1.0
```

## Architecture

**FileCompass** is a cross-platform desktop app built with .NET 9 and Avalonia UI for indexing files on drives/folders and searching them when disconnected.

### Project Structure
- `FileCompass.Core` - Business logic, models, data access (no UI dependencies)
- `FileCompass.Desktop` - Avalonia UI with MVVM pattern
- `FileCompass.Tests` - xUnit tests for Core functionality
- `FileCompass.Translations` - Localization resources (.resx files)

### Core Patterns
- **MVVM**: ViewModels use `CommunityToolkit.MVVM` with `[ObservableProperty]` and `[RelayCommand]` attributes
- **Repository Pattern**: `DatabaseService`, `LocationRepository`, `FileRepository`, `SettingsRepository`
- **SQLite + FTS5**: Full-text search for efficient file querying
- **File System Abstraction**: Uses `System.IO.Abstractions` for testability

## Code Style

Style is enforced via `.editorconfig` and Meziantou.Analyzer. Key conventions:
- File-scoped namespaces (warning level)
- Private fields: `_camelCase`
- Async methods: end with `Async` suffix
- Prefer `var` when type is apparent
- 4-space indentation, LF line endings

## Development Guidelines

- Always use TDD
- Ensure linting passes and build succeeds after changes
- Use translations for all user-facing strings (via `FileCompass.Translations`)
- Features must work on Windows, Linux, and macOS
- Keep functions easy to consume
- Add comments only where logic is confusing
