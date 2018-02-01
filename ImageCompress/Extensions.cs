using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace ImageCompress
{
    public enum ImageFormats
    {
        JPG,
        PNG,
        BMP,
        TIFF,
        GIF,
        ICO
    }

    public static class ImageExtensions
    {
        public static Image GetCompressedBitmap(this Bitmap bmp, ImageFormats imageFormats = ImageFormats.PNG, long quality = 100L) //[0-100]
        {
            using (MemoryStream mss = new MemoryStream())
            {
                EncoderParameter qualityParam = new EncoderParameter(Encoder.Quality, quality);
                ImageCodecInfo imageCodec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(o => o.FormatID == GetFormatGuidFromEnum(imageFormats));
                EncoderParameters parameters = new EncoderParameters(1);
                parameters.Param[0] = qualityParam;
                bmp.Save(mss, imageCodec, parameters);
                return Image.FromStream(mss);
            }
        }

        private static Guid GetFormatGuidFromEnum(ImageFormats format)
        {
            switch (format)
            {
                case ImageFormats.JPG:
                    return ImageFormat.Jpeg.Guid;

                case ImageFormats.PNG:
                    return ImageFormat.Png.Guid;

                case ImageFormats.BMP:
                    return ImageFormat.Bmp.Guid;

                case ImageFormats.GIF:
                    return ImageFormat.Gif.Guid;

                case ImageFormats.TIFF:
                    return ImageFormat.Tiff.Guid;

                case ImageFormats.ICO:
                    return ImageFormat.Icon.Guid;
            }
            return default(Guid);
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
        public static byte[] Serialize<T>(this T objectToWrite)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(stream, objectToWrite);

                return stream.GetBuffer();
            }
        }

        /// <summary>
        /// Reads an object instance from a binary file.
        /// </summary>
        /// <typeparam name="T">The type of object to read from the XML.</typeparam>
        /// <param name="filePath">The file path to read the object instance from.</param>
        /// <returns>Returns a new instance of the object read from the binary file.</returns>
        public static T ReadFromBinaryFile<T>(byte[] arr)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                stream.Write(arr, 0, arr.Length);

                return (T)binaryFormatter.Deserialize(stream);
            }
        }
    }
}