using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace OCOverlay {
    class DOTPon {
        public const string DOT_PON_EXT = ".pon";
        public const string DOT_PON_DESCRIPTION = "OC Pony File";
        public const int HEADER_LENGTH = 255;

        public static List<BlinkFrame> Load(string path) {
            try {
                return doLoad(path);
            } catch (FileFormatException) {
                MessageBox.Show("That is not a valid OCOverlay Pony File. Either it was saved in a newer version of OCOverlay or has become damaged.", "File Load Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            } catch (IOException e) {
                MessageBox.Show("An unknown error has occured whilst opening.\n" + e.Message, "File Load Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            return null;
        }

        private static List<BlinkFrame> doLoad(string path) {
            if (!path.EndsWith(DOT_PON_EXT)) {
                throw new UriFormatException();
            }

            List<BlinkFrame> result = new List<BlinkFrame>();
            using (FileStream file = File.OpenRead(path)) {
                int hlength = file.readHeader(HEADER_LENGTH);

                int dataLength = file.readHeader(HEADER_LENGTH);

                string[] data = file.readLine(dataLength).Split('\t');

                ImageRegistry.readBytes(file);
                foreach (string i in data) {
                    result.Add(FileChunk.getBlink(i.Replace("\0", "0"), hlength));
                }
            }

            return result;
        }

        public static void Save(string path, params BlinkFrame[] frames) {
            if (Path.GetExtension(path) == ".gif") {
                SaveGif(path, frames);
            } else {
                if (!path.EndsWith(DOT_PON_EXT)) path += DOT_PON_EXT;

                int hlength = 0;
                
                List<FileChunk> chunks = new List<FileChunk>();
                List<int> usedImages = new List<int>();

                foreach (BlinkFrame i in frames) {
                    foreach (int m in i.ImageKeys) {
                        if (!usedImages.Contains(m)) {
                            usedImages.Add(m);
                        }
                    }
                    FileChunk f = new FileChunk(i);
                    int h = f.headerLengthValue();
                    if (h > hlength) {
                        hlength = h;
                    }
                    chunks.Add(f);
                }

                List<byte> blinks = new List<byte>();
                byte[] delimiter = FileParsing.getStringBytes("\t");
                foreach (FileChunk i in chunks) {
                    if (blinks.Count > 0) {
                        blinks.AddRange(delimiter);
                    }
                    blinks.AddRange(i.getBytes(hlength));
                }

                using (FileStream file = File.Create(path)) {
                    file.Write(FileParsing.getHeader(hlength, HEADER_LENGTH), 0, HEADER_LENGTH);

                    byte[] data = blinks.ToArray();
                    file.Write(FileParsing.getHeader(data.Length, HEADER_LENGTH), 0, HEADER_LENGTH);
                    file.Write(data, 0, data.Length);

                    byte[] images = ImageRegistry.getBytesForSave(usedImages);
                    file.Write(images, 0, images.Length);
                    file.Flush();
                }
            }
        }

        /// <summary>
        /// Saves a collection of blink frames to a gif file.
        /// [Work in progress .NET provides very limited support for writing gif files just that it animates is an achievement]
        /// [Gif files generated are not correctly formed and are not recognized by this program but may work in other programs]
        /// [There is currently no way to make it loop]
        /// </summary>
        /// <param name="path">location of file</param>
        /// <param name="frames">blink frames to save</param>
        public static void SaveGif(string path, params BlinkFrame[] frames) {
            GifBitmapEncoder enc = new GifBitmapEncoder();
            List<MemoryStream> streams = new List<MemoryStream>();

            BitmapFrame last = null;

            for (int i = 0; i < frames.Length; i++) {
                BlinkFrame currentBlink = frames[i];
                for (int k = 0; k < currentBlink.ImageKeys.Length; k++) {
                    MemoryStream st = new MemoryStream(ImageRegistry.getBytes(currentBlink.ImageKeys[k]));
                    BitmapDecoder dec = GifBitmapDecoder.Create(st, BitmapCreateOptions.None, BitmapCacheOption.None);
                    streams.Add(st);
                    for (int s = 0; s < dec.Frames.Count; s++) {
                        InPlaceBitmapMetadataWriter writer = dec.Frames[s].CreateInPlaceBitmapMetadataWriter();
                        if (writer.TrySave()) {
                            UInt16 del = (ushort)(s == 0 ? currentBlink.MaxDelay : currentBlink.Duration);
                            writer.SetQuery("/grctlext/Delay", del);
                            writer.SetQuery("/appext/Application", ASCIIEncoding.ASCII.GetBytes("NETSCAPE2.0"));
                            writer.SetQuery("/appext/Data", new byte[] { 3, 1, 0, 0, 0 });
                        }
                        enc.Frames.Add(dec.Frames[s]);
                    }

                    if (i == 0 && k == 0) {
                        last = dec.Frames[0];
                    }
                }
            }

            if (last != null) {
                InPlaceBitmapMetadataWriter writer = last.CreateInPlaceBitmapMetadataWriter();
                if (writer.TrySave() == true) {
                    UInt16 del = (ushort)frames[0].MaxDelay;
                    writer.SetQuery("/grctlext/Delay", del);
                    writer.SetQuery("/appext/Application", ASCIIEncoding.ASCII.GetBytes("NETSCAPE2.0"));
                    writer.SetQuery("/appext/Data", new byte[] { 3, 1, 0, 0, 0 });
                }
                enc.Frames.Add(last);
            }

            using (FileStream file = File.Create(path)) {
                enc.Save(file);
            }

            foreach (MemoryStream st in streams) {
                st.Dispose();
                st.Close();
            }
        }
    }

    public static class FileParsing {
        /// <summary>
        /// Convert a byte array to a BitmapImage
        /// </summary>
        public static BitmapImage getImage(byte[] data) {
            BitmapImage image = new BitmapImage();

            using (Stream s = new MemoryStream(data)) {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = s;
                image.EndInit();
            }

            image.Freeze();
            return image;
        }

        /// <summary>
        /// Convert a string to a byte array
        /// </summary>
        public static byte[] getStringBytes(string text) {
            return Encoding.ASCII.GetBytes(text);
        }

        /// <summary>
        /// Convert a byte array to a string
        /// </summary>
        public static string getString(byte[] data) {
            return Encoding.ASCII.GetString(data);
        }

        public static string getFileStringData(String path) {
            using (FileStream s = File.OpenRead(path)) {
                byte[] data = new byte[s.Length];
                s.Read(data, 0, data.Length);
                return getString(data);
            }
        }

        /// <summary>
        /// Interpret a header of fixed length and returns the value it contains
        /// </summary>
        /// <param name="data">The stream to read the header from</param>
        /// <param name="hlength">Length of the header</param>
        /// <returns>Header value</returns>
        public static int readHeader(this Stream data, int hlength) {
            byte[] bytes = new byte[hlength];
            data.Read(bytes, 0, bytes.Length);
            return readHeader(bytes, bytes.Length);
        }

        /// <summary>
        /// Reads a string of given length from the Stream
        /// </summary>
        /// <param name="data">Stream to read the data from</param>
        /// <param name="length">Length of the string</param>
        /// <returns></returns>
        public static string readLine(this Stream data, int length) {
            byte[] bytes = new byte[length];
            data.Read(bytes, 0, bytes.Length);
            return getString(bytes);
        }

        /// <summary>
        /// Interpret a header of fixed length and returns the value it contains
        /// </summary>
        /// <param name="data">The byte array to read the header from</param>
        /// <param name="hlength">Length of the header</param>
        /// <returns>Header value</returns>
        internal static int readHeader(byte[] data, int hlength) {
            if (data.Length != hlength) throw new FileFormatException("Header data is not of expected length");

            string result = "";
            bool read = false;
            for (int i = 0; i < hlength; i++) {
                if (data[i] != 0) read = true;
                if (read) result += Convert.ToChar(data[i]);
            }

            try {
                return Convert.ToInt32(result);
            } catch (FormatException) {
                throw new FileFormatException("Malformed Header");
            }
        }

        /// <summary>
        /// Interpret a header of fixed length and returns the value it contains
        /// </summary>
        /// <param name="data">The string to read the header from</param>
        /// <param name="hlength">Length of the header</param>
        /// <returns>Header value</returns>
        internal static int readHeader(string data, int hlength) {
            if (data.Length != hlength) throw new FileFormatException("Header data is not of expected length");

            string result = "";
            bool read = false;
            for (int i = 0; i < hlength; i++) {
                if (data[i] != '0') read = true;
                if (read) result += data[i];
            }

            try {
                return Convert.ToInt32(result);
            } catch (FormatException) {
                throw new FileFormatException("Malformed Header");
            }
        }

        /// <summary>
        /// Create the bytes of a header of the given length and the given value
        /// </summary>
        /// <param name="value">Value contained in the header</param>
        /// <param name="hlength">Length of the header</param>
        /// <returns>Header as a byte array</returns>
        public static byte[] getHeader(int value, int hlength) {
            byte[] data = new byte[hlength];
            byte[] l = FileParsing.getStringBytes(value.ToString());

            if (l.Length > data.Length) {
                throw new OverflowException();
            }

            int dif = data.Length - l.Length;
            for (int i = dif; i - dif < l.Length; i++) {
                data[i] = l[i - dif];
            }

            return data;
        }
    }

    public class FileChunk {
        private byte[] name;
        private byte[] timingsNew;
        private byte[] framesNew;

        private static Dictionary<int, int> keyChanges = new Dictionary<int, int>();

        public static void MarkKeyForChange(int original, int newKey) {
            if (keyChanges.ContainsKey(original)) {
                keyChanges.Remove(original);
            }

            keyChanges.Add(original, newKey);
        }

        public FileChunk(BlinkFrame frame) {
            name = FileParsing.getStringBytes(frame.Name);
            timingsNew = getTimingBytes(frame);
            framesNew = getFramesBytes(frame.ImageKeys);
        }

        public byte[] getBytes(int hlength) {
            List<byte> result = new List<byte>();

            result.AddRange(FileParsing.getHeader(name.Length, hlength));
            result.AddRange(name);

            result.AddRange(FileParsing.getHeader(timingsNew.Length, hlength));
            result.AddRange(timingsNew);

            result.AddRange(FileParsing.getHeader(framesNew.Length, hlength));
            result.AddRange(framesNew);

            return result.ToArray();
        }

        public static BlinkFrame getBlink(string data, int hlength) {

            int length = FileParsing.readHeader(data.Substring(0, hlength), hlength);
            data = data.Substring(hlength);

            string name = data.Substring(0, length);
            data = data.Substring(length);


            length = FileParsing.readHeader(data.Substring(0, hlength), hlength);
            data = data.Substring(hlength);

            double[] timings = getTimings(data.Substring(0, length));
            data = data.Substring(length);

            length = FileParsing.readHeader(data.Substring(0, hlength), hlength);
            data = data.Substring(hlength);

            int[] frames = getFrames(data.Substring(0, length));

            for (int i = 0; i < frames.Length; i++) {
                if (keyChanges.ContainsKey(i)) {
                    frames[i] = keyChanges[i];
                }
            }

            BlinkFrame result = new BlinkFrame(timings[0], timings[1], timings[2]);
            result.Name = name;
            result.imageKeys.AddRange(frames);

            return result;
        }

        public int headerLengthValue() {
            int length = 0;
            foreach (byte[] i in new byte[][] { name, timingsNew, framesNew }) {
                int l = ((byte[])i).Length;

                if (l > length) {
                    length = l;
                }
            }

            return length;
        }

        private static double[] getTimings(string data) {
            string[] items = data.Split(',');
            double[] result = new double[items.Length];
            for (int i = 0; i < items.Length; i++) {
                result[i] = Double.Parse(items[i]);
            }
            return result;
        }

        private static int[] getFrames(string data) {
            string[] items = data.Split(',');
            int[] result = new int[items.Length];
            for (int i = 0; i < items.Length; i++) {
                result[i] = Int32.Parse(items[i]);
            }
            return result;
        }

        private byte[] getTimingBytes(BlinkFrame frame) {
            string timings = "";
            timings += frame.MinDelay.ToString() + ",";
            timings += frame.MaxDelay.ToString() + ",";
            timings += frame.Duration.ToString();

            return FileParsing.getStringBytes(timings);
        }

        private byte[] getFramesBytes(int[] frames) {
            string framesString = "";
            foreach (int i in frames) {
                if (framesString != "") {
                    framesString += ",";
                }
                framesString += i.ToString();
            }

            return FileParsing.getStringBytes(framesString);
        }
    }
}