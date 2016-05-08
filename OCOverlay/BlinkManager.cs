using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Threading;

namespace OCOverlay {
    public static class BlinkManager {
        private static DispatcherTimer timer = new DispatcherTimer();

        internal static readonly ObservableCollection<BlinkFrame> Frames = new ObservableCollection<BlinkFrame>();
        private static BlinkFrame selectedFrame;

        public static bool IsEnabled { get; private set; }
        public static Image Screen { get; private set; }
        private static bool init = false;

        private static void Init(Image screen) {
            Screen = screen;
            if (!init) {
                init = true;
                timer.Tick += timer_Tick;
                Frames.CollectionChanged += Frames_CollectionChanged;
            }
        }

        private static void timer_Tick(object sender, EventArgs e) {
            if (Screen != null && selectedFrame != null) {
                MainWindow.Animator.Stop();
                if (selectedFrame.RunBlink(Screen, timer)) {
                    if (Frames.Count > 0) {
                        selectedFrame = Frames.pickOne();
                        timer.Interval = selectedFrame.Delay;
                    } else {
                        Stop();
                    }
                }
            }
        }

        private static void Frames_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (Frames.Count == 0) {
                Pause();
            } else {
                Continue();
            }

            if (e.Action == NotifyCollectionChangedAction.Remove) {
                foreach (BlinkFrame i in e.OldItems) {
                    foreach (int g in i.ImageKeys) {
                        if (countImage(g) < 1) {
                            ImageRegistry.unRegisterImage(g);
                        }
                    }
                }
            }
        }

        public static int countImage(int img) {
            int result = 0;
            foreach (BlinkFrame i in Frames) {
                foreach (int g in i.ImageKeys) {
                    if (g == img) result++;
                }
            }
            return result;
        }

        public static void Start(Image screen) {
            Init(screen);
            if (Frames.Count > 0) {
                selectedFrame = Frames.pickOne();
                timer.Interval = selectedFrame.Delay;
                timer.Start();
                IsEnabled = true;
            } else {
                IsEnabled = false;
            }
        }

        public static void Pause() {
            timer.Stop();
        }

        public static void Continue() {
            if (!init) {
                Start((Image)App.Current.MainWindow.FindName("pony"));
            } else {
                timer.Start();
            }
        }

        public static void Stop() {
            timer.Stop();
            IsEnabled = false;
            selectedFrame = null;
            Init(null);
        }
    }
}
