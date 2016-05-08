using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OCOverlay {
    public class WindowResizer : ILockable {
        private Window activeWin;
        private ILockable lockable;

        private HwndSource hwndSource;

        public ResizeDirection ResizeDirection { get; private set; }

        public bool isHorizontal {
            get { return ResizeDirection != ResizeDirection.None && ResizeDirection != ResizeDirection.Top && ResizeDirection != ResizeDirection.Bottom; }
        }

        public bool isVertical {
            get { return ResizeDirection != ResizeDirection.None && (ResizeDirection == ResizeDirection.Top || ResizeDirection == ResizeDirection.Bottom); }
        }

        public bool Active {
            get { return activeWin.WindowState != WindowState.Normal || ResizeDirection != ResizeDirection.None; }
        }

        public bool IsLocked {
            get {
                return lockable != null && lockable.IsLocked;
            }
        }

        public WindowResizer(Window win) {
            activeWin = win;
            activeWin.SourceInitialized += new EventHandler(InitializeWindowSource);
            if (win is ILockable) lockable = win as ILockable;
        }
        
        private void InitializeWindowSource(object sender, EventArgs e) {
            hwndSource = PresentationSource.FromVisual((Visual)sender) as HwndSource;
            InitializeSource(activeWin);
        }

        private void InitializeSource(Window win) {
            win.MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e) {
                if (e.LeftButton == MouseButtonState.Released) resetCursor();
            };
            var values = Enum.GetNames(typeof(ResizeDirection));
            foreach (String direction in values) {
                Rectangle rect = win.FindName("rect" + direction) as Rectangle;
                if (rect != null) {
                    rect.MouseLeftButtonDown += resizeWindow;
                    rect.MouseLeave += delegate(object sender, MouseEventArgs e) {
                        if (e.LeftButton == MouseButtonState.Released) resetCursor();
                    };
                    rect.MouseEnter += displayResizeCursor;
                }
            }
        }

        private void resetCursor() {
            if (Mouse.LeftButton != MouseButtonState.Pressed) {
                activeWin.Cursor = Cursors.Arrow;
                ResizeDirection = ResizeDirection.None;
            }
        }

        private void resizeWindow(object sender, MouseButtonEventArgs e) {
            if (Active || IsLocked) return;
            Rectangle rect = sender as Rectangle;
            ResizeDirection = getDirection(rect.Name);
            if (ResizeDirection != ResizeDirection.None) {
                activeWin.Cursor = getCursor(rect.Name);
                WindowsInterop.ResizeWindow(hwndSource, ResizeDirection);
            }
        }

        private void displayResizeCursor(object sender, MouseEventArgs e) {
            if (Active || IsLocked) return;
            activeWin.Cursor = getCursor((sender as Rectangle).Name);
        }

        private Cursor getCursor(String name) {
            switch (name) {
                case "rectTop":
                case "rectBottom": return Cursors.SizeNS;
                case "rectLeft":
                case "rectRight": return Cursors.SizeWE;
                case "rectTopLeft":
                case "rectBottomRight": return Cursors.SizeNWSE;
                case "rectTopRight":
                case "rectBottomLeft": return Cursors.SizeNESW;
                default: return Cursors.Arrow;
            }
        }

        private ResizeDirection getDirection(String name) {
            switch (name) {
                case "rectTop": return ResizeDirection.Top;
                case "rectBottom": return ResizeDirection.Bottom;
                case "rectLeft": return ResizeDirection.Left;
                case "rectRight": return ResizeDirection.Right;
                case "rectTopLeft": return ResizeDirection.TopLeft;
                case "rectTopRight": return ResizeDirection.TopRight;
                case "rectBottomLeft": return ResizeDirection.BottomLeft;
                case "rectBottomRight": return ResizeDirection.BottomRight;
                default: return ResizeDirection.None;
            }
        }
    }

    public interface ILockable {
        bool IsLocked {
            get;
        }
    }
}
