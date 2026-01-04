# Plan: Multiple Catalogs Feature

## Overview
Add the ability to manage multiple database catalogs, allowing users to organize their indexed files into separate databases.

## Requirements
- Multiple databases called "Catalogs"
- User can create/rename/select catalogs
- UI: Dropdown above location list (left panel) with existing catalogs + "+ Create" option
- Creating a new catalog shows a name prompt
- Cog icon next to dropdown opens menu with Rename/Delete options
- Default "Default" catalog created on first run (or migrated from existing db)
- Each catalog stored as its own .db file

## Key Files to Modify

### Core Layer
- `src/FileCompass.Core/Data/DatabaseService.cs` - Add catalog path helpers
- `src/FileCompass.Core/Constants/AppConstants.cs` - Catalog folder/file constants
- `src/FileCompass.Core/Models/Catalog.cs` - New model (create)

### Desktop Layer
- `src/FileCompass.Desktop/Services/ServiceLocator.cs` - Catalog switching support
- `src/FileCompass.Desktop/Services/CatalogService.cs` - New service (create)
- `src/FileCompass.Desktop/ViewModels/MainWindowViewModel.cs` - Catalog properties/commands
- `src/FileCompass.Desktop/Views/MainWindow.axaml` - Catalog UI in left panel

### Translations
- `src/FileCompass.Translations/Strings.resx` - Catalog-related strings

## Storage Strategy
```
{AppDataPath}/
├── config.json                    # Stores last-used catalog name
└── catalogs/
    ├── Default.db                 # Default catalog
    ├── Work Projects.db           # User-created catalog
    └── Archive.db                 # User-created catalog
```

## Implementation Steps

### Step 1: Core - Catalog Model & Constants
1. Create `src/FileCompass.Core/Models/Catalog.cs`:
   ```csharp
   public record Catalog(string Name, string FilePath);
   ```
2. Update `AppConstants.cs`:
   - Add `CatalogsFolderName = "catalogs"`
   - Add `ConfigFileName = "config.json"`
   - Add `DefaultCatalogName = "Default"`

### Step 2: Core - CatalogService
Create `src/FileCompass.Desktop/Services/CatalogService.cs`:
- `GetCatalogsAsync()` - List .db files in catalogs folder
- `CreateCatalogAsync(string name)` - Create new catalog db
- `RenameCatalogAsync(string oldName, string newName)` - Rename db file
- `DeleteCatalogAsync(string name)` - Delete db file (with safety check)
- `GetCurrentCatalogAsync()` - Read from config.json
- `SetCurrentCatalogAsync(string name)` - Write to config.json
- `MigrateLegacyDatabaseAsync()` - Move old catalog.db → catalogs/Default.db

### Step 3: Desktop - ServiceLocator Updates
Modify `ServiceLocator.cs`:
- Add `CurrentCatalog` property
- Add `SwitchCatalogAsync(Catalog catalog)` method:
  - Dispose existing DatabaseService and repositories
  - Create new instances pointing to selected catalog
  - Reinitialize schema if needed
- Update `InitializeAsync()` to:
  - Run migration check first
  - Load last-used catalog or default

### Step 4: Desktop - ViewModel Updates
Modify `MainWindowViewModel.cs`:
- Add properties:
  - `ObservableCollection<Catalog> Catalogs`
  - `Catalog? SelectedCatalog`
- Add commands:
  - `CreateCatalogCommand`
  - `RenameCatalogCommand`
  - `DeleteCatalogCommand`
- Handle `SelectedCatalog` change → switch database, reload locations

### Step 5: Desktop - UI Components
Modify `MainWindow.axaml` - Add above LocationListBox in left panel:
```xml
<StackPanel Orientation="Horizontal" Margin="8" Spacing="4">
  <ComboBox ItemsSource="{Binding Catalogs}"
            SelectedItem="{Binding SelectedCatalog}"
            HorizontalAlignment="Stretch">
    <ComboBox.ItemTemplate>
      <DataTemplate>
        <TextBlock Text="{Binding Name}"/>
      </DataTemplate>
    </ComboBox.ItemTemplate>
    <!-- Footer item: "+ Create" -->
  </ComboBox>
  <Button>
    <iconPacks:PackIconMaterial Kind="Cog" Width="14" Height="14"/>
    <Button.Flyout>
      <MenuFlyout>
        <MenuItem Header="Rename..." Command="{Binding RenameCatalogCommand}"/>
        <MenuItem Header="Delete..." Command="{Binding DeleteCatalogCommand}"/>
      </MenuFlyout>
    </Button.Flyout>
  </Button>
</StackPanel>
```

### Step 6: Dialogs
- Create catalog dialog: Text input for name (reuse existing pattern)
- Rename catalog dialog: Text input with current name prefilled
- Delete confirmation: Standard confirmation dialog

### Step 7: Translations
Add to `Strings.resx`:
- `CatalogCreate`, `CatalogRename`, `CatalogDelete`
- `CatalogNameLabel`, `CatalogNamePlaceholder`
- `CatalogDeleteConfirm`, `CatalogDeleteWarning`
- `CatalogCreateTitle`, `CatalogRenameTitle`

### Step 8: Migration Logic
On first startup:
1. Check if `catalogs/` folder exists
2. If not, check for legacy `catalog.db`
3. If legacy exists, create `catalogs/` folder and move file as `Default.db`
4. If nothing exists, create `catalogs/Default.db`
5. Set current catalog to "Default" in config.json

### Step 9: Tests
- `CatalogServiceTests.cs` - CRUD operations, migration
- Update existing tests to work with catalog-based paths
