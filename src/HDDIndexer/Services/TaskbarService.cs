using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using WinRT;

namespace HDDIndexer.Services
{
    public interface ITaskbarService
    {
        void SetProgress(double progress);
        void SetProgressState(TaskbarProgressState state);
        void ShowBadge(int count);
        void ClearBadge();
        void FlashWindow();
        void SetOverlayIcon(string iconPath, string description);
        void ClearOverlayIcon();
    }

    public class TaskbarService : ITaskbarService
    {
        private IntPtr _windowHandle;
        private ITaskbarList4? _taskbarList;

        public TaskbarService()
        {
            InitializeTaskbar();
        }

        private void InitializeTaskbar()
        {
            try
            {
                if (App.Current?.m_window != null)
                {
                    _windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.m_window);

                    // Create TaskbarList COM object
                    var taskbarListType = Type.GetTypeFromCLSID(new Guid("56FDF344-FD6D-11d0-958A-006097C9A090"));
                    if (taskbarListType != null)
                    {
                        _taskbarList = Activator.CreateInstance(taskbarListType) as ITaskbarList4;
                        _taskbarList?.HrInit();
                    }
                }
            }
            catch
            {
                // Taskbar integration not available
                _taskbarList = null;
            }
        }

        public void SetProgress(double progress)
        {
            if (_taskbarList == null || _windowHandle == IntPtr.Zero) return;

            try
            {
                var progressValue = (ulong)(progress * 100);
                _taskbarList.SetProgressValue(_windowHandle, progressValue, 100);
            }
            catch
            {
                // Ignore errors
            }
        }

        public void SetProgressState(TaskbarProgressState state)
        {
            if (_taskbarList == null || _windowHandle == IntPtr.Zero) return;

            try
            {
                _taskbarList.SetProgressState(_windowHandle, (TBPFLAG)state);
            }
            catch
            {
                // Ignore errors
            }
        }

        public void ShowBadge(int count)
        {
            if (_taskbarList == null || _windowHandle == IntPtr.Zero) return;

            try
            {
                // Create a simple overlay icon with count
                // This would typically use an icon resource
                var iconHandle = CreateCountIcon(count);
                if (iconHandle != IntPtr.Zero)
                {
                    _taskbarList.SetOverlayIcon(_windowHandle, iconHandle, $"{count} items");
                    DestroyIcon(iconHandle);
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        public void ClearBadge()
        {
            ClearOverlayIcon();
        }

        public void FlashWindow()
        {
            if (_windowHandle == IntPtr.Zero) return;

            try
            {
                var flashInfo = new FLASHWINFO
                {
                    cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
                    hwnd = _windowHandle,
                    dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
                    uCount = 3,
                    dwTimeout = 0
                };

                FlashWindowEx(ref flashInfo);
            }
            catch
            {
                // Ignore errors
            }
        }

        public void SetOverlayIcon(string iconPath, string description)
        {
            if (_taskbarList == null || _windowHandle == IntPtr.Zero) return;

            try
            {
                var iconHandle = LoadIcon(iconPath);
                if (iconHandle != IntPtr.Zero)
                {
                    _taskbarList.SetOverlayIcon(_windowHandle, iconHandle, description);
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        public void ClearOverlayIcon()
        {
            if (_taskbarList == null || _windowHandle == IntPtr.Zero) return;

            try
            {
                _taskbarList.SetOverlayIcon(_windowHandle, IntPtr.Zero, null);
            }
            catch
            {
                // Ignore errors
            }
        }

        private IntPtr CreateCountIcon(int count)
        {
            // This is a simplified implementation
            // In a real app, you'd create an actual icon with the count drawn on it
            return IntPtr.Zero;
        }

        private IntPtr LoadIcon(string iconPath)
        {
            // Load icon from path
            return LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 16, 16, LR_LOADFROMFILE);
        }

        #region Win32 Interop

        [ComImport]
        [Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITaskbarList4
        {
            void HrInit();
            void AddTab(IntPtr hwnd);
            void DeleteTab(IntPtr hwnd);
            void ActivateTab(IntPtr hwnd);
            void SetActiveAlt(IntPtr hwnd);
            void MarkFullscreenWindow(IntPtr hwnd, bool fFullscreen);
            void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
            void SetProgressState(IntPtr hwnd, TBPFLAG tbpFlags);
            void RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);
            void UnregisterTab(IntPtr hwndTab);
            void SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);
            void SetTabActive(IntPtr hwndTab, IntPtr hwndMDI, uint dwReserved);
            void ThumbBarAddButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray)] THUMBBUTTON[] pButton);
            void ThumbBarUpdateButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray)] THUMBBUTTON[] pButton);
            void ThumbBarSetImageList(IntPtr hwnd, IntPtr himl);
            void SetOverlayIcon(IntPtr hwnd, IntPtr hIcon, [MarshalAs(UnmanagedType.LPWStr)] string? pszDescription);
            void SetThumbnailTooltip(IntPtr hwnd, [MarshalAs(UnmanagedType.LPWStr)] string pszTip);
            void SetThumbnailClip(IntPtr hwnd, ref RECT prcClip);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct THUMBBUTTON
        {
            public uint dwMask;
            public uint iId;
            public uint iBitmap;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szTip;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        private enum TBPFLAG
        {
            TBPF_NOPROGRESS = 0x00000000,
            TBPF_INDETERMINATE = 0x00000001,
            TBPF_NORMAL = 0x00000002,
            TBPF_ERROR = 0x00000004,
            TBPF_PAUSED = 0x00000008
        }

        private const uint FLASHW_STOP = 0;
        private const uint FLASHW_CAPTION = 1;
        private const uint FLASHW_TRAY = 2;
        private const uint FLASHW_ALL = 3;
        private const uint FLASHW_TIMER = 4;
        private const uint FLASHW_TIMERNOFG = 12;

        private const uint IMAGE_ICON = 1;
        private const uint LR_LOADFROMFILE = 0x00000010;

        [DllImport("user32.dll")]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadImage(
            IntPtr hinst,
            string lpszName,
            uint uType,
            int cxDesired,
            int cyDesired,
            uint fuLoad);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        #endregion
    }

    public enum TaskbarProgressState
    {
        None = 0,
        Indeterminate = 1,
        Normal = 2,
        Error = 4,
        Paused = 8
    }
}