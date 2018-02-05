using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace ImageCompress
{
    internal class Program
    {
        //private static byte[] arr1 = { 2, 3, 4, 2, 3, 3, 1, 0, 0, 1, 1 },
        //                      arr2 = { 2, 3, 4, 5, 3, 3, 1, 6, 7, 1, 1 };

        private static void Main(string[] args)
        {
            //Console.WriteLine(arr1.GetArrDiff(arr2));

            ProgramHandler.CreateImages();

            ImageFormats status = ImageFormats.JPG; //ImageFormats.PNG | ImageFormats.GIF |

            ProgramHandler.GetRawLength();

            foreach (ImageFormats x in Enum.GetValues(typeof(ImageFormats)))
                for (int i = 100; i >= 0; i -= 5)
                    if (status.HasFlag(x))
                        ProgramHandler.GetCompressedLength(x, i);

            Console.Read();
        }
    }

    public static class ProgramHandler
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        private static PrintScreen printer = new PrintScreen();
        private static Image[] img = new Image[2];

        public static void CreateImages()
        {
            IntPtr handle = GetConsoleWindow();

            img[0] = printer.CaptureScreen();

            Thread.Sleep(1500);

            //Aqui tenemos q minimizar la ventana y tomar otra captura y hacer un diff con offset y decir el tamaño q nos hemos ahorrado y volver a maximizar
            ShowWindow(handle, SW_HIDE);

            Thread.Sleep(100);

            img[1] = printer.CaptureScreen();

            Thread.Sleep(1500);

            ShowWindow(handle, SW_SHOW);
        }

        public static void GetRawLength(bool dump = false) //Aqui tengo q hacer las imagenes dentro, tomas las capturas cambiando la visibilidad de la imagen
        { //Maybe is better to have a different file for this
            byte[][] arr = new byte[2][];

            for (byte i = 0; i < img.Length; ++i)
                using (MemoryStream ms = img[i].ToMemoryStream(ImageFormat.Png, 100, true, i == 1).GetAwaiter().GetResult())
                    arr[i] = ms.GetBuffer();

            Console.WriteLine("Uncompressed image: {0} | {1}", arr[0].Length, arr[1].Length);

            Dictionary<int, List<byte>> diff = arr[0].GetArrDiff(arr[1]);

            if (dump)
                File.WriteAllText(Path.Combine(PathExtensions.AssemblyPath, "raw_diff.txt"), diff.DumpDict(true));

            Console.WriteLine("Uncompressed image (diff): " + diff.CountDict());
        }

        public static void GetCompressedLength(ImageFormats imageFormats, long quality = 100L)
        {
            IEnumerable<byte> firstArr = null;

            int lastC = 0;
            for (byte i = 0; i < img.Length; ++i)
                using (MemoryStream ms = new Bitmap(img[i]).GetCompressedBitmap(imageFormats, quality, true, i == 1).GetAwaiter().GetResult())
                {
                    IEnumerable<byte> arr = ms.Zip().GetAwaiter().GetResult();

                    int c = i == 0 ? arr.Count() : arr.GetArrDiff(firstArr).CountDict();
                    Console.WriteLine("Compressed image {0} ({1}%){2}: {3}{4}",
                        imageFormats.ToString(),
                        quality,
                        i > 0 ? string.Format(" (diff | except) (loss {0}%)", (100f - (float)c * 100 / lastC).ToString("F3")) : "",
                        c,
                        i > 0 ? string.Format(" | {0}", arr.MultisetIntersect(firstArr).Count()) : "");

                    if (i == 0) lastC = c;

                    firstArr = arr;
                }
        }
    }
}