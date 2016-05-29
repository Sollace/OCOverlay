using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;

namespace OCOverlay {
    public partial class MainWindow : Window, WindowUtils.ExtendedWindow, ILockable {
        public static GifSource Animator;
        public static BlinkManager BlinkManager;

        private WindowResizer Resizer;

        private int snap = 5;

        private bool _flipX = false;
        public bool FlipX {
            get { return _flipX; }
            set { flipper_2.ScaleX = flipper.ScaleX = value ? -1 : 1;
                _flipX = value; }
        }

        private bool _flipY = false;
        public bool FlipY {
            get { return _flipY; }
            set { flipper_2.ScaleY = flipper.ScaleY = value ? -1 : 1;
                _flipY = value; }
        }

        public bool TransOnHover {get;set;}

        private int _resetTimeout = 0;
        private double _opacityDestination = 1;
        public double OpacityOffset {
            get {
                if (_resetTimeout == 0) {
                    return (pony_holder.Opacity - _opacityDestination) / 5;
                } else {
                    _resetTimeout = (_resetTimeout + 1) % 8;
                    return 0;
                }
            }
            set {
                _resetTimeout = 1;
                _opacityDestination = value;
            }
        }

        public double Rotate {
            get { return rotator.Angle; }
            set { rotator_2.Angle = rotator.Angle = value % 360; }
        }

        public double ImageOpacity {
            get { return pony.Opacity * 100; }
            set { gilda.Opacity = pony.Opacity = value / 100; }
        }

        public double Scale {
            get { return (scaler.ScaleX - 1) * 100; }
            set { scaler_2.ScaleY = scaler.ScaleY = scaler_2.ScaleX = scaler.ScaleX = 1 + (value / 100); }
        }

        public double Aspect {
            get { return pony.Source.Width / pony.Source.Height; }
        }

        private WindowState coverState = WindowState.Maximized;
        WindowState normalState = WindowState.Normal;
        bool _cover = false;
        public bool Cover {
            get {
                return _cover;
            }
            set {
                if (value != _cover) {
                    if (value) {
                        normalState = WindowState;
                        WindowState = coverState;
                        pony.Stretch = Stretch.UniformToFill;
                    } else {
                        coverState = WindowState;
                        WindowState = normalState;
                        pony.Stretch =  Stretch.Uniform;
                    }
                    _cover = value;
                }
            }
        }

        private bool _pinned = false;
        public bool Pinned {
            get { return _pinned; }
            set {
                if (value != _pinned) {
                    pin.ShiftVert(value ? 1 : 0);
                }
                Topmost = value;
                _pinned = value;
            }
        }

        private bool _islocked = false;
        public bool IsLocked {
            get { return _islocked; }
            set {
                if (value != _islocked) {
                    locked.ShiftVert(value ? 1 : 0);
                }
                _islocked = value;
            }
        }

        private bool _loop = false;
        public bool Loop {
            get { return _loop; }
            set {
                if (value != _loop) {
                    loop.ShiftVert(value ? 1 : 0);
                }
                _loop = value;
            }
        }

        private bool _playing = true;
        public bool isPlaying {
            get { return _playing; }
            private set {
                if (value != _playing) {
                    play.ShiftVert(value ? 0 : 1);
                    try {
                        if (value) {
                            gilda.Play();
                        } else {
                            gilda.Pause();
                        }
                    } catch (Exception a) {
                        throw a;
                    }
                }
                _playing = value;
            }
        }

        private bool _passthrough = false;
        public bool PassThrough {
            get { return _passthrough; }
            set {
                if (_passthrough != value) {
                    if (value) {
                        this.SetWindowTransparent();
                    } else {
                        this.SetWindowOpaque();
                    }
                    _passthrough = value;
                }
            }
        }

        private Visibility UIVisibility {
            set {
                title.Visibility = buttons.Visibility = edit.Visibility = value;
                if (gilda.Visibility == Visibility.Visible) {
                    playback.Visibility = value;
                } else {
                    playback.Visibility = gilda.Visibility;
                }
            }
            get { return title.Visibility; }
        }

        public MainWindow(string startupImage, bool autoAnimate) : this(startupImage) {
            if (autoAnimate) {
                BlinkManager.Start();
            }
        }

        public MainWindow(string startupImage) : this() {
            if (startupImage != null && startupImage != String.Empty) {
                loadFile(startupImage);
            }
        }

        public MainWindow() {
            InitializeComponent();
            Timeline.DesiredFrameRateProperty.OverrideMetadata(typeof(Timeline), new FrameworkPropertyMetadata { DefaultValue = 10 });
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            Animator = new GifSource(this, pony);
            BlinkManager = new BlinkManager(pony);
            Resizer = new WindowResizer(this);
        }

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            HwndSource source = HwndSource.FromVisual(this) as HwndSource;
            if (source != null) {
                source.AddHook(new HwndSourceHook(WinProc));
            }
        }

        private unsafe IntPtr WinProc(IntPtr hwnd, Int32 msg, IntPtr wParam, IntPtr lParam, ref Boolean handled) {
            switch (msg) {
                case WindowsInterop.WM_WINDOWPOSCHANGING:
                    if (WindowState == WindowState.Normal) {
                        WINDOWPOS pos = WindowsInterop.readWindowPos(lParam);
                        if ((pos.flags & WindowsInterop.SWP_NOSIZE) != 0 || WindowsInterop.getRootVisual(hwnd) == null) {
                            return IntPtr.Zero;
                        }
                        Rectangle bounds = this.GetScreen().Bounds;
                        Rectangle area = this.GetScreen().WorkingArea;

                        
                        if (pos.cx < bounds.Width || pos.cy < bounds.Height) {
                            if (Resizer.ResizeDirection == ResizeDirection.TopLeft || Resizer.ResizeDirection == ResizeDirection.TopRight) {
                                int oldY = pos.y + pos.cy;
                                pos.cy = (int)(pos.cx / Aspect);
                                if (pos.cy >= bounds.Height) {
                                    pos.cy = bounds.Height;
                                }
                                if (oldY >= area.Height) {
                                    oldY = area.Height;
                                }
                                pos.y = oldY - pos.cy;
                            } else if (Resizer.isVertical) {
                                pos.cx = (int)(pos.cy * Aspect);
                            } else if (Resizer.ResizeDirection != ResizeDirection.None) {
                                pos.cy = (int)(pos.cx / Aspect);
                            } else {
                                if (pos.cx == area.Width / 2) {
                                    if (pos.cy == area.Height / 2) {
                                        pos.cx = (int)(pos.cy * Aspect);
                                        if (pos.x == area.Width / 2) {
                                            pos.x = area.Width - pos.cx;
                                        }
                                    } else {
                                        int oldY = pos.cy;
                                        pos.cy = (int)(pos.cx / Aspect);
                                        if (pos.cy > area.Height) {
                                            pos.cy = oldY;
                                            pos.cx = (int)(pos.cy * Aspect);
                                            if (pos.x == area.Width / 2) {
                                                pos.x = area.Width - pos.cx;
                                            }
                                        } else {
                                            pos.y = (area.Height - pos.cy) / 2;
                                        }
                                    }
                                } else if (pos.cx / pos.cy != Aspect) {
                                    pos.cx = (int)(pos.cy * Aspect);
                                }
                            }
                        }
                        
                        if (pos.x > -snap && pos.x < snap) pos.x = 0;
                        if (pos.y > -snap && pos.y < snap) pos.y = 0;
                        
                        if (pos.x + pos.cx >= area.Width - snap && pos.x + pos.cx < area.Width + snap) {
                            if (pos.cx < bounds.Width - snap * 2) {
                                pos.x = area.Width - pos.cx;
                            } else {
                                pos.cx = bounds.Width - pos.x;
                            }
                        }
                        
                        if (pos.y + pos.cy >= area.Height - snap && pos.y + pos.cy < area.Height + snap) {
                            if (pos.cy < bounds.Height - snap * 2) {
                                pos.y = area.Height - pos.cy;
                            } else {
                                pos.cy = bounds.Height - pos.y;
                            }
                        }

                        if (pos.y + pos.cy >= bounds.Height - snap && pos.y + pos.cy < bounds.Height + snap) {
                            if (pos.cy >= bounds.Height - snap * 2) {
                                pos.cy = bounds.Height - pos.y;
                            }
                        }

                        pos.writeWindowPos(lParam);
                        handled = true;
                    }
                    break;
            }

            return IntPtr.Zero;
        }
        
        void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e) {
            WindowPropSyncroniser.enablePositionReset();
        }

        public void updateLocation() {
            setLocation(_sector, _savedX, _savedY);
        }

        double? _savedX = null;
        double? _savedY = null;
        PinSector _sector = PinSector.TOP_LEFT;
        public void setLocation(PinSector sector, double? x, double? y) {
            _savedX = x;
            _savedY = y;
            _sector = sector;
            if (x != null) {
                switch (sector) {
                    case PinSector.TOP_LEFT:
                    case PinSector.MIDDLE_LEFT:
                    case PinSector.BOTTOM_LEFT:
                        double max = this.GetScreen().Bounds.Width - Width;
                        Left = x.Value > max ? max : x.Value;
                        break;
                    case PinSector.TOP_MIDDLE:
                    case PinSector.MIDDLE_MIDDLE:
                    case PinSector.BOTTOM_MIDDLE:
                        Left = (this.GetScreen().Bounds.Width / 2) - (Width / 2);
                        break;
                    case PinSector.TOP_RIGHT:
                    case PinSector.MIDDLE_RIGHT:
                    case PinSector.BOTTOM_RIGHT:
                        double left = this.GetScreen().Bounds.Width - (x.Value + Width);
                        Left = left < 0 ? 0 : left;
                        break;
                }
            }
            if (y != null) {
                switch (sector) {
                    case PinSector.TOP_LEFT:
                    case PinSector.TOP_MIDDLE:
                    case PinSector.TOP_RIGHT:
                        double max = this.GetScreen().Bounds.Height - Height;
                        Top = y > max ? max : y.Value;
                        break;
                    case PinSector.MIDDLE_LEFT:
                    case PinSector.MIDDLE_MIDDLE:
                    case PinSector.MIDDLE_RIGHT:
                        Top = (this.GetScreen().Bounds.Height / 2) - (Height / 2);
                        break;
                    case PinSector.BOTTOM_LEFT:
                    case PinSector.BOTTOM_MIDDLE:
                    case PinSector.BOTTOM_RIGHT:
                        double top = this.GetScreen().Bounds.Height - (y.Value + Height);
                        Top = top < 0 ? 0 : top;
                        break;
                }
            }
        }

        public void updateSize() {
            if (WindowState == WindowState.Normal) {
                double oldWidth = Width;
                double oldHeight = Height;
                Rectangle bounds = this.GetScreen().Bounds;
                if (Width / Aspect > bounds.Height) {
                    Width = Height * Aspect;
                } else {
                    Height = Width / Aspect;
                }
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e) {
            if (WindowState == WindowState.Normal) {
                Canvas.SetTop(editBlinks, e.NewSize.Width < 150 ? -30 : 0);
                Canvas.SetTop(locked, e.NewSize.Width < 210 ? -30 : 0);
                Canvas.SetTop(pin, e.NewSize.Width < 175 ? -30 : 0);
                Canvas.SetTop(minimize, e.NewSize.Width < 105 ? -30 : 0);
                Canvas.SetTop(restore, e.NewSize.Width < 70 ? -30 : 0);
                Canvas.SetTop(close, e.NewSize.Width < 35 ? -30 : 0);
                return;
            }

            Canvas.SetTop(close, 0);
            Canvas.SetTop(restore, 0);
            Canvas.SetTop(minimize, 0);
            Canvas.SetTop(editBlinks, 0);
            Canvas.SetTop(locked, 0);
            Canvas.SetTop(pin, 0);
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e) {
            this.RegisterWindow();
            ButtonUtils.setPinBoxes(pin);
            ButtonUtils.setEditBoxes(editBlinks);
            ButtonUtils.setBoxesGen(7, locked);
            ButtonUtils.setBoxesGen(11, play);
            ButtonUtils.setBoxesGen(12, loop);
            WindowPropSyncroniser.Start(this);
        }

        private void Window_Drop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                loadFile(((string[])e.Data.GetData(DataFormats.FileDrop, true))[0]);
            }
        }

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if (UIVisibility != Visibility.Visible) {
                UIVisibility = Visibility.Visible;
            } else {
                UIVisibility = Visibility.Collapsed;
            }
        }

        private void Pony_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (!IsLocked && e.LeftButton == MouseButtonState.Pressed) {
                DragMove();
            }
        }

        private void Pony_MouseEnter(object sender, MouseEventArgs e) {
            if (TransOnHover) OpacityOffset = 0.3;
        }

        private void Pony_MouseLeave(object sender, MouseEventArgs e) {
            if (TransOnHover) OpacityOffset = 1;
        }

        private void locked_Click(object sender, RoutedEventArgs e) { IsLocked = !IsLocked; }

        public bool isLocked() {
            return IsLocked;
        }

        private void pin_Click(object sender, RoutedEventArgs e) { Pinned = !Pinned; }
        
        private void play_Click(object sender, RoutedEventArgs e) { isPlaying = !isPlaying; }

        private void loop_Click(object sender, RoutedEventArgs e) { Loop = !Loop; }

        private void buttonGotFocus(object sender, MouseEventArgs e) {
            ButtonUtils.buttonGotFocus((Button)sender);
        }

        private void buttonLostFocus(object sender, MouseEventArgs e) {
            ButtonUtils.buttonLostFocus((Button)sender);
        }

        private void edit_Click(object sender, RoutedEventArgs e) {
            if (OwnedWindows.Count == 0) {
                Options bli = new Options(this);
                bli.Owner = this;
                bli.Show();
            } else {
                OwnedWindows[0].Focus();
                OwnedWindows[0].WindowState = WindowState.Normal;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e) {
            if (WindowState == WindowState.Minimized) {
                BlinkManager.Stop();
            } else {
                BlinkManager.Continue();
            }
            
            if (!ShowInTaskbar) {
                if (WindowState == WindowState.Minimized) {
                    WindowState = WindowState.Normal;
                }
            }
        }

        internal void cleanup() {
            gilda.Stop();
            gilda.Visibility = Visibility.Hidden;
            playback.Visibility = gilda.Visibility;
            gilda.Source = null;
            pony.Visibility = Visibility.Visible;
        }

        internal void loadFile(string path) {
            path = Environment.ExpandEnvironmentVariables(path);
            Animator.Stop();
            BlinkManager.Stop();
            Cursor = Cursors.Wait;
            string extra = "";
            if (File.Exists(path)) {
                string ext = Path.GetExtension(path);
                if (ext == ".lnk") {
                    IWshRuntimeLibrary.IWshShell_Class wsh = new IWshRuntimeLibrary.IWshShell_Class();
                    IWshRuntimeLibrary.IWshShortcut sc = (IWshRuntimeLibrary.IWshShortcut)wsh.CreateShortcut(path);

                    string[] split = sc.IconLocation.Split(',');
                    string iconLocation = Environment.ExpandEnvironmentVariables(split[0]);
                    if (File.Exists(iconLocation) || Directory.Exists(iconLocation)) {
                        path = iconLocation;
                        if (split.Length > 1) extra = split[1];
                    } else if (File.Exists(sc.TargetPath) || Directory.Exists(sc.TargetPath)) {
                        path = sc.TargetPath;
                    }
                } else if (ext == ".url") {
                    using (StreamReader reader = new StreamReader(path)) {
                        string line;
                        while ((line = reader.ReadLine()) != null) {
                            if (line.StartsWith("IconFile=")) {
                                string iconLocation = Environment.ExpandEnvironmentVariables(line.Split('=')[1]);
                                string[] split = iconLocation.Split(',');
                                if (File.Exists(split[0])) {
                                    path = split[0];
                                    if (split.Length > 1) extra = split[1];
                                    break;
                                }
                            } else if (line.StartsWith("IconIndex=")) {
                                extra = line.Split('=')[1];
                            }
                        }
                    }
                }
            }
            if (File.Exists(path)) {
                cleanup();
                string ext = Path.GetExtension(path);
                if (ext == DOTPon.DOT_PON_EXT) {
                    List<BlinkFrame> f = DOTPon.Load(path);
                    if (f != null && f.Count > 0) {
                        foreach (BlinkFrame i in f) {
                            BlinkManager.Frames.Add(i);
                        }
                        BlinkManager.Start();
                        Icon = pony.Source = f[0].NextFrame;
                        updateSize();
                        Title = Path.GetFileNameWithoutExtension(path);
                    }   
                } else {
                    loadImageFile(ext, path, extra);
                }
            } else if (Directory.Exists(path)) {
                cleanup();
                ImageSource image = Utils.IconFromExeDir(path, extra, true);
                Title = Path.GetFileNameWithoutExtension(path);
                Icon = image;
                pony.Source = image;
                updateSize();
            }
            Cursor = Cursors.Arrow;
        }
        
        private void loadImageFile(string ext, string path, string extra) {
            ImageSource image = null;

            Title = Path.GetFileNameWithoutExtension(path);

            if (ext == ".exe" || ext == ".dll" || ext == ".icl") {
                image = Utils.IconFromExeDir(path, extra, false);
            } else if (ext == ".gif") {
                Animator.setSource(path);
                updateSize();
                return;
            } else if (ext == ".svg") {
                image = SVGParser.parse(path);
            } else {
                BitmapImage finalImage = null;
                try {
                    finalImage = new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
                } catch (Exception) { }

                if (finalImage != null) {
                    image = finalImage;
                } else {
                    image = IconUtils.getIcon(path, true, false, true);
                }
            }
            if (image == null) {
                image = Utils.IconforExtension(path);
            }

            Icon = image;
            pony.Source = image;
            if (Utils.isVideoFile(path)) tryLoadVideo(path);
            if (Utils.isAudioFile(path)) tryLoadAudio(path);
            updateSize();
        }

        private void tryLoadVideo(string path) {
            try {
                Uri src;
                Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out src);
                gilda.Source = src;
                gilda.Play();
            } catch (InvalidOperationException) {
                cleanup();
                return;
            }
            pony.Visibility = Visibility.Hidden;
            gilda.Visibility = Visibility.Visible;
            if (UIVisibility == Visibility.Visible) {
                playback.Visibility = gilda.Visibility;
            } else {
                playback.Visibility = UIVisibility;
            }
        }

        private void tryLoadAudio(string path) {
            try {
                Uri src;
                Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out src);
                gilda.Source = src;
                gilda.Play();
            } catch (InvalidOperationException) {
                cleanup();
                return;
            }
            pony.Visibility = Visibility.Visible;
            gilda.Visibility = Visibility.Visible;
            if (UIVisibility == Visibility.Visible) {
                playback.Visibility = gilda.Visibility;
            } else {
                playback.Visibility = UIVisibility;
            }
        }

        private void gilda_Loaded(object sender, RoutedEventArgs e) {
            
        }

        private void gilda_MediaEnded(object sender, RoutedEventArgs e) {
            if (Loop) {
                gilda.Position = TimeSpan.FromSeconds(0);
            }
        }
    }
}
