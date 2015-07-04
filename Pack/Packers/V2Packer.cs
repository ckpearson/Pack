using System;
using System.Collections.Generic;
using System.Drawing;
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

        public bool DataIsForPacker(byte[] data)
        {
            throw new NotImplementedException();
        }

        public UnpackedFile Unpack(byte[] data, IInput input)
        {
            throw new NotImplementedException();
        }

        public Bitmap CreateImage(byte[] data, string fileName, IInput input)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (data.Length == 0) throw new ArgumentException(@"Data is empty", "data");
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");
            if (input == null) throw new ArgumentNullException("input");

            var manip = data.Manipulate(new SevenZipCompressionStep())
                .Then(new TrailingZeroEliminationStep());

            var serdManipulations =
                manip.StepsApplied.Select(
                    k => new {Id = k.Key.StepIdent, Ser = k.Key.SerializeState(k.Value, () => StandardSer(k.Value))})
                    .ToList();

            var manipSerDataCost =
                serdManipulations.Aggregate(0,
                    (agg, x) => agg + (1 + x.Ser.Length));

            var fNameBytes = Encoding.UTF8.GetBytes(fileName);

            var totalDataCost = manip.CurrentData.Length +
                                manipSerDataCost + fNameBytes.Length;

            return null;
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
}