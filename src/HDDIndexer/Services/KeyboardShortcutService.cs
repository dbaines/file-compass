using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace HDDIndexer.Services
{
    public interface IKeyboardShortcutService
    {
        void RegisterGlobalShortcuts();
        void RegisterShortcut(VirtualKey key, VirtualKeyModifiers modifiers, Action action, string description);
        void UnregisterShortcut(VirtualKey key, VirtualKeyModifiers modifiers);
        Dictionary<string, string> GetAllShortcuts();
        void ShowShortcutsDialog();
    }

    public class KeyboardShortcutService : IKeyboardShortcutService
    {
        private readonly Dictionary<ShortcutKey, ShortcutAction> _shortcuts = new();
        private readonly INavigationService _navigationService;

        public KeyboardShortcutService(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public void RegisterGlobalShortcuts()
        {
            // Navigation shortcuts
            RegisterShortcut(VirtualKey.F1, VirtualKeyModifiers.None, () => ShowHelp(), "Show Help (F1)");
            RegisterShortcut(VirtualKey.F5, VirtualKeyModifiers.None, () => RefreshCurrentView(), "Refresh (F5)");
            RegisterShortcut(VirtualKey.Escape, VirtualKeyModifiers.None, () => CancelCurrentOperation(), "Cancel (Esc)");

            // Application shortcuts
            RegisterShortcut(VirtualKey.N, VirtualKeyModifiers.Control, () => StartNewScan(), "New Scan (Ctrl+N)");
            RegisterShortcut(VirtualKey.O, VirtualKeyModifiers.Control, () => OpenDrive(), "Open Drive (Ctrl+O)");
            RegisterShortcut(VirtualKey.S, VirtualKeyModifiers.Control, () => SaveData(), "Save (Ctrl+S)");
            RegisterShortcut(VirtualKey.F, VirtualKeyModifiers.Control, () => ShowSearch(), "Search (Ctrl+F)");
            RegisterShortcut(VirtualKey.P, VirtualKeyModifiers.Control, () => ShowPrint(), "Print (Ctrl+P)");
            RegisterShortcut(VirtualKey.B, VirtualKeyModifiers.Control, () => CreateBackup(), "Backup (Ctrl+B)");

            // View shortcuts
            RegisterShortcut(VirtualKey.Number1, VirtualKeyModifiers.Control, () => NavigateTo("DashboardView"), "Dashboard (Ctrl+1)");
            RegisterShortcut(VirtualKey.Number2, VirtualKeyModifiers.Control, () => NavigateTo("DriveListView"), "Drives (Ctrl+2)");
            RegisterShortcut(VirtualKey.Number3, VirtualKeyModifiers.Control, () => NavigateTo("FileBrowserView"), "Browse Files (Ctrl+3)");
            RegisterShortcut(VirtualKey.Number4, VirtualKeyModifiers.Control, () => NavigateTo("SearchView"), "Search (Ctrl+4)");
            RegisterShortcut(VirtualKey.Number5, VirtualKeyModifiers.Control, () => NavigateTo("BackupRestoreView"), "Backup (Ctrl+5)");
            RegisterShortcut(VirtualKey.Number6, VirtualKeyModifiers.Control, () => NavigateTo("SettingsView"), "Settings (Ctrl+6)");

            // Advanced shortcuts
            RegisterShortcut(VirtualKey.F, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, () => ShowAdvancedSearch(), "Advanced Search (Ctrl+Shift+F)");
            RegisterShortcut(VirtualKey.R, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, () => RestoreBackup(), "Restore (Ctrl+Shift+R)");
            RegisterShortcut(VirtualKey.E, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, () => ExportData(), "Export (Ctrl+Shift+E)");

            // Accessibility shortcuts
            RegisterShortcut(VirtualKey.H, VirtualKeyModifiers.Alt, () => ToggleHighContrast(), "Toggle High Contrast (Alt+H)");
            RegisterShortcut(VirtualKey.T, VirtualKeyModifiers.Alt, () => ToggleTheme(), "Toggle Theme (Alt+T)");
            RegisterShortcut(VirtualKey.Plus, VirtualKeyModifiers.Control, () => IncreaseFontSize(), "Increase Font Size (Ctrl++)");
            RegisterShortcut(VirtualKey.Subtract, VirtualKeyModifiers.Control, () => DecreaseFontSize(), "Decrease Font Size (Ctrl+-)");

            // Register window-level key handlers
            if (App.Current?.m_window?.Content is FrameworkElement rootElement)
            {
                rootElement.KeyDown += OnGlobalKeyDown;
            }
        }

        public void RegisterShortcut(VirtualKey key, VirtualKeyModifiers modifiers, Action action, string description)
        {
            var shortcutKey = new ShortcutKey(key, modifiers);
            _shortcuts[shortcutKey] = new ShortcutAction(action, description);
        }

        public void UnregisterShortcut(VirtualKey key, VirtualKeyModifiers modifiers)
        {
            var shortcutKey = new ShortcutKey(key, modifiers);
            _shortcuts.Remove(shortcutKey);
        }

        public Dictionary<string, string> GetAllShortcuts()
        {
            var result = new Dictionary<string, string>();
            foreach (var kvp in _shortcuts)
            {
                var keyString = FormatShortcutKey(kvp.Key);
                result[keyString] = kvp.Value.Description;
            }
            return result;
        }

        public void ShowShortcutsDialog()
        {
            // This would show a dialog with all available shortcuts
            var shortcuts = GetAllShortcuts();
            // Implementation would create and show a shortcuts help dialog
        }

        private void OnGlobalKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var modifiers = GetCurrentModifiers();
            var shortcutKey = new ShortcutKey(e.Key, modifiers);

            if _shortcuts.TryGetValue(shortcutKey, out var shortcutAction))
            {
                try
                {
                    shortcutAction.Action.Invoke();
                    e.Handled = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error executing shortcut: {ex.Message}");
                }
            }
        }

        private VirtualKeyModifiers GetCurrentModifiers()
        {
            var modifiers = VirtualKeyModifiers.None;

            if (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
                modifiers |= VirtualKeyModifiers.Control;

            if (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
                modifiers |= VirtualKeyModifiers.Shift;

            if (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
                modifiers |= VirtualKeyModifiers.Menu;

            return modifiers;
        }

        private string FormatShortcutKey(ShortcutKey key)
        {
            var parts = new List<string>();

            if (key.Modifiers.HasFlag(VirtualKeyModifiers.Control))
                parts.Add("Ctrl");
            if (key.Modifiers.HasFlag(VirtualKeyModifiers.Shift))
                parts.Add("Shift");
            if (key.Modifiers.HasFlag(VirtualKeyModifiers.Menu))
                parts.Add("Alt");

            parts.Add(key.Key.ToString());

            return string.Join("+", parts);
        }

        #region Shortcut Actions

        private void ShowHelp()
        {
            _navigationService.NavigateTo("HelpView");
        }

        private void RefreshCurrentView()
        {
            // Refresh current view - implementation depends on current page
            if (App.Current?.m_window?.Content is FrameworkElement element)
            {
                if (element.DataContext is ViewModels.ViewModelBase viewModel)
                {
                    viewModel.Initialize();
                }
            }
        }

        private void CancelCurrentOperation()
        {
            // Cancel any ongoing operations - would need to integrate with services
        }

        private void StartNewScan()
        {
            _navigationService.NavigateTo("DriveListView");
        }

        private void OpenDrive()
        {
            _navigationService.NavigateTo("FileBrowserView");
        }

        private void SaveData()
        {
            // Save current data - implementation depends on context
        }

        private void ShowSearch()
        {
            _navigationService.NavigateTo("SearchView");
        }

        private void ShowPrint()
        {
            _navigationService.NavigateTo("PrintExportView");
        }

        private void CreateBackup()
        {
            _navigationService.NavigateTo("BackupRestoreView");
        }

        private void NavigateTo(string viewName)
        {
            _navigationService.NavigateTo(viewName);
        }

        private void ShowAdvancedSearch()
        {
            // Open advanced search dialog
        }

        private void RestoreBackup()
        {
            // Open restore dialog
        }

        private void ExportData()
        {
            // Open export dialog
        }

        private void ToggleHighContrast()
        {
            // Toggle high contrast mode
            var settingsService = App.Current.Services.GetService(typeof(ISettingsService)) as ISettingsService;
            // Implementation would toggle high contrast
        }

        private void ToggleTheme()
        {
            var settingsService = App.Current.Services.GetService(typeof(ISettingsService)) as ISettingsService;
            if (settingsService != null)
            {
                settingsService.Theme = settingsService.Theme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
            }
        }

        private void IncreaseFontSize()
        {
            var settingsService = App.Current.Services.GetService(typeof(ISettingsService)) as ISettingsService;
            if (settingsService != null && settingsService.DefaultFontSize < 24)
            {
                settingsService.DefaultFontSize += 2;
            }
        }

        private void DecreaseFontSize()
        {
            var settingsService = App.Current.Services.GetService(typeof(ISettingsService)) as ISettingsService;
            if (settingsService != null && settingsService.DefaultFontSize > 8)
            {
                settingsService.DefaultFontSize -= 2;
            }
        }

        #endregion
    }

    public record ShortcutKey(VirtualKey Key, VirtualKeyModifiers Modifiers);
    public record ShortcutAction(Action Action, string Description);
}