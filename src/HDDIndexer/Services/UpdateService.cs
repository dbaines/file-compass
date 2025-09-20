using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.UI.Xaml.Controls;

namespace HDDIndexer.Services
{
    public interface IUpdateService
    {
        Task<bool> CheckForUpdatesAsync();
        Task<UpdateInfo?> GetLatestVersionAsync();
        Task<bool> DownloadUpdateAsync(UpdateInfo updateInfo, IProgress<double>? progress = null);
        Task<bool> InstallUpdateAsync(string packagePath);
        string GetCurrentVersion();
        Task<bool> IsUpdateAvailableAsync();
        void ScheduleUpdateCheck(TimeSpan interval);
    }

    public class UpdateService : IUpdateService
    {
        private const string UpdateCheckUrl = "https://api.github.com/repos/yourorg/hdd-indexer/releases/latest";
        private const string AppName = "HDDIndexer";

        private readonly HttpClient _httpClient;
        private readonly INotificationService _notificationService;
        private readonly ISettingsService _settingsService;
        private System.Threading.Timer? _updateTimer;

        public UpdateService(
            INotificationService notificationService,
            ISettingsService settingsService)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"{AppName}/{GetCurrentVersion()}");
            _notificationService = notificationService;
            _settingsService = settingsService;
        }

        public async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                var updateInfo = await GetLatestVersionAsync();
                if (updateInfo == null) return false;

                var currentVersion = Version.Parse(GetCurrentVersion());
                var latestVersion = Version.Parse(updateInfo.Version);

                if (latestVersion > currentVersion)
                {
                    await ShowUpdateNotificationAsync(updateInfo);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
                return false;
            }
        }

        public async Task<UpdateInfo?> GetLatestVersionAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(UpdateCheckUrl);
                var releaseInfo = JsonSerializer.Deserialize<GitHubRelease>(response);

                if (releaseInfo?.TagName == null) return null;

                // Find the appropriate asset for the current platform
                string assetName = "HDDIndexer.msix"; // Default to MSIX package
                var asset = Array.Find(releaseInfo.Assets, a => a.Name.EndsWith(".msix"));

                return new UpdateInfo
                {
                    Version = releaseInfo.TagName.TrimStart('v'),
                    ReleaseNotes = releaseInfo.Body ?? "",
                    DownloadUrl = asset?.BrowserDownloadUrl ?? "",
                    PublishedAt = releaseInfo.PublishedAt,
                    IsPrerelease = releaseInfo.Prerelease
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get latest version: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DownloadUpdateAsync(UpdateInfo updateInfo, IProgress<double>? progress = null)
        {
            try
            {
                if (string.IsNullOrEmpty(updateInfo.DownloadUrl))
                    return false;

                var tempPath = Path.Combine(Path.GetTempPath(), $"HDDIndexer_Update_{updateInfo.Version}.msix");

                using var response = await _httpClient.GetAsync(updateInfo.DownloadUrl);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var downloadedBytes = 0L;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = File.Create(tempPath);

                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    downloadedBytes += bytesRead;

                    if (totalBytes > 0)
                    {
                        var progressPercent = (double)downloadedBytes / totalBytes * 100;
                        progress?.Report(progressPercent);
                    }
                }

                return File.Exists(tempPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Download failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> InstallUpdateAsync(string packagePath)
        {
            try
            {
                if (!File.Exists(packagePath))
                    return false;

                // For MSIX packages, we can use the Windows Package Manager
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Add-AppxPackage -Path '{packagePath}'\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                if (process == null) return false;

                await process.WaitForExitAsync();

                // Clean up downloaded file
                try
                {
                    File.Delete(packagePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Installation failed: {ex.Message}");
                return false;
            }
        }

        public string GetCurrentVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString(3) ?? "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }

        public async Task<bool> IsUpdateAvailableAsync()
        {
            var updateInfo = await GetLatestVersionAsync();
            if (updateInfo == null) return false;

            var currentVersion = Version.Parse(GetCurrentVersion());
            var latestVersion = Version.Parse(updateInfo.Version);

            return latestVersion > currentVersion;
        }

        public void ScheduleUpdateCheck(TimeSpan interval)
        {
            _updateTimer?.Dispose();
            _updateTimer = new System.Threading.Timer(
                async _ => await CheckForUpdatesAsync(),
                null,
                TimeSpan.Zero,
                interval
            );
        }

        private async Task ShowUpdateNotificationAsync(UpdateInfo updateInfo)
        {
            var dialog = new ContentDialog
            {
                Title = "Update Available",
                Content = CreateUpdateContent(updateInfo),
                PrimaryButtonText = "Download & Install",
                SecondaryButtonText = "Later",
                CloseButtonText = "Skip This Version",
                XamlRoot = App.Current.m_window?.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await DownloadAndInstallUpdateAsync(updateInfo);
            }
        }

        private StackPanel CreateUpdateContent(UpdateInfo updateInfo)
        {
            var content = new StackPanel { Spacing = 10 };

            content.Children.Add(new TextBlock
            {
                Text = $"Version {updateInfo.Version} is now available!",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });

            if (!string.IsNullOrEmpty(updateInfo.ReleaseNotes))
            {
                content.Children.Add(new TextBlock
                {
                    Text = "What's New:",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Microsoft.UI.Xaml.Thickness(0, 10, 0, 5)
                });

                content.Children.Add(new ScrollViewer
                {
                    Content = new TextBlock
                    {
                        Text = updateInfo.ReleaseNotes,
                        TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
                    },
                    MaxHeight = 200
                });
            }

            return content;
        }

        private async Task DownloadAndInstallUpdateAsync(UpdateInfo updateInfo)
        {
            try
            {
                await _notificationService.ShowProgressNotificationAsync(
                    "Downloading Update",
                    $"Downloading version {updateInfo.Version}...",
                    0
                );

                var progress = new Progress<double>(p =>
                {
                    _ = _notificationService.ShowProgressNotificationAsync(
                        "Downloading Update",
                        $"Downloading version {updateInfo.Version}...",
                        (int)p
                    );
                });

                var downloadSuccess = await DownloadUpdateAsync(updateInfo, progress);

                if (downloadSuccess)
                {
                    await _notificationService.ShowProgressNotificationAsync(
                        "Installing Update",
                        "Installing the new version...",
                        100
                    );

                    var packagePath = Path.Combine(Path.GetTempPath(), $"HDDIndexer_Update_{updateInfo.Version}.msix");
                    var installSuccess = await InstallUpdateAsync(packagePath);

                    if (installSuccess)
                    {
                        await _notificationService.ShowScanCompleteNotificationAsync(
                            "Update Complete",
                            0 // Using this method for its notification template
                        );
                    }
                    else
                    {
                        await _notificationService.ShowErrorNotificationAsync(
                            "Update Failed",
                            "Failed to install the update. Please try again later."
                        );
                    }
                }
                else
                {
                    await _notificationService.ShowErrorNotificationAsync(
                        "Download Failed",
                        "Failed to download the update. Please check your internet connection."
                    );
                }
            }
            catch (Exception ex)
            {
                await _notificationService.ShowErrorNotificationAsync(
                    "Update Error",
                    $"An error occurred during the update: {ex.Message}"
                );
            }
        }

        public void Dispose()
        {
            _updateTimer?.Dispose();
            _httpClient?.Dispose();
        }
    }

    public class UpdateInfo
    {
        public string Version { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public DateTime PublishedAt { get; set; }
        public bool IsPrerelease { get; set; }
    }

    // GitHub API response models
    internal class GitHubRelease
    {
        public string? TagName { get; set; }
        public string? Body { get; set; }
        public DateTime PublishedAt { get; set; }
        public bool Prerelease { get; set; }
        public GitHubAsset[] Assets { get; set; } = Array.Empty<GitHubAsset>();
    }

    internal class GitHubAsset
    {
        public string Name { get; set; } = "";
        public string BrowserDownloadUrl { get; set; } = "";
    }
}