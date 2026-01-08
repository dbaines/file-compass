# FileCompass UX Limitations Analysis

## Executive Summary

This document identifies discoverable limitations in the FileCompass application that could impose poor user experience. The findings are categorized by severity and impact area.

---

## Critical Issues

### - [x] 1. No Virtualization for File Lists

**Location:** `src/FileCompass.Desktop/Views/MainWindow.axaml:350-437`

The DataGrid displaying files has no virtualization. All rows are created immediately when bound.

**Impact:**
- UI freezes when displaying 500+ files
- Memory grows linearly with result count
- Severe lag during search/filter operations

**User Experience:** App becomes unresponsive when searching common terms

**Fix:** Added fixed `RowHeight="28"` to enable proper row virtualization.

---

### - [x] 2. Async Void Event Handlers (13 handlers)

**Location:** `src/FileCompass.Desktop/Views/MainWindow.axaml.cs`

Multiple critical handlers use async void including:
- OnAddLocationClick, OnSettingsClick, OnBackupClick, OnRestoreClick
- All export handlers (CSV, Excel, HTML)
- OnAdvancedSearchClick, OnRenameLocationClick

**Impact:**
- Exceptions are swallowed silently
- No error recovery possible
- Unpredictable UI behavior on failures

**Fix:** Added `SafeExecuteAsync` helper that wraps async operations with try-catch and displays error dialogs.

---

## High Severity Issues

### - [x] 3. Hardcoded 1,000 Result Limit (No User Feedback)

**Locations:**
- `src/FileCompass.Core/Models/SearchQuery.cs:14`
- `src/FileCompass.Desktop/ViewModels/MainWindowViewModel.cs:632`

Search results are capped at 1,000 with:
- No indication results are truncated
- No pagination support
- No user-configurable limit

**User Experience:** User searches "*.mp3" on a large music collection, sees 1,000 results, assumes that's all there is

**Fix:** Removed the limit entirely. With virtualization, large result sets display without performance issues.

---

### - [x] 4. Fire-and-Forget Async Patterns

**Location:** `src/FileCompass.Desktop/ViewModels/MainWindowViewModel.cs:163-385`

Property change handlers use `_ = AsyncMethod()`:
```csharp
partial void OnShowDirectoriesChanged(bool value) {
    _ = ApplyFiltersAsync();  // Unobserved
}
```

**Impact:**
- Race conditions when rapidly toggling filters
- No error handling
- Column visibility changes saved 6x in wrong order

**Fix:** Added `SafeFireAndForget` helper that catches and logs exceptions from fire-and-forget operations.

---

### - [ ] 5. Parent Directory Hierarchy Not Tracked

**Location:** `src/FileCompass.Core/Services/FileScannerService.cs:272`

Comment states: "We're not tracking parent IDs for simplicity in this version"

**Impact:**
- Cannot browse folder tree structure
- Cannot navigate up/down directory hierarchy
- Limited to flat file list view

**Status:** Deferred - Tree view feature planned for future release.

---

## Medium Severity Issues

### - [ ] 6. No Database Performance Tuning

**Location:** `src/FileCompass.Core/Data/DatabaseService.cs:47-50`

Only WAL mode enabled. Missing:
- `PRAGMA page_size`, `cache_size`, `synchronous`, `temp_store`

**Impact:** Large catalogs (100k+ files) may experience slowdowns

---

### - [ ] 7. No Database Size Management

The SQLite database can grow indefinitely with no:
- Size warnings
- Old scan cleanup
- Archive functionality

---

### - [ ] 8. ObservableCollection Thrashing

**Location:** `src/FileCompass.Desktop/ViewModels/MainWindowViewModel.cs:287-372`

Collections replaced entirely on every filter/search:
```csharp
Files = new ObservableCollection<FileEntry>(filtered);
```

**Impact:** Memory spikes, GC pressure during repeated operations

---

### - [ ] 9. No Filter Cancellation

**Location:** `src/FileCompass.Desktop/ViewModels/MainWindowViewModel.cs:283`

`ApplyFiltersAsync()` has no CancellationToken. Rapid filter changes run all operations to completion.

---

### - [ ] 10. No Pause/Resume for Indexing

Cancelling a scan requires restarting from scratch. All files for location are deleted before re-scanning.

---

### - [ ] 11. Permission Errors Skipped Silently

**Location:** `src/FileCompass.Core/Services/FileScannerService.cs:198-202`

Directories with access denied are skipped. User sees no warning during scan - errors only in log.

---

### - [ ] 12. Windows MAX_PATH Not Handled

No explicit handling of 260-character path limit. Files with long paths may fail silently on Windows.

---

## Low Severity Issues

### - [x] 13. Search History Limited to 20 Items

**Location:** `src/FileCompass.Core/Data/SettingsRepository.cs:9`

Older searches automatically discarded.

**Fix:** Increased limit from 20 to 50 items.

---

### - [ ] 14. Indeterminate Progress Only

**Location:** `src/FileCompass.Desktop/Views/MainWindow.axaml:334-347`

No percentage progress, ETA, or file count during operations.

---

### - [ ] 15. Excel Export Sheet Name Limit

**Location:** `src/FileCompass.Core/Services/ExportService.cs:51`

Sheet names truncated to 31 characters (Excel limitation, not app's fault).

**Status:** N/A - This is an Excel format limitation, not a bug.

---

### - [ ] 16. FTS5 Wildcard Search Disabled

**Location:** `src/FileCompass.Core/Data/FileRepository.cs:238-244`

`*` and `?` characters are stripped from search queries. Users cannot use wildcard patterns.

---

### - [x] 17. Context Menu Async Population

**Location:** `src/FileCompass.Desktop/Views/MainWindow.axaml.cs:943`

Tags context menu populated asynchronously - may appear empty briefly.

**Fix:** Added "Loading..." placeholder while tags are being fetched.

---

## Summary

| Status | Count |
|--------|-------|
| Fixed | 6 |
| Outstanding | 10 |
| N/A | 1 |

### Fixed Issues:
1. Virtualization for file lists
2. Async void event handlers
3. 1,000 result limit removed
4. Fire-and-forget async patterns
5. Search history limit increased
6. Context menu loading flicker

### Outstanding Issues (by priority):
- **Deferred:** Parent directory hierarchy (tree view)
- **Medium:** Database tuning, size management, ObservableCollection optimization, filter cancellation, pause/resume indexing, permission error warnings, MAX_PATH handling
- **Low:** Indeterminate progress, wildcard search
