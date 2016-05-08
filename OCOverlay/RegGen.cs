using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace OCOverlay {
    public class RegGen {
        private const string REG_HANDLER = "OCOverlay.PON";
        private const string REG_DEFAULT_ICON = "DefaultIcon";
        private const string REG_OPEN_COMMAND = @"Shell\open\command";

        /// <summary>Location from which the current process is running</summary>
        private static readonly string DIR = validate(Assembly.GetExecutingAssembly().GetName().CodeBase);

        /// <summary>Setup argumant</summary>
        private const string ARG_SETUP = "--setup";
        /// <summary>Update argument</summary>
        private const string ARG_UPDATE = "--reset";

        internal static bool Startup(string arg) {
            if (arg == ARG_SETUP) {
                Associate(REG_HANDLER, DOTPon.DOT_PON_EXT, DOTPon.DOT_PON_DESCRIPTION, 1);
                return true;
            } else if (arg == ARG_UPDATE) {
                CreateHandler(REG_HANDLER, DOTPon.DOT_PON_DESCRIPTION, 1);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the file type is associated correctly in the registry and is still valid
        /// </summary>
        internal static void CheckAssociations() {
            if (!KeyExists(REG_HANDLER) || !KeyExists(DOTPon.DOT_PON_EXT)) {
                elevateGenReg(ARG_SETUP);
            } else if (!HandlerIsValid(REG_HANDLER)) {
                elevateGenReg(ARG_UPDATE);
            }
        }

        /// <summary>
        /// Aquires elevated permissions to modify the registry
        /// </summary>
        /// <param name="arg"></param>
        private static void elevateGenReg(string arg) {
            Process p = new Process() {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo(Assembly.GetEntryAssembly().Location, arg) {
                    Verb = "runas"
                }
            };
            p.Start();
            p.WaitForExit();
        }

        /// <summary>
        /// Associates a file extension
        /// </summary>
        /// <param name="handler">Handler used for file</param>
        /// <param name="extension">File extension</param>
        /// <param name="description">File description</param>
        /// <param name="icon">File icon</param>
        internal static void Associate(string handler, string extension, string description, int icon) {
            Registry.ClassesRoot.CreateSubKey(extension).SetValue("", REG_HANDLER);
            CreateHandler(handler, description, icon);
        }

        /// <summary>
        /// Creates the handler for a file extension
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="description"></param>
        /// <param name="icon"></param>
        internal static void CreateHandler(string handler, string description, int icon) {
            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(handler)) {
                if (description != null) {
                    key.SetValue("", description);
                }
                key.CreateSubKey(REG_DEFAULT_ICON).SetValue("", iconPath(icon));
                key.CreateSubKey(REG_OPEN_COMMAND).SetValue("", openCommand);
            }

            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Returns true if the extension is already associated in registry
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        private static bool KeyExists(string handler) {
            return (Registry.ClassesRoot.OpenSubKey(handler, false) != null);
        }

        /// <summary>
        /// Determines if the handler for a file extensin exists and is still valid
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        private static bool HandlerIsValid(string handler) {
            string storedPath = (string)Registry.ClassesRoot.OpenSubKey(handler, false).OpenSubKey(REG_OPEN_COMMAND, false).GetValue("");

            if (storedPath.ToUpper() != openCommand.ToUpper() && !File.Exists(storedPath.Substring(1, storedPath.Length - 7))) {
                return false;
            }

            storedPath = (string)Registry.ClassesRoot.OpenSubKey(handler, false).OpenSubKey(REG_DEFAULT_ICON, false).GetValue("");

            if (storedPath.ToUpper() != iconPath(1) && !File.Exists(storedPath.Split(',')[0].Trim())) {
                return false;
            }

            return true;
            
        }

        private static string iconPath(int icon) {
            return DIR + "," + icon.ToString();
        }

        private static string openCommand {
            get { return "\"" + DIR + "\" \"%1\""; }
        }

        /// <summary>
        /// Cleans a file path.
        /// </summary>
        /// <param name="path">path to clean</param>
        /// <returns>cleaned path</returns>
        private static string validate(string path) {
            path = path.Replace("file:///", "");

            path = path.Replace("/", @"\");
            while (path.Contains(@"\\")) {
                path = path.Replace(@"\\", @"\");
            }
            return path;
        }

        /// <summary>
        /// Notifies windows explorer that a file association has changed and it must reload file icons.
        /// </summary>
        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }
}
