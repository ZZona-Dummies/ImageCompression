using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ImageCompress
{
    internal class Program
    {
        private static byte[] arr1 = { 2, 3, 4, 2, 3, 3, 1, 0, 0, 1, 1 },
                              arr2 = { 2, 3, 4, 5, 3, 3, 1, 6, 7, 1, 1 };

        private static void Main(string[] args)
        {
            Console.WriteLine(arr1.GetArrDiff(arr2));

            /*PrintScreen printer = new PrintScreen();
            Image img = printer.CaptureScreen();

            ImageFormats status = ImageFormats.JPG; //ImageFormats.PNG | ImageFormats.GIF |

            ProgramHandler.GetRawLength(img);
            foreach (ImageFormats x in Enum.GetValues(typeof(ImageFormats)))
                for (int i = 100; i >= 0; i -= 5)
                    if (status.HasFlag(x))
                        ProgramHandler.GetCompressedLength(img, x, i);*/

            Console.Read();
        }
    }

    public static class ProgramHandler
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ChangeWindowVisibility(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        private static void CommonCheck(byte[] arr1, byte[] arr2)
        {
            IntPtr handle = GetConsoleWindow();

            //Aqui tenemos q minimizar la ventana y tomar otra captura y hacer un diff con offset y decir el tamaño q nos hemos ahorrado y volver a maximizar
            ChangeWindowVisibility(handle, SW_HIDE);

            ChangeWindowVisibility(handle, SW_SHOW);
        }

        public static void GetRawLength(Image img1, Image img2)
        { //Maybe is better to have a different file for this
            byte[] arr1, arr2;
            using (MemoryStream ms = img1.ToMemoryStream(ImageFormat.Png, 100, true).GetAwaiter().GetResult())
            {
                arr1 = ms.GetBuffer();
                Console.WriteLine("Uncompressed image: " + arr1.Length);
            }

            using (MemoryStream ms = img2.ToMemoryStream(ImageFormat.Png, 100, true).GetAwaiter().GetResult())
                arr2 = ms.GetBuffer();

            CommonCheck(arr1, arr2);

            Console.WriteLine("Uncompressed image (diff): " + arr1.Length);
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