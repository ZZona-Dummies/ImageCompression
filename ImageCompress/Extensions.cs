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
        None,
        JPG,
        PNG,
        BMP,
        TIFF,
        GIF,
        ICO
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

        public static async Task<Image> GetCompressedBitmap(this Bitmap bmp, ImageFormats imageFormats = ImageFormats.PNG, long quality = 100L, bool outputFile = false) //[0-100]
        {
            using (MemoryStream mss = new MemoryStream())
            {
                EncoderParameter qualityParam = new EncoderParameter(Encoder.Quality, quality);
                ImageCodecInfo imageCodec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(o => o.FormatID == GetFormatFromEnum(imageFormats).Guid);
                EncoderParameters parameters = new EncoderParameters(1);
                parameters.Param[0] = qualityParam;
                bmp.Save(mss, imageCodec, parameters);

                if (outputFile)
                    await mss.ImageDump(imageFormats);

                return Image.FromStream(mss);
            }
        }

        public static async Task<MemoryStream> ToMemoryStream(this Image image, ImageFormat format, bool outputFile = false)
        {
            MemoryStream stream = new MemoryStream();
            image.Save(stream, format);
            stream.Position = 0;

            if (outputFile)
                await stream.ImageDump(GetEnumFromFormat(format));

            return stream;
        }

        public static async Task<FileStream> ImageDump(this MemoryStream mss, ImageFormats imageFormats)
        {
            using (FileStream file = new FileStream(GetFileString(imageFormats), FileMode.OpenOrCreate, FileAccess.Read))
            {
                await file.CopyToAsync(mss);
                return file;
            }
        }

        private static string GetFileString(ImageFormats imageFormats)
        {
            return string.Format("{0}.{1}", Path.Combine(AssemblyPath,
                new DirectoryInfo(AssemblyPath).GetFiles(string.Format("*.{0}", imageFormats.ToString().ToLower()), SearchOption.AllDirectories).Length.ToString("0000")),
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

        public static async Task<object> Unzip(this byte[] bytes)
        {
            using (MemoryStream msi = new MemoryStream(bytes))
            using (MemoryStream mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    //gs.CopyTo(mso);
                    await gs.CopyToAsync(mso);
                }

                return mso.ToArray().Deserialize();
            }
        }
    }
}