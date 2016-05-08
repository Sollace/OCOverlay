using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace OCOverlay {
    public class BlinkFrame {
        public readonly List<int> imageKeys = new List<int>();

        public void AddImage(int img) {
            imageKeys.Add(img);
        }

        public void AddImages(int[] img) {
            foreach (int i in img) {
                AddImage(i);
            }
        }

        public void RemoveImageAt(int index) {
            if (index >= 0 && index < imageKeys.Count) {
                int key = imageKeys[index];
                if (BlinkManager.countImage(key) <= 1) {
                    ImageRegistry.unRegisterImage(key);
                }
                imageKeys.RemoveAt(index);
            }
        }

        public int[] ImageKeys { get { return imageKeys.ToArray(); } }

        private int index = 0;
        private bool first = true;

        private static Random r = new Random();

        public string Name { get; set; }
        public double Duration { get; set; }
        private double _delayMin = 0;
        public double MinDelay {
            get {
                return _delayMin;
            }
            set {
                if (value <= MaxDelay) {
                    _delayMin = value;
                } else {
                    _delayMin = MaxDelay;
                }
            }
        }
        private double _delayMax = 0;
        public double MaxDelay {
            get {
                return _delayMax;
            }
            set {
                if (value >= MinDelay) {
                    _delayMax = value;
                } else {
                    _delayMax = MinDelay;
                }
            }
        }

        public TimeSpan Delay {
            get {
                return TimeSpan.FromSeconds(r.Next((int)(MinDelay * 1000), (int)((MaxDelay + 1) * 1000)) / 1000);
            }
        }

        public BitmapImage NextFrame {
            get {
                return ImageRegistry.getImage(NextKey);
            }
        }

        private int NextKey {
            get {
                return imageKeys.ElementAt(index);
            }
        }

        public BlinkFrame(double minDelay, double maxDelay, double duration) {
            Name = "Untitled";
            Duration = duration;
            MaxDelay = maxDelay;
            MinDelay = minDelay;
        }

        public bool RunBlink(Image element, DispatcherTimer timer) {
            if (ImageKeys.Length <= 0) {
                return true;
            }

            index = (index + 1) % ImageKeys.Length;

            BitmapImage img = NextFrame;

            App.Current.MainWindow.Icon = img;
            element.Source = img;

            if (first) {
                first = false;
                timer.Interval = TimeSpan.FromMilliseconds(Duration);
            }

            if (index == 0) {
                first = true;
                return true;
            }
            return false;
        }
    }
}
