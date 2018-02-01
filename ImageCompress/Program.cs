using System;
using System.Drawing;

namespace ImageCompress
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            PrintScreen printer = new PrintScreen();
            Image img = printer.CaptureScreen();

            Console.WriteLine(img.Serialize().Length);
            Console.WriteLine("Compressed image PNG: " + new Bitmap(img).GetCompressedBitmap().Serialize().Length);
            Console.WriteLine("Compressed image PNG (50%): " + new Bitmap(img).GetCompressedBitmap(ImageFormats.PNG, 50).Serialize().Length);
            Console.WriteLine("Compressed image JPG: " + new Bitmap(img).GetCompressedBitmap().Serialize().Length);
            Console.WriteLine("Compressed image JPG (50%): " + new Bitmap(img).GetCompressedBitmap(ImageFormats.JPG, 50).Serialize().Length);
        }
    }
}