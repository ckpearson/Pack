using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Pack.Packers.Manipulators;

namespace Pack.Tests.Manipulators
{
    public class ContiguousRangeCollapseStepTests : ManipulatorTests<ContiguousRangeCollapseStep, IEnumerable<ContiguousRangeCollapseStep.CollapseDetails>>
    {
        protected override ContiguousRangeCollapseStep BuildStepInstance()
        {
            return new ContiguousRangeCollapseStep();
        }

        protected override byte[] ProduceBytesExpectedToProduceState()
        {
            using (var ms = new MemoryStream())
            {
                using (
                    var rs =
                        Assembly.GetExecutingAssembly().GetManifestResourceStream("Pack.Tests.Resources.testbook.xlsx"))
                {
                    rs.CopyTo(ms);
                    return ms.GetBuffer();
                }
            }
        }

        protected override byte[] ProduceBytesNotExpectedToProduceState()
        {
            return new byte[] {1, 2, 3, 4};
        }

        protected override IEnumerable<ContiguousRangeCollapseStep.CollapseDetails> ProduceState()
        {
            return new[]
            {
                new ContiguousRangeCollapseStep.CollapseDetails(
                    Random.Next(1, int.MaxValue),
                    (byte) Random.Next(1, 256),
                    (byte) Random.Next(7, 256))
            };
        }

        protected override bool StateEqualCheck(IEnumerable<ContiguousRangeCollapseStep.CollapseDetails> left, IEnumerable<ContiguousRangeCollapseStep.CollapseDetails> right)
        {
            return left.SequenceEqual(right);
        }
    }
}