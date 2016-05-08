using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Cb = System.Windows.Media;

namespace OCOverlay {
    public static class ButtonUtils {
        internal const int TEX_SIZE = 44;
        internal const int MAX_COLUMNS = 7;

        private static Cb.Brush savedBackground;

        public static void setEditBoxes(Button e) { setBoxesGen(5, e); }
        public static void setPinBoxes(Button p) { setBoxesGen(3, p); }
        public static void setMinimizeBoxes(Button m) { setBoxesGen(0, m); }
        public static void setRestoreBoxes(Button r) { setBoxesGen(1, r); }

        public static void setBoxesGen(int left, Button m) { setBoxesGen(left, 0, m); }
        public static void setBoxesGen(int left, int top, Button m) {
            while (left >= MAX_COLUMNS) {
                top += 2;
                left -= MAX_COLUMNS;
            }

            m.setImageRect(new Int32Rect(TEX_SIZE * left, TEX_SIZE * top, TEX_SIZE, TEX_SIZE));
        }

        public static void ShiftVert(this Button m, int top) {
            CroppedBitmap b = (CroppedBitmap)m.brushFromButton().Source;
            if (b != null) {
                int oldTop = (int)b.SourceRect.Y / TEX_SIZE;
                if (oldTop % 2 != top % 2) {
                    if (oldTop % 2 == 1) {
                        top = oldTop - 1;
                    } else {
                        top = oldTop + 1;
                    }
                }

                m.setImageRect(new Int32Rect(b.SourceRect.X, top * TEX_SIZE, b.SourceRect.Width, b.SourceRect.Height));
            }
        }

        public static void setImageRect(this Button obj, Int32Rect rect) {
            Image brush = obj.brushFromButton();
            if (brush != null) {
                brush.Source = new CroppedBitmap(((CroppedBitmap)brush.Source).Source, rect);
            }
        }

        public static Image brushFromButton(this Button obj) {
            return ((Image)obj.getElementFromButton("box"));
        }

        public static object getElementFromButton(this Button obj, string name) {
            return obj.Template.FindName(name, obj);
        }

        public static void repaintButton(this Button sender) {
            if (sender.IsMouseOver) {
                sender.buttonLostFocus();
                sender.buttonGotFocus();
            }
        }

        public static void buttonGotFocus(this Button sender) {
            Grid g = ((Grid)ButtonUtils.getElementFromButton(sender, "backColor"));
            savedBackground = g.Background;
            g.Background = ((String)sender.Tag) == "close" ? Cb.Brushes.DarkRed : ((String)sender.Tag) == "open" ? Cb.Brushes.DarkGreen : Cb.Brushes.DarkBlue;
        }

        public static void buttonLostFocus(this Button sender) {
            ((Grid)ButtonUtils.getElementFromButton(sender, "backColor")).Background = savedBackground;
        }
    }

    public static class WindowUtils {
        /// <summary>
        /// Configures title buttons and registeres events
        /// </summary>
        public static void RegisterWindow(this Window context) {
            Button r = (Button)context.FindName("restore");
            if (r != null) {
                r.Click += delegate { context.RestoreButtonClicked(r); };
                r.MouseEnter += delegate { r.buttonGotFocus(); };
                r.MouseLeave += delegate { r.buttonLostFocus(); };
                ButtonUtils.setRestoreBoxes(r);

                context.StateChanged += delegate { context.WindowStateChanged(r); };
            }

            Button m = (Button)context.FindName("minimize");
            if (m != null) {
                m.Click += delegate { context.WindowMinimized(m); };
                m.MouseEnter += delegate { m.buttonGotFocus(); };
                m.MouseLeave += delegate { m.buttonLostFocus(); };
                ButtonUtils.setMinimizeBoxes(m);
            }

            Button c = (Button)context.FindName("close");
            if (c != null) {
                c.Click += delegate { context.Close(); if (context.Owner != null) context.Owner.Focus(); };
                c.MouseEnter += delegate { c.buttonGotFocus(); };
                c.MouseLeave += delegate { c.buttonLostFocus(); };
                c.Tag = "close";
            }

            Canvas t = (Canvas)context.FindName("title");
            if (t != null) {
                context.MouseMove += delegate(object sender, MouseEventArgs e) {
                    if ((context is ExtendedWindow) && ((ExtendedWindow)context).isLocked()) return;
                    if (e.LeftButton == MouseButtonState.Pressed) {
                        double left = e.GetPosition(null).X;
                        double top = e.GetPosition(null).Y;
                        if (e.GetPosition(context).Y < t.Height) {
                            if (context.WindowState == WindowState.Maximized) {
                                context.WindowState = WindowState.Normal;
                                context.Left = left - context.Width / 2;
                                context.Top = top - 15;
                            }
                            context.DragMove();
                        }
                    }
                };

                if (context.ResizeMode != ResizeMode.NoResize && context.ResizeMode != ResizeMode.CanMinimize) {
                    context.MouseDoubleClick += delegate(object sender, MouseButtonEventArgs e) {
                        if (context.WindowState == WindowState.Maximized) {
                            if (System.Windows.Forms.Cursor.Position.Y < t.Height) {
                                context.WindowState = WindowState.Normal;
                            }
                        } else {
                            if (System.Windows.Forms.Cursor.Position.Y < context.Top + t.Height) {
                                context.WindowState = WindowState.Maximized;
                            }
                        }
                    };
                }

                
                t.MouseLeftButtonDown += delegate {context.DragMove();};
            }

            Label tL = (Label)context.FindName("title2");
            if (tL != null) {
                tL.MouseLeftButtonDown += delegate {context.DragMove();};
            }
        }

        public static void WindowMinimized(this Window context, Button minimizeButton) {
            minimizeButton.buttonLostFocus();
            context.WindowState = WindowState.Minimized;
        }

        public static void WindowStateChanged(this Window context, Button restoreButton) {
            restoreButton.ShiftVert(context.WindowState == WindowState.Normal ? 0 : 1);
            Canvas t = (Canvas)context.FindName("title");
            if (t != null) {
                Thickness margin = t.Margin;
                margin.Top = context.WindowState == WindowState.Maximized ? 5 : 0;
                t.Margin = margin;
            }
        }

        public static void RestoreButtonClicked(this Window context, Button sender) {
            context.WindowState = context.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        }

        public interface ExtendedWindow {
            bool isLocked();
        }
    }
}