using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TsudaKageyu;

namespace OCOverlay {
    public static class Utils {
        /// <summary>
        /// Retrieves the icon associated with a file type
        /// </summary>
        /// <param name="path">location of file</param>
        /// <returns>ImageSource containing the icon</returns>
        internal static ImageSource IconforExtension(string path) {
            return IconUtils.SourceForIcon(Icon.ExtractAssociatedIcon(path));
        }

        /// <summary>
        /// Retrieves an icon from an executable or folder.
        /// Always attempts to retrieve the highest resolution available
        /// </summary>
        /// <param name="path">location of file</param>
        /// <param name="dir">when true the path will be treated as a directory other wise as a executable</param>
        /// <returns>ImageSource containing the icon</returns>
        internal static ImageSource IconFromExeDir(string path, string extra, bool dir) {
            int iconIndex = 0;
            Int32.TryParse(extra, out iconIndex);
            Icon icon = null;
            Icon[] theIcons = null;
            try {
                IconExtractor ext = new IconExtractor(path);
                icon = ext.GetIcon(iconIndex);
                theIcons = IconUtil.Split(icon);
                icon.Dispose();
            } catch (Exception) {
            }

            if (theIcons != null) {
                int selected = -1;
                double max = 0;
                for (int i = 0; i < theIcons.Count(); i++ ) {
                    if (theIcons[i].Height > max) {
                        selected = i;
                        max = theIcons[i].Height;
                        icon = theIcons[i];
                    }
                }
                for (int i = 0; i < theIcons.Count(); i++) {
                    if (i != selected) theIcons[i].Dispose();
                }
            }

            if (icon != null) {
                return IconUtils.SourceForIcon(icon);
            }

            return IconUtils.getIconLarge(path, true, dir, true);
        }

        /// <summary>
        /// Saves a single blink frame to a .pon file or an animated gif.
        /// </summary>
        /// <param name="w">context</param>
        /// <param name="item">blink item</param>
        internal static void SaveBlinkItem(Window w, BlinkFrame item) {
            SaveFileDialog save = new SaveFileDialog();
            save.Title = "Save Blink Frame";
            save.Filter = "Pony files (*.pon)|*.pon|Compuserve GIF (*.gif)|*.gif|All files (*.*)|*.*";
            save.DefaultExt = "*.pon";
            save.FileName = item.Name;
            bool? result = save.ShowDialog(w);
            if (result != null && result == true) {
                DOTPon.Save(save.FileName, item);
            }
        }

        /// <summary>
        /// Saves multiple blink frames to a .pon file.
        /// </summary>
        /// <param name="w">context</param>
        /// <param name="item">array of blink items</param>
        internal static void SaveBlinkItems(Window w, BlinkFrame[] item) {
            SaveFileDialog save = new SaveFileDialog();
            save.Title = "Save Blink Frame";
            save.Filter = "Pony files (*.pon)|*.pon|All files (*.*)|*.*";
            save.DefaultExt = "*.pon";
            save.FileName = "Profile";
            bool? result = save.ShowDialog(w);
            if (result != null && result == true) {
                DOTPon.Save(save.FileName, item);
            }
        }

        internal static bool isVideoFile(string path) {
            string mime = System.Web.MimeMapping.GetMimeMapping(path);
            return mime.Split('/')[0].ToLower() == "video" || VIDEO_FORMATS.Contains(Path.GetExtension(path).ToLower());
        }

        internal static bool isAudioFile(string path) {
            string mime = System.Web.MimeMapping.GetMimeMapping(path);
            return mime.Split('/')[0].ToLower() == "audio";
        }

        private static readonly string[] VIDEO_FORMATS = {
            ".mp4", ".wav", ".wmv", ".webm", ".avi"
        };
    }

    public static class WindowsInterop {

        #region Constants

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_SHOWWINDOW = 0x0040;
        
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int GWL_EXSTYLE = (-20);

        public const int WM_WINDOWPOSCHANGING = 0x0046;
        public const int WM_WINDOWPOSCHANGED = 0x0047;
        public const int WM_SYSCOMMAND = 0x112;

        #endregion
        #region Imports

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
        [DllImport("user32.dll")]
        internal static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        [DllImport("user32.dll")]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        internal static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")]
        internal static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hwnd, uint Msg, IntPtr wParam, IntPtr lParam);
        #endregion

        /// <summary>
        /// Activate a window from anywhere by attaching to the foreground window
        /// </summary>
        public static void GlobalActivate(this Window w) {
            var interopHelper = new WindowInteropHelper(w);
            var thisWindowThreadId = GetWindowThreadProcessId(interopHelper.Handle, IntPtr.Zero);
            var currentForegroundWindow = GetForegroundWindow();
            var currentForegroundWindowThreadId = GetWindowThreadProcessId(currentForegroundWindow, IntPtr.Zero);
            
            AttachThreadInput(currentForegroundWindowThreadId, thisWindowThreadId, true);
            SetWindowPos(interopHelper.Handle, new IntPtr(0), 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW);
            AttachThreadInput(currentForegroundWindowThreadId, thisWindowThreadId, false);

            if (w.WindowState == WindowState.Minimized) w.WindowState = WindowState.Normal;
            w.Show();
            w.Activate();
        }

        public static void ToTransparentWindow(this Window x) {
            x.SourceInitialized += delegate { x.SetWindowTransparent(); };
        }

        public static int SetWindowTransparent(this Window x) {
            IntPtr hwnd = new WindowInteropHelper(x).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            return extendedStyle;
        }

        public static void SetWindowOpaque(this Window x, int extendedStyle) {
            IntPtr hwnd = new WindowInteropHelper(x).Handle;
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle);
        }

        public static System.Windows.Forms.Screen GetScreen(this Window window) {
            return System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(window).Handle);
        }

        public static Visual getRootVisual(IntPtr hwnd) {
            return (Window)HwndSource.FromHwnd(hwnd).RootVisual;
        }

        internal static WINDOWPOS readWindowPos(IntPtr lParam) {
            return (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));
        }

        internal static void writeWindowPos(this WINDOWPOS pos, IntPtr lParam) {
            Marshal.StructureToPtr(pos, lParam, true);
        }

        public static void ResizeWindow(this HwndSource hwndSource, ResizeDirection direction) {
            SendMessage(hwndSource.Handle, WM_SYSCOMMAND, (IntPtr)(61440 + direction), IntPtr.Zero);
        }
    }

    public enum ResizeDirection {
        Left = 1,
        Right = 2,
        Top = 3,
        TopLeft = 4,
        TopRight = 5,
        Bottom = 6,
        BottomLeft = 7,
        BottomRight = 8,
        None = 9
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WINDOWPOS {
        public IntPtr hwnd, hwndInsertAfter;
        public int x, y, cx, cy, flags;
    }
}
