# HDDIndexer

A professional Windows 11 desktop application for cataloging and indexing hard drive files, enabling users to search across multiple drives even when they're not connected.

## Features

### 🗂️ Drive Management
- **Smart Drive Detection**: Automatically detects and catalogs all connected drives
- **Custom Drive Naming**: Assign user-friendly names to drives for easy identification
- **Drive Status Tracking**: Monitor online/offline status and scan progress
- **Serial Number Tracking**: Unique identification prevents duplicate entries

### 🔍 Powerful Search
- **Quick Search**: Instant filename matching as you type
- **Advanced Search**: Multi-criteria search with filters for:
  - File size ranges
  - Date ranges (created/modified)
  - File types and extensions
  - Specific drives or all drives
- **Search History**: Quick access to previous searches
- **Real-time Results**: Instant search results with file details

### 📊 File Cataloging
- **Complete File Indexing**: Stores filename, path, size, dates, and metadata
- **Hierarchical Organization**: Maintains folder structure for easy browsing
- **Batch Processing**: Efficiently handles drives with millions of files
- **Background Scanning**: Non-blocking UI during long scan operations

### 💾 Backup & Restore
- **Full Database Backup**: Complete catalog backup with compression
- **Incremental Backups**: Save only changes since last backup
- **Conflict Resolution**: Smart handling of duplicate drives during restore
- **Backup Validation**: Ensures backup integrity

### 📄 Export & Print
- **Multiple Formats**: Export catalogs to PDF, Excel, CSV, or HTML
- **Print Layouts**: Professional formatted catalogs for printing
- **Custom Reports**: Generate summary or detailed file listings
- **Search Result Export**: Export specific search results

## Technology Stack

- **Framework**: C# with WinUI 3 and Windows App SDK
- **Runtime**: .NET 6.0+
- **Database**: SQLite with Entity Framework Core
- **Architecture**: MVVM pattern with dependency injection
- **UI**: Modern Windows 11 Fluent Design

## System Requirements

- **Operating System**: Windows 11 (Windows 10 version 19041+ supported)
- **Memory**: 4GB RAM minimum, 8GB recommended
- **Storage**: 100MB for application + space for database
- **Architecture**: x64, x86, or ARM64

## Installation

### Option 1: Microsoft Store (Recommended)
Download from the Microsoft Store for automatic updates and easy installation.

### Option 2: Direct Download
1. Download the latest release from the releases page
2. Run the installer (`HDDIndexer.msix`)
3. Follow the installation wizard

### Option 3: Build from Source
```bash
# Clone the repository
git clone https://github.com/yourusername/hdd-indexer.git
cd hdd-indexer

# Build using the included scripts
./build.sh        # Linux with Docker
# OR
build.bat         # Windows native
```

## Getting Started

1. **Launch HDDIndexer** from the Start menu
2. **Add Drives**: Click "Add Drive" to scan your first drive
3. **Assign Names**: Give your drives memorable names (e.g., "Family Photos", "Work Documents")
4. **Wait for Scan**: The initial scan catalogs all files on the drive
5. **Start Searching**: Use the search bar to find files across all cataloged drives

## Usage

### Scanning Drives
- Connect any drive (USB, external HDD, network drive)
- Select it from the available drives list
- Click "Scan" and assign a custom name
- Monitor progress in the status bar

### Searching Files
- **Quick Search**: Type in the search bar for instant results
- **Advanced Search**: Use filters for size, date, type, and location
- **Browse Mode**: Navigate folder structures like Windows Explorer

### Managing Catalogs
- **Backup**: Regularly backup your catalog database
- **Export**: Generate reports in PDF, Excel, or CSV format
- **Settings**: Configure scan options, themes, and preferences

## Development

### Project Structure
```
src/HDDIndexer/
├── Models/           # Data models (Drive, FileEntry)
├── Services/         # Business logic (Scanner, Search, Backup)
├── ViewModels/       # MVVM view models
├── Views/           # UI views and dialogs
├── Converters/      # Data binding converters
├── Data/            # Entity Framework context
└── Assets/          # Application icons and resources
```

### Building
For detailed build instructions including cross-platform compilation, see [BUILD.md](BUILD.md).

### Architecture
The application follows MVVM architecture with:
- **Models**: Entity Framework entities for database mapping
- **Services**: Business logic with dependency injection
- **ViewModels**: Data binding and command handling
- **Views**: WinUI 3 XAML user interface

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **Documentation**: Check the in-app help system
- **Issues**: Report bugs on GitHub Issues
- **Questions**: Use GitHub Discussions

## Roadmap

### Current Version
- ✅ Drive scanning and cataloging
- ✅ Advanced search functionality
- ✅ Backup and restore system
- ✅ Export to multiple formats
- ✅ Modern Windows 11 UI

### Planned Features
- 🔄 Network drive support
- 🔄 Duplicate file detection
- 🔄 File preview integration
- 🔄 Cloud storage integration
- 🔄 Mobile companion app

## Screenshots

[Screenshots would be added here showing the main interface, search results, and key features]

---

**HDDIndexer** - Never lose track of your files again! 🗂️✨