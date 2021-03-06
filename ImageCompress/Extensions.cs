﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace ImageCompress
{
    public enum ImageFormats
    {
        None = 2,
        JPG = 4,
        PNG = 8,
        BMP = 16,
        TIFF = 32,
        GIF = 64,
        ICO = 128
    }

    public static class PathExtensions
    {
        public static string AssemblyPath
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
        }
    }

    public static class ImageExtensions
    {
        public static async Task<MemoryStream> GetCompressedBitmap(this Bitmap bmp, ImageFormats imageFormats = ImageFormats.PNG, long quality = 100L, bool outputFile = false, string suffix = "") //[0-100]
        {
            MemoryStream mss = new MemoryStream();
            EncoderParameter qualityParam = new EncoderParameter(Encoder.Quality, quality);
            ImageCodecInfo imageCodec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(o => o.FormatID == GetFormatFromEnum(imageFormats).Guid);
            EncoderParameters parameters = new EncoderParameters(1);
            parameters.Param[0] = qualityParam;
            bmp.Save(mss, imageCodec, parameters);

            if (outputFile)
                (await mss.ImageDump(imageFormats, quality, suffix)).Dispose();

            return mss;
        }

        public static Image FromTask(this Task<Image> task)
        {
            return task.GetAwaiter().GetResult();
        }

        public static async Task<MemoryStream> ToMemoryStream(this Image image, ImageFormat format, long quality, bool outputFile = false, string suffix = "")
        {
            MemoryStream stream = new MemoryStream();
            image.Save(stream, format);
            stream.Position = 0;

            if (outputFile)
                (await stream.ImageDump(GetEnumFromFormat(format), quality, suffix)).Dispose();

            return stream;
        }

        public static async Task<FileStream> ImageDump(this MemoryStream mss, ImageFormats imageFormats, long quality, string suffix)
        {
            string filePath = GetFileString(imageFormats, quality, suffix);

            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (FileStream file = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                byte[] buf = mss.GetBuffer();
                await file.WriteAsync(buf, 0, buf.Length);
                return file;
            }
        }

        public static Bitmap ToFile(this Bitmap map, ImageFormats imageFormats, long quality, string suffix)
        {
            using (MemoryStream ms = ((Image)map).ToMemoryStream(GetFormatFromEnum(imageFormats), quality, true, suffix).GetAwaiter().GetResult())
            {
            }

            return map;
        }

        private static string GetFileString(ImageFormats imageFormats, long quality, string suffix)
        {
            return string.Format("{0}_{1}{2}.{3}", Path.Combine(PathExtensions.AssemblyPath,
                imageFormats.ToString(),
                new DirectoryInfo(PathExtensions.AssemblyPath).GetFiles(string.Format("*.{0}", imageFormats.ToString().ToLower()), SearchOption.AllDirectories).Length.ToString("0000")),
                quality,
                suffix,
                imageFormats.ToString().ToLower());
        }

        private static ImageFormat GetFormatFromEnum(this ImageFormats format)
        {
            switch (format)
            {
                case ImageFormats.JPG:
                    return ImageFormat.Jpeg;

                case ImageFormats.PNG:
                    return ImageFormat.Png;

                case ImageFormats.BMP:
                    return ImageFormat.Bmp;

                case ImageFormats.GIF:
                    return ImageFormat.Gif;

                case ImageFormats.TIFF:
                    return ImageFormat.Tiff;

                case ImageFormats.ICO:
                    return ImageFormat.Icon;
            }
            return null;
        }

        private static ImageFormats GetEnumFromFormat(this ImageFormat format)
        {
            if (format == ImageFormat.Jpeg)
                return ImageFormats.JPG;
            else if (format == ImageFormat.Png)
                return ImageFormats.PNG;
            else if (format == ImageFormat.Gif)
                return ImageFormats.GIF;
            else if (format == ImageFormat.Bmp)
                return ImageFormats.BMP;
            else if (format == ImageFormat.Tiff)
                return ImageFormats.TIFF;
            else if (format == ImageFormat.Icon)
                return ImageFormats.ICO;
            else
                return default(ImageFormats);
        }

        public static LockBitmap SafeCompare(Bitmap bmp1, Bitmap bmp2)
        {
            if ((bmp1 == null) != (bmp2 == null)) throw new Exception("Null bitmap passed!");
            if (bmp1.Size != bmp2.Size) throw new Exception("Different sizes between bitmap A & B!");

            Bitmap ret = bmp2.Clone(new Rectangle(0, 0, bmp2.Width, bmp2.Height), bmp2.PixelFormat);
            LockBitmap lockBitmap1 = new LockBitmap(bmp1),
                       lockBitmap2 = new LockBitmap(ret);

            /*int c = 0,
                fx = 0,
                fy = 0,
                lx = 0,
                ly = 0;*/

            try
            {
                lockBitmap1.LockBits();
                lockBitmap2.LockBits();

                for (int y = 0; y < lockBitmap1.Height; y++)
                    for (int x = 0; x < lockBitmap2.Width; x++)
                        if (lockBitmap1.GetPixel(x, y) == lockBitmap2.GetPixel(x, y))
                        {
                            /*++c;
                            if (c == 1)
                            {
                                fx = x;
                                fy = y;
                            }*/
                            lockBitmap2.SetPixel(x, y, Color.Transparent);
                            //lx = x;
                            //ly = y;
                        }
            }
            finally
            {
                lockBitmap1.UnlockBits();
                lockBitmap2.UnlockBits();
            }

            return lockBitmap2;
        }

        public static IEnumerable<byte> DataMapper(this Bitmap bmp1, Bitmap bmp2)
        {
            if ((bmp1 == null) != (bmp2 == null)) throw new Exception("Null bitmap passed!");
            if (bmp1.Size != bmp2.Size) throw new Exception("Different sizes between bitmap A & B!");

            //Bitmap ret = bmp2.Clone(new Rectangle(0, 0, bmp2.Width, bmp2.Height), bmp2.PixelFormat); //We aren't modifying
            LockBitmap lockBitmap1 = new LockBitmap(bmp1),
                       lockBitmap2 = new LockBitmap(bmp2);

            try
            {
                lockBitmap1.LockBits();
                lockBitmap2.LockBits();

                int cCount = 0,
                    i = 0,
                    w = lockBitmap1.Width,
                    h = lockBitmap1.Height;

                byte rpt = 0;
                //int lastColor = -1;
                byte[] lastColor = new byte[3];
                bool changedColor = false;
                Color bmp1Color = default(Color);

                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        byte r = lockBitmap2.Pixels[i], g = lockBitmap2.Pixels[i + 1], b = lockBitmap2.Pixels[i + 2]; //Nos tenemos asegurar que este es el formato
                        if (changedColor)
                        {
                            //En teoria no necesario
                            //if (ColorExtensions.CompareColors(r, lastColor[0], g, lastColor[1], b, lastColor[2]) < 90) //Si la similitud es menor al 90% entonces ya hay que hacer algo...
                            //{
                            // Get color components count
                            cCount = lockBitmap2.Depth / 8;

                            // Get start index of the specified pixel
                            i = (y * lockBitmap2.RowSize) + (x * cCount);

                            yield return rpt;
                            if (cCount == 3 || cCount == 4) // For 24 bpp get Red, Green and Blue
                            {
                                yield return r;
                                yield return g;
                                yield return b;
                            }
                            else if (cCount == 1)
                                // For 8 bpp get color value (Red, Green and Blue values are the same)
                                yield return r;

                            rpt = 0;
                            /*}
                            else
                            {
                                ++rpt;
                                if (rpt == byte.MaxValue)
                                {
                                    yield return 255;
                                    if (cCount == 3 || cCount == 4) // For 24 bpp get Red, Green and Blue
                                    {
                                        yield return lockBitmap2.Pixels[i];
                                        yield return lockBitmap2.Pixels[i + 1];
                                        yield return lockBitmap2.Pixels[i + 2];
                                    }
                                    else if (cCount == 1)
                                        // For 8 bpp get color value (Red, Green and Blue values are the same)
                                        yield return lockBitmap2.Pixels[i];
                                    rpt = 0;
                                }
                            }*/
                            changedColor = false;
                        }

                        bmp1Color = lockBitmap1.GetPixel(x, y); //Comparando bitmaps
                        if (ColorExtensions.CompareColors(r, bmp1Color.R, g, bmp1Color.G, b, bmp1Color.B) < 90) //Si la similitud es menor al 90% entonces ya hay que hacer algo...
                        {
                            lastColor[0] = r;
                            lastColor[1] = g;
                            lastColor[2] = b;

                            changedColor = true;
                        }
                        else
                        {
                            ++rpt;
                            if (rpt == 255)
                                changedColor = true;
                        }
                    }
            }
            finally
            {
                lockBitmap1.UnlockBits();
                lockBitmap2.UnlockBits();
            }
        }

        public static IEnumerable<byte> GetARGBBytes(this Bitmap bmp)
        {
            if (bmp == null) throw new Exception("Null bitmap passed!");

            //Bitmap ret = bmp2.Clone(new Rectangle(0, 0, bmp2.Width, bmp2.Height), bmp2.PixelFormat); //We aren't modifying
            LockBitmap lockBitmap = new LockBitmap(bmp);

            try
            {
                lockBitmap.LockBits();

                int cCount = 0, i = 0, w = lockBitmap.Width, h = lockBitmap.Height;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        // Get color components count
                        cCount = lockBitmap.Depth / 8;

                        // Get start index of the specified pixel
                        i = (y * lockBitmap.RowSize) + (x * cCount);

                        //there we have a problem because we are losing position references
                        if (cCount == 4) // For 32 bpp get Red, Green, Blue and Alpha
                        {
                            yield return lockBitmap.Pixels[i + 3]; //Alpha first
                            yield return lockBitmap.Pixels[i];
                            yield return lockBitmap.Pixels[i + 1];
                            yield return lockBitmap.Pixels[i + 2];
                        }
                        else if (cCount == 3) // For 24 bpp get Red, Green and Blue
                        {
                            yield return lockBitmap.Pixels[i];
                            yield return lockBitmap.Pixels[i + 1];
                            yield return lockBitmap.Pixels[i + 2];
                        }
                        else if (cCount == 1)
                            // For 8 bpp get color value (Red, Green and Blue values are the same)
                            yield return lockBitmap.Pixels[i];
                    }
            }
            finally
            {
                lockBitmap.UnlockBits();
            }
        }

        public static IEnumerable<byte> SafeCompareBytes(this Bitmap bmp1, Bitmap bmp2)
        {
            if ((bmp1 == null) != (bmp2 == null)) throw new Exception("Null bitmap passed!");
            if (bmp1.Size != bmp2.Size) throw new Exception("Different sizes between bitmap A & B!");

            //Bitmap ret = bmp2.Clone(new Rectangle(0, 0, bmp2.Width, bmp2.Height), bmp2.PixelFormat); //We aren't modifying
            LockBitmap lockBitmap1 = new LockBitmap(bmp1),
                       lockBitmap2 = new LockBitmap(bmp2);

            try
            {
                lockBitmap1.LockBits();
                lockBitmap2.LockBits();

                int cCount = 0, i = 0, w = lockBitmap1.Width, h = lockBitmap1.Height;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        if (lockBitmap1.GetPixel(x, y) != lockBitmap2.GetPixel(x, y))
                        {
                            // Get color components count
                            cCount = lockBitmap2.Depth / 8;

                            // Get start index of the specified pixel
                            i = (y * lockBitmap2.RowSize) + (x * cCount);

                            //there we have a problem because we are losing position references
                            if (cCount == 4) // For 32 bpp get Red, Green, Blue and Alpha
                            {
                                yield return lockBitmap2.Pixels[i + 3]; //Alpha first
                                yield return lockBitmap2.Pixels[i];
                                yield return lockBitmap2.Pixels[i + 1];
                                yield return lockBitmap2.Pixels[i + 2];
                            }
                            else if (cCount == 3) // For 24 bpp get Red, Green and Blue
                            {
                                yield return lockBitmap2.Pixels[i];
                                yield return lockBitmap2.Pixels[i + 1];
                                yield return lockBitmap2.Pixels[i + 2];
                            }
                            else if (cCount == 1)
                                // For 8 bpp get color value (Red, Green and Blue values are the same)
                                yield return lockBitmap2.Pixels[i];
                        }
                        else
                        {
                            if (cCount == 4)
                                yield return 0;
                        }
            }
            finally
            {
                lockBitmap1.UnlockBits();
                lockBitmap2.UnlockBits();
            }
        }

        public static Bitmap SafeCompareTrimmer(Bitmap ret, Rectangle r)
        {
            try
            {
                Bitmap dest = new Bitmap(r.Width, r.Height);
                Rectangle destRect = new Rectangle(0, 0, r.Width, r.Height);
                using (Graphics graphics = Graphics.FromImage(dest))
                    graphics.DrawImage(ret, destRect, r, GraphicsUnit.Pixel);
            }
            catch
            {
                Console.WriteLine("Exception trimming!");
            }

            return ret;
        }

        private static IEnumerable<Color> SolveVertical(Bitmap bmp2, int x, int y)
        {
            yield return bmp2.GetPixel(x, y);
        }
    }

    public static class SerializerExtensions
    {
        /// <summary>
        /// Writes the given object instance to a binary file.
        /// <para>Object type (and all child types) must be decorated with the [Serializable] attribute.</para>
        /// <para>To prevent a variable from being serialized, decorate it with the [NonSerialized] attribute; cannot be applied to properties.</para>
        /// </summary>
        /// <typeparam name="T">The type of object being written to the XML file.</typeparam>
        /// <param name="filePath">The file path to write the object instance to.</param>
        /// <param name="objectToWrite">The object instance to write to the XML file.</param>
        /// <param name="append">If false the file will be overwritten if it already exists. If true the contents will be appended to the file.</param>
        public static byte[] Serialize<T>(this T obj)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, obj);

                return stream.GetBuffer();
            }
        }

        public static byte[] SerializeWithMemoryStream<T>(this T obj, MemoryStream stream)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(stream, obj);

            return stream.GetBuffer();
        }

        /// <summary>
        /// Reads an object instance from a binary file.
        /// </summary>
        /// <typeparam name="T">The type of object to read from the XML.</typeparam>
        /// <param name="filePath">The file path to read the object instance from.</param>
        /// <returns>Returns a new instance of the object read from the binary file.</returns>
        public static async Task<T> _Deserialize<T>(this byte[] arr)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                await stream.WriteAsync(arr, 0, arr.Length);

                return (T)binaryFormatter.Deserialize(stream);
            }
        }

        public static async Task<object> Deserialize(this byte[] arr)
        {
            object obj = await arr._Deserialize<object>();
            return obj;
        }
    }

    public static class CompressionExtensions
    {
        public static async Task<IEnumerable<byte>> Zip(this object obj)
        {
            byte[] bytes = obj.Serialize();

            using (MemoryStream msi = new MemoryStream(bytes))
            using (MemoryStream mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                    await msi.CopyToAsync(gs);

                return mso.ToArray().AsEnumerable();
            }
        }

        public static async Task<IEnumerable<byte>> ZipBytes(this IEnumerable<byte> bytes)
        {
            using (MemoryStream msi = new MemoryStream(bytes.ToArray()))
            using (MemoryStream mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                    await msi.CopyToAsync(gs);

                return mso.ToArray().AsEnumerable();
            }
        }

        public static async Task<IEnumerable<byte>> DeflateCompress(this IEnumerable<byte> data)
        {
            using (MemoryStream output = new MemoryStream())
            {
                using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
                    await dstream.WriteAsync(data.ToArray(), 0, data.Count());
                return output.ToArray().AsEnumerable();
            }
        }

        public static async Task<IEnumerable<byte>> DeflateDecompress(this IEnumerable<byte> data)
        {
            using (MemoryStream input = new MemoryStream(data.ToArray()))
            using (MemoryStream output = new MemoryStream())
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                await dstream.WriteAsync(data.ToArray(), 0, data.Count());
                return output.ToArray().AsEnumerable();
            }
        }

        public static async Task<IEnumerable<byte>> ZipWithMemoryStream(this MemoryStream ms, object obj)
        {
            byte[] bytes = obj.SerializeWithMemoryStream(ms);

            using (MemoryStream msi = new MemoryStream(bytes))
            using (MemoryStream mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                    await msi.CopyToAsync(gs);

                return mso.ToArray().AsEnumerable();
            }
        }

        public static async Task<object> Unzip(this byte[] bytes)
        {
            using (MemoryStream msi = new MemoryStream(bytes))
            using (MemoryStream mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                    await gs.CopyToAsync(mso);

                return mso.ToArray().Deserialize();
            }
        }
    }

    public static class ByteExtensions
    { //Deberia crear una clase especifica para Dictionary<int, List<byte>>, no se si generica
        public static Dictionary<int, List<byte>> GetArrDiff(this IEnumerable<byte> or, IEnumerable<byte> newByte)
        { //Optimize this
            return or.ToArray().GetArrDiff(newByte.ToArray());
        }

        public static Dictionary<int, List<byte>> GetArrDiff(this byte[] or, byte[] newByte)
        {
            int mlen = Math.Max(or.Length, newByte.Length);
            byte[] small = or.Length != mlen ? or : newByte,
                   big = or.Length == mlen ? or : newByte;

            Dictionary<int, List<byte>> ret = new Dictionary<int, List<byte>>();
            List<byte> offset = new List<byte>();

            int y = 0;
            for (int i = 0; i < mlen; ++i) //Esto está mal porque hay q pensar q or.Length != newByte.Length
            {
                if (i < small.Length)
                {
                    if (small[i] != big[i])
                    {
                        if (y == 0) y = i;

                        if (!ret.ContainsKey(y))
                        {
                            List<byte> bb = new List<byte>();
                            bb.Add(newByte[i]);
                            ret.Add(y, bb);
                        }
                        else
                            ret[y].Add(newByte[i]);
                    }
                    else
                    {
                        if (y != 0) y = 0;
                    }
                }
                else
                { //Realmente esto no hace ni falta
                  //if (or.Length < newByte.Length) break;
                  //offset.Add(big[i]);
                    break;
                }
            }

            ret.ChangeOrAdd(y, offset);

            return ret;
        }
    }

    public static class CollectionExtensions
    {
        public static bool ChangeOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        { //True == add; false == change
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, value);
                return true;
            }
            else
            {
                dict[key] = value;
                return false;
            }
        }

        public static IEnumerable<T> MultisetIntersect<T>(this IEnumerable<T> first,
    IEnumerable<T> second)
        {
            // Call the overload with the default comparer.
            return first.MultisetIntersect(second, EqualityComparer<T>.Default);
        }

        public static IEnumerable<T> MultisetIntersect<T>(this IEnumerable<T> first,
            IEnumerable<T> second, IEqualityComparer<T> comparer)
        {
            // Validate parameters.  Do this separately so check
            // is performed immediately, and not when execution
            // takes place.
            if (first == null) throw new ArgumentNullException("first");
            if (second == null) throw new ArgumentNullException("second");
            if (comparer == null) throw new ArgumentNullException("comparer");

            // Defer execution on the internal
            // instance.
            return first.MultisetIntersectImplementation(second, comparer);
        }

        private static IEnumerable<T> MultisetIntersectImplementation<T>(
            this IEnumerable<T> first, IEnumerable<T> second,
            IEqualityComparer<T> comparer)
        {
            // Validate parameters.
            Debug.Assert(first != null);
            Debug.Assert(second != null);
            Debug.Assert(comparer != null);

            // Get the dictionary of the first.
            IDictionary<T, long> counts = first.GroupBy(t => t, comparer).
                ToDictionary(g => g.Key, g => g.LongCount(), comparer);

            // Scan
            foreach (T t in second)
            {
                // The count.
                long count;

                // If the item is found in a.
                if (counts.TryGetValue(t, out count))
                {
                    // This is positive.
                    Debug.Assert(count > 0);

                    // Yield the item.
                    yield return t;

                    // Decrement the count.  If
                    // 0, remove.
                    if (--count == 0) counts.Remove(t);
                }
            }
        }
    }

    public static class DumpExtensions
    {
        public static string DumpArray<T>(this T[][] arr)
        {
            string r = "";

            for (int x = 0; x < arr.GetLength(0); ++x)
            {
                if (arr[x].Length > 0)
                    r += x + " => { ";

                for (int y = 0; y < arr[x].Length; ++y)
                    r += arr[x][y] + ", ";

                if (arr[x].Length > 0)
                    r += " }\n";
            }

            return r;
        }

        public static string DumpDict<TKey, TValue>(this Dictionary<TKey, List<TValue>> dictionary, bool debug = false)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            string r = "";

            int i = 0, j = 0,
                t = dictionary.Values.Sum(x => x.Count);

            if (debug) Console.WriteLine();

            foreach (KeyValuePair<TKey, List<TValue>> kv in dictionary)
            {
                if (kv.Value.Count > 0)
                    r += kv.Key + " => { ";

                int k = kv.Value.Count;

                foreach (TValue vs in kv.Value)
                {
                    ++j;
                    r += vs + (j != k ? ", " : "");

                    if (debug && i % 10000 == 0)
                        Console.WriteLine("{0} => ({1} / {2}): {3} ... {4} s",
                            (i * 100 / (float)t).ToString("F2") + "%",
                            i,
                            t,
                            r.Length,
                            (sw.ElapsedMilliseconds / 1000f).ToString("F2"));

                    if (k == j) j = 0;

                    ++i;
                }
                if (kv.Value.Count > 0)
                    r += " }\n";
            }

            if (debug) Console.WriteLine();

            sw.Stop();

            return r;
        }

        public static int CountDict<TKey, TValue>(this Dictionary<TKey, List<TValue>> dictionary)
        { //Esto lo usare para ver la diff total
            int c = 0;

            foreach (KeyValuePair<TKey, List<TValue>> kv in dictionary)
                foreach (TValue vs in kv.Value)
                    ++c;

            return c;
        }
    }

    public static class ColorExtensions
    {
        public static double CompareColors(Color a, Color b)
        {
            return 100.0 * (
                1.0 - (
                    Math.Abs(a.R - b.R) +
                    Math.Abs(a.G - b.G) +
                    Math.Abs(a.B - b.B)
                ) / (256.0 * 3)
            );
        }

        public static double CompareColors(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
        {
            return 100.0 * (
                1.0 - (
                    Math.Abs(r1 - r2) +
                    Math.Abs(g1 - g2) +
                    Math.Abs(b1 - b2)
                ) / (256.0 * 3)
            );
        }
    }

    public class ColorData
    {
        public int x, y;
        public Color c;

        public ColorData(int x, int y, Color c)
        {
            this.x = x;
            this.y = y;
            this.c = c;
        }
    }
}