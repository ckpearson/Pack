using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using Pack_v2.Tools;
using SevenZip;

namespace Pack_v2.Packers
{
    public class V1Packer
    {
        private const string Token = "{F7FDD8CF-96F0-4043-A0E9-72D07B30FAC9}";
        private const int TokenLength = 38;
        private static readonly byte[] SecureIndicator = {1, 2, 3, 4, 5, 200};

        public double Version
        {
            get { return 1.0; }
        }

        public bool DataIsForPacker(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var bmp = new Bitmap(ms))
                {
                    if (bmp.Width < TokenLength || bmp.Height < 1) return false;
                    var rect = new Rectangle(0, 0, (int)Math.Round((TokenLength / (double)4)), 1);
                    var tknData = new byte[38];
                    var locked = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    var stride = locked.Stride;
                    unsafe
                    {
                        var pos = 0;
                        var ptr = (byte*)locked.Scan0;
                        for (var x = rect.Left; x < rect.Right; x++)
                        {
                            var currX = x;
                            tknData.SetIfInIndex(pos, () => ptr[(0 * stride) + (currX * 4)]);
                            tknData.SetIfInIndex(pos + 1, () => ptr[(0 * stride) + (currX * 4) + 1]);
                            tknData.SetIfInIndex(pos + 2, () => ptr[(0 * stride) + (currX * 4) + 2]);
                            tknData.SetIfInIndex(pos + 3, () => ptr[(0 * stride) + (currX * 4) + 3]);
                            pos += 4;
                        }
                    }

                    return Encoding.UTF8.GetString(tknData) == Token;
                }
            }
        }

        private static byte[] UnsquashImageData(byte[] imgData)
        {
            byte[] data;
            using (var ms = new MemoryStream(imgData))
            {
                using (var bmp = new Bitmap(ms))
                {
                    var rect = new Rectangle(new Point(0, 0), bmp.Size);
                    data = new byte[(rect.Width * rect.Height) * 4];
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
                }
            }
            return data;
        }

        public UnpackedFile Unpack(byte[] data)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (data.Length == 0) throw new ArgumentException(@"Data is empty", "data");

            if (!DataIsForPacker(data)) throw new InvalidOperationException("Data not for this packer!");
            data = UnsquashImageData(data);

            var startPoint = TokenLength;
            var secured = false;

            if (data.Skip(startPoint).Take(SecureIndicator.Length).SequenceEqual(SecureIndicator))
            {
                startPoint = TokenLength + SecureIndicator.Length;
                secured = true;
            }

            var fNameLength = BitConverter.ToInt32(data.Skip(startPoint).Take(4).ToArray(), 0);
            var fName = Encoding.UTF8.GetString(data.Skip(startPoint + 4).Take(fNameLength).ToArray());

            var dropped = BitConverter.ToInt32(data.Skip(startPoint + 4 + fNameLength).Take(4).ToArray(), 0);
            var dataLength = BitConverter.ToInt32(data.Skip(startPoint + 4 + fNameLength + 4).Take(4).ToArray(), 0);

            var unpacked = data.Skip(startPoint + 4 + fNameLength + 8).Take(dataLength)
                .Concat(Enumerable.Repeat((byte)0, dropped)).ToArray();

            var decomp = SevenZipExtractor.ExtractBytes(unpacked);
            return new UnpackedFile(fName, decomp, secured);
        }

        public Bitmap CreateImage(byte[] data, string fileName, bool secured)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (data.Length == 0) throw new ArgumentException(@"Data is empty", "data");
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");

            var dropped = SevenZipCompressor.CompressBytes(data).DropTrailingZeroes();
            var fNameBytes = Encoding.UTF8.GetBytes(fileName);
            var fNameLengthBytes = BitConverter.GetBytes(fNameBytes.Length);
            var lengthBytes = BitConverter.GetBytes(dropped.Item2.Length);
            var droppedBytes = BitConverter.GetBytes(dropped.Item1);
            var tokenBytes = Encoding.UTF8.GetBytes(Token);

            var dataToWrite = new Queue<byte[]>(
                tokenBytes
                .Concat(secured ? SecureIndicator : new byte[] { })
                .Concat(fNameLengthBytes)
                    .Concat(fNameBytes)
                    .Concat(droppedBytes)
                    .Concat(lengthBytes)
                    .Concat(dropped.Item2)
                    .Select((item, idx) => new { item, idx })
                    .GroupBy(x => x.idx / 4)
                    .Select(g => g.Select(x => x.item).ToArray()));

            var cols = (int)Math.Sqrt(dataToWrite.Count);
            var rows = (int)Math.Ceiling((double)dataToWrite.Count / cols);

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