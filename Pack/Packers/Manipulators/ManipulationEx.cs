using System.Collections.Generic;
using System.Linq;
using Functional.Maybe;

namespace Pack.Packers.Manipulators
{
    public static class ManipulationEx
    {
        public static ManipulationResult Manipulate<TState>(this byte[] bytes, IDataManipulationStep<TState> step)
        {
            var res = step.Apply(bytes);
            if (res.IsNothing())
                return new ManipulationResult(bytes, new Dictionary<IDataManipulationStep, object>());
            return new ManipulationResult(res.Value.Item1, new Dictionary<IDataManipulationStep, object>
            {
                {step, res.Value.Item2}
            });
        }

        public static ManipulationResult Then<TState>(this ManipulationResult manipulationResult,
            IDataManipulationStep<TState> step)
        {
            return manipulationResult.Append(manipulationResult.CurrentData.Manipulate(step));
        }

        public static byte[] Reverse(this IDictionary<IDataManipulationStep, object> stepStates,
            byte[] data)
        {
            return stepStates.Reverse()
                .Aggregate(data,
                    (agg, item) => item.Key.Reverse(agg, item.Value));
        }
    }
}