using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace OCOverlay {
    public static class ImageRegistry {
        internal static Dictionary<int, BitmapImage> Bitmaps = new Dictionary<int, BitmapImage>();
        internal static Dictionary<int, byte[]> Bytes = new Dictionary<int, byte[]>();

        /// <summary>
        /// Adds an image to the registry if it is not already registered.
        /// </summary>
        /// <param name="path">Location of image file</param>
        /// <returns>Id of image in registry</returns>
        public static int registerImage(string path) {
            byte[] data = null;
            try {
                data = File.ReadAllBytes(path);
            } catch (UnauthorizedAccessException) {
                return -1;
            }

            if (!matches(data)) {
                Uri uri = new Uri(path);
                BitmapImage img = null;
                try {
                    if (path.EndsWith(".svg")) {
                        img = SVGParser.parse(path);
                    } else {
                        img = new BitmapImage(uri);
                    }

                    if (img != null) {
                        img.Freeze();
                        int result = NextId();

                        Bitmaps.Add(result, img);
                        Bytes.Add(result, data);

                        return result;
                    }
                } catch (IOException) {
                } catch (NotSupportedException) { }


                return -1;
            }

            return findKey(data);
        }

        private static bool matches(byte[] data) {
            foreach (byte[] b in Bytes.Values) {
                if (b.Length == data.Length) {
                    return arraysMatch(b, data);
                }
            }

            return false;
        }

        private static bool arraysMatch(byte[] a, byte[] b) {
            if (a.Length == b.Length) {
                for (int i = 0; i < a.Length; i++) {
                    if (a[i] != b[i]) {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private static int findKey(byte[] img) {
            for (int i = 0; i < Bytes.Count; i++) {
                byte[] value = Bytes.Values.ElementAt(i);

                if (arraysMatch(value, img)) {
                    return Bytes.Keys.ElementAt(i);
                }
            }

            return -1;
        }

        /// <summary>
        /// Retrieves a BitmapImage from the registry
        /// </summary>
        /// <param name="index">Id of the image</param>
        /// <returns>Bitmap or null if it does not exist</returns>
        public static BitmapImage getImage(int index) {
            if (Bitmaps.ContainsKey(index)) {
                return Bitmaps[index];
            }
            return null;
        }

        /// <summary>
        /// Retrieves the bytes for an image in the registry
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static byte[] getBytes(int index) {
            if (Bytes.ContainsKey(index)) {
                return Bytes[index];
            }
            return null;
        }

        /// <summary>
        /// Remove an image from the registry and closes resources if needed
        /// </summary>
        /// <param name="key">Id of image to remove</param>
        public static void unRegisterImage(int key) {
            if (Bitmaps.ContainsKey(key)) {
                BitmapImage g = Bitmaps[key];
                if (g.StreamSource != null) {
                    g.StreamSource.Dispose();
                    g.StreamSource.Close();
                }
                Bitmaps.Remove(key);
            }
            if (Bytes.ContainsKey(key)) {
                Bytes.Remove(key);
            }
        }

        /// <summary>
        /// Compiles the byte data for a set of images for save in a .pon file.
        /// </summary>
        /// <param name="usedImages">List of image ids</param>
        /// <returns>resulting byte data</returns>
        public static byte[] getBytesForSave(List<int> usedImages) {
            List<byte> data = new List<byte>();

            int[] keys = Bytes.Keys.Where(X => usedImages.Contains(X)).ToArray();
            byte[][] rawData = Bytes.Values.Where(X => usedImages.Contains(Bytes.Keys.ElementAt(Bytes.Values.ToList().IndexOf(X)))).ToArray();

            int indexLen = getIndexHeaderLength(keys);
            int dataLen = getDataHeaderLength(rawData);

            if (indexLen > dataLen) {
                dataLen = indexLen;
            }

            dataLen = FileParsing.getStringBytes(dataLen.ToString()).Length;
            indexLen = FileParsing.getStringBytes(indexLen.ToString()).Length;

            data.AddRange(FileParsing.getHeader(dataLen, DOTPon.HEADER_LENGTH));
            data.AddRange(FileParsing.getHeader(indexLen, dataLen));

            for (int i = 0; i < keys.Length; i++) {
                string s = keys[i].ToString();
                data.AddRange(FileParsing.getHeader(s.Length, indexLen));
                data.AddRange(FileParsing.getStringBytes(s));

                data.AddRange(FileParsing.getHeader(rawData[i].Length, dataLen));
                data.AddRange(rawData[i]);
            }

            return data.ToArray();
        }

        /// <summary>
        /// Reads byte data from a stream and adds resulting images to the registry.
        /// Used for loading .pon files.
        /// </summary>
        /// <param name="data">Stream to load from</param>
        public static void readBytes(Stream data) {
            byte[] bytes = new byte[DOTPon.HEADER_LENGTH];
            data.Read(bytes, 0, bytes.Length);
            int dataLen = FileParsing.readHeader(bytes, bytes.Length);

            bytes = new byte[dataLen];
            data.Read(bytes, 0, bytes.Length);
            int indexLen = FileParsing.readHeader(bytes, bytes.Length);

            while (data.Position < data.Length) {
                int keyLen = data.readHeader(indexLen);
                int key = Int32.Parse(data.readLine(keyLen));

                int bytesLen = data.readHeader(dataLen);
                bytes = new byte[bytesLen];
                data.Read(bytes, 0, bytes.Length);

                if (Bytes.ContainsKey(key)) {
                    if (!arraysMatch(Bytes[key], bytes)) {
                        int next = NextId();
                        FileChunk.MarkKeyForChange(key, next);
                        key = next;
                    }
                }

                if (!Bytes.ContainsKey(key)) {
                    Bytes.Add(key, bytes);
                }
            }

            foreach (KeyValuePair<int, byte[]> i in Bytes) {
                if (!Bitmaps.ContainsKey(i.Key)) {
                    Bitmaps.Add(i.Key, FileParsing.getImage(i.Value));
                }
            }
        }

        private static int NextId() {
            for (int i = 0; ; i++) {
                if (!Bitmaps.ContainsKey(i) && !Bytes.ContainsKey(i)) {
                    return i;
                }
            }
        }

        private static int getIndexHeaderLength(int[] indexes) {
            int length = 0;
            foreach (int i in indexes) {
                if (i.ToString().Length > length) {
                    length = i.ToString().Length;
                }
            }
            return length;
        }

        private static int getDataHeaderLength(byte[][] data) {
            int length = 0;
            foreach (byte[] i in data) {
                if (i.Length > length) {
                    length = i.Length;
                }
            }
            return length;
        }
    }

}
