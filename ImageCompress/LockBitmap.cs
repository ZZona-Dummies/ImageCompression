using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ImageCompress
{
    public class LockBitmap : IDisposable
    {
        private Bitmap source = null;
        private IntPtr Iptr = IntPtr.Zero;
        private BitmapData bitmapData = null;

        public byte[] Pixels { get; set; }
        public int Depth { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int RowSize { get; private set; }

        public bool IsDisposed { get; private set; }

        public LockBitmap(Bitmap source)
        {
            this.source = source;
            IsDisposed = false;
        }

        /// <summary>
        /// Lock bitmap data
        /// </summary>
        public void LockBits()
        {
            try
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(typeof(LockBitmap).Name);

                // Get width and height of bitmap
                Width = source.Width;
                Height = source.Height;

                // Create rectangle to lock
                Rectangle rect = new Rectangle(0, 0, Width, Height);

                // get source bitmap pixel format size
                Depth = System.Drawing.Bitmap.GetPixelFormatSize(source.PixelFormat);

                // Check if bpp (Bits Per Pixel) is 8, 24, or 32
                if (Depth != 8 && Depth != 24 && Depth != 32)
                {
                    throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");
                }

                // Lock bitmap and return bitmap data
                bitmapData = source.LockBits(rect, ImageLockMode.ReadWrite,
                                             source.PixelFormat);

                // create byte array to copy pixel values
                RowSize = bitmapData.Stride < 0 ? -bitmapData.Stride : bitmapData.Stride;
                Pixels = new byte[Height * RowSize];
                Iptr = bitmapData.Scan0;

                // Copy data from pointer to array
                // Not working for negative Stride see. http://stackoverflow.com/a/10360753/1498252
                //Marshal.Copy(Iptr, Pixels, 0, Pixels.Length);
                // Solution for positive and negative Stride:
                for (int y = 0; y < Height; y++)
                {
                    Marshal.Copy(IntPtr.Add(Iptr, y * bitmapData.Stride),
                        Pixels, y * RowSize,
                        RowSize);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Unlock bitmap data
        /// </summary>
        public void UnlockBits()
        {
            try
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(typeof(LockBitmap).Name);
                if (bitmapData == null)
                    throw new InvalidOperationException("Image is not locked.");

                // Copy data from byte array to pointer
                //Marshal.Copy(Pixels, 0, Iptr, Pixels.Length);
                for (int y = 0; y < Height; y++)
                {
                    Marshal.Copy(Pixels, y * RowSize,
                        IntPtr.Add(Iptr, y * bitmapData.Stride),
                        RowSize);
                }

                // Unlock bitmap data
                source.UnlockBits(bitmapData);
                bitmapData = null;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Get the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Color GetPixel(int x, int y)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(typeof(LockBitmap).Name);

            Color clr = Color.Empty;

            // Get color components count
            int cCount = Depth / 8;

            // Get start index of the specified pixel
            int i = (y * RowSize) + (x * cCount);

            if (i > Pixels.Length - cCount)
                throw new IndexOutOfRangeException();

            if (Depth == 32) // For 32 bpp get Red, Green, Blue and Alpha
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                byte a = Pixels[i + 3]; // a
                clr = Color.FromArgb(a, r, g, b);
            }
            if (Depth == 24) // For 24 bpp get Red, Green and Blue
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                clr = Color.FromArgb(r, g, b);
            }
            if (Depth == 8)
            // For 8 bpp get color value (Red, Green and Blue values are the same)
            {
                byte c = Pixels[i];
                clr = Color.FromArgb(c, c, c);
            }
            return clr;
        }

        /// <summary>
        /// Set the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public void SetPixel(int x, int y, Color color)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(typeof(LockBitmap).Name);

            // Get color components count
            int cCount = Depth / 8;

            // Get start index of the specified pixel
            int i = (y * RowSize) + (x * cCount);
            if (i > Pixels.Length - cCount)
                throw new IndexOutOfRangeException();

            if (Depth == 32) // For 32 bpp set Red, Green, Blue and Alpha
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
                Pixels[i + 3] = color.A;
            }
            if (Depth == 24) // For 24 bpp set Red, Green and Blue
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
            }
            if (Depth == 8)
            // For 8 bpp set color value (Red, Green and Blue values are the same)
            {
                Pixels[i] = color.B;
            }
        }

        #region IDisposable Members

        ~LockBitmap()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (bitmapData != null)
            {
                source.UnlockBits(bitmapData);
                bitmapData = null;
            }
            source = null;
            IsDisposed = true;
        }

        #endregion IDisposable Members
    }
}

/*using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ImageCompress
{
    public class LockBitmap : IDisposable
    {
        private readonly Bitmap source;
        private IntPtr iptr = IntPtr.Zero;
        private BitmapData bitmapData;

        private bool locked = false;
        private bool unlocked = false;
        private static readonly object lockObject = new object();
        private static readonly object unlockObject = new object();

        public byte[] Pixels { get; set; }
        public int Depth { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public LockBitmap(Bitmap source)
        {
            this.source = source;
            LockBits();
        }

        /// <summary>
        /// Lock bitmap data
        /// </summary>
        public void LockBits()
        {
            try
            {
                // double-checked lock
                if (locked)
                    return;

                lock (lockObject)
                {
                    if (locked)
                        return;

                    // Get width and height of bitmap
                    Width = source.Width;
                    Height = source.Height;

                    // get total locked pixels count
                    int pixelCount = Width * Height;

                    // Create rectangle to lock
                    Rectangle rect = new Rectangle(0, 0, Width, Height);

                    // get source bitmap pixel format size
                    Depth = Image.GetPixelFormatSize(source.PixelFormat);

                    // Check if bpp (Bits Per Pixel) is 8, 24, or 32
                    if (Depth != 8 && Depth != 24 && Depth != 32)
                        throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");

                    // Lock bitmap and return bitmap data
                    bitmapData = source.LockBits(rect, ImageLockMode.ReadWrite,
                                                 source.PixelFormat);

                    // create byte array to copy pixel values
                    int step = Depth / 8;
                    Pixels = new byte[pixelCount * step];
                    iptr = bitmapData.Scan0;

                    //Console.WriteLine(Pixels.Length + " " + Math.Abs(bitmapData.Stride * source.Height));

                    // Copy data from pointer to array
                    Marshal.Copy(iptr, Pixels, 0, Pixels.Length);
                    locked = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// Unlock bitmap data
        /// </summary>
        public void UnlockBits()
        {
            try
            {
                if (unlocked)
                    return;

                lock (unlockObject)
                {
                    if (unlocked)
                        return;

                    // Copy data from byte array to pointer
                    Marshal.Copy(Pixels, 0, iptr, Pixels.Length);

                    // Unlock bitmap data
                    source.UnlockBits(bitmapData);
                    unlocked = true;
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Get the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Color GetPixel(int x, int y)
        {
            var clr = Color.Empty;

            // Get color components count
            var cCount = Depth / 8;

            // Get start index of the specified pixel
            var i = ((y * Width) + x) * cCount;

            if (i > Pixels.Length - cCount)
                throw new IndexOutOfRangeException();

            if (Depth == 32) // For 32 BPP get Red, Green, Blue and Alpha
            {
                var b = Pixels[i];
                var g = Pixels[i + 1];
                var r = Pixels[i + 2];
                var a = Pixels[i + 3]; // a
                clr = Color.FromArgb(a, r, g, b);
            }

            if (Depth == 24) // For 24 BPP get Red, Green and Blue
            {
                var b = Pixels[i];
                var g = Pixels[i + 1];
                var r = Pixels[i + 2];
                clr = Color.FromArgb(r, g, b);
            }

            // For 8 BPP get color value (Red, Green and Blue values are the same)
            if (Depth == 8)
            {
                var c = Pixels[i];
                clr = Color.FromArgb(c, c, c);
            }
            return clr;
        }

        /// <summary>
        /// Set the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public void SetPixel(int x, int y, Color color)
        {
            // Get color components count
            var cCount = Depth / 8;

            // Get start index of the specified pixel
            var i = ((y * Width) + x) * cCount;

            if (Depth == 32) // For 32 BPP set Red, Green, Blue and Alpha
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
                Pixels[i + 3] = color.A;
            }

            if (Depth == 24) // For 24 BPP set Red, Green and Blue
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
            }

            // For 8 BPP set color value (Red, Green and Blue values are the same)
            if (Depth == 8)
            {
                Pixels[i] = color.B;
            }
        }

        public void Dispose()
        {
            UnlockBits();
        }
    }
}*/