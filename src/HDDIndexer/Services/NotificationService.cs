using System;
using System.Threading.Tasks;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using Windows.ApplicationModel.Core;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Microsoft.UI.Xaml;

namespace HDDIndexer.Services
{
    public interface INotificationService
    {
        Task ShowScanCompleteNotificationAsync(string driveName, int filesScanned);
        Task ShowBackupCompleteNotificationAsync(string backupPath);
        Task ShowErrorNotificationAsync(string title, string message);
        Task ShowProgressNotificationAsync(string title, string message, int progress);
        void ClearAllNotifications();
        void RegisterNotificationHandlers();
    }

    public class NotificationService : INotificationService
    {
        private const string AppId = "HDDIndexer";
        private ToastNotifier? _notifier;

        public NotificationService()
        {
            try
            {
                _notifier = ToastNotificationManager.CreateToastNotifier(AppId);
            }
            catch
            {
                // Fallback if toast notifications aren't available
                _notifier = null;
            }
        }

        public async Task ShowScanCompleteNotificationAsync(string driveName, int filesScanned)
        {
            var toastXml = CreateToastXml(
                "Scan Complete",
                $"Successfully scanned {filesScanned:N0} files on {driveName}",
                "ms-appx:///Assets/Square44x44Logo.png"
            );

            await ShowToastAsync(toastXml);
        }

        public async Task ShowBackupCompleteNotificationAsync(string backupPath)
        {
            var toastXml = CreateToastXml(
                "Backup Complete",
                $"Database backup saved to {System.IO.Path.GetFileName(backupPath)}",
                "ms-appx:///Assets/Square44x44Logo.png"
            );

            await ShowToastAsync(toastXml);
        }

        public async Task ShowErrorNotificationAsync(string title, string message)
        {
            var toastXml = CreateToastXml(
                title,
                message,
                "ms-appx:///Assets/Square44x44Logo.png",
                ToastTemplateType.ToastImageAndText02
            );

            await ShowToastAsync(toastXml);
        }

        public async Task ShowProgressNotificationAsync(string title, string message, int progress)
        {
            // Create progress notification template
            var toastContent = $@"
                <toast>
                    <visual>
                        <binding template='ToastGeneric'>
                            <image placement='appLogoOverride' src='ms-appx:///Assets/Square44x44Logo.png'/>
                            <text>{title}</text>
                            <text>{message}</text>
                            <progress value='{progress / 100.0}' status='{progress}%'/>
                        </binding>
                    </visual>
                </toast>";

            var toastXml = new XmlDocument();
            toastXml.LoadXml(toastContent);

            await ShowToastAsync(toastXml);
        }

        public void ClearAllNotifications()
        {
            try
            {
                ToastNotificationManager.History.Clear(AppId);
            }
            catch
            {
                // Ignore if history isn't available
            }
        }

        public void RegisterNotificationHandlers()
        {
            // Register for notification activation if the app is closed
            try
            {
                CoreApplication.MainView.CoreWindow.Activated += (sender, args) =>
                {
                    if (args.WindowActivationState != CoreWindowActivationState.Deactivated)
                    {
                        // Handle notification activation
                        HandleNotificationActivation();
                    }
                };
            }
            catch
            {
                // Ignore if not available
            }
        }

        private XmlDocument CreateToastXml(
            string title,
            string message,
            string imagePath = "",
            ToastTemplateType template = ToastTemplateType.ToastImageAndText02)
        {
            var toastTemplate = ToastNotificationManager.GetTemplateContent(template);

            // Set title
            var titleElements = toastTemplate.GetElementsByTagName("text");
            if (titleElements.Count > 0)
            {
                titleElements[0].AppendChild(toastTemplate.CreateTextNode(title));
            }

            // Set message
            if (titleElements.Count > 1)
            {
                titleElements[1].AppendChild(toastTemplate.CreateTextNode(message));
            }

            // Set image if provided
            if (!string.IsNullOrEmpty(imagePath))
            {
                var imageElements = toastTemplate.GetElementsByTagName("image");
                if (imageElements.Count > 0)
                {
                    imageElements[0].Attributes?.GetNamedItem("src")?.SetValue(imagePath);
                }
            }

            // Add launch parameter for activation handling
            var toastElement = toastTemplate.SelectSingleNode("/toast");
            if (toastElement is XmlElement element)
            {
                element.SetAttribute("launch", "fromNotification=true");
            }

            return toastTemplate;
        }

        private async Task ShowToastAsync(XmlDocument toastXml)
        {
            if (_notifier == null) return;

            try
            {
                var toast = new ToastNotification(toastXml);

                // Set expiration time
                toast.ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(5);

                // Show notification
                await Task.Run(() => _notifier.Show(toast));
            }
            catch
            {
                // Ignore notification failures
            }
        }

        private void HandleNotificationActivation()
        {
            // Bring app to foreground when notification is clicked
            var window = App.Current.m_window;
            if (window != null)
            {
                // Activate the window
                window.Activate();

                // Bring to front
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                Microsoft.UI.Win32Interop.SetForegroundWindow(hwnd);
            }
        }
    }
}

// Extension methods for easier notification usage
public static class NotificationExtensions
{
    public static async Task NotifyScanCompleteAsync(this INotificationService service, string driveName, int filesScanned)
    {
        await service.ShowScanCompleteNotificationAsync(driveName, filesScanned);
    }

    public static async Task NotifyBackupCompleteAsync(this INotificationService service, string backupPath)
    {
        await service.ShowBackupCompleteNotificationAsync(backupPath);
    }

    public static async Task NotifyErrorAsync(this INotificationService service, string title, string message)
    {
        await service.ShowErrorNotificationAsync(title, message);
    }
}