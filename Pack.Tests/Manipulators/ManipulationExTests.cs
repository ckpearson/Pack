using System;
using System.Collections.Generic;
using System.Linq;
using Functional.Maybe;
using Moq;
using Pack.Packers;
using Pack.Packers.Manipulators;
using Xunit;

namespace Pack.Tests.Manipulators
{
    public class ManipulationExTests
    {
        private readonly Random _random = new Random();

        [Fact]
        public void ManipulateWithStepThatProducesNoStateLeavesDataUntouched()
        {
            var step = new Mock<IDataManipulationStep<int>>();
            var data = Enumerable.Range(1, 150).Select(_ => (byte) _random.Next(1, 256)).ToArray();
            step.Setup(m => m.Apply(It.IsAny<byte[]>())).Returns(Maybe<Tuple<byte[], int>>.Nothing);

            var res = data.Manipulate(step.Object);
            Assert.Equal(data, res.CurrentData);
            Assert.Empty(res.StepsApplied);
        }

        [Fact]
        public void ManipulateWithStepThatProducesStateHasChangedDataAndStepInListWithState()
        {
            var step = new Mock<IDataManipulationStep<int>>();
            var originalData = Enumerable.Range(1, 10).Select(_ => (byte) _random.Next(1, 256)).ToArray();
            var newData = Enumerable.Range(1, 5).Select(_ => (byte) _random.Next(1, 256)).ToArray();
            const int state = 80;

            step.Setup(m => m.Apply(It.IsAny<byte[]>())).Returns(Tuple.Create(newData, 80).ToMaybe());

            var res = originalData.Manipulate(step.Object);
            Assert.Equal(newData, res.CurrentData);
            Assert.Equal(step.Object, res.StepsApplied.Single().Key);
            Assert.Equal(state, res.StepsApplied.Single().Value);
        }

        [Fact]
        public void ThenWithStepThatProducesNoStateHasOriginalResult()
        {
            var origData = Enumerable.Range(1, 10).Select(_ => (byte) _random.Next(1, 256)).ToArray();
            const int origStepState = 10;

            var step = new Mock<IDataManipulationStep<int>>();

            var origResult = new ManipulationResult(origData,
                new Dictionary<IDataManipulationStep, object> { { step.Object, origStepState } });
            step.Setup(m => m.Apply(It.IsAny<byte[]>())).Returns(Maybe<Tuple<byte[], int>>.Nothing);

            var res = origResult.Then(step.Object);

            Assert.Equal(origResult, res);
        }

        [Fact]
        public void ThenWithStepThatProducesStateProducesAppendedResult()
        {
            var origData = Enumerable.Range(1, 10).Select(_ => (byte) _random.Next(1, 256)).ToArray();
            const int origStepState = 10;

            var origResult = new ManipulationResult(origData,
                new Dictionary<IDataManipulationStep, object> { { new Mock<IDataManipulationStep>().Object, origStepState } });

            var newData = Enumerable.Range(1, 150).Select(_ => (byte) _random.Next(1, 256)).ToArray();
            const byte newStepIdent = 10;
            const int newStepState = 80;
            var step = new Mock<IDataManipulationStep<int>>();

            

            step.Setup(m => m.StepIdent).Returns(newStepIdent);
            step.Setup(m => m.Apply(It.IsAny<byte[]>())).Returns(Tuple.Create(newData, newStepState).ToMaybe());

            var newRes = origResult.Then(step.Object);

            Assert.Equal(origResult.Append(newData.Manipulate(step.Object)), newRes);
        }

        [Fact]
        public void ReverseCorrectlyProducesOriginalData()
        {
            var origData = Enumerable.Range(1, 150).Select(_ => (byte) _random.Next(1, 256)).ToArray();
            var newData = Enumerable.Range(1, 10).Select(_ => (byte) _random.Next(1, 256)).ToArray();

            const byte stepIdent = 10;
            const int stepState = 5;
            var step = new Mock<IDataManipulationStep<int>>();
            step.Setup(m => m.StepIdent).Returns(stepIdent);
            step.Setup(m => m.Apply(It.IsAny<byte[]>())).Returns(Tuple.Create(newData, stepState).ToMaybe());
            step.Setup(m => m.Reverse(It.Is<byte[]>(b => b == newData),
                It.Is<object>(o => o.Equals(stepState)))).Returns(origData).Verifiable();

            var res = origData.Manipulate(step.Object);

            var reversed = res.StepsApplied.Reverse(res.CurrentData);
            Assert.Equal(origData, reversed);
            step.Verify();
        }
    }
}