using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace OCOverlay {
    public class GifSource {
        public List<GifFrame> Frames { get; private set; }

        private Window contextWindow;
        private System.Windows.Controls.Image context;
        private DispatcherTimer timer = new DispatcherTimer();
        private int currentIndex;

        public GifSource(Window window, System.Windows.Controls.Image destination) : this(destination) {
            SetWindow(window);
        }

        public GifSource(System.Windows.Controls.Image destination) {
            Frames = new List<GifFrame>();
            context = destination;
            timer.Tick += delegate { updateImage(); };
        }

        public void SetWindow(Window win) {
            contextWindow = win;
        }

        public void setSource(string path) {
            configure(Image.FromFile(path));
        }

        private void configure(Image img) {
            Stop();
            if (ImageAnimator.CanAnimate(img)) {
                int frames = img.GetFrameCount(FrameDimension.Time);

                if (frames > 1) {
                    byte[] times = img.GetPropertyItem(0x5100).Value;
                    
                    for (int frame = 0; frame < frames; frame++) {
                        img.SelectActiveFrame(FrameDimension.Time, frame);
                        Frames.Add(new GifFrame(new Bitmap(img), BitConverter.ToInt32(times, 4 * frame)));
                    }

                    updateImage();
                    timer.Start();
                } else {
                    context.Source = img.convertToBitmap();
                }
            } else {
                context.Source = img.convertToBitmap();
            }

            img.Dispose();
        }

        public void Stop() {
            currentIndex = 0;
            timer.Stop();
            foreach (GifFrame i in Frames) {
                i.Dispose();
            }
            Frames.Clear();
            GC.Collect();
        }

        private void updateImage() {
            currentIndex = (currentIndex + 1) % Frames.Count;
            timer.Interval = Frames[currentIndex].Duration;
            context.Source = Frames[currentIndex].TheImage;
            if (contextWindow != null) {
                contextWindow.Icon = context.Source;
            }
        }
    }

    public static class GifUtils {
        /// <summary>
        /// Converts a Image to a BitmapImage
        /// </summary>
        /// <param name="image">Image to convert</param>
        /// <returns>Resulting BitmapImage</returns>
        public static BitmapImage convertToBitmap(this Image image) {
            BitmapImage result = new BitmapImage();
            using (MemoryStream s = new MemoryStream()) {
                image.Save(s, ImageFormat.Png);
                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = s;
                result.EndInit();
            }
            return result;
        }
    }

    public class GifFrame : IDisposable {
        private int _duration;
        public TimeSpan Duration {
            get { return TimeSpan.FromMilliseconds(_duration * 8); }
        }

        public BitmapImage TheImage { get; private set; }

        internal GifFrame(Image img, int duration) {
            TheImage = img.convertToBitmap();
            img.Dispose();
            _duration = duration;
        }

        public void Dispose() {
            TheImage.StreamSource.Dispose();
        }
    }
}
