using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace ImageCompress
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            PrintScreen printer = new PrintScreen();
            Image img = printer.CaptureScreen();

            ImageFormats status = ImageFormats.PNG | ImageFormats.GIF | ImageFormats.JPG;

            ProgramHandler.GetRawLength(img);
            foreach (ImageFormats x in Enum.GetValues(typeof(ImageFormats)))
                for (int i = 100; i >= 0; i -= 20)
                    if (status.HasFlag(x))
                        ProgramHandler.GetCompressedLength(img, x, i);

            Console.Read();
        }
    }

    public static class ProgramHandler
    {
        public static void GetRawLength(Image img)
        {
            using (MemoryStream ms = img.ToMemoryStream(ImageFormat.Png, 100, true).GetAwaiter().GetResult())
                Console.WriteLine("Uncompressed image: " + ms.GetBuffer().Length);
        }

        public static void GetCompressedLength(Image img, ImageFormats imageFormats, long quality = 100L)
        {
            //Console.WriteLine("{0} {1}", imageFormats, quality);
            using (MemoryStream ms = new Bitmap(img).GetCompressedBitmap(imageFormats, quality, true).GetAwaiter().GetResult())
                Console.WriteLine("Compressed image {0} ({1}%): " + ms.Zip().GetAwaiter().GetResult().Count(),
                    imageFormats.ToString(),
                    quality);
        }
    }
}