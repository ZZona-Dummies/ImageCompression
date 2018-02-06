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
        private static void Main(string[] args)
        {
            ProgramHandler.CreateImages();

            ImageFormats status = ImageFormats.JPG; //ImageFormats.PNG | ImageFormats.GIF |

            ProgramHandler.GetRawLength();

            foreach (ImageFormats x in Enum.GetValues(typeof(ImageFormats)))
                for (int i = 100; i >= 0; i -= 5)
                    if (status.HasFlag(x))
                        ProgramHandler.GetCompressedLength(x, i, true);

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
        private static Bitmap[] bmp = new Bitmap[2];

        public static void CreateImages()
        {
            IntPtr handle = GetConsoleWindow();

            bmp[0] = printer.CaptureScreenToBitmap();

            Thread.Sleep(100);

            //Aqui tenemos q minimizar la ventana y tomar otra captura y hacer un diff con offset y decir el tamaño q nos hemos ahorrado y volver a maximizar
            ShowWindow(handle, SW_HIDE);

            Thread.Sleep(200);

            bmp[1] = printer.CaptureScreenToBitmap();

            Thread.Sleep(100);

            ShowWindow(handle, SW_SHOW);
        }

        public static void GetRawLength(bool dump = false) //Aqui tengo q hacer las imagenes dentro, tomas las capturas cambiando la visibilidad de la imagen
        { //Maybe is better to have a different file for this
            byte[][] arr = new byte[2][];

            for (byte i = 0; i < bmp.Length; ++i)
                using (MemoryStream ms = bmp[i].ToMemoryStream(ImageFormat.Png, 100, true, "_raw").GetAwaiter().GetResult())
                    arr[i] = ms.GetBuffer();

            Console.WriteLine("Uncompressed image: {0} | {1}", arr[0].Length, arr[1].Length);

            Dictionary<int, List<byte>> diff = arr[0].GetArrDiff(arr[1]);

            if (dump)
                File.WriteAllText(Path.Combine(PathExtensions.AssemblyPath, "raw_diff.txt"), diff.DumpDict(true));

            Console.WriteLine("Uncompressed image (diff): " + diff.CountDict());
        }

        public static void GetCompressedLength(ImageFormats imageFormats, long quality = 100L, bool altConvert = false)
        {
            IEnumerable<byte> firstArr = null;

            int lastC = 0;
            for (byte i = 0; i < bmp.Length; ++i)
                using (MemoryStream ms = bmp[i].GetCompressedBitmap(imageFormats, quality, true, i == 1 ? "_diff" : "").GetAwaiter().GetResult())
                {
                    IEnumerable<byte> arr = ms.ZipWithMemoryStream(Image.FromStream(ms)).GetAwaiter().GetResult();
                    IEnumerable<byte> carr = null;

                    if (i == 1 && altConvert)
                    {
                        bool jpg = true,
                             usingmem = false; //Implementar usingmem para usar lo de más abajo
                        carr = ImageExtensions.SafeCompareBytes(jpg ? (Bitmap)Image.FromStream(ms) : bmp[0], jpg ? (Bitmap)Image.FromStream(bmp[1].GetCompressedBitmap(imageFormats, quality).GetAwaiter().GetResult()) : bmp[1]).ZipBytes().GetAwaiter().GetResult();
                    }

                    //Only compare
                    //using (MemoryStream mss = ImageExtensions.SafeCompare(bmp[0], bmp[1], true).GetCompressedBitmap(imageFormats, quality, true, "_compare").GetAwaiter().GetResult())
                    //    carr = mss.ZipWithMemoryStream(Image.FromStream(mss)).GetAwaiter().GetResult(); //mss.ZipWithMemoryStream(Image.FromStream(mss)).GetAwaiter().GetResult();
                    //ccount = ImageExtensions.CommonBitmap(bmp[0], bmp[1]).SelectMany(x => x).Count() * 3; //carr = ImageExtensions.CommonBitmap(bmp[0], bmp[1]).Zip().GetAwaiter().GetResult();

                    int c = i == 0 ? arr.Count() : arr.GetArrDiff(firstArr).CountDict();
                    Console.WriteLine("Compressed image {0} ({1}%){2}: {3}{4}",
                        imageFormats.ToString(),
                        quality,
                        i > 0 ? string.Format(" (diff | compare) (loss {0}% | {1}%)", (100f - (float)c * 100 / lastC).ToString("F3"), altConvert ? ((100f - (float)carr.Count() * 100 / lastC).ToString("F3")) : "") : "",
                        c,
                        i > 0 ? string.Format(" | {0}", altConvert ? carr.Count() : 0) : ""); // arr.MultisetIntersect(firstArr).Count()

                    if (i == 0) lastC = c;

                    firstArr = arr;
                }
        }
    }
}