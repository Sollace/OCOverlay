using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OCOverlay {
    class IconUtils {

        public static BitmapSource getIcon(string fileName, bool jumbo, bool directory, bool checkDisk) {
            BitmapSource result = getIconLargeViaFactory(fileName);
            if (result == null) result = getIconLarge(fileName, jumbo, directory, checkDisk);
            if (result == null) return getIconSmall(fileName, false, directory, checkDisk);
            return result;
        }

        public static BitmapSource getIconSmall(string FileName, bool small, bool directory, bool checkDisk) {
            SHFILEINFO shinfo = new SHFILEINFO();

            uint flags = SHGFI_ICON;
            flags |= small ? SHGFI_SMALLICON : SHGFI_LARGEICON;
            if (!checkDisk) flags |= SHGFI_USEFILEATTRIBUTES;

            uint attr = FILE_ATTRIBUTE_NORMAL;
            if (directory) attr |= FILE_ATTRIBUTE_DIRECTORY;

            if (SHGetFileInfo(FileName, attr, out shinfo, (uint)Marshal.SizeOf(shinfo), flags) != 0) {
                var bs = SourceForIcon(Icon.FromHandle(shinfo.hIcon));
                DestroyIcon(shinfo.hIcon);
                return bs;
            }
            return null;
        }

        public static BitmapSource getIconLarge(string FileName, bool jumbo, bool directory, bool checkDisk) {
            SHFILEINFO shinfo = new SHFILEINFO();

            uint flags = SHGFI_SYSICONINDEX;
            if (!checkDisk) flags |= SHGFI_USEFILEATTRIBUTES;

            uint attr = FILE_ATTRIBUTE_NORMAL;
            if (directory) attr |= FILE_ATTRIBUTE_DIRECTORY;

            if (SHGetFileInfo(FileName, attr, out shinfo, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), flags) != 0) {
                var iconIndex = shinfo.iIcon;

                IImageList iml;
                int size = jumbo ? SHIL_JUMBO : SHIL_EXTRALARGE;

                // Get the System IImageList object from the Shell:
                Guid iidImageList = new Guid(IIMAGELISTIID);
                var hres = SHGetImageList(size, ref iidImageList, out  iml);
                
                IntPtr hIcon = IntPtr.Zero;
                hres = iml.GetIcon(iconIndex, ILD_TRANSPARENT, ref hIcon);
                
                var bs = SourceForIcon(Icon.FromHandle(hIcon));
                DestroyIcon(hIcon);
                return bs;
            }
            return null;
        }

        public static BitmapSource getIconLargeViaFactory(string fileName) {
            BitmapSource result = null;
            IShellItem nativeShellItem;
            Guid shellItem2Guid = new Guid(IShellItem2UUID);
            if (SHCreateItemFromParsingName(fileName, IntPtr.Zero, ref shellItem2Guid, out nativeShellItem) == 0) {
                NativeSize nativeSize = new NativeSize() { Width = 256, Height = 256 };
                IntPtr hBitmap;
                if (((IShellItemImageFactory)nativeShellItem).GetImage(nativeSize, SIIGBF_SCALEUP | SIIGBF_BIGGERSIZEOK, out hBitmap) == 0) {
                    result = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
                Marshal.ReleaseComObject(nativeShellItem);
            }
            return result;
        }

        /// <summary>
        /// Extracts a bitmap source and discard the given Icon.
        /// </summary>
        public static BitmapSource SourceForIcon(System.Drawing.Icon icon) {
            BitmapSource result = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(icon.Width, icon.Height));
            result.Freeze(); // very important to avoid memory leak
            icon.Dispose();
            return result;
        }

        #region Win32 Interop

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string path,
            // The following parameter is not used - binding context.
            IntPtr pbc, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IShellItem shellItem);

        [DllImport("shell32.dll", EntryPoint = "#727")]
        internal static extern int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);
        
        [DllImport("shell32.dll")]
        internal static extern int SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint flags);

        [DllImport("shell32.dll", SetLastError = true)]
        internal static extern int SHGetSpecialFolderLocation(IntPtr hwndOwner, Int32 nFolder, ref IntPtr ppidl);

        [DllImport("user32")]
        internal static extern int DestroyIcon(IntPtr hIcon);

        #endregion

        #region Constants

        public const uint ILD_NORMAL = 0x00000000;
        public const uint ILD_TRANSPARENT = 0x00000001;
        public const uint ILD_FOCUS = 0x00000002;
        public const uint ILD_SELECTED = 0x00000004;
        public const uint ILD_MASK = 0x00000010;
        public const uint ILD_IMAGE = 0x00000020;
        public const uint ILD_ROP = 0x00000040;
        public const uint ILD_OVERLAYMASK = 0x00000F00;
        public const uint ILD_PRESERVEALPHA = 0x00001000;
        public const uint ILD_SCALE = 0x00002000;
        public const uint ILD_DPISCALE = 0x00004000;
        public const uint ILD_ASYNC = 0x00008000; //Vista+

        public const uint FILE_ATTRIBUTE_READONLY = 0x00000001;
        public const uint FILE_ATTRIBUTE_HIDDEN = 0x00000002;
        public const uint FILE_ATTRIBUTE_SYSTEM = 0x00000004;
        public const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        public const uint FILE_ATTRIBUTE_ARCHIVE = 0x00000020;
        public const uint FILE_ATTRIBUTE_DEVICE = 0x00000040;
        public const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        public const uint FILE_ATTRIBUTE_TEMPORARY = 0x00000100;
        public const uint FILE_ATTRIBUTE_SPARSE_FILE = 0x00000200;
        public const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;
        public const uint FILE_ATTRIBUTE_COMPRESSED = 0x00000800;
        public const uint FILE_ATTRIBUTE_OFFLINE = 0x00001000;
        public const uint FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000;
        public const uint FILE_ATTRIBUTE_ENCRYPTED = 0x00004000;
        public const uint FILE_ATTRIBUTE_VIRTUAL = 0x00010000;

        public const uint SHGFI_ICON = 0x000000100;     // get icon
        public const uint SHGFI_DISPLAYNAME = 0x000000200;     // get display name
        public const uint SHGFI_TYPENAME = 0x000000400;     // get type name
        public const uint SHGFI_ATTRIBUTES = 0x000000800;     // get attributes
        public const uint SHGFI_ICONLOCATION = 0x000001000;     // get icon location
        public const uint SHGFI_EXETYPE = 0x000002000;     // return exe type
        public const uint SHGFI_SYSICONINDEX = 0x000004000;     // get system icon index
        public const uint SHGFI_LINKOVERLAY = 0x000008000;     // put a link overlay on icon
        public const uint SHGFI_SELECTED = 0x000010000;     // show icon in selected state
        public const uint SHGFI_ATTR_SPECIFIED = 0x000020000;     // get only specified attributes
        public const uint SHGFI_LARGEICON = 0x000000000;     // get large icon
        public const uint SHGFI_SMALLICON = 0x000000001;     // get small icon
        public const uint SHGFI_OPENICON = 0x000000002;     // get open icon
        public const uint SHGFI_SHELLICONSIZE = 0x000000004;     // get shell size icon
        public const uint SHGFI_PIDL = 0x000000008;     // pszPath is a pidl
        public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;     // use passed dwFileAttribute

        public const int SHIL_LARGE = 0x0;
        public const int SHIL_SMALL = 0x1;
        public const int SHIL_EXTRALARGE = 0x2;
        public const int SHIL_SYSSMALL = 0x3;
        public const int SHIL_JUMBO = 0x4;

        public const uint SIIGBF_RESIZETOFIT = 0x00000000;
        public const uint SIIGBF_BIGGERSIZEOK = 0x00000001;
        public const uint SIIGBF_MEMORYONLY = 0x00000002;
        public const uint SIIGBF_ICONONLY = 0x00000004;
        public const uint SIIGBF_THUMBNAILONLY = 0x00000008;
        public const uint SIIGBF_INCACHEONLY = 0x00000010;

        public const uint SIIGBF_CROPTOSQUARE = 0x00000020; //Win8+
        public const uint SIIGBF_WIDETHUMBNAILS = 0x00000040; //Win8+
        public const uint SIIGBF_ICONBACKGROUND = 0x00000080; //Win8+
        public const uint SIIGBF_SCALEUP = 0x00000100; //Win8+
        #endregion

        #region Managed Types

        private const string IShellItem2UUID = "7E9FB0D3-919F-4307-AB2E-9B1860310C93";

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
        internal interface IShellItem {
            void BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)]Guid bhid, [MarshalAs(UnmanagedType.LPStruct)]Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName(uint sigdnName, out IntPtr ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }

        [ComImportAttribute()]
        [GuidAttribute("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IShellItemImageFactory {
            [PreserveSig] int GetImage([In, MarshalAs(UnmanagedType.Struct)] NativeSize size, [In] uint flags, [Out] out IntPtr phbm);
        }
        
        [StructLayout(LayoutKind.Sequential)]
        internal struct SHFILEINFO {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }


        private const string IIMAGELISTIID = "46EB5926-582E-4017-9FDF-E8998DAA0950";
        [ComImportAttribute()]
        [GuidAttribute("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IImageList {
            [PreserveSig] int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);
            [PreserveSig] int ReplaceIcon(int i, IntPtr hicon, ref int pi);
            [PreserveSig] int SetOverlayImage(int iImage, int iOverlay);
            [PreserveSig] int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);
            [PreserveSig] int AddMasked(IntPtr hbmImage, int crMask, ref int pi);
            [PreserveSig] unsafe int Draw(void **pimldp);
            [PreserveSig] int Remove(int i);
            [PreserveSig] int GetIcon(int i, uint flags, ref IntPtr picon);
            [PreserveSig] int GetImageInfo(int i, ref IMAGEINFO pImageInfo);
            [PreserveSig] int Copy(int iDst, IImageList punkSrc, int iSrc, int uFlags);
            [PreserveSig] int Merge(int i1, IImageList punk2, int i2, int dx, int dy, ref Guid riid, ref IntPtr ppv);
            [PreserveSig] int Clone(ref Guid riid, ref IntPtr ppv);
            [PreserveSig] int GetImageRect(int i, ref RECT prc);
            [PreserveSig] int GetIconSize(ref int cx, ref int cy);
            [PreserveSig] int SetIconSize(int cx, int cy);
            [PreserveSig] int GetImageCount(ref int pi);
            [PreserveSig] int SetImageCount(int uNewCount);
            [PreserveSig] int SetBkColor(int clrBk, ref int pclr);
            [PreserveSig] int GetBkColor(ref int pclr);
            [PreserveSig] int BeginDrag(int iTrack, int dxHotspot, int dyHotspot);
            [PreserveSig] int EndDrag();
            [PreserveSig] int DragEnter(IntPtr hwndLock, int x, int y);
            [PreserveSig] int DragLeave(IntPtr hwndLock);
            [PreserveSig] int DragMove(int x, int y);
            [PreserveSig] int SetDragCursorImage(ref IImageList punk, int iDrag, int dxHotspot, int dyHotspot);
            [PreserveSig] int DragShowNolock(int fShow);
            [PreserveSig] int GetDragImage(ref POINT ppt, ref POINT pptHotspot, ref Guid riid, ref IntPtr ppv);
            [PreserveSig] int GetItemFlags(int i, ref int dwFlags);
            [PreserveSig] int GetOverlayImage(int iOverlay, ref int piIndex);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct IMAGEINFO {
            public IntPtr hbmImage, hbmMask;
            public int Unused1, Unused2;
            public RECT rcImage;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT {
            private int _Left, _Top, _Right, _Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT {
            public int X, Y;
            public POINT(int x, int y) {
                this.X = x;
                this.Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeSize {
            private int width, height;
            public int Width { set { width = value; } }
            public int Height { set { height = value; } }
        }
        #endregion
    }
}
