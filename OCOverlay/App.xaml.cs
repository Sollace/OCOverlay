using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Shell;

namespace OCOverlay {

    /*
     * Startup Parameters:
     * {file} always first parameter.
     * Order doesn't count for the others
     * 
     * --w { window width }
     * --h { window height }
     * --x { horizontal position }
     * --y { vertical position }
     * --o { opacity }
     * --sector { TOP_LEFT | TOP_CENTER | TOP_RIGHT }
     *          { MIDDLE_LEFT | MIDDLE_CENTER | MIDDLE_RIGHT }
     *          { BOTTOM_LEFT | BOTTOM_CENTER | BOTTOM_RIGHT }
     * 
     * -nofocus clicks pass through window (only if window is shown in taskbar)
     * -notask hides window in taskbar
     * -a autostart animation
     * -t turn transparent when hovered
     * -c start in canvas mode
     * 
     * Hidden:
     * ~#ghost&no forces -nofocus without being shown in taskbar
     */


    public partial class App : Application {
        public App() : base() {
            SessionEnding += delegate { Shutdown(); };
        }

        protected override void OnStartup(StartupEventArgs e) {
            if (e.Args.Count() == 1) {
                if (RegGen.Startup(e.Args[0])) {
                    Shutdown();
                }
            }

            runNormaly(e.Args);
        }

        private void runNormaly(string[] args) {
            RegGen.CheckAssociations();
            MainWindow window;

            if (args.Count() > 0) {
                double? width = getArgument("--w", args);
                double? height = getArgument("--h", args);
                double? posX = getArgument("--x", args);
                double? posY = getArgument("--y", args);
                double? opacity = getArgument("--o", args);

                if (args.Count() > 1) {
                    window = new MainWindow(args[0], args.Contains("-a"));   
                } else {
                    window = new MainWindow(args[0]);
                }
                
                if (width != null) window.Width = width.Value;
                if (height != null) window.Height = height.Value;
                window.setLocation(getSector(args), posX, posY);
                if (opacity != null) window.ImageOpacity = opacity.Value;
                if ((args.Contains("-nofocus") && (!args.Contains("-notask") || args.Contains("~#ghost&no"))) && window.ImageOpacity > 7) {
                    window.ContentRendered += delegate {
                        window.Pinned = true;
                    };
                    window.ToTransparentWindow();
                }
                window.TransOnHover = args.Contains("-t");

                window.ContentRendered += delegate {
                    ApplyPostParameters(window, args);
                };
            } else {
                window = new MainWindow();
            }

            window.ShowInTaskbar = window.minimize.IsEnabled = !args.Contains("-notask");
            window.Show();
        }

        private void ApplyPostParameters(MainWindow context, string[] args) {
            if (args.Contains("-pin")) {
                context.GlobalActivate();
                context.Pinned = true;
            }
            if (args.Contains("-lock")) context.IsLocked = true;
            if (args.Contains("-flipX")) context.FlipX = true;
            if (args.Contains("-flipY")) context.FlipY = true;
            if (args.Contains("-cover")) context.Cover = true;
            if (args.Contains("-loop")) context.Loop = true;

            double? scale = getArgument("--scale", args);
            double? rot = getArgument("--rot", args);

            if (scale != null) context.Scale = scale.Value;
            if (rot != null) context.Rotate = rot.Value;
        }

        private double? getArgument(string arg, string[] args) {
            if (args.Contains(arg)) {
                try {
                    double val = double.Parse(args[args.ToList().IndexOf(arg) + 1]);
                    return val;
                } catch {}
            }

            return null;
        }

        private PinSector getSector(string[] args) {
            PinSector result = PinSector.TOP_LEFT;
            if (args.Contains("--sector")) {
                try {
                    string val = args[args.ToList().IndexOf("--sector") + 1];

                    result = (PinSector)Enum.Parse(typeof(PinSector), val, true);
                } catch {}
            }

            return result;
        }
    }

    public enum PinSector {
        TOP_LEFT,
        TOP_MIDDLE,
        TOP_RIGHT,
        MIDDLE_LEFT,
        MIDDLE_MIDDLE,
        MIDDLE_RIGHT,
        BOTTOM_LEFT,
        BOTTOM_MIDDLE,
        BOTTOM_RIGHT
    }
}
