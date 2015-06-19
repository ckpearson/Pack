using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using SevenZip;
using Point = System.Drawing.Point;

namespace Pak
{
    public static class Packer
    {
        private const string Token = "{F7FDD8CF-96F0-4043-A0E9-72D07B30FAC9}";
        private const int TokenLength = 38;

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

        public static Tuple<string, byte[]> UnpackImage(byte[] imageData)
        {

            byte[] data;
            using (var ms = new MemoryStream(imageData))
            {
                using (var bmp = new Bitmap(ms))
                {
                    var rect = new Rectangle(new Point(0, 0), bmp.Size);
                    data = new byte[(rect.Width*rect.Height)*4];
                    var locked = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    var stride = locked.Stride;
                    unsafe
                    {
                        var pos = 0;
                        var ptr = (byte*) locked.Scan0;
                        for (var y = rect.Top; y < rect.Bottom; y++)
                        {
                            for (var x = rect.Left; x < rect.Right; x++)
                            {
                                data[pos] = ptr[(y*stride) + (x*4)];
                                data[pos + 1] = ptr[(y*stride) + (x*4) + 1];
                                data[pos + 2] = ptr[(y*stride) + (x*4) + 2];
                                data[pos + 3] = ptr[(y*stride) + (x*4) + 3];

                                pos += 4;
                            }
                        }
                    }
                    bmp.UnlockBits(locked);
                }
            }

            if (data.Length < TokenLength) return null;

            var tkn = Encoding.UTF8.GetString(data.Take(TokenLength).ToArray());
            if (tkn != Token) return null;

            var fNameLength = BitConverter.ToInt32(data.Skip(TokenLength).Take(4).ToArray(), 0);
            var fName = Encoding.UTF8.GetString(data.Skip(TokenLength + 4).Take(fNameLength).ToArray());
            var dropped = BitConverter.ToInt32(data.Skip(TokenLength + 4 + fNameLength).Take(4).ToArray(), 0);
            var dataLength = BitConverter.ToInt32(data.Skip(TokenLength + 4 + fNameLength + 4).Take(4).ToArray(), 0);
            var unpacked = data.Skip(TokenLength + 4 + fNameLength + 4 + 4).Take(dataLength)
                .Concat(Enumerable.Repeat((byte) 0, dropped)).ToArray();
            var decomp = SevenZipExtractor.ExtractBytes(unpacked);
            return new Tuple<string, byte[]>(fName, decomp);
        }

        public static void CreateImage(string sourceFile, string targetPath)
        {
            var dropped = DropTrailingZeroes(CompressFile(sourceFile));
            var fNameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(sourceFile));
            var fNameLengthBytes = BitConverter.GetBytes(fNameBytes.Length);
            var lengthBytes = BitConverter.GetBytes(dropped.Item2.Length);
            var droppedBytes = BitConverter.GetBytes(dropped.Item1);
            var tokenBytes = Encoding.UTF8.GetBytes(Token);

            var data = new Queue<byte[]>(
                tokenBytes.Concat(fNameLengthBytes)
                    .Concat(fNameBytes)
                    .Concat(droppedBytes)
                    .Concat(lengthBytes)
                    .Concat(dropped.Item2)
                    .Select((item, idx) => new {item, idx})
                    .GroupBy(x => x.idx/4)
                    .Select(g => g.Select(x => x.item).ToArray()));

            var cols = (int) Math.Sqrt(data.Count);
            var rows = (int) Math.Ceiling((double) data.Count/cols);

            const int minWidth = 200;
            const int minHeight = 200;

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
                Color.FromArgb(86,81,117),
                Color.FromArgb(83,138,149),
                Color.FromArgb(103,183,158),
                Color.FromArgb(255,183,39),
                Color.FromArgb(228,73,28)
            };

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
                            if (data.Count == 0)
                            {
                                if (y == 0 && x < cols ||
                                    y > 0 && x == (cols - 1) ||
                                    y == (rows - 1) && x < cols ||
                                    x == 0 && y > 0)
                                {
                                    ptr[(y*stride) + (x*4) + 3] = 255;
                                    continue;
                                }

                                var color = colors[rnd.Next(0, colors.Length)];

                                ptr[(y*stride) + (x*4)] = color.B;
                                ptr[(y*stride) + (x*4) + 1] = color.G;
                                ptr[(y*stride) + (x*4) + 2] = color.R;
                                ptr[(y*stride) + (x*4) + 3] = 255;
                                
                                continue;
                            }
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