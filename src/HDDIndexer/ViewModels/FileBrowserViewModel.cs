using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HDDIndexer.Data;
using HDDIndexer.Models;
using Microsoft.EntityFrameworkCore;

namespace HDDIndexer.ViewModels
{
    public partial class FileBrowserViewModel : ViewModelBase
    {
        private readonly CatalogDbContext _dbContext;

        [ObservableProperty]
        private Drive? _currentDrive;

        [ObservableProperty]
        private ObservableCollection<FileEntry> _files = new();

        [ObservableProperty]
        private ObservableCollection<FileEntry> _currentPath = new();

        [ObservableProperty]
        private FileEntry? _selectedFile;

        [ObservableProperty]
        private FileEntry? _currentDirectory;

        [ObservableProperty]
        private string _currentPathString = string.Empty;

        [ObservableProperty]
        private bool _showHiddenFiles;

        public FileBrowserViewModel(CatalogDbContext dbContext)
        {
            _dbContext = dbContext;
            Title = "File Browser";
        }

        public void SetDrive(Drive drive)
        {
            CurrentDrive = drive;
            _ = LoadRootFilesAsync();
        }

        [RelayCommand]
        private async Task LoadRootFilesAsync()
        {
            if (CurrentDrive == null) return;

            try
            {
                IsBusy = true;
                var rootFiles = await _dbContext.Files
                    .Where(f => f.DriveId == CurrentDrive.DriveId && f.ParentFileId == null)
                    .OrderBy(f => !f.IsDirectory)
                    .ThenBy(f => f.FileName)
                    .ToListAsync();

                Files.Clear();
                foreach (var file in rootFiles)
                {
                    if (!ShowHiddenFiles && file.FileName.StartsWith("."))
                        continue;
                    Files.Add(file);
                }

                CurrentPath.Clear();
                CurrentPathString = CurrentDrive.DriveName;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task NavigateToFolderAsync(FileEntry? folder)
        {
            if (folder == null || !folder.IsDirectory) return;

            try
            {
                IsBusy = true;
                CurrentDirectory = folder;

                var childFiles = await _dbContext.Files
                    .Where(f => f.ParentFileId == folder.FileId)
                    .OrderBy(f => !f.IsDirectory)
                    .ThenBy(f => f.FileName)
                    .ToListAsync();

                Files.Clear();
                foreach (var file in childFiles)
                {
                    if (!ShowHiddenFiles && file.FileName.StartsWith("."))
                        continue;
                    Files.Add(file);
                }

                // Update breadcrumb
                CurrentPath.Add(folder);
                UpdatePathString();
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task NavigateUpAsync()
        {
            if (CurrentDirectory?.ParentFileId != null)
            {
                var parentFolder = await _dbContext.Files
                    .FirstOrDefaultAsync(f => f.FileId == CurrentDirectory.ParentFileId);

                if (parentFolder != null)
                {
                    await NavigateToFolderAsync(parentFolder);
                    if (CurrentPath.Count > 0)
                    {
                        CurrentPath.RemoveAt(CurrentPath.Count - 1);
                        UpdatePathString();
                    }
                }
            }
            else
            {
                await LoadRootFilesAsync();
            }
        }

        [RelayCommand]
        private async Task NavigateToBreadcrumbAsync(FileEntry? folder)
        {
            if (folder == null) return;

            var index = CurrentPath.IndexOf(folder);
            if (index >= 0)
            {
                // Remove all items after the selected one
                while (CurrentPath.Count > index + 1)
                {
                    CurrentPath.RemoveAt(CurrentPath.Count - 1);
                }

                await NavigateToFolderAsync(folder);
            }
        }

        [RelayCommand]
        private void OpenFileProperties(FileEntry? file)
        {
            if (file == null) return;
            // Show file properties dialog
        }

        [RelayCommand]
        private void CopyPath(FileEntry? file)
        {
            if (file == null) return;
            // Copy file path to clipboard
            Windows.ApplicationModel.DataTransfer.DataPackage package = new();
            package.SetText(file.FullPath);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
        }

        private void UpdatePathString()
        {
            if (CurrentDrive == null)
            {
                CurrentPathString = string.Empty;
                return;
            }

            var pathParts = new[] { CurrentDrive.DriveName }
                .Concat(CurrentPath.Select(f => f.FileName));
            CurrentPathString = string.Join(" > ", pathParts);
        }

        partial void OnShowHiddenFilesChanged(bool value)
        {
            if (CurrentDirectory != null)
            {
                _ = NavigateToFolderAsync(CurrentDirectory);
            }
            else if (CurrentDrive != null)
            {
                _ = LoadRootFilesAsync();
            }
        }
    }
}