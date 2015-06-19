using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using SevenZip;

namespace Pak
{
    public static class Packer
    {
        private static byte[] CompressFile(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var ms = new MemoryStream())
                {
                    SevenZipCompressor.CompressStream(fs, ms, null, null);
                    return ms.GetBuffer();
                }
            }
        }

        private static Tuple<int, byte[]> DropTrailingZeroes(byte[] bytes)
        {
            var count = 0;
            var length = bytes.Length;
            for (var i = length - 1; i > 0; i--)
            {
                if (bytes[i] == 0)
                {
                    count++;
                    continue;
                }
                break;
            }
            return Tuple.Create(count, bytes.Take(length - count).ToArray());
        }

        public static void CreateImage(string sourceFile, string targetPath)
        {
            var dropped = DropTrailingZeroes(CompressFile(sourceFile));
            var fNameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(sourceFile));
            var fNameLengthBytes = BitConverter.GetBytes(fNameBytes.Length);
            var lengthBytes = BitConverter.GetBytes(dropped.Item2.Length);
            var droppedBytes = BitConverter.GetBytes(dropped.Item1);

            var data = new Queue<byte[]>(
                fNameLengthBytes.Concat(fNameBytes).Concat(droppedBytes).Concat(droppedBytes).Concat(lengthBytes).Concat(dropped.Item2)
                    .Select((item, idx) => new {item, idx})
                    .GroupBy(x => x.idx/4)
                    .Select(g => g.Select(x => x.item).ToArray()));

            var cols = (int) Math.Sqrt(data.Count);
            var rows = (int) Math.Ceiling((double) data.Count/cols);

            using (var bmp = new Bitmap(cols, rows))
            {
                var rect = new Rectangle(0, 0, cols, rows);
                var locked = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                var stride = locked.Stride;
                unsafe
                {
                    var ptr = (byte*) locked.Scan0;
                    for (var y = 0; y < rows; y++)
                    {
                        for (var x = 0; x < cols; x ++)
                        {
                            if (data.Count == 0) break;
                            var grp = data.Dequeue();

                            ptr[(y*stride) + (x*4)] = grp.ElementAtOrDefault(0);
                            ptr[(y*stride) + (x*4) + 1] = grp.ElementAtOrDefault(1);
                            ptr[(y*stride) + (x*4) + 2] = grp.ElementAtOrDefault(2);
                            ptr[(y*stride) + (x*4) + 3] = grp.ElementAtOrDefault(3);
                        }
                    }
                }
                bmp.UnlockBits(locked);
                bmp.Save(targetPath, ImageFormat.Png);
            }
        }
    }
}