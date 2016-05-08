using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace OCOverlay {
    static class WindowPropSyncroniser {
        private static MainWindow context;
        private static DispatcherTimer timer = new DispatcherTimer() {
            Interval = TimeSpan.FromMilliseconds(20)
        };

        private static int posTimer = 0;
        public static void enablePositionReset() {
            posTimer = 50;
        }

        private static void timer_Tick(object sender, EventArgs e) {
            try {
                context.Pinned = context.Pinned;
                if (posTimer > 0) {
                    posTimer--;
                    context.updateLocation();
                }
            } catch (Exception) {
            }
            context.pony_holder.Opacity -= context.OpacityOffset;
        }

        public static void Start(MainWindow w) {
            context = w;
            timer.Tick += timer_Tick;
            timer.Start();
        }
    }
}
