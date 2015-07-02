using System.Drawing;

namespace Pack
{
    public interface IPacker
    {
        double Version { get; }
        bool DataIsForPacker(byte[] data);
        UnpackedFile Unpack(byte[] data, IInput input);
        Bitmap CreateImage(byte[] data, string fileName, IInput input);
    }
}