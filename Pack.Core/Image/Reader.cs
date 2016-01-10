using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using Pack.Core.Utils;

namespace Pack.Core.Image
{
    public static class Reader
    {
        private static byte[] UnsquashImageData(Bitmap bmp)
        {
            var rect = new Rectangle(new Point(0, 0), bmp.Size);
            var data = new byte[(rect.Width * rect.Height) * 4];
            var locked = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var stride = locked.Stride;
            unsafe
            {
                var pos = 0;
                var ptr = (byte*)locked.Scan0;
                for (var y = rect.Top; y < rect.Bottom; y++)
                {
                    var currY = y;
                    for (var x = rect.Left; x < rect.Right; x++)
                    {
                        var currX = x;
                        data.SetIfInIndex(pos, () => ptr[(currY * stride) + (currX * 4)]);
                        data.SetIfInIndex(pos + 1, () => ptr[(currY * stride) + (currX * 4) + 1]);
                        data.SetIfInIndex(pos + 2, () => ptr[(currY * stride) + (currX * 4) + 2]);
                        data.SetIfInIndex(pos + 3, () => ptr[(currY * stride) + (currX * 4) + 3]);
                        pos += 4;
                    }
                }
            }

            bmp.UnlockBits(locked);

            return data;
        }

        public static Tuple<string, byte[]> ReadData(
            Bitmap bitmap,
            Func<byte[], byte[]> decompressor)
        {
            if (!Detector.ImageIsPacked(bitmap))
                throw new ArgumentException(@"Image is not a packed image", nameof(bitmap));

            var data = UnsquashImageData(bitmap);

            var fNameLength = BitConverter.ToInt32(data.Skip(Constants.TokenDataLength).Take(4).ToArray(), 0);
            var fName = Encoding.UTF8.GetString(data.Skip(Constants.TokenDataLength + 4).Take(fNameLength).ToArray());

            var dropped = BitConverter.ToInt32(
                data.Skip(Constants.TokenDataLength + 4 + fNameLength).Take(4).ToArray(), 0);
            var dataLength =
                BitConverter.ToInt32(data.Skip(Constants.TokenDataLength + 4 + fNameLength + 4).Take(4).ToArray(), 0);

            var unpacked = data.Skip(Constants.TokenDataLength + 4 + fNameLength + 8).Take(dataLength)
                .Concat(Enumerable.Repeat((byte) 0, dropped)).ToArray();
            var decomp = decompressor(unpacked);
            return new Tuple<string, byte[]>(fName, decomp);
        }
    }
}