// http://www.nullskull.com/a/768/7zip-lzma-inmemory-compression-with-c.aspx
// http://www.7-zip.org/sdk.html
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SevenZip.Compression.LZMA
{
    public static class SevenZipHelper
    {
        private static int dictionary = 1 << 23;

        // static Int32 posStateBits = 2;
        // static Int32 litContextBits = 3; // for normal files
        // UInt32 litContextBits = 0; // for 32-bit data
        // static Int32 litPosBits = 0;
        // UInt32 litPosBits = 2; // for 32-bit data
        // static Int32 algorithm = 2;
        // static Int32 numFastBytes = 128;

        private static bool eos = false;

        private static CoderPropID[] propIDs =
                {
                    CoderPropID.DictionarySize,
                    CoderPropID.PosStateBits,
                    CoderPropID.LitContextBits,
                    CoderPropID.LitPosBits,
                    CoderPropID.Algorithm,
                    CoderPropID.NumFastBytes,
                    CoderPropID.MatchFinder,
                    CoderPropID.EndMarker
                };

        // these are the default properties, keeping it simple for now:
        private static object[] properties =
                {
                    dictionary,
                    2,
                    3,
                    0,
                    2,
                    128,
                    "bt4",
                    eos
                };

        public static IEnumerable<byte> Compress(this IEnumerable<byte> inputBytes)
        {
            return inputBytes.ToArray().Compress().AsEnumerable();
        }

        public static byte[] Compress(this byte[] inputBytes)
        {
            byte[] retVal = null;
            Encoder encoder = new Encoder();
            encoder.SetCoderProperties(propIDs, properties);

            using (MemoryStream strmInStream = new MemoryStream(inputBytes))
            using (MemoryStream strmOutStream = new MemoryStream())
            {
                encoder.WriteCoderProperties(strmOutStream);
                long fileSize = strmInStream.Length;
                for (int i = 0; i < 8; i++) //Esto se puede optimizar?
                    strmOutStream.WriteByte((byte)(fileSize >> (8 * i)));

                encoder.Code(strmInStream, strmOutStream, -1, -1, null);
                retVal = strmOutStream.ToArray();
            } // End Using outStream
            // End Using inStream

            return retVal;
        } // End Function Compress

        public static byte[] Compress(string inFileName)
        {
            byte[] retVal = null;
            Encoder encoder = new Encoder();
            encoder.SetCoderProperties(propIDs, properties);

            using (Stream strmInStream = new FileStream(inFileName, FileMode.Open, FileAccess.Read))
            {
                using (MemoryStream strmOutStream = new MemoryStream())
                {
                    encoder.WriteCoderProperties(strmOutStream);
                    long fileSize = strmInStream.Length;
                    for (int i = 0; i < 8; i++)
                        strmOutStream.WriteByte((byte)(fileSize >> (8 * i)));

                    encoder.Code(strmInStream, strmOutStream, -1, -1, null);
                    retVal = strmOutStream.ToArray();
                } // End Using outStream
            } // End Using inStream

            return retVal;
        } // End Function Compress

        public static void Compress(string inFileName, string outFileName)
        {
            Encoder encoder = new Encoder();
            encoder.SetCoderProperties(propIDs, properties);

            using (Stream strmInStream = new FileStream(inFileName, FileMode.Open, FileAccess.Read))
            {
                using (Stream strmOutStream = new FileStream(outFileName, FileMode.Create))
                {
                    encoder.WriteCoderProperties(strmOutStream);
                    long fileSize = strmInStream.Length;
                    for (int i = 0; i < 8; i++)
                        strmOutStream.WriteByte((byte)(fileSize >> (8 * i)));

                    encoder.Code(strmInStream, strmOutStream, -1, -1, null);

                    strmOutStream.Flush();
                    strmOutStream.Close();
                } // End Using outStream
            } // End Using inStream
        } // End Function Compress

        public static byte[] Decompress(string inFileName)
        {
            byte[] retVal = null;

            Decoder decoder = new Decoder();

            using (Stream strmInStream = new FileStream(inFileName, FileMode.Open, FileAccess.Read))
            {
                strmInStream.Seek(0, 0);

                using (MemoryStream strmOutStream = new MemoryStream())
                {
                    byte[] properties2 = new byte[5];
                    if (strmInStream.Read(properties2, 0, 5) != 5)
                        throw (new System.Exception("input .lzma is too short"));

                    long outSize = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        int v = strmInStream.ReadByte();
                        if (v < 0)
                            throw (new System.Exception("Can't Read 1"));
                        outSize |= ((long)(byte)v) << (8 * i);
                    } //Next i

                    decoder.SetDecoderProperties(properties2);

                    long compressedSize = strmInStream.Length - strmInStream.Position;
                    decoder.Code(strmInStream, strmOutStream, compressedSize, outSize, null);

                    retVal = strmOutStream.ToArray();
                } // End Using newOutStream
            } // End Using newInStream

            return retVal;
        } // End Function Decompress

        public static void Decompress(string inFileName, string outFileName)
        {
            Decoder decoder = new Decoder();

            using (Stream strmInStream = new FileStream(inFileName, FileMode.Open, FileAccess.Read))
            {
                strmInStream.Seek(0, 0);

                using (Stream strmOutStream = new FileStream(outFileName, FileMode.Create))
                {
                    byte[] properties2 = new byte[5];
                    if (strmInStream.Read(properties2, 0, 5) != 5)
                        throw (new System.Exception("input .lzma is too short"));

                    long outSize = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        int v = strmInStream.ReadByte();
                        if (v < 0)
                            throw (new System.Exception("Can't Read 1"));
                        outSize |= ((long)(byte)v) << (8 * i);
                    } // Next i

                    decoder.SetDecoderProperties(properties2);

                    long compressedSize = strmInStream.Length - strmInStream.Position;
                    decoder.Code(strmInStream, strmOutStream, compressedSize, outSize, null);

                    strmOutStream.Flush();
                    strmOutStream.Close();
                } // End Using newOutStream
            } // End Using newInStream
        } // End Function Decompress

        public static IEnumerable<byte> Decompress(this IEnumerable<byte> inputBytes)
        {
            return inputBytes.ToArray().Decompress().AsEnumerable();
        }

        public static byte[] Decompress(this byte[] inputBytes)
        {
            byte[] retVal = null;

            Decoder decoder = new Decoder();

            using (MemoryStream strmInStream = new MemoryStream(inputBytes))
            {
                strmInStream.Seek(0, 0);

                using (MemoryStream strmOutStream = new MemoryStream())
                {
                    byte[] properties2 = new byte[5];
                    if (strmInStream.Read(properties2, 0, 5) != 5)
                        throw (new System.Exception("input .lzma is too short"));

                    long outSize = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        int v = strmInStream.ReadByte();
                        if (v < 0)
                            throw (new System.Exception("Can't Read 1"));
                        outSize |= ((long)(byte)v) << (8 * i);
                    } // Next i

                    decoder.SetDecoderProperties(properties2);

                    long compressedSize = strmInStream.Length - strmInStream.Position;
                    decoder.Code(strmInStream, strmOutStream, compressedSize, outSize, null);

                    retVal = strmOutStream.ToArray();
                } // End Using newOutStream
            } // End Using newInStream

            return retVal;
        } // End Function Decompress
    } // End Class SevenZipHelper
} // End Namespace SevenZip.Compression.LZMA 