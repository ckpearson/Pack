using System;
using Functional.Maybe;
using SevenZip;

namespace Pack.Packers.Manipulators
{
    public class SevenZipCompressionStep : IDataManipulationStep<object>
    {
        public Maybe<Tuple<byte[], object>> Apply(byte[] data)
        {
            var comp = SevenZipCompressor.CompressBytes(data);
            return comp.Length < data.Length
                ? Tuple.Create(comp, (object) null).ToMaybe()
                : Maybe<Tuple<byte[], object>>.Nothing;
        }

        public byte[] Reverse(byte[] data, object state)
        {
            return SevenZipExtractor.ExtractBytes(data);
        }

        public byte[] SerializeState(object state, Func<byte[]> standardFunc)
        {
            return standardFunc();
        }

        public object DeserializeState(byte[] stateBytes, Func<object> standardFunc)
        {
            return standardFunc();
        }

        public byte StepIdent { get { return 1; }}
    }
}