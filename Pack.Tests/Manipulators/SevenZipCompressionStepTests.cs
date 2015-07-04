using System.Collections.Generic;
using System.Linq;
using Pack.Packers;
using Pack.Packers.Manipulators;

namespace Pack.Tests.Manipulators
{
    public sealed class SevenZipCompressionStepTests : ManipulatorTests<SevenZipCompressionStep, object>
    {
        protected override SevenZipCompressionStep BuildStepInstance()
        {
            return new SevenZipCompressionStep();
        }

        protected override byte[] ProduceBytesExpectedToProduceState()
        {
            return Enumerable.Range(1, 10)
                .Aggregate(new List<byte>(),
                    (agg, _) => Enumerable.Repeat((byte) Random.Next(1, 255), Random.Next(50, 300)).ToList()).ToArray();
        }

        protected override byte[] ProduceBytesNotExpectedToProduceState()
        {
            return new byte[] {1, 2, 3};
        }

        protected override object ProduceState()
        {
            return null;
        }
    }
}