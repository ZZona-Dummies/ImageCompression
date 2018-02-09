using ImageCompress.Properties;
using LZ4;
using SevenZip.Compression.LZMA;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ImageCompress
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ProgramHandler.CreateImages(true);

            ImageFormats status = ImageFormats.JPG; //ImageFormats.PNG | ImageFormats.GIF |

            ProgramHandler.GetRawLength();

            bool onlyOne = true;

            if (!onlyOne)
            {
                foreach (ImageFormats x in Enum.GetValues(typeof(ImageFormats)))
                    for (int i = 100; i >= 0; i -= 5)
                        if (status.HasFlag(x))
                            ProgramHandler.GetCompressedLength(x, i, true);
            }
            else
                foreach (ImageFormats x in Enum.GetValues(typeof(ImageFormats)))
                    if (status.HasFlag(x))
                        ProgramHandler.GetCompressedLength(x, 50, true);

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

        private static int lastC = 0, orCount;
        private static Stopwatch sw = new Stopwatch();

        private static float diffellapsed,
                            zipbytesellapsed,
                            deflateellapsed,
                            lzmaellapsed,
                            sharpellapsed,
                            lz4ellapsed,
                            zstdellapsed,
                            compareellapsed,
                            //commonellapsed,
                            mapperellapsed,
                            mapperjpgellapsed;

        private static long LastEllapsed;

        private static Dictionary<string, float> ratios = new Dictionary<string, float>();

        private static List<Tuple<byte, int, float, float, float, float>> graphData = new List<Tuple<byte, int, float, float, float, float>>(); //I will implement this later

        private const byte algoritms = 9;
        private static string[] algCaptions = new string[algoritms] { "Diff", "ZipBytes", "Deflate", "LZMA", "SharpZipLib", "LZ4", "Compare", "Common", "Mapper" };
        private static int algCount = 0;

        private static IEnumerable<byte> pngMapper, bmpMapper;

        private static long CurrentEllapsed
        {
            get
            {
                long cur = sw.ElapsedTicks;
                LastEllapsed = cur;
                return LastEllapsed;
            }
        }

        public static void CreateImages(bool useResources = false)
        {
            if (!useResources)
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
            else
            {
                bmp[0] = Resources.cap1;
                bmp[1] = Resources.cap2;
            }
        }

        //bool dump = false
        public static void GetRawLength() //Aqui tengo q hacer las imagenes dentro, tomas las capturas cambiando la visibilidad de la imagen
        { //Maybe is better to have a different file for this
            byte w = 0;
            byte[][] arr = new byte[4][];
            Bitmap[] maps = new Bitmap[4];

            for (byte i = 0; i < bmp.Length; ++i)
                using (MemoryStream ms = bmp[i].ToMemoryStream(ImageFormat.Bmp, 100).GetAwaiter().GetResult())
                { //Este no lo voy a dumpear en HDD xD
                    maps[w] = (Bitmap)Image.FromStream(ms);
                    ++w;
                    arr[i] = ms.GetBuffer();
                }

            for (byte i = 0; i < bmp.Length; ++i)
                using (MemoryStream ms = bmp[i].ToMemoryStream(ImageFormat.Png, 100, true, "_raw").GetAwaiter().GetResult())
                {
                    maps[w] = (Bitmap)Image.FromStream(ms);
                    arr[w] = ms.GetBuffer();
                    ++w;
                }

            Console.WriteLine("Uncompressed image (BMP): {0} | {1}", arr[0].Length, arr[1].Length);
            Console.WriteLine("Uncompressed image (PNG): {0} | {1}", arr[2].Length, arr[3].Length);

            //Dictionary<int, List<byte>> diff = arr[0].GetArrDiff(arr[1]);
            //Aqui voy a implementar un runlength encoding y pasarlo a un ienumerable para sustituir valores
            // O bien, pasarle 4 bitmaps y hacerle un mapper y ver que devuelve

            //if (dump)
            //    File.WriteAllText(Path.Combine(PathExtensions.AssemblyPath, "raw_diff.txt"), diff.DumpDict(true));

            bmpMapper = ImageExtensions.DataMapper(maps[0], maps[1]);
            pngMapper = ImageExtensions.DataMapper(maps[2], maps[3]);

            Console.WriteLine("Uncompressed image (mapper) (BMP): " + bmpMapper.Count());
            Console.WriteLine("Uncompressed image (mapper) (PNG): " + pngMapper.Count());

            GC.Collect();
        }

        public static void GetCompressedLength(ImageFormats imageFormats, long quality = 100L, bool altConvert = false)
        {
            IEnumerable<byte> firstArr = null;
            byte[] lastzstd = null;

            Console.WriteLine();

            for (byte i = 0; i < bmp.Length; ++i)
            {
                sw.Start();
                using (MemoryStream ms = bmp[i].GetCompressedBitmap(imageFormats, quality, true, i == 1 ? "_diff" : "").GetAwaiter().GetResult())
                {
                    Console.WriteLine("GetCompressedBitmap: {0} s (Loop #{1})", (sw.ElapsedMilliseconds / 1000f).ToString("F3"), i);

                    sw.Stop();
                    sw.Reset();
                    sw.Start();

                    IEnumerable<byte> arr = ms.ZipWithMemoryStream(Image.FromStream(ms)).GetAwaiter().GetResult(),
                                      zipbytes = null,
                                      deflate = null,
                                      lzma = null,
                                      sharp = null,
                                      lz4 = null,
                                      zstd = null,
                                      //compare = null,
                                      //common = null,
                                      mapper = null,
                                      mapperjpg = null;

                    Console.WriteLine("ZipWithMemoryStream: {0} s (Loop #{1})", (sw.ElapsedMilliseconds / 1000f).ToString("F3"), i);
                    Console.WriteLine();

                    sw.Stop();
                    sw.Reset();

                    int diff = 0;

                    if (i == 1 && altConvert)
                    {
                        bool jpg = false;
                        //usingmem = false; //Implementar usingmem para usar lo de más abajo

                        //Esto no hace nada, es incluso mejor manejar directamente un array del uncrompressed.
                        //IEnumerable<byte> or = ImageExtensions.SafeCompareBytes(jpg ? (Bitmap)Image.FromStream(ms) : bmp[0], jpg ? (Bitmap)Image.FromStream(bmp[1].GetCompressedBitmap(imageFormats, quality).GetAwaiter().GetResult()) : bmp[1]);

                        sw.Start();

                        diff = arr.GetArrDiff(firstArr).CountDict();

                        diffellapsed = GetEllapsedTime();

                        Task<IEnumerable<byte>> zr = pngMapper.ZipBytes();

                        zipbytesellapsed = GetEllapsedTime();

                        zipbytes = zr.GetAwaiter().GetResult();

                        Console.WriteLine("Ellapsed ZipBytes GetAwaiter time: {0} ms\n", GetEllapsedTime(3));

                        deflate = pngMapper.DeflateCompress().GetAwaiter().GetResult();

                        deflateellapsed = GetEllapsedTime();

                        lzma = pngMapper.Compress();

                        lzmaellapsed = GetEllapsedTime();

                        sharp = pngMapper.CreateToMemoryStream("sharp", quality, true);

                        sharpellapsed = GetEllapsedTime();

                        //byte[] codeclz4 = null;

                        orCount = pngMapper.Count();

                        Console.WriteLine("Ellapsed PNGMapper count time: {0} ms", GetEllapsedTime(3));

                        byte[] orarr = pngMapper.ToArray();

                        Console.WriteLine("\nEllapsed PNGMapper to arr time: {0} ms\n", GetEllapsedTime(3));

                        lz4 = LZ4Codec.Encode(orarr, 0, orCount); //Not efficient
                        //LZ4Codec.Wrap(or.ToArray(), 0, or.Count()).AsEnumerable();

                        lz4ellapsed = GetEllapsedTime();

                        Console.WriteLine("Ellapsed ZSTD to arr time: {0} ms\n", GetEllapsedTime(3));

                        byte[] zstddict = lastzstd == null ? bmp[0].GetARGBBytes().ToArray() : lastzstd;

                        Console.WriteLine("Ellapsed ZSTD dict gen time: {0} ms\n", GetEllapsedTime(3));

                        byte[] azstd = ZStdHelper.Compress(orarr, zstddict, GetLevelFromQuality(quality, 22, true));
                        lastzstd = azstd;

                        zstd = azstd;

                        zstdellapsed = GetEllapsedTime();

                        //compare = ImageExtensions.SafeCompareBytes(bmp[0], bmp[1]); //mss.ZipWithMemoryStream(Image.FromStream(mss)).GetAwaiter().GetResult();

                        //compareellapsed = GetEllapsedTime();

                        mapper = ImageExtensions.DataMapper(bmp[0], bmp[1]); //carr = ImageExtensions.CommonBitmap(bmp[0], bmp[1]).Zip().GetAwaiter().GetResult();

                        mapperellapsed = GetEllapsedTime();

                        Bitmap bmpjpg1 = null, bmpjpg2 = null;

                        using (MemoryStream ms1 = bmp[0].GetCompressedBitmap(imageFormats, quality).GetAwaiter().GetResult())
                            bmpjpg1 = (Bitmap)Image.FromStream(ms1);

                        using (MemoryStream ms2 = bmp[1].GetCompressedBitmap(imageFormats, quality).GetAwaiter().GetResult())
                            bmpjpg2 = (Bitmap)Image.FromStream(ms2);

                        mapperjpg = ImageExtensions.DataMapper(bmpjpg1, bmpjpg2);

                        mapperjpgellapsed = GetEllapsedTime();

                        sw.Stop();
                        sw.Reset();
                        ResetEllapsedTime();

                        GC.Collect();
                    }

                    //Only compare
                    //using (MemoryStream mss = ImageExtensions.SafeCompare(bmp[0], bmp[1], true).GetCompressedBitmap(imageFormats, quality, true, "_compare").GetAwaiter().GetResult())
                    //    carr = mss.ZipWithMemoryStream(Image.FromStream(mss)).GetAwaiter().GetResult(); //mss.ZipWithMemoryStream(Image.FromStream(mss)).GetAwaiter().GetResult();
                    //ccount = ImageExtensions.CommonBitmap(bmp[0], bmp[1]).SelectMany(x => x).Count() * 3; //carr = ImageExtensions.CommonBitmap(bmp[0], bmp[1]).Zip().GetAwaiter().GetResult();

                    int c = i == 0 ? arr.Count() : 0; // arr.GetArrDiff(firstArr).CountDict();

                    if (i == 1)
                    {
                        int zipbytescount = zipbytes.Count(),
                            deflatecount = deflate.Count(),
                            lzmacount = lzma.Count(),
                            sharpcount = sharp.Count(),
                            lz4count = lz4.Count(),
                            zstdcount = zstd.Count(),
                            //comparecount = compare.Count(),
                            //commoncount = common.Count(),
                            mappercount = mapper.Count(),
                            mapperjpgcount = mapperjpg.Count();

                        float jpgRatio = lastC * 100f / orCount;
                        Console.WriteLine("PNG Length: " + orCount + " => " + (orCount / 1024f / 1024f).ToString("F3") + " MB");
                        Console.WriteLine("JPG Length: " + lastC + " => " + (lastC / 1024f / 1024f).ToString("F3") + " MB");
                        Console.WriteLine("JPG Working Ratio: " + jpgRatio.ToString("F3") + " %");
                        Console.WriteLine();

                        Console.WriteLine("Compressed image {0} format with {1}% quality", imageFormats, quality);
                        Console.WriteLine("   Diff        => Size: {0} / {4}; Ratio: {1}; Ellapsed: {2}; Transfer Rate: {3}", diff, GetLossPercentage(diff, orCount), GetEllapsedString(diffellapsed), GetBytesRate(diffellapsed), lastC);
                        Console.WriteLine("   ZipBytes    => Size: {0} / {4}; Ratio: {1}; Ellapsed: {2}; Transfer Rate: {3}", zipbytescount, GetLossPercentage(zipbytescount, orCount), GetEllapsedString(zipbytesellapsed), GetBytesRate(zipbytesellapsed), lastC);
                        Console.WriteLine("   Deflate     => Size: {0} / {4}; Ratio: {1}; Ellapsed: {2}; Transfer Rate: {3}", deflatecount, GetLossPercentage(deflatecount, orCount), GetEllapsedString(deflateellapsed), GetBytesRate(deflateellapsed), lastC);
                        Console.WriteLine("   LZMA        => Size: {0} / {4}; Ratio: {1}; Ellapsed: {2}; Transfer Rate: {3}", lzmacount, GetLossPercentage(lzmacount, orCount), GetEllapsedString(lzmaellapsed), GetBytesRate(lzmaellapsed), lastC);
                        Console.WriteLine("   SHARP       => Size: {0} / {4}; Ratio: {1}; Ellapsed: {2}; Transfer Rate: {3}", sharpcount, GetLossPercentage(sharpcount, orCount), GetEllapsedString(sharpellapsed), GetBytesRate(sharpellapsed), lastC);
                        Console.WriteLine("   LZ4         => Size: {0} / {4}; Ratio: {1}; Ellapsed: {2}; Transfer Rate: {3}", lz4count, GetLossPercentage(lz4count, orCount), GetEllapsedString(lz4ellapsed), GetBytesRate(lz4ellapsed), lastC);
                        Console.WriteLine("   ZSTD        => Size: {0} / {4}; Ratio: {1}; Ellapsed: {2}; Transfer Rate: {3}", zstdcount, GetLossPercentage(zstdcount, orCount), GetEllapsedString(zstdellapsed), GetBytesRate(zstdellapsed), lastC);
                        //Console.WriteLine("   Compare  => Size: {0} / {4}; Ratio: {1}; Ellapsed: {2}; Transfer Rate: {3}", comparecount, GetLossPercentage(comparecount, orCount), GetEllapsedString(compareellapsed), GetBytesRate(compareellapsed), lastC);
                        //Console.WriteLine("   Common   => Size: {0} / {4}; Ratio: {1}; Ellapsed: {2}; Transfer Rate: {3}", commoncount, GetLossPercentage(commoncount, orCount), GetEllapsedString(commonellapsed), GetBytesRate(commonellapsed), lastC);
                        Console.WriteLine("   Mapper       => Size: {0} / {4}; Ratio: {1}; Ellapsed: {2}; Transfer Rate: {3}", mappercount, GetLossPercentage(mappercount, orCount), GetEllapsedString(mapperellapsed), GetBytesRate(mapperellapsed), lastC);
                        Console.WriteLine("   Mapper + JPG => Size: {0} / {4}; Ratio: {1}; Ellapsed: {2}; Transfer Rate: {3}", mapperjpgcount, GetLossPercentage(mapperjpgcount, orCount), GetEllapsedString(mapperjpgellapsed), GetBytesRate(mapperjpgellapsed), lastC);
                        Console.WriteLine();
                        IEnumerable<KeyValuePair<string, float>> passed = ratios.Where(x => x.Value < jpgRatio),
                                                                 npassed = ratios.Where(x => x.Value >= jpgRatio);
                        Console.WriteLine("Passed algoritms: {0}", passed.Count() > 0 ? string.Join(", ", passed.Select(x => string.Format("{0} ({1}%)", x.Key, x.Value.ToString("F3")))) : "None");
                        Console.WriteLine("Non-Passed algoritms: {0}", npassed.Count() > 0 ? string.Join(", ", npassed.Select(x => string.Format("{0} ({1}%)", x.Key, x.Value.ToString("F3")))) : "None");
                    }
                    else lastC = c;

                    firstArr = arr;
                }
            }
        }

        private static string GetLossPercentage(float c, float or)
        {
            if (algCount == 6)
            {
                ratios.Clear();
                algCount = 0;
            }

            float val = (c * 100f / or);
            ratios.Add(algCaptions[algCount], val);

            //Console.WriteLine("Adding: {0} ({1} % {2} = {3}) Val: {4}", algCaptions[algCount % algoritms], algCount, algoritms, algCount % algoritms, val);

            ++algCount;

            return (c * 100f / lastC).ToString("F3") + "%";
        }

        private static void RegisterEllapsedTime()
        {
            LastEllapsed = sw.ElapsedTicks;
        }

        private static float GetEllapsedTime(byte dec = 0)
        {
            float ret = Math.Abs(LastEllapsed - CurrentEllapsed) / (float)Stopwatch.Frequency;
            return dec > 0 ? (float)Math.Round(ret, dec) : ret;
        }

        private static void ResetEllapsedTime()
        {
            LastEllapsed = 0;
        }

        private static string GetEllapsedString(float s)
        {
            return s.ToString("F3") + "s";
        }

        private static string GetBytesRate(float el)
        {
            return string.Format("{0:#,##0.##} {1}", orCount / (el * 1024f * 1024f), "MB/s");
        }

        public static int GetLevelFromQuality(long quality, byte maxquality, bool debug = false, byte minquality = 1)
        {
            int level = (int)Math.Round((quality / 100f) * (maxquality - minquality) + minquality);
            if (debug) Console.WriteLine("{0} Level: " + level, GetLibFromMQuality(maxquality));
            return level; //10 - (int)Math.Ceiling((double)quality / 10) - (quality == 0 ? 1 : 0);
        }

        private static string GetLibFromMQuality(int mquality)
        {
            switch (mquality)
            {
                case 9:
                    return "SharpZip";

                case 22:
                    return "ZSTD";

                default:
                    return "Undefined";
            }
        }
    }
}