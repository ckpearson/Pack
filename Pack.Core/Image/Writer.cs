using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using Pack.Core.Utils;

namespace Pack.Core.Image
{
    public static class Writer
    {
        public static Bitmap CreatePackedImage(
            byte[] data,
            string fileName,
            Func<Point, Color> nonDataColorPicker,
            Func<byte[], byte[]> compressor,
            int minWidth,
            int minHeight)
        {
            var dropped = compressor(data).DropTrailingZeroes();
            var fNameBytes = Encoding.UTF8.GetBytes(fileName);
            var fNameLengthBytes = BitConverter.GetBytes(fNameBytes.Length);
            var lengthBytes = BitConverter.GetBytes(dropped.Item2.Length);
            var droppedBytes = BitConverter.GetBytes(dropped.Item1);
            var tokenBytes = Encoding.UTF8.GetBytes(Constants.ImageToken);

            var dataToWrite = new Queue<byte[]>(
                tokenBytes.Concat(fNameLengthBytes)
                    .Concat(fNameBytes)
                    .Concat(droppedBytes)
                    .Concat(lengthBytes)
                    .Concat(dropped.Item2)
                    .Select((item, idx) => new { item, idx })
                    .GroupBy(x => x.idx / 4)
                    .Select(g => g.Select(x => x.item).ToArray()));

            var cols = (int)Math.Sqrt(dataToWrite.Count);
            var rows = (int)Math.Ceiling((double)dataToWrite.Count / cols);
            

            if (cols < minWidth)
            {
                cols = minWidth;
            }

            if (rows < minHeight)
            {
                rows = minHeight;
            }

            var rnd = new Random();
            var colors = new[]
            {
                Color.FromArgb(86, 81, 117),
                Color.FromArgb(83, 138, 149),
                Color.FromArgb(103, 183, 158),
                Color.FromArgb(255, 183, 39),
                Color.FromArgb(228, 73, 28)
            };

            var bmp = new Bitmap(cols, rows);

            var rect = new Rectangle(0, 0, cols, rows);
            var locked = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var stride = locked.Stride;
            unsafe
            {
                var ptr = (byte*)locked.Scan0;
                for (var y = 0; y < rows; y++)
                {
                    for (var x = 0; x < cols; x++)
                    {
                        if (dataToWrite.Count == 0)
                        {
                            if (y == 0 && x < cols ||
                                y > 0 && x == (cols - 1) ||
                                y == (rows - 1) && x < cols ||
                                x == 0 && y > 0)
                            {
                                ptr[(y * stride) + (x * 4) + 3] = 255;
                                continue;
                            }

                            var color = colors[rnd.Next(0, colors.Length)];

                            ptr[(y * stride) + (x * 4)] = color.B;
                            ptr[(y * stride) + (x * 4) + 1] = color.G;
                            ptr[(y * stride) + (x * 4) + 2] = color.R;
                            ptr[(y * stride) + (x * 4) + 3] = 255;

                            continue;
                        }
                        var grp = dataToWrite.Dequeue();

                        ptr[(y * stride) + (x * 4)] = grp.ElementAtOrDefault(0);
                        ptr[(y * stride) + (x * 4) + 1] = grp.ElementAtOrDefault(1);
                        ptr[(y * stride) + (x * 4) + 2] = grp.ElementAtOrDefault(2);
                        ptr[(y * stride) + (x * 4) + 3] = grp.ElementAtOrDefault(3);
                    }
                }
            }
            bmp.UnlockBits(locked);
            return bmp;
        }
    }
}
