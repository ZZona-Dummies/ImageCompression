using System;
using System.IO;
using System.IO.Compression;
using Zstandard.Net;

namespace ImageCompress
{
    public static class ZStdHelper
    {
        public static byte[] Compress(this byte[] data, byte[] dictionaryRaw, int compressionLevel)
        {
            if (data == null) throw new Exception("Null data passed in ZSTD compression!");

            using (MemoryStream memoryStream = new MemoryStream())
            using (ZstandardStream compressionStream = new ZstandardStream(memoryStream, CompressionMode.Compress))
            using (ZstandardDictionary dictionary = new ZstandardDictionary(dictionaryRaw))
            {
                compressionStream.CompressionLevel = compressionLevel;
                compressionStream.CompressionDictionary = dictionary;
                compressionStream.Write(data, 0, data.Length);
                compressionStream.Close();
                return memoryStream.ToArray();
            }
        }

        public static byte[] Decompress(this byte[] compressed, byte[] dictionaryRaw)
        {
            using (MemoryStream memoryStream = new MemoryStream(compressed))
            using (ZstandardStream compressionStream = new ZstandardStream(memoryStream, CompressionMode.Decompress))
            using (ZstandardDictionary dictionary = new ZstandardDictionary(dictionaryRaw))
            using (MemoryStream temp = new MemoryStream())
            {
                compressionStream.CompressionDictionary = dictionary;
                compressionStream.CopyTo(temp);
                return temp.ToArray();
            }
        }
    }
}