using System.Linq;
using Pack.Packers;
using Pack.Packers.Manipulators;
using Xunit;

namespace Pack.Tests.Manipulators
{
    public class TrailingZeroEliminationStepTests : ManipulatorTests<TrailingZeroEliminationStep, int>
    {
        protected override TrailingZeroEliminationStep BuildStepInstance()
        {
            return new TrailingZeroEliminationStep();
        }

        protected override byte[] ProduceBytesExpectedToProduceState()
        {
            return Enumerable.Range(1, 150).Select(_ => (byte) Random.Next(1, 256))
                .Concat(Enumerable.Repeat((byte) 0, Random.Next(1, 300))).ToArray();
        }

        protected override byte[] ProduceBytesNotExpectedToProduceState()
        {
            return Enumerable.Range(1, 150).Select(_ => (byte) Random.Next(1, 256)).ToArray();
        }

        protected override int ProduceState()
        {
            return Random.Next(1, 10);
        }

        [Fact]
        public void ApplyProducesStateWithExpectedNumberOfZeroes()
        {
            for (var x = 1; x < 100; x++)
            {
                var numZeroes = Random.Next(1, 150);
                var data = Enumerable.Range(1, 10).Select(_ => (byte) Random.Next(1, 256))
                    .Concat(Enumerable.Repeat((byte) 0, numZeroes)).ToArray();

                var res = Step.Apply(data);
                Assert.Equal(numZeroes, res.Value.Item2);
            }
        }
    }
}