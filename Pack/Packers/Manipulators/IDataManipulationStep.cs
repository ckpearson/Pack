using System;
using Functional.Maybe;

namespace Pack.Packers.Manipulators
{
    public interface IDataManipulationStep<TState> : IDataManipulationStep
    {
        Maybe<Tuple<byte[], TState>> Apply(byte[] data);
        byte[] Reverse(byte[] data, TState state);
    }

    public interface IDataManipulationStep
    {
        byte StepIdent { get; }

        byte[] Reverse(byte[] data, object state);

        byte[] SerializeState(object state, Func<byte[]> standardFunc);
        object DeserializeState(byte[] stateBytes, Func<object> standardFunc);
    }
}