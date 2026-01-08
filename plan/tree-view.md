# Tree View Implementation Plan

## Summary
Add a hierarchical tree view for browsing files alongside the existing list view, with a toggle in the filter bar, lazy loading, and persisted view preference.

## Requirements
- Tree view shows folders/files hierarchically with expandable folders
- List view remains available as an option
- Toggle between views via two icon buttons (FileTree / FormatListBulleted) in filter bar
- Tree view is the default
- "Show Folders" checkbox and file type filter hidden when tree view is active (both only apply to list view)
- Search results always display as list view
- View preference persisted across sessions
- Lazy loading: children loaded only when folder expanded

---

## New Files to Create

### 1. `src/FileCompass.Desktop/ViewModels/FileViewMode.cs`
```csharp
namespace FileCompass.Desktop.ViewModels;

public enum FileViewMode
{
    Tree,
    List
}
```

### 2. `src/FileCompass.Desktop/ViewModels/FileTreeItemViewModel.cs`
Wrapper around `FileEntry` for tree-specific state:
- `IsExpanded` - triggers lazy loading on change
- `IsLoading` - shows loading indicator
- `Children` - ObservableCollection of child items
- Pass-through properties to underlying `FileEntry`
- Placeholder child for folders to show expand arrow before loading

### 3. `src/FileCompass.Desktop/Converters/ViewModeButtonBackgroundConverter.cs`
Converter to highlight the active view mode button (returns accent color for active, transparent for inactive).

---

## Files to Modify

### 4. `src/FileCompass.Desktop/ViewModels/MainWindowViewModel.cs`

**Add properties:**
```csharp
[ObservableProperty]
private FileViewMode _viewMode = FileViewMode.Tree;

[ObservableProperty]
private ObservableCollection<FileTreeItemViewModel> _treeItems = [];

// Computed
public bool ShowListViewFilters => ViewMode == FileViewMode.List || _isShowingSearchResults;
public bool ShowTreeView => !IsLoadingFiles && !ShowEmptyState && ViewMode == FileViewMode.Tree && !_isShowingSearchResults;
```

**Update `ShowFileList`:**
```csharp
public bool ShowFileList => !IsLoadingFiles && !ShowEmptyState && (ViewMode == FileViewMode.List || _isShowingSearchResults);
```

**Add commands:**
```csharp
[RelayCommand]
private void SetTreeView() => ViewMode = FileViewMode.Tree;

[RelayCommand]
private void SetListView() => ViewMode = FileViewMode.List;
```

**Add methods:**
- `LoadViewModeAsync()` - load persisted view mode setting
- `LoadTreeForLocationAsync(Location)` - load root items and create tree item ViewModels
- `LoadChildrenAsync(locationId, parentId)` - passed to tree items for lazy loading

**Update existing methods:**
- `InitializeAsync()` - call `LoadViewModeAsync()`
- `OnViewModeChanged()` - persist setting, reload data in appropriate view
- `OnSelectedLocationChanged()` - check view mode, call appropriate load method
- `SearchAsync()` / `ClearSearchAsync()` - notify `ShowListViewFilters` and `ShowTreeView`
- `NotifyEmptyStateChanged()` - add `ShowTreeView`, `ShowListViewFilters`

### 5. `src/FileCompass.Desktop/Views/MainWindow.axaml`

**Update filter bar grid** (line 229):
Change from `ColumnDefinitions="Auto,Auto,*,Auto"` to `ColumnDefinitions="Auto,Auto,Auto,*,Auto"`

**Add view toggle buttons** (new Column 0):
```xml
<StackPanel Grid.Column="0" Orientation="Horizontal" Margin="0,0,16,0">
    <Button ToolTip.Tip="Tree View"
            Command="{Binding SetTreeViewCommand}"
            Padding="6"
            Background="{Binding ViewMode, Converter={x:Static local:ViewModeButtonBackgroundConverter.TreeInstance}}">
        <iconPacks:PackIconMaterial Kind="FileTree" Width="16" Height="16"/>
    </Button>
    <Button ToolTip.Tip="List View"
            Command="{Binding SetListViewCommand}"
            Padding="6" Margin="4,0,0,0"
            Background="{Binding ViewMode, Converter={x:Static local:ViewModeButtonBackgroundConverter.ListInstance}}">
        <iconPacks:PackIconMaterial Kind="FormatListBulleted" Width="16" Height="16"/>
    </Button>
</StackPanel>
```

**Update Show Folders checkbox** (move to Column 1, add visibility):
```xml
<CheckBox Grid.Column="1"
          Content="Show Folders"
          IsChecked="{Binding ShowDirectories}"
          IsVisible="{Binding ShowListViewFilters}"
          .../>
```

**Update File Type Filter dropdown** (Column 2, add visibility):
```xml
<Button Grid.Column="2"
        x:Name="FileTypeFilterButton"
        IsVisible="{Binding ShowListViewFilters}"
        .../>
```

**Update remaining columns:** Spacer -> Column 3, Export -> Column 4

**Update DataGrid visibility:**
```xml
IsVisible="{Binding ShowFileList}"
```

**Add TreeView** (after DataGrid):
```xml
<TreeView ItemsSource="{Binding TreeItems}"
          IsVisible="{Binding ShowTreeView}"
          SelectionMode="Single">
    <TreeView.ItemTemplate>
        <TreeDataTemplate ItemsSource="{Binding Children}">
            <!-- Icon, Name, Size, Loading indicator -->
        </TreeDataTemplate>
    </TreeView.ItemTemplate>
    <TreeView.ItemContainerTheme>
        <ControlTheme TargetType="TreeViewItem" BasedOn="{StaticResource {x:Type TreeViewItem}}">
            <Setter Property="IsExpanded" Value="{Binding IsExpanded, Mode=TwoWay}"/>
        </ControlTheme>
    </TreeView.ItemContainerTheme>
</TreeView>
```

### 6. `src/FileCompass.Desktop/Views/MainWindow.axaml.cs`
Add TreeView selection handler to sync `SelectedFile` with selected tree item.

---

## Implementation Order

1. Create `FileViewMode.cs` enum
2. Create `FileTreeItemViewModel.cs` with lazy loading logic
3. Create `ViewModeButtonBackgroundConverter.cs`
4. Update `MainWindowViewModel.cs`:
   - Add properties and commands
   - Add `LoadViewModeAsync()` and `LoadTreeForLocationAsync()`
   - Update `InitializeAsync()`, `OnSelectedLocationChanged()`, etc.
5. Update `MainWindow.axaml`:
   - Add view toggle buttons to filter bar
   - Update grid columns and move existing controls
   - Add visibility binding to Show Folders checkbox and File Type Filter
   - Update DataGrid visibility binding
   - Add TreeView control
6. Update `MainWindow.axaml.cs` with selection handler

---

## Settings Key
- Key: `"view_mode"`
- Values: `"Tree"` or `"List"`
- Default: `FileViewMode.Tree`

---

## Notes
- Tree view shows all files/folders unfiltered (filters are hidden and don't apply)
- Reuses existing `FileIconKindConverter` and `FileIconColorConverter` for tree item icons
- Uses existing `GetByParentAsync(locationId, parentId)` in FileRepository for lazy loading
