using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageCompress
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            StartAsync().GetAwaiter().GetResult();
            Console.Read();
        }

        private static async Task StartAsync()
        {
            PrintScreen printer = new PrintScreen();
            Image img = printer.CaptureScreen();

            Console.WriteLine("Uncompressed image: " + img.ToMemoryStream(ImageFormat.Png, true).GetAwaiter().GetResult().GetBuffer().Length);
            Console.WriteLine("Compressed image PNG: " + (await new Bitmap(img).GetCompressedBitmap(ImageFormats.PNG, 100L, true).FromTask().Zip()).Count());
            Console.WriteLine("Compressed image PNG (50%): " + (await new Bitmap(img).GetCompressedBitmap(ImageFormats.PNG, 50L, true).FromTask().Zip()).Count());
            Console.WriteLine("Compressed image JPG: " + (await new Bitmap(img).GetCompressedBitmap(ImageFormats.JPG, 100L, true).FromTask().Zip()).Count());
            //Console.WriteLine("Compressed image JPG: " + (await new Bitmap(img).GetCompressedBitmap(ImageFormats.JPG).Zip()).Count());
            Console.WriteLine("Compressed image GIF: " + (await new Bitmap(img).GetCompressedBitmap(ImageFormats.GIF, 100L, true).FromTask().Zip()).Count());
            //Console.WriteLine("Compressed image JPG (50%): " + new Bitmap(img).GetCompressedBitmap(ImageFormats.JPG, 50).Serialize().Length);
        }
    }
}