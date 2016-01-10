using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using Pack.Core.Utils;

namespace Pack.Core.Image
{
    public static class Detector
    {
        public static bool ImageIsPacked(Bitmap bitmap)
        {
            if (bitmap.Width < Constants.TokenDataLength || bitmap.Height < 1) return false;
            var tokenBounds = new Rectangle(0, 0, (int) Math.Round((Constants.TokenDataLength/(double) 4)), 1);
            var lockBits = bitmap.LockBits(tokenBounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var stride = lockBits.Stride;
            var tokenData = new byte[Constants.TokenDataLength];
            unsafe
            {
                var pos = 0;
                var ptr = (byte*) lockBits.Scan0;
                for (var x = tokenBounds.Left; x < tokenBounds.Right; x++)
                {
                    tokenData.SetIfInIndex(pos, ptr[(0*stride) + (x*4)]);
                    tokenData.SetIfInIndex(pos + 1, ptr[(0*stride) + (x*4) + 1]);
                    tokenData.SetIfInIndex(pos + 2, ptr[(0*stride) + (x*4) + 2]);
                    tokenData.SetIfInIndex(pos + 3, ptr[(0*stride) + (x*4) + 3]);
                    pos += 4;
                }
            }
            bitmap.UnlockBits(lockBits);

            return Encoding.UTF8.GetString(tokenData) == Constants.ImageToken;
        }
    }
}