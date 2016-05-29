using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Threading;

namespace OCOverlay {
    public class BlinkManager {
        private readonly DispatcherTimer timer = new DispatcherTimer();

        internal readonly ObservableCollection<BlinkFrame> Frames = new ObservableCollection<BlinkFrame>();
        private BlinkFrame selectedFrame;

        public bool IsEnabled {
            get {
                return timer != null && timer.IsEnabled;
            }
        }
        public Image Screen { get; private set; }
        private bool init = false;

        public BlinkManager(Image screen) {
            Screen = screen;
        }

        private void Init() {
            if (!init) {
                init = true;
                timer.Tick += timer_Tick;
                Frames.CollectionChanged += Frames_CollectionChanged;
            }
        }

        private void timer_Tick(object sender, EventArgs e) {
            if (selectedFrame != null) {
                MainWindow.Animator.Stop();
                if (selectedFrame.RunBlink(Screen, timer)) {
                    if (Frames.Count > 0) {
                        selectedFrame = Frames.pickOne();
                        timer.Interval = selectedFrame.Delay;
                    } else {
                        Stop();
                    }
                }
            } else {
                Stop();
            }
        }

        private void Frames_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
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

        public int countImage(int img) {
            int result = 0;
            foreach (BlinkFrame i in Frames) {
                foreach (int g in i.ImageKeys) {
                    if (g == img) result++;
                }
            }
            return result;
        }

        public void Start() {
            Init();
            if (Frames.Count > 0) {
                selectedFrame = Frames.pickOne();
                timer.Interval = selectedFrame.Delay;
                timer.Start();
            }
        }

        public void Pause() {
            if (IsEnabled) timer.Stop();
        }

        public void Continue() {
            if (selectedFrame != null && Frames.Count > 0) {
                if (!IsEnabled) timer.Start();
            } else {
                Pause();
            }
        }

        public void Stop() {
            if (IsEnabled) {
                Pause();
                selectedFrame = null;
            }
        }
    }
}
