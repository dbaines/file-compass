# FileCompass

[![Build](https://github.com/dbaines/hdd-indexer/actions/workflows/build.yml/badge.svg)](https://github.com/dbaines/hdd-indexer/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)

A cross-platform desktop application for indexing and cataloging files on hard drives, external storage, and folders. Search for files across multiple drives even when they're not connected.

## Disclosure

This application is entirely vibe-coded using Claude Code

## Features

- **Scan & Index**: Quickly scan folders and drives to create a searchable file catalog
- **Cross-Location Search**: Search across all indexed locations simultaneously
- **Advanced Search**: Filter by file type, size range, date modified, and more
- **Multiple Export Formats**: Export your file catalog to CSV, PDF, Excel, or HTML
- **Backup & Restore**: Create portable backups of your catalog to share or restore
- **Theme Support**: Light, dark, and system theme options
- **Keyboard Navigation**: Full keyboard shortcuts for power users

## Supported Platforms

- Windows 10/11 (x64, arm64)
- Linux (x64) - tested on Fedora and Linux Mint
- macOS (x64, arm64)

## Installation

### Prerequisites

- [.NET 9 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) or later

### Running from Source

```bash
# Clone the repository
git clone https://github.com/dbaines/file-compass.git
cd file-compass

# Build and run
dotnet build
dotnet run --project src/FileCompass.Desktop
```

### Building a Release

```bash
# Build for current platform
dotnet publish src/FileCompass.Desktop -c Release -o publish

# Build for Linux
dotnet publish src/FileCompass.Desktop -c Release -r linux-x64 --self-contained -o publish

# Build for windows
dotnet publish src/FileCompass.Desktop/FileCompass.Desktop.csproj -c Release -r win-x64
```

## Testing

The project uses xUnit for unit testing. Tests are located in `src/FileCompass.Tests/`.

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run a specific test class
dotnet test --filter "FullyQualifiedName~DatabaseServiceTests"
```

## Usage

1. **Add a Location**: Click the "+" button or use File > Add Location to select a folder or drive to index
2. **Wait for Scan**: The application will scan all files and folders in the selected location
3. **Search**: Use the search box to find files across all indexed locations
4. **Export**: Export your results to CSV, PDF, Excel, or HTML via File > Export

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+N | Add new location |
| Ctrl+F | Focus search box |
| F5 | Refresh selected location |
| Escape | Clear search |
| Enter | Execute search |

## Technology Stack

- **.NET 9**: Cross-platform runtime
- **Avalonia UI 11**: Cross-platform XAML-based UI framework
- **SQLite**: Local database with FTS5 full-text search
- **QuestPDF**: PDF generation
- **ClosedXML**: Excel export

## Project Structure

```
file-compass/
├── src/
│   ├── FileCompass.Core/      # Business logic, models, services
│   ├── FileCompass.Desktop/   # Avalonia UI application
│   └── FileCompass.Tests/     # Unit tests
├── plan/
│   └── plan.md               # Development plan
└── README.md
```

## Architecture

The project follows a clean separation of concerns:

- **FileCompass.Core**: Business logic, data access, and services. Has no UI dependencies and is fully testable.
- **FileCompass.Desktop**: Avalonia UI application using MVVM pattern with CommunityToolkit.Mvvm.
- **FileCompass.Tests**: xUnit-based unit tests for Core functionality.

## Contributing

Contributions are welcome! Please read our [Contributing Guidelines](CONTRIBUTING.md) for details on:

- Setting up your development environment
- Code style and conventions
- How to submit pull requests

## Security

For security issues, please see our [Security Policy](SECURITY.md). Do not open public issues for security vulnerabilities.

## License

MIT License - see [LICENSE](LICENSE) for details.
