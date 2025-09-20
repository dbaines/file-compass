# Hard Drive File Catalog Application

## Project Overview

A Windows 11 desktop application for indexing and cataloging hard drive files, enabling users to search for files across multiple drives even when they're not connected. The application provides comprehensive file management capabilities including scanning, indexing, searching, backup/restore, and printing functionalities.

## Core Requirements

- Scan and index files from any hard drive or external storage
- Store file information in a local database for offline access
- Allow users to assign custom names to scanned drives
- Provide powerful search capabilities across all cataloged files
- Enable manual browsing of cataloged drive contents
- Support printing of drive catalogs
- Include backup and restore functionality for the catalog database
- Full compatibility with Windows 11

## Technology Stack

### Primary Framework
- **Language**: C#
- **UI Framework**: WinUI 3 with Windows App SDK
- **Runtime**: .NET 6.0+ for modern Windows support
- **Database**: SQLite with Entity Framework Core
- **Architecture**: MVVM pattern with dependency injection

### Key Libraries
- **Entity Framework Core**: Database ORM and migrations
- **SQLite**: Lightweight, embedded database
- **Windows App SDK**: Modern Windows 11 integration
- **iTextSharp/PDFSharp**: PDF generation for printing
- **EPPlus**: Excel export functionality

## Database Design

### Tables Structure

```sql
-- Hard drives/volumes
CREATE TABLE Drives (
    DriveId INTEGER PRIMARY KEY,
    DriveName TEXT NOT NULL,           -- User-assigned name
    VolumeLabel TEXT,                  -- Original volume label
    SerialNumber TEXT,                 -- Hardware serial number
    TotalSize INTEGER,                 -- Drive capacity in bytes
    FileSystem TEXT,                   -- NTFS, FAT32, etc.
    ScanDate DATETIME,                 -- Last scan timestamp
    Description TEXT                   -- User description
);

-- Files and directories
CREATE TABLE Files (
    FileId INTEGER PRIMARY KEY,
    DriveId INTEGER,                   -- Foreign key to Drives
    FileName TEXT NOT NULL,            -- File/folder name
    FilePath TEXT NOT NULL,            -- Full path from drive root
    FileSize INTEGER,                  -- Size in bytes (NULL for directories)
    DateCreated DATETIME,              -- File creation date
    DateModified DATETIME,             -- File modification date
    IsDirectory BOOLEAN,               -- True for folders, false for files
    FileExtension TEXT,                -- File extension (lowercase)
    ParentFileId INTEGER,              -- Self-referencing for hierarchy
    FOREIGN KEY (DriveId) REFERENCES Drives(DriveId),
    FOREIGN KEY (ParentFileId) REFERENCES Files(FileId)
);

-- Performance indexes
CREATE INDEX idx_filename ON Files(FileName);
CREATE INDEX idx_extension ON Files(FileExtension);
CREATE INDEX idx_drive ON Files(DriveId);
CREATE INDEX idx_path ON Files(FilePath);
CREATE INDEX idx_parent ON Files(ParentFileId);
```

## User Interface Design

### Main Window Layout
- **Header Bar**: Application title, menu bar (File, Edit, View, Tools, Help)
- **Left Panel**: Drive management with tree view of cataloged drives
- **Center Panel**: File browser with hierarchical tree view or search results
- **Right Panel**: File/drive details and properties
- **Bottom Panel**: Status bar with progress indicators and statistics

### Key Views

#### 1. Drive Management View
- List of all cataloged drives with custom names
- Scan status indicators (never scanned, scanning, up to date, outdated)
- Drive properties (size, file system, last scan date)
- Context menu for scan, rename, remove, properties

#### 2. File Browser View
- Hierarchical tree view of files and folders
- Sortable columns: Name, Size, Type, Date Modified, Path
- Icon display for file types
- Breadcrumb navigation
- Context menu for file operations

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
- Modern Windows 11 Fluent Design principles
- Automatic dark/light theme support
- Rounded corners and modern visual elements
- Smooth animations and transitions
- Responsive layout with resizable panels

## Core Features Implementation

### 1. File Scanning System

#### Scanning Process
```csharp
public class DriveScanner
{
    public async Task<ScanResult> ScanDriveAsync(
        DriveInfo drive,
        string customName,
        IProgress<ScanProgress> progress,
        CancellationToken cancellationToken)
    {
        // Validate drive accessibility
        // Create or update drive record
        // Enumerate files recursively
        // Batch insert into database
        // Update progress reporting
        // Handle errors gracefully
    }
}
```

#### Key Features
- **Asynchronous Processing**: Non-blocking UI during scans
- **Progress Reporting**: Real-time updates with file count and speed
- **Batch Processing**: Insert files in batches of 1000 for performance
- **Error Handling**: Skip inaccessible files, log errors
- **Pause/Resume**: Allow interruption of long-running scans
- **Incremental Updates**: Detect changes since last scan
- **Memory Optimization**: Stream processing for large directories

### 2. Search Functionality

#### Search Types
1. **Quick Search**: Real-time filename matching as user types
2. **Advanced Search**: Multiple criteria with Boolean operators
3. **Full-Text Search**: SQLite FTS for comprehensive searching

#### Search Implementation
```csharp
public class SearchService
{
    // Quick filename search
    public async Task<List<FileResult>> QuickSearchAsync(string query);

    // Advanced multi-criteria search
    public async Task<List<FileResult>> AdvancedSearchAsync(SearchCriteria criteria);

    // Full-text search across file names and paths
    public async Task<List<FileResult>> FullTextSearchAsync(string query);
}
```

#### Search Criteria
- **Filename**: Exact match, contains, starts with, ends with, regex
- **File Extension**: Single or multiple extensions
- **File Size**: Range queries (greater than, less than, between)
- **Date Range**: Created or modified within specified dates
- **Drive Location**: Specific drives or all drives
- **File Type**: Predefined categories (documents, images, videos, etc.)

### 3. Drive Management

#### Drive Operations
- **Auto-Detection**: Scan for available drives on startup
- **Custom Naming**: User-friendly names for drives
- **Drive Properties**: Display detailed information
- **Scan Scheduling**: Automatic periodic rescans
- **Drive Removal**: Remove drives from catalog with confirmation

#### Drive Status Tracking
- **Never Scanned**: New drives requiring initial scan
- **Scanning**: Currently in progress
- **Up to Date**: Recently scanned
- **Outdated**: Scan older than configurable threshold
- **Offline**: Drive not currently connected

### 4. Backup and Restore System

#### Backup Features
```csharp
public class BackupService
{
    // Create full database backup
    public async Task<BackupResult> CreateFullBackupAsync(string path);

    // Create incremental backup (changes since last backup)
    public async Task<BackupResult> CreateIncrementalBackupAsync(string path);

    // Schedule automatic backups
    public void ScheduleBackup(BackupSchedule schedule);
}
```

#### Backup Types
1. **Full Backup**: Complete database with compression
2. **Incremental Backup**: Only changes since last backup
3. **Selective Backup**: Choose specific drives to backup
4. **Cloud Backup**: Integration with OneDrive, Google Drive

#### Restore Options
- **Full Restore**: Replace entire database
- **Merge Restore**: Add drives to existing database
- **Selective Restore**: Choose specific drives to restore
- **Conflict Resolution**: Handle duplicate drives intelligently

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

## Development Phases

### Phase 1: Foundation (Weeks 1-3)
**Core Infrastructure**

#### Tasks
- [ ] Set up C# WinUI 3 project with Windows App SDK
- [ ] Implement SQLite database with Entity Framework
- [ ] Create basic MVVM architecture and dependency injection
- [ ] Design and implement database schema with migrations
- [ ] Build basic drive detection functionality
- [ ] Implement file system enumeration engine

#### Deliverables
- Project structure with proper architecture
- Database schema with Entity Framework models
- Basic drive scanning capability (console-based)
- Unit tests for core functionality

### Phase 2: User Interface (Weeks 4-6)
**Basic UI and Navigation**

#### Tasks
- [ ] Design main window layout with panels
- [ ] Implement drive list view with basic operations
- [ ] Create file browser with tree view control
- [ ] Add basic search interface (quick search only)
- [ ] Implement Windows 11 theming and styling
- [ ] Add progress indicators and status reporting

#### Deliverables
- Functional main window with navigation
- Drive management interface
- Basic file browsing capability
- Search interface (filename only)
- Responsive UI with proper theming

### Phase 3: Core Functionality (Weeks 7-10)
**Scanning and Search**

#### Tasks
- [ ] Complete asynchronous drive scanning implementation
- [ ] Add pause/resume functionality for scans
- [ ] Implement advanced search with multiple criteria
- [ ] Add search history and saved searches
- [ ] Create file details panel with metadata display
- [ ] Implement incremental scan updates

#### Deliverables
- Full drive scanning with progress tracking
- Advanced search functionality
- Complete file browsing with details
- Scan management (pause/resume/cancel)
- Search result export capability

### Phase 4: Data Management (Weeks 11-13)
**Backup, Restore, and Export**

#### Tasks
- [ ] Implement backup system with compression
- [ ] Add restore functionality with conflict resolution
- [ ] Create print system with multiple formats
- [ ] Add PDF and Excel export capabilities
- [ ] Implement settings and configuration management
- [ ] Add data validation and error recovery

#### Deliverables
- Complete backup and restore system
- Print functionality with multiple formats
- Export capabilities (PDF, Excel, CSV)
- Application settings and preferences
- Error handling and recovery mechanisms

### Phase 5: Polish and Integration (Weeks 14-16)
**Windows 11 Integration and Final Features**

#### Tasks
- [ ] Implement Windows 11 specific features (notifications, taskbar)
- [ ] Add keyboard shortcuts and accessibility features
- [ ] Create installer with MSIX packaging
- [ ] Implement auto-update mechanism
- [ ] Add help documentation and user guide
- [ ] Performance optimization and testing

#### Deliverables
- Complete Windows 11 integration
- Professional installer and deployment
- Comprehensive help system
- Performance-optimized application
- Full test coverage and documentation

## Technical Considerations

### Performance Optimization
- **Database Indexing**: Optimize queries with proper indexes
- **Memory Management**: Efficient handling of large file lists
- **UI Virtualization**: Virtual scrolling for large datasets
- **Background Processing**: Non-blocking operations
- **Caching**: Intelligent caching of search results and file data

### Security and Privacy
- **Data Protection**: Encrypt sensitive backup files
- **Access Control**: Respect file system permissions
- **Privacy**: No external data transmission without consent
- **Validation**: Input validation and SQL injection prevention

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

## Deployment Strategy

### Distribution Options
1. **Microsoft Store**: MSIX package for easy installation and updates
2. **Direct Download**: Traditional installer for enterprise environments
3. **Portable Version**: No-installation option for USB drives

### System Requirements
- **Operating System**: Windows 11 (21H2 or later)
- **Memory**: 4GB RAM minimum, 8GB recommended
- **Storage**: 100MB for application, additional space for database
- **Architecture**: x64 and ARM64 support

### Installation Features
- **Silent Installation**: Command-line options for enterprise deployment
- **User vs System**: Per-user or system-wide installation options
- **Auto-Updates**: Background update checking and installation
- **Uninstall Cleanup**: Complete removal of application data

## Future Enhancements

### Potential Features
- **Network Drive Support**: Catalog shared network locations
- **Cloud Integration**: Direct integration with cloud storage services
- **File Preview**: Built-in preview for common file types
- **Duplicate Detection**: Find and manage duplicate files across drives
- **Metadata Extraction**: Extract and search file metadata (EXIF, tags)
- **Synchronization**: Sync catalogs across multiple computers

### Technology Evolution
- **Machine Learning**: Smart categorization and file suggestions
- **OCR Integration**: Search within document text content
- **API Development**: REST API for third-party integrations
- **Mobile Companion**: Mobile app for remote access to catalogs

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

This comprehensive plan provides a roadmap for developing a professional-grade hard drive cataloging application that meets all specified requirements while maintaining high standards for performance, usability, and reliability.