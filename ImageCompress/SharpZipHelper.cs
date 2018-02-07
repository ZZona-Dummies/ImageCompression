using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImageCompress
{
    public static class SharpZipHelper
    {
        public static IEnumerable<byte> CreateToMemoryStream(this IEnumerable<byte> bytes, string zipEntryName, long quality)
        {
            return CreateToMemoryStream(bytes.ToArray(), zipEntryName, quality);
        }

        public static byte[] CreateToMemoryStream(this byte[] bytes, string zipEntryName, long quality)
        {
            using (MemoryStream memStreamIn = new MemoryStream(bytes))
            using (MemoryStream outputMemStream = new MemoryStream())
            using (ZipOutputStream zipStream = new ZipOutputStream(outputMemStream))
            {
                int level = ProgramHandler.GetLevelFromQuality(quality, 11);
                zipStream.SetLevel(level); //0-9, 9 being the highest level of compression

                ZipEntry newEntry = new ZipEntry(zipEntryName);
                newEntry.DateTime = DateTime.Now;

                zipStream.PutNextEntry(newEntry);

                StreamUtils.Copy(memStreamIn, zipStream, new byte[4096]);
                zipStream.CloseEntry();

                zipStream.IsStreamOwner = false;    // False stops the Close also Closing the underlying stream.
                zipStream.Close();          // Must finish the ZipOutputStream before using outputMemStream.

                outputMemStream.Position = 0;
                return outputMemStream.GetBuffer();
            }

            // Alternative outputs:
            // ToArray is the cleaner and easiest to use correctly with the penalty of duplicating allocated memory.
            /*byte[] byteArrayOut = outputMemStream.ToArray();

            // GetBuffer returns a raw buffer raw and so you need to account for the true length yourself.
            byte[] byteArrayOut = outputMemStream.GetBuffer();
            long len = outputMemStream.Length;*/
        }
    }
}