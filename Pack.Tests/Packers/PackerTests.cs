using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using Moq;
using Xunit;

namespace Pack.Tests.Packers
{
    public abstract class PackerTests<T> where T : IPacker
    {
        private readonly T _packer = Activator.CreateInstance<T>();
        private static readonly Random _random = new Random();

        protected static Random Random { get { return _random; } }

        public T Packer { get { return _packer;} }

        [Fact]
        public void VersionIsOneOrGreater()
        {
            Assert.True(Packer.Version >= 1.0);
        }

        [Fact]
        public void DataIsForPackerGuardsNullData()
        {
            Assert.Throws<ArgumentNullException>(() => Packer.DataIsForPacker(null));
        }

        [Fact]
        public void DataIsForPackerGuardsEmptyData()
        {
            Assert.Throws<ArgumentException>(() => Packer.DataIsForPacker(new byte[] {}));
        }

        [Fact]
        public void CreateImageGuardsNullData()
        {
            Assert.Throws<ArgumentNullException>(() => Packer.CreateImage(null, "a", new Mock<IInput>().Object));
        }

        [Fact]
        public void CreateImageGuardsEmptyData()
        {
            Assert.Throws<ArgumentException>(() => Packer.CreateImage(new byte[] {}, "a", new Mock<IInput>().Object));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void CreateImageGuardsNullOrEmptyFileName(string fileName)
        {
            Assert.Throws<ArgumentNullException>(
                () => Packer.CreateImage(new byte[] {12}, fileName, new Mock<IInput>().Object));
        }

        [Fact]
        public void CreateImageGuardsNullInput()
        {
            Assert.Throws<ArgumentNullException>(() => Packer.CreateImage(new byte[] { 12}, "a", null));
        }

        [Fact]
        public void UnpackGuardsNullData()
        {
            Assert.Throws<ArgumentNullException>(() => Packer.Unpack(null, new Mock<IInput>().Object));
        }

        [Fact]
        public void UnpackGuardsEmptyData()
        {
            Assert.Throws<ArgumentException>(() => Packer.Unpack(new byte[] {}, new Mock<IInput>().Object));
        }

        [Fact]
        public void UnpackGuardsNullInput()
        {
            Assert.Throws<ArgumentNullException>(() => Packer.Unpack(new byte[] {12}, null));
        }

        private static byte[] ProduceLikelyData()
        {
            using (var ms = new MemoryStream())
            {
                using (var rs = Assembly.GetExecutingAssembly().GetManifestResourceStream("Pack.Tests.Resources.testbook.xlsx"))
                {
                    rs.CopyTo(ms);
                    return ms.GetBuffer();
                }
            }
        }

        [Fact]
        public void DataIsForPackerRecognisesItsOwnData()
        {
            for (var x = 0; x < 100; x++)
            {
                using (var bmp = Packer.CreateImage(ProduceLikelyData(), "a", BuildInputForCreate()))
                {
                    using (var ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Png);

                        Assert.True(Packer.DataIsForPacker(ms.GetBuffer()));
                    }
                } 
            }
        }

        //[Theory]
        //[InlineData("mushrooms", "a.txt")]
        //[InlineData("everything is spiders", "z")]
        //[InlineData("my fingers are trees", "jkjjjhdhhdhdh")]
        //public void UnpackProducesSameData(string message, string fileName)
        //{
        //    var inData = Encoding.UTF8.GetBytes(message);
        //    using (var bmp = Packer.CreateImage(inData, fileName, BuildInputForCreate()))
        //    {
        //        using (var ms = new MemoryStream())
        //        {
        //            bmp.Save(ms, ImageFormat.Png);

        //            var unpacked = Packer.Unpack(ms.GetBuffer(), BuildInputForUnpack());
        //            Assert.Equal(inData, unpacked.Data);
        //            Assert.Equal(fileName, unpacked.Name);
        //        }
        //    }
        //}

        protected virtual IInput BuildInputForCreate()
        {
            return new Mock<IInput>().Object;
        }

        protected virtual IInput BuildInputForUnpack()
        {
            return new Mock<IInput>().Object;
        }
    }
}