# Hard Drive File Catalog Application

## Project Info

- **Working Name**: FileCompass (defined as constant `AppConstants.AppName` for easy rebranding)
- **License**: MIT
- **Repository**: Open source

## Project Overview

An open source, universal (Windows 11, Linux, MacOS) desktop application for indexing and cataloging files in folders or drives, enabling users to search for files across multiple drives even when they're not connected. The application provides comprehensive file management capabilities including scanning, indexing, searching, backup/restore, and printing functionalities.

The application should be easy to use, intuitive, fully accessible and respect user settings eg. colour modes, contrast settings, zoom levels, prefers-reduced-motion etc.

## Core Requirements

- Scan and index files from any folder, hard drive or external storage
- Store file names and sizes in a local database for offline access
- Allow users to assign custom names to scanned locations
- Provide powerful search capabilities across all cataloged files
- Enable manual browsing of cataloged drive contents
- Support printing of drive catalogs
- Include backup and restore functionality for the catalog database

## Technology Stack

### Platform: .NET 9 + Avalonia UI

**Core Framework:**
- **.NET 9**: Cross-platform runtime with AOT compilation support (latest STS, upgrade to .NET 10 LTS when available)
- **Avalonia UI 11**: Cross-platform XAML-based UI framework
- **C# 12**: Modern language features, nullable reference types enabled

**Key Libraries:**
- **Microsoft.Data.Sqlite**: SQLite database access
- **CommunityToolkit.Mvvm**: MVVM implementation with source generators
- **Avalonia.Svg.Skia**: SVG icon support
- **QuestPDF** or **SkiaSharp**: PDF generation for exports
- **ClosedXML**: Excel export functionality
- **System.IO.Abstractions**: Testable file system operations

**Build & Distribution:**
- Single-file portable executable (self-contained)
- AOT compilation for fast startup
- No installer required - extract and run

**Supported Platforms:**
- Windows 10/11 (x64, arm64)
- Linux (x64) - tested on Fedora and Linux Mint
- macOS (x64, arm64) - secondary priority

## Database Design

### Schema

```sql
-- Cataloged locations (drives/folders)
CREATE TABLE locations (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    path TEXT NOT NULL UNIQUE,
    custom_name TEXT,
    volume_label TEXT,
    file_system TEXT,
    total_size INTEGER,
    free_space INTEGER,
    total_files INTEGER DEFAULT 0,
    total_folders INTEGER DEFAULT 0,
    last_scan_start TEXT,
    last_scan_complete TEXT,
    scan_duration_seconds INTEGER,
    status TEXT DEFAULT 'never_scanned',
    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

-- Indexed files and folders
CREATE TABLE files (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    location_id INTEGER NOT NULL,
    parent_id INTEGER,
    name TEXT NOT NULL,
    extension TEXT,
    relative_path TEXT NOT NULL,
    size INTEGER DEFAULT 0,
    is_directory INTEGER DEFAULT 0,
    modified_at TEXT,
    created_at TEXT,
    attributes TEXT,
    FOREIGN KEY (location_id) REFERENCES locations(id) ON DELETE CASCADE,
    FOREIGN KEY (parent_id) REFERENCES files(id) ON DELETE CASCADE
);

-- Full-text search virtual table
CREATE VIRTUAL TABLE files_fts USING fts5(
    name,
    relative_path,
    content='files',
    content_rowid='id'
);

-- Performance indexes
CREATE INDEX idx_files_location ON files(location_id);
CREATE INDEX idx_files_parent ON files(parent_id);
CREATE INDEX idx_files_extension ON files(extension);
CREATE INDEX idx_files_name ON files(name);
CREATE INDEX idx_files_is_directory ON files(is_directory);

-- Scan error log
CREATE TABLE scan_errors (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    location_id INTEGER NOT NULL,
    path TEXT NOT NULL,
    error_message TEXT,
    error_type TEXT,
    occurred_at TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (location_id) REFERENCES locations(id) ON DELETE CASCADE
);

-- Application settings
CREATE TABLE settings (
    key TEXT PRIMARY KEY,
    value TEXT
);
```

### File System Support

The application reads file metadata using standard .NET APIs which support:
- **Windows**: NTFS, FAT32, exFAT, ReFS
- **Linux**: ext2/3/4, Btrfs, XFS, FAT32, exFAT, NTFS (via ntfs-3g)
- **macOS**: APFS, HFS+, FAT32, exFAT

Note: File system-specific attributes may not be available across all platforms.

## User Interface Design

### Main Window Layout
- **Header Bar**: Application title
- **Left Panel**: Location management with tree view of cataloged locations
- **Center Panel**: File browser with hierarchical tree view or search results
- **Right Panel**: File/location details and properties
- **Bottom Panel**: Status bar with progress indicators and statistics

### Key Views

#### 1. Location Management View
- List of all cataloged locations with optional custom names
- Scan status indicators (never scanned, scanning, up to date, outdated)
- Drive properties (size, file system, last scan date)
- Browse for a folder/drive to catalog and initiate scan/indexing

#### 2. File Browser View
- Hierarchical tree view of files and folders
- Sortable columns: Name, Size, Type, Date Modified, Path
- Icon display for file types
- Breadcrumb navigation

#### 3. Search Interface
- Quick search bar for instant filename matching
- Advanced search dialog with multiple criteria
- Search filters: file type, size range, date range, drive location
- Search history dropdown
- Export search results functionality

#### 4. Print Preview
- Catalog formatting options (detailed, compact, summary)
- Page layout settings (margins, orientation, font size)
- Print preview with pagination
- Export options (PDF, Excel, HTML)

### UI Styling
- Modern application design
- Automatic dark/light theme support, allow user to customise
- Rounded corners and modern visual elements
- Smooth animations and transitions
- Responsive layout with resizable panels

## Core Features Implementation

### 1. File Scanning System

#### Key Features
- **Asynchronous Processing**: Non-blocking UI during scans
- **Progress Reporting**: Real-time updates with file count and speed
- **Batch Processing**: Insert files in batches of 1000 for performance
- **Error Handling**: Skip inaccessible files, log errors
- **Pause/Resume**: Allow interruption of long-running scans
- **Incremental Updates**: Detect changes since last scan
- **Memory Optimization**: Stream processing for large directories
- **Reportability**: Display errors to the user for them to action

### 2. Search Functionality

#### Search Types
1. **Quick Search**: Real-time filename matching as user types
2. **Advanced Search**: Multiple criteria with Boolean operators
3. **Full-Text Search**: SQLite FTS for comprehensive searching

#### Search Criteria
- **Filename**: Exact match, contains, starts with, ends with, regex
- **File Extension**: Single or multiple extensions
- **File Size**: Range queries (greater than, less than, between)
- **Date Range**: Created or modified within specified dates
- **Location**: Specific locations or all locations
- **File Type**: Predefined categories (documents, images, videos, etc.)

### 3. Location Management

#### Location Operations
- **Custom Naming**: User-friendly names for drives/folders
- **Location Properties**: Display detailed information
- **Location Removal**: Remove locations from catalog with confirmation

#### Location Status Tracking
- **Never Scanned**: New locations requiring initial scan
- **Scanning**: Currently in progress
- **Up to Date**: Recently scanned
- **Outdated**: Scan older than configurable threshold
- **Offline**: Location not currently connected or not found

### 4. Backup and Restore System

#### Backup Features
- **Full Database Export**: Complete SQLite database as a single file
- **Compressed Backup**: ZIP archive with database and metadata
- **Portable Format**: Backups can be shared with other users running the app
- **Automatic Backup**: Optional auto-backup on application exit or schedule

#### Backup File Contents
- `catalog.db`: The SQLite database
- `metadata.json`: App version, backup date, location summary

#### Restore Options
- **Full Restore**: Replace entire database (with confirmation)
- **Merge Restore**: Import locations into existing database
- **Conflict Resolution**: Skip, replace, or rename duplicate locations

#### Sharing Workflow
1. User exports backup to `.hdi` file (ZIP with custom extension)
2. File can be shared via email, USB, cloud storage, etc.
3. Recipient imports the backup to browse the catalog offline

### 5. Print and Export System

#### Print Formats
1. **Detailed Catalog**: Complete file listing with all metadata
2. **Compact Listing**: File names only with tree structure
3. **Summary Report**: Statistics and folder summaries
4. **Custom Selection**: Print specific folders or search results

#### Export Options
- **PDF Generation**: Professional formatted catalogs
- **Excel Export**: Spreadsheet format for data analysis
- **CSV Export**: Compatible with other applications
- **HTML Report**: Web-viewable format with navigation

#### Print Configuration
- **Layout Options**: Portrait/landscape, columns, font sizes
- **Content Selection**: Full drive, specific folders, search results
- **Header/Footer**: Custom titles, page numbers, dates
- **Sorting Options**: By name, size, date, type

### 6. Application Settings

#### User Preferences
- **Theme**: System default, Light, Dark
- **Language**: English (extensible for localization)
- **Default View**: Last used location or home screen
- **Startup Behavior**: Normal window, minimized, remember last position/size

#### Scanning Options
- **Exclusion Patterns**: Folders/files to skip (e.g., `node_modules`, `.git`, `$RECYCLE.BIN`)
- **Default Exclusions**: Common system/temp folders pre-configured
- **Follow Symlinks**: Enable/disable symbolic link traversal
- **Hidden Files**: Include or exclude hidden files/folders

#### Database Settings
- **Database Location**: Default (app folder) or custom path
- **Auto-Backup**: Frequency (never, daily, weekly, on exit)
- **Backup Location**: Where to store automatic backups

#### Search Preferences
- **Default Search Scope**: All locations or current location
- **Search History Size**: Number of recent searches to remember
- **Regex Timeout**: Maximum seconds for regex pattern matching (default: 5s)

### 7. File Type Categories

File types are determined by extension for performance. Categories enable filtering:

| Category | Extensions |
|----------|------------|
| Documents | .doc, .docx, .pdf, .txt, .rtf, .odt, .xls, .xlsx, .ppt, .pptx |
| Images | .jpg, .jpeg, .png, .gif, .bmp, .svg, .webp, .ico, .tiff |
| Videos | .mp4, .avi, .mkv, .mov, .wmv, .flv, .webm |
| Audio | .mp3, .wav, .flac, .aac, .ogg, .wma, .m4a |
| Archives | .zip, .rar, .7z, .tar, .gz, .bz2 |
| Code | .cs, .js, .ts, .py, .java, .cpp, .h, .html, .css, .json, .xml |
| Executables | .exe, .msi, .app, .sh, .bat, .cmd |

Categories are defined in a configuration file and can be extended by users.

## Accessibility

The application must be fully accessible:

### Keyboard Navigation
- All features accessible without a mouse
- Logical tab order through UI elements
- Arrow key navigation in lists and trees
- Keyboard shortcuts for common actions (documented in Help)
- Focus indicators visible in all themes

### Screen Reader Support
- All interactive elements have accessible names
- Status changes announced (scan progress, search results count)
- Data tables have proper row/column headers
- Error messages associated with relevant controls

### Visual Accessibility
- Respects system high contrast mode
- Minimum 4.5:1 contrast ratio for text
- No information conveyed by color alone
- Resizable text (respects system font scaling)
- Respects `prefers-reduced-motion` for animations

### Motor Accessibility
- Large click targets (minimum 44x44 pixels)
- No time-limited interactions
- Drag operations have keyboard alternatives

## Development

- Use test-driven development strategies wherever possible.
- Build after changes and fix build errors immediately without being prompted
- Test the application periodically to ensure it's working as intended

## Technical Considerations

### Performance Optimization
- **Database Indexing**: Optimize queries with proper indexes
- **Memory Management**: Efficient handling of large file lists
- **UI Virtualization**: Virtual scrolling for large datasets
- **Background Processing**: Non-blocking operations
- **Caching**: Intelligent caching of search results and file data

### Security and Privacy
- **Local Only**: All data stays on user's machine, no network calls except optional update check
- **Access Control**: Respect file system permissions during scanning
- **Privacy**: No telemetry, no external data transmission
- **Validation**: Input validation and parameterized queries (no SQL injection)

### Error Handling
- **Graceful Degradation**: Handle inaccessible files and drives
- **Recovery**: Automatic recovery from database corruption
- **Logging**: Comprehensive error logging for troubleshooting
- **User Feedback**: Clear error messages and resolution guidance

### Scalability
- **Large Drives**: Handle drives with millions of files
- **Multiple Drives**: Support dozens of cataloged drives
- **Database Size**: Optimize for databases exceeding 1GB
- **Concurrent Operations**: Multiple scans and searches simultaneously

## Success Metrics

### User Experience
- **Scan Speed**: Index 1 million files in under 30 minutes
- **Search Performance**: Sub-second search results for typical queries
- **Memory Usage**: Peak memory under 1GB during normal operation
- **Startup Time**: Application launch under 3 seconds

### Reliability
- **Uptime**: 99.9% operation without crashes
- **Data Integrity**: Zero data loss during normal operations
- **Recovery**: Automatic recovery from 95% of error conditions
- **Compatibility**: Support for all common file systems and drive types

## Development Phases

### Phase 1: Core Foundation
**Goal**: Minimal viable product with essential functionality

- [ ] Project setup (.NET 8, Avalonia, folder structure)
- [ ] SQLite database initialization and schema
- [ ] Basic UI shell (main window, panels, navigation)
- [ ] Add location dialog (folder picker)
- [ ] File scanning engine (async, with progress)
- [ ] Basic file browser (tree view of scanned files)
- [ ] Simple search (filename contains)
- [ ] CSV export of search results
- [ ] Cross-platform build verification (Windows + Linux)

**Deliverable**: App can scan a folder, browse results, search by name, export to CSV

### Phase 2: Search & Polish
**Goal**: Full search capabilities and professional UI

- [ ] Full-text search with SQLite FTS5
- [ ] Advanced search dialog (filters, size range, date range)
- [ ] Search history
- [ ] Location management (rename, delete, rescan)
- [ ] Location status indicators
- [ ] Theme support (light/dark/system)
- [ ] Settings dialog
- [ ] File type icons
- [ ] Column sorting in file browser
- [ ] Keyboard navigation

**Deliverable**: Polished search experience with customizable appearance

### Phase 3: Export & Backup
**Goal**: Complete data portability

- [ ] PDF export (file listings, summaries)
- [ ] Excel export
- [ ] HTML report generation
- [ ] Print functionality with preview
- [ ] Backup/restore system (.hdi files)
- [ ] Import from backup (merge/replace)
- [ ] Automatic backup option

**Deliverable**: Full export suite and shareable backups

### Phase 4: Advanced Features
**Goal**: Power user features and optimization

- [ ] Incremental scanning (detect changes)
- [ ] Scan exclusion patterns
- [ ] Multiple concurrent scans
- [ ] Scan pause/resume
- [ ] Scan error reporting dialog
- [ ] Performance optimization for 1M+ files
- [ ] Memory usage optimization
- [ ] Application auto-update check (optional)

**Deliverable**: Production-ready application

## Project Structure

```
FileCompass/
├── src/
│   ├── FileCompass.Core/           # Business logic, no UI dependencies
│   │   ├── Models/                # Domain models
│   │   ├── Services/              # Scanning, search, export services
│   │   ├── Data/                  # Database access, repositories
│   │   └── FileCompass.Core.csproj
│   │
│   ├── FileCompass.Desktop/        # Avalonia UI application
│   │   ├── Views/                 # XAML views
│   │   ├── ViewModels/            # MVVM view models
│   │   ├── Controls/              # Custom controls
│   │   ├── Styles/                # Theme and styling
│   │   ├── Assets/                # Icons, images
│   │   └── FileCompass.Desktop.csproj
│   │
│   └── FileCompass.Tests/          # Unit and integration tests
│       └── FileCompass.Tests.csproj
│
├── plan/
│   └── plan.md
├── FileCompass.sln
└── README.md
```