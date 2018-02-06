using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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

        public static IEnumerable<IEnumerable<ColorData>> CommonBitmap(Bitmap bmp1, Bitmap bmp2, float sim = 95)
        { //La cuestion esq aqui se puede hacer secuencia y teniendo nada mas q la anchura se puede hacer el resto, creo... ???
            int mwidth = Math.Max(bmp1.Width, bmp2.Width),
                mheight = Math.Max(bmp1.Height, bmp2.Height);

            Bitmap bigmap = bmp1.Width == mwidth ? bmp1 : bmp2,
                   smallmap = bmp1.Width != mwidth ? bmp1 : bmp2;

            //Color[,] compare = new Color[mwidth, mheight];

            //Not neccesary ???
            int lx = 0,
                ly = 0,
                fx = 0,
                fy = 0,
                c = 0;

            // ??? Aqui lo que tengo q hacer es juntar los centros de ambos bitmaps
            for (int x = 0; x < mwidth; ++x)
            { // ??? De esto tengo ya 2 casos que no se que hacer muy bien
                if (x > smallmap.Width) continue;
                yield return VerticalMap(bmp1, bmp2, smallmap, bigmap, x, mheight);
            }

            //Bitmap ret = new Bitmap(lx - fx, ly - fy);

            //for (int xx = 0; xx < lx - fx; ++xx)
            //    for (int yy = 0; yy < ly - fy; ++yy)
            //        ret.SetPixel(xx, yy, compare[xx, yy]);

            //return ret;
        }

        private static IEnumerable<ColorData> VerticalMap(Bitmap bmp1, Bitmap bmp2, Bitmap smallmap, Bitmap bigmap, int x, int mheight) // ref int c, ref int fx, ref int fy, ref int lx, ref int ly)
        {
            for (int y = 0; y < mheight; ++y)
            {
                if (y > smallmap.Height) continue; //Aqui tengo q skipear

                //if (ColorExtensions.CompareColors(bigmap.GetPixel(x, y), smallmap.GetPixel(x, y)) > sim)
                if (bigmap.GetPixel(x, y) != smallmap.GetPixel(x, y))
                {
                    /*++c;
                    if (c == 1)
                    {
                        fx = x;
                        fy = y;
                    }*/
                    // Y en el small map hacer la cuenta
                    //lx = x;
                    //ly = y;
                    //return SolveVertical(bmp2, x, y); // Y aqui segun si el bmp2 es el grande o el chico hay q obtener el pixel de una u otra forma
                    yield return new ColorData(x, y, bmp2.GetPixel(x, y));
                }
            }
        }

        public static Bitmap SafeCompare(Bitmap bmp1, Bitmap bmp2)
        {
            if ((bmp1 == null) != (bmp2 == null)) throw new Exception("Null bitmap passed!");
            if (bmp1.Size != bmp2.Size) throw new Exception("Different sizes between bitmap A & B!");

            LockBitmap lockBitmap1 = new LockBitmap(bmp1),
                       lockBitmap2 = new LockBitmap(bmp2);
            try
            {
                lockBitmap1.LockBits();
                lockBitmap2.LockBits();

                Color compareClr = Color.FromArgb(255, 255, 255, 255);
                for (int y = 0; y < lockBitmap1.Height; y++)
                    for (int x = 0; x < lockBitmap2.Width; x++)
                        if (lockBitmap1.GetPixel(x, y) == lockBitmap2.GetPixel(x, y))
                            lockBitmap2.SetPixel(x, y, Color.Transparent);
            }
            finally
            {
                lockBitmap1.UnlockBits();
                lockBitmap2.UnlockBits();
            }

            return bmp2;
        }

        /*public static Bitmap UnsafeCompare(Bitmap bitmapA, Bitmap bitmapB, int height)
        {
            if ((bitmapA == null) != (bitmapB == null)) throw new Exception("Null bitmap passed!");
            if (bitmapA.Size != bitmapB.Size) throw new Exception("Different sizes between bitmap A & B!");

            Rectangle bounds = new Rectangle(0, 0, bitmapA.Width, bitmapA.Height);
            BitmapData bmpDataA = bitmapA.LockBits(bounds, ImageLockMode.ReadWrite, bitmapA.PixelFormat),
                       bmpDataB = bitmapB.LockBits(bounds, ImageLockMode.ReadWrite, bitmapB.PixelFormat);

            int npixels = Math.Abs(height * bmpDataA.Stride) / 4, i = 0;
            unsafe
            {
                int* pPixelsA = (int*)bmpDataA.Scan0.ToPointer();
                int* pPixelsB = (int*)bmpDataB.Scan0.ToPointer();

                for (; i < npixels; ++i)
                    if (pPixelsA[i] == pPixelsB[i])
                        pPixelsB[i] = Color.Transparent.ToArgb();
            }
            bitmapA.UnlockBits(bmpDataA);
            bitmapB.UnlockBits(bmpDataB);

            Console.WriteLine("I: {0}; N: {1}", i, npixels);

            return bitmapB;
        }*/

        public static Bitmap TrimBitmap(this Bitmap source)
        {
            Rectangle srcRect = default(Rectangle);
            BitmapData data = null;
            try
            {
                data = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                byte[] buffer = new byte[data.Height * data.Stride];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
                int xMin = int.MaxValue;
                int xMax = 0;
                int yMin = int.MaxValue;
                int yMax = 0;
                for (int y = 0; y < data.Height; y++)
                    for (int x = 0; x < data.Width; x++)
                    {
                        byte alpha = buffer[y * data.Stride + 4 * x + 3];
                        if (alpha != 0)
                        {
                            if (x < xMin) xMin = x;
                            if (x > xMax) xMax = x;
                            if (y < yMin) yMin = y;
                            if (y > yMax) yMax = y;
                        }
                    }

                if (xMax < xMin || yMax < yMin)
                {
                    // Image is empty...
                    return null;
                }
                srcRect = Rectangle.FromLTRB(xMin, yMin, xMax, yMax);
            }
            finally
            {
                if (data != null)
                    source.UnlockBits(data);
            }

            Bitmap dest = new Bitmap(srcRect.Width, srcRect.Height);
            Rectangle destRect = new Rectangle(0, 0, srcRect.Width, srcRect.Height);
            using (Graphics graphics = Graphics.FromImage(dest))
            {
                graphics.DrawImage(source, destRect, srcRect, GraphicsUnit.Pixel);
            }
            return dest;
        }

        public static Bitmap Crop(this Bitmap bmp)
        {
            int w = bmp.Width;
            int h = bmp.Height;

            Func<int, bool> allWhiteRow = row =>
            {
                for (int i = 0; i < w; ++i)
                    if (bmp.GetPixel(i, row).A == 0)
                        return false;
                return true;
            };

            Func<int, bool> allWhiteColumn = col =>
            {
                for (int i = 0; i < h; ++i)
                    if (bmp.GetPixel(col, i).A == 0)
                        return false;
                return true;
            };

            int topmost = 0;
            for (int row = 0; row < h; ++row)
            {
                if (allWhiteRow(row))
                    topmost = row;
                else break;
            }

            int bottommost = 0;
            for (int row = h - 1; row >= 0; --row)
            {
                if (allWhiteRow(row))
                    bottommost = row;
                else break;
            }

            int leftmost = 0, rightmost = 0;
            for (int col = 0; col < w; ++col)
            {
                if (allWhiteColumn(col))
                    leftmost = col;
                else
                    break;
            }

            for (int col = w - 1; col >= 0; --col)
            {
                if (allWhiteColumn(col))
                    rightmost = col;
                else
                    break;
            }

            if (rightmost == 0) rightmost = w; // As reached left
            if (bottommost == 0) bottommost = h; // As reached top.

            int croppedWidth = rightmost - leftmost;
            int croppedHeight = bottommost - topmost;

            if (croppedWidth == 0) // No border on left or right
            {
                leftmost = 0;
                croppedWidth = w;
            }

            if (croppedHeight == 0) // No border on top or bottom
            {
                topmost = 0;
                croppedHeight = h;
            }

            try
            {
                var target = new Bitmap(croppedWidth, croppedHeight);
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(bmp,
                      new RectangleF(0, 0, croppedWidth, croppedHeight),
                      new RectangleF(leftmost, topmost, croppedWidth, croppedHeight),
                      GraphicsUnit.Pixel);
                }
                return target;
            }
            catch (Exception ex)
            {
                throw new Exception(
                  string.Format("Values are topmost={0} btm={1} left={2} right={3} croppedWidth={4} croppedHeight={5}", topmost, bottommost, leftmost, rightmost, croppedWidth, croppedHeight),
                  ex);
            }
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
        public static int CompareColors(Color a, Color b)
        {
            return 100 * (int)(
                1.0 - (
                    Math.Abs(a.R - b.R) +
                    Math.Abs(a.G - b.G) +
                    Math.Abs(a.B - b.B)
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