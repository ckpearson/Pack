using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Pack.Packers.Manipulators;

namespace Pack.Packers
{
    public sealed class V2Packer : IPacker
    {
        public double Version { get { return 2; } }

        public readonly byte[] Markers = new byte[]
        {
            150,
            80,
            70,
        };

        public bool DataIsForPacker(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var bmp = new Bitmap(ms))
                {
                    return bmp.GetPixel(0, 0).A == Markers[0] &&
                           bmp.GetPixel(bmp.Width - 1, 0).A == Markers[1] &&
                           bmp.GetPixel(bmp.Width/2, bmp.Height - 1).A == Markers[2];
                }
            }
        }

        public UnpackedFile Unpack(byte[] data, IInput input)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var bmp = new Bitmap(ms))
                {
                    var locked = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly,
                        PixelFormat.Format32bppArgb);

                    var fileDetailsBoxLocationData = locked.ReadBlue(new Rectangle(2, 0, 8, 1));
                    var fileDetailsStartBoxLocation = new Point(BitConverter.ToInt32(fileDetailsBoxLocationData, 0),
                        BitConverter.ToInt32(fileDetailsBoxLocationData, 4));

                    var fileDetailsLength =
                        BitConverter.ToInt32(
                            locked.ReadBlue(new Rectangle(fileDetailsStartBoxLocation.X, fileDetailsStartBoxLocation.Y,
                                4, 1)), 0);
                }
            }

            throw new NotImplementedException();
        }

        public Bitmap CreateImage(byte[] data, string fileName, IInput input)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (data.Length == 0) throw new ArgumentException(@"Data is empty", "data");
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");
            if (input == null) throw new ArgumentNullException("input");

            var manip = data.Manipulate(new SevenZipCompressionStep())
                .Then(new ContiguousRangeCollapseStep())
                .Then(new TrailingZeroEliminationStep());

            var serdManipulations =
                manip.StepsApplied.Select(
                    k => new {Id = k.Key.StepIdent, Ser = k.Key.SerializeState(k.Value, () => StandardSer(k.Value))})
                    .ToList();

            var fNameBytes = Encoding.UTF8.GetBytes(fileName);

            var manipMetadata =
                serdManipulations.SelectMany(
                    x => new[] {x.Id}.Concat(BitConverter.GetBytes(x.Ser.Length)).Concat(x.Ser).ToArray())
                    .ToArray();

            manipMetadata = BitConverter.GetBytes(manipMetadata.Length).Concat(manipMetadata).ToArray();

            var dataRect = manip.CurrentData.ComputeRectangleForData(4, false);

            var fileDetailsData = BitConverter.GetBytes(fNameBytes.Length)
                .Concat(fNameBytes)
                .Concat(BitConverter.GetBytes(manip.CurrentData.Length))
                .ToArray();

            var fileDetailsRect = BitConverter.GetBytes(fileDetailsData.Length).Concat(fileDetailsData).ToArray().ComputeRectangleForData(1, false);

            var manipRect = manipMetadata.ComputeRectangleForData(1, false);

            var rnd = new Random();
            var colors = new[]
            {
                Color.FromArgb(86, 81, 117),
                Color.FromArgb(83, 138, 149),
                Color.FromArgb(103, 183, 158),
                Color.FromArgb(255, 183, 39),
                Color.FromArgb(228, 73, 28)
            };

            var purelyRandomColor = new Func<Point, Color>(_ => colors[rnd.Next(0, colors.Length)]);

            var reservedArea = manipRect.Area() +
                               fileDetailsRect.Area() +
                               dataRect.Area() +
                               new Rectangle(0, 0, 5, 5).Area()*2;
                // *2 to account for the x/y indent for file and manip rectangles

            var cols = (int) Math.Sqrt(reservedArea);
            var rows = (int) Math.Ceiling((double) reservedArea/cols);

            var minWidth = 100;
            var minHeight = 100;

            if (cols < minWidth)
            {
                cols = minWidth;
            }

            if (rows < minHeight)
            {
                rows = minHeight;
            }

            var mainRect = new Rectangle(0, 0, cols, rows);

            var bmp = new Bitmap(mainRect.Width, mainRect.Height);
            var locked = bmp.LockBits(mainRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                var ptr = (byte*) locked.Scan0;
                var stride = locked.Stride;

                locked.DrawBorder(mainRect, 4, Color.Black);

                var mainRectCenterPoint = new Point(mainRect.Left + mainRect.Width/2,
                    mainRect.Top + mainRect.Height/2);

                var dataRectMidPos = new Point(mainRectCenterPoint.X - dataRect.Width/2,
                    mainRectCenterPoint.Y - dataRect.Height/2);

                dataRect = new Rectangle(dataRectMidPos, dataRect.Size);

                locked.DrawBorder(dataRect, 4, Color.Black);

                //locked.EncodeAreaWithData(new Rectangle(dataRect.X, dataRect.Y,
                //    dataRect.Width, dataRect.Height), manip.CurrentData,
                //    () => colors[rnd.Next(0, colors.Length)]);

                manipRect = new Rectangle(5, 5, manipRect.Width, manipRect.Height);

                locked.BlueEncode(new Rectangle(manipRect.X, manipRect.Y,
                    manipRect.Width, manipRect.Height), manipMetadata,
                    purelyRandomColor);

                fileDetailsRect = new Rectangle(5, (mainRect.Bottom - 6) - fileDetailsRect.Height, fileDetailsRect.Width,
                    fileDetailsRect.Height);

                locked.BlueEncode(fileDetailsRect, fileDetailsData, purelyRandomColor);

                var dat = locked.ReadBlue(fileDetailsRect);


                for (var y = mainRect.Top + 1; y < mainRect.Bottom - 1; y++)
                {
                    for (var x = mainRect.Left + 1; x < mainRect.Right - 1; x++)
                    {
                        if (!manipRect.Contains(x, y) &&
                            !dataRect.Contains(x, y) &&
                            !fileDetailsRect.Contains(x, y))
                        {
                            //locked.SetColor(x, y, 4, colors[rnd.Next(0, colors.Length)]);
                        }
                    }
                }

                locked.SetAlpha(0, 0, 4, Markers[0]);
                locked.SetAlpha(mainRect.Right - 1, 0, 4, Markers[1]);
                locked.SetAlpha(mainRectCenterPoint.X, mainRect.Bottom - 1, 4, Markers[2]);

                locked.BlueEncode(new Rectangle(2, 0, 8, 1),
                    BitConverter.GetBytes(fileDetailsRect.Left)
                        .Concat(BitConverter.GetBytes(fileDetailsRect.Top))
                        .ToArray(),
                    _ => Color.Black);

            }
            bmp.UnlockBits(locked);
            return bmp;
        }

        private static byte[] StandardSer(object value)
        {
            if (value == null) return new byte[] {};
            var bform = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bform.Serialize(ms, value);
                return ms.GetBuffer();
            }
        }
    }

    public static class RenderExtensions
    {
        public static Rectangle ComputeRectangleForData(this byte[] data, int packDensity = 4, bool border = true)
        {
            var cols = (int) Math.Sqrt(((double)data.Length/packDensity));
            var rows = (int) Math.Ceiling(data.Length/(double) packDensity/cols);

            return new Rectangle(0, 0, cols + (border ? 2 : 0), rows + (border ? 2 : 0));
        }

        private static int PointerBaseline(int x, int y, int perPixel, int stride)
        {
            return (y*stride) + (x*perPixel);
        }

        public static void SetAlpha(this BitmapData locked, int x, int y, int perPixel, byte value)
        {
            unsafe
            {
                ((byte*) locked.Scan0)[PointerBaseline(x, y, perPixel, locked.Stride) + 3] = value;
            }
        }

        public static void SetBlue(this BitmapData locked, int x, int y, int perPixel, byte value)
        {
            unsafe
            {
                ((byte*) locked.Scan0)[PointerBaseline(x, y, perPixel, locked.Stride)] = value;
            }
        }

        public static void SetGreen(this BitmapData locked, int x, int y, int perPixel, byte value)
        {
            unsafe
            {
                ((byte*) locked.Scan0)[PointerBaseline(x, y, perPixel, locked.Stride) +1] = value;
            }
        }

        public static void SetRed(this BitmapData locked, int x, int y, int perPixel, byte value)
        {
            unsafe
            {
                ((byte*) locked.Scan0)[PointerBaseline(x, y, perPixel, locked.Stride) + 2] = value;
            }
        }

        public static void SetColor(this BitmapData locked, int x, int y, int perPixel, Color color)
        {
            locked.SetAlpha(x, y, perPixel, color.A);
            locked.SetRed(x, y, perPixel, color.R);
            locked.SetGreen(x, y, perPixel, color.G);
            locked.SetBlue(x, y, perPixel, color.B);
        }

        public static void DrawBorder(this BitmapData locked, Rectangle borderRect, int perPixel, Color color)
        {
            unsafe
            {
                var ptr = (byte*) locked.Scan0;
                var stride = locked.Stride;

                for (var y = borderRect.Top; y < borderRect.Bottom; y++)
                {
                    for (var x = borderRect.Left; x < borderRect.Right; x++)
                    {
                        if (x == borderRect.Left || y == borderRect.Top || x == borderRect.Right - 1 ||
                            y == borderRect.Bottom - 1)
                        {
                            locked.SetColor(x, y, perPixel, color);
                        }
                    }
                }
            }
        }

        public static void EncodeAreaWithData(this BitmapData locked, Rectangle dataArea, byte[] data,
            Func<Color> colorSelectorFunc)
        {
            unsafe
            {
                var ptr = (byte*) locked.Scan0;
                var stride = locked.Stride;

                for (var y = dataArea.Top; y < dataArea.Bottom; y++)
                {
                    for (var x = dataArea.Left; x < dataArea.Right; x++)
                    {
                        var normY = (y - dataArea.Top);
                        var normX = (x - dataArea.Left);
                        var idx = (normY*dataArea.Width +normX) * 4;

                        if (idx < data.Length) locked.SetAlpha(x, y, 4, data[idx]);
                        if (idx + 1 < data.Length) locked.SetRed(x, y, 4, data[idx + 1]);
                        if (idx + 2 < data.Length) locked.SetGreen(x, y, 4, data[idx + 2]);
                        if (idx + 3 < data.Length) locked.SetBlue(x, y, 4, data[idx + 3]);

                        if (idx >= data.Length)
                        {
                            locked.SetColor(x, y, 4, colorSelectorFunc());
                        }
                    }
                }
            }
        }

        public static void AlphaEncode(this BitmapData locked, Rectangle dataArea, byte[] data,
            Func<Point, Color> colorSelectFunc)
        {
            unsafe
            {
                var ptr = (byte*) locked.Scan0;
                var stride = locked.Stride;

                for (var y = dataArea.Top; y < dataArea.Bottom; y++)
                {
                    for (var x = dataArea.Left; x < dataArea.Right; x++)
                    {
                        var normY = (y - dataArea.Top);
                        var normX = (x - dataArea.Left);
                        var idx = (normY*dataArea.Width + normX);

                        locked.SetColor(x, y, 4, colorSelectFunc(new Point(x, y)));
                        if (idx < data.Length)
                        {
                            locked.SetAlpha(x, y, 4, data[idx]);
                        }
                        else
                        {
                            locked.SetAlpha(x, y, 4, 255);
                        }
                    }
                }
            }
        }

        public static void BlueEncode(this BitmapData locked, Rectangle dataArea, byte[] data,
            Func<Point, Color> colorSelectFunc)
        {
            unsafe
            {
                var ptr = (byte*) locked.Scan0;
                var stride = locked.Stride;

                for (var y = dataArea.Top; y < dataArea.Bottom; y++)
                {
                    for (var x = dataArea.Left; x < dataArea.Right; x++)
                    {
                        var normY = (y - dataArea.Top);
                        var normX = (x - dataArea.Left);
                        var idx = (normY*dataArea.Width + normX);

                        locked.SetColor(x, y, 4, colorSelectFunc(new Point(x, y)));
                        locked.SetAlpha(x, y, 4, 255);
                        if (idx < data.Length)
                            locked.SetBlue(x, y, 4, data[idx]);
                    }
                }
            }
        }

        public static int Area(this Rectangle rect)
        {
            return rect.Width*rect.Height;
        }

        public static byte[] ReadAlpha(this BitmapData locked, Rectangle area)
        {
            var data = new byte[area.Area()];

            unsafe
            {
                var ptr = (byte*) locked.Scan0;
                var stride = locked.Stride;

                for (var y = area.Top; y < area.Bottom; y++)
                {
                    for (var x = area.Left; x < area.Right; x++)
                    {
                        var normY = (y - area.Top);
                        var normX = (x - area.Left);
                        var idx = (normY*area.Width + normX);

                        data[idx] = ptr[PointerBaseline(x, y, 4, stride) + 3];
                    }
                }
            }

            return data;
        }

        public static byte[] ReadBlue(this BitmapData locked, Rectangle area)
        {
            var data = new byte[area.Area()];

            unsafe
            {
                var ptr = (byte*) locked.Scan0;
                var stride = locked.Stride;

                for (var y = area.Top; y < area.Bottom; y++)
                {
                    for (var x = area.Left; x < area.Right; x++)
                    {
                        var normY = (y - area.Top);
                        var normX = (x - area.Left);
                        var idx = (normY*area.Width + normX);

                        data[idx] = ptr[PointerBaseline(x, y, 4, stride)];
                    }
                }
            }

            return data;
        }
    }
}