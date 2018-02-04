using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

//using PathIO = System.IO.Path;

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

    public static class ImageExtensions
    {
        private static string AssemblyPath
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
        }

        public static async Task<MemoryStream> GetCompressedBitmap(this Bitmap bmp, ImageFormats imageFormats = ImageFormats.PNG, long quality = 100L, bool outputFile = false) //[0-100]
        {
            MemoryStream mss = new MemoryStream();
            EncoderParameter qualityParam = new EncoderParameter(Encoder.Quality, quality);
            ImageCodecInfo imageCodec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(o => o.FormatID == GetFormatFromEnum(imageFormats).Guid);
            EncoderParameters parameters = new EncoderParameters(1);
            parameters.Param[0] = qualityParam;
            bmp.Save(mss, imageCodec, parameters);

            if (outputFile)
                (await mss.ImageDump(imageFormats, quality)).Dispose();

            return mss;
        }

        public static Image FromTask(this Task<Image> task)
        {
            return task.GetAwaiter().GetResult();
        }

        public static async Task<MemoryStream> ToMemoryStream(this Image image, ImageFormat format, long quality, bool outputFile = false)
        {
            MemoryStream stream = new MemoryStream();
            image.Save(stream, format);
            stream.Position = 0;

            if (outputFile)
                (await stream.ImageDump(GetEnumFromFormat(format), quality)).Dispose();

            return stream;
        }

        public static async Task<FileStream> ImageDump(this MemoryStream mss, ImageFormats imageFormats, long quality)
        {
            string filePath = GetFileString(imageFormats, quality);

            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (FileStream file = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                byte[] buf = mss.GetBuffer();
                await file.WriteAsync(buf, 0, buf.Length);
                return file;
            }
        }

        private static string GetFileString(ImageFormats imageFormats, long quality)
        {
            return string.Format("{0}_{1}.{2}", Path.Combine(AssemblyPath,
                imageFormats.ToString(),
                new DirectoryInfo(AssemblyPath).GetFiles(string.Format("*.{0}", imageFormats.ToString().ToLower()), SearchOption.AllDirectories).Length.ToString("0000")),
                quality,
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
    { //Deberiaa crear una clase especifica para Dictionary<int, List<byte>>, no se si generica
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
                    offset.Add(big[i]);
            }

            ret.ChangeOrAdd(y, offset);

            return ret;
        }
    }

    public static class DictionaryExtensions
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
                {
                    r += arr[x][y] + ", ";
                }
                if (arr[x].Length > 0)
                    r += " }\n";
            }

            return r;
        }

        public static string DumpDict<TKey, TValue>(this Dictionary<TKey, List<TValue>> dictionary)
        {
            string r = "";

            foreach (KeyValuePair<TKey, List<TValue>> kv in dictionary)
            {
                if (kv.Value.Count > 0)
                    r += kv.Key + " => { ";
                foreach (TValue vs in kv.Value)
                    r += vs + (!vs.Equals(kv.Value.Last()) ? ", " : "");
                if (kv.Value.Count > 0)
                    r += " }\n";
            }

            return r;
        }

        public static long CountDict<TKey, TValue>(this Dictionary<TKey, List<TValue>> dictionary)
        { //Esto lo usare para ver la diff total
            if (!typeof(TValue).IsNumericType())
                throw new Exception("Not supported type in CountDict!");

            long l = 0;

            foreach (KeyValuePair<TKey, List<TValue>> kv in dictionary)
                foreach (TValue vs in kv.Value)
                    l += vs;

            return l;
        }
    }

    public static class TypeExtensions
    {
        public static bool IsNumericType<T>(this T o)
        {
            return typeof(T).IsNumericType();
        }

        public static bool IsNumericType(this Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;

                default:
                    return false;
            }
        }
    }
}