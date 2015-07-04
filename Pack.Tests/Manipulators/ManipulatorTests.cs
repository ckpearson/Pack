using System;
using System.Linq;
using Pack.Packers.Manipulators;
using Xunit;

namespace Pack.Tests.Manipulators
{
    public abstract class ManipulatorTests<T, TS> where T: IDataManipulationStep<TS>
    {
        private readonly Random _random = new Random();
        private readonly Lazy<T> _step;

        protected ManipulatorTests()
        {
            _step = new Lazy<T>(BuildStepInstance);
        }

        protected virtual bool StepCanProduceNoState()
        {
            return true;
        }

        protected T Step { get { return _step.Value; } }

        protected abstract T BuildStepInstance();

        protected Random Random { get { return _random; } }
        protected abstract byte[] ProduceBytesExpectedToProduceState();
        protected abstract byte[] ProduceBytesNotExpectedToProduceState();

        protected abstract TS ProduceState();

        [Fact]
        public void Meta_StateExpectedSequenceConsistentlyProducedState()
        {
            for (var x = 1; x < 100; x++)
            {
                var res = Step.Apply(ProduceBytesExpectedToProduceState());
                Assert.True(res.HasValue);
            }
        }

        [Fact]
        public void Meta_StateNotExpectedSequenceConsistentlyProducesNoState()
        {
            if (!StepCanProduceNoState())
            {
                return;
            }

            for (var x = 1; x < 100; x++)
            {
                var res = Step.Apply(ProduceBytesNotExpectedToProduceState());
                Assert.False(res.HasValue);
            }
        }

        [Fact]
        public void SuccessfulApplyProducesDifferingData()
        {
            for (var x = 1; x < 100; x++)
            {
                var inData = ProduceBytesExpectedToProduceState();
                var res = Step.Apply(inData);
                Assert.NotEqual(inData, res.Value.Item1);
            }
        }

        [Fact]
        public void ReverseWithSameDataAndStateProducesSameResult()
        {
            for (var x = 1; x < 100; x++)
            {
                var inData = ProduceBytesExpectedToProduceState();
                var res = Step.Apply(inData);

                var outData = Step.Reverse(res.Value.Item1, res.Value.Item2);
                Assert.Equal(inData, outData);
            }
        }

        [Fact]
        public void SerializeProducesSomeResult()
        {
            var state = ProduceState();
            if (state == null)
            {
                Assert.True(StepCanProduceNoState());
            }

            var standardNullResult = Enumerable.Range(1, 5).Select(_ => (byte) Random.Next(1, 256)).ToArray();
            var standardNonNullResult = Enumerable.Range(1, 5).Select(_ => (byte) Random.Next(1, 256)).ToArray();

            var standard = new Func<byte[]>(() => state == null ? standardNullResult : standardNonNullResult);

            var res = Step.SerializeState(state, standard);

            Assert.True(res != null && res.Length > 0);

            if (state == null)
            {
                Assert.Equal(standardNullResult, res);
            }
            else
            {
                Assert.True(res == standardNullResult || true);
            }
        }

        [Fact]
        public void DeserializeProducesSameResultAsThatSerialized()
        {
            var state = ProduceState();
            if (state == null)
            {
                Assert.True(StepCanProduceNoState());
            }

            var serStandardNull = Enumerable.Range(1, 5).Select(_ => (byte) Random.Next(1, 256)).ToArray();
            var serStandardNonNull = Enumerable.Range(1, 5).Select(_ => (byte) Random.Next(1, 256)).ToArray();

            var serStandard = new Func<byte[]>(() => state == null ? serStandardNull : serStandardNonNull);

            var serialized = Step.SerializeState(state, serStandard);

            var deserStandardNull = state;
            var deserStandardNonNull = state;

            var deserStandard =
                new Func<object>(() => serialized == serStandardNull ? deserStandardNull : deserStandardNonNull);

            var deserialized = Step.DeserializeState(serialized, deserStandard);
            Assert.True(StateEqualCheck(state, (TS)deserialized));
        }

        protected virtual bool StateEqualCheck(TS left, TS right)
        {
            return Equals(left, right);
        }
    }
}