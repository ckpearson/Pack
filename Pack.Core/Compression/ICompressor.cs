namespace Pack.Core.Compression
{
    public interface ICompressor
    {
        byte[] Compress(byte[] data);
        byte[] DeCompress(byte[] compressedData);
    }
}