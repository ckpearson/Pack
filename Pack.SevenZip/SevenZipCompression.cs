using Pack.Core.Compression;
using SevenZip;

namespace Pack.Compression.SevenZip
{
    public class Compressor : ICompressor
    {
        public byte[] Compress(byte[] data)
        {
            return SevenZipCompressor.CompressBytes(data);
        }

        public byte[] DeCompress(byte[] compressedData)
        {
            return SevenZipExtractor.ExtractBytes(compressedData);
        }
    }
}